﻿using System;
using System.Numerics;
using System.Threading;

using AAEmu.Game.Core.Managers.AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.DoodadObj.Static;
using AAEmu.Game.Models.Game.Slaves;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.Game.Units.Movements;
using AAEmu.Game.Models.Game.Units.Static;
using AAEmu.Game.Physics.Forces;
using AAEmu.Game.Physics.Util;
using AAEmu.Game.Utils;

using Jitter.Collision;
using Jitter.Collision.Shapes;
using Jitter.Dynamics;
using Jitter.LinearMath;

using NLog;

using InstanceWorld = AAEmu.Game.Models.Game.World.World;

namespace AAEmu.Game.Core.Managers.World;

public class BoatPhysicsManager//: Singleton<BoatPhysicsManager>
{
    /// <summary>
    /// Ticks per second for the physics engine
    /// </summary>
    private float TargetPhysicsTps { get; set; } = 15f;
    private Thread _thread;
    private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

    private CollisionSystem _collisionSystem;
    private Jitter.World _physWorld;
    private Buoyancy _buoyancy;
    private uint _tickCount;
    private bool ThreadRunning { get; set; }
    public InstanceWorld SimulationWorld { get; set; }
    private object _slaveListLock = new();
    private Random _random = new();

    private bool CustomWater(ref JVector area)
    {
        // Query world if it's water and treat everything below 100 as water as a fallback
        return SimulationWorld?.IsWater(new Vector3(area.X, area.Z, area.Y)) ?? area.Y <= 100f;
    }

    public void Initialize()
    {
        _collisionSystem = new CollisionSystemSAP();
        _physWorld = new Jitter.World(_collisionSystem);
        _buoyancy = new Buoyancy(_physWorld);
        _buoyancy.UseOwnFluidArea(CustomWater);
        // _buoyancy.FluidBox = new JBBox(new JVector(0, 0, 0), new JVector(100000, 100, 100000));

        // Добавим поверхность земли // Add ground surface
        if (SimulationWorld.Name != "main_world") { return; }
        try
        {
            var hmap = WorldManager.Instance.GetWorld(0).HeightMaps;
            var heightMaxCoefficient = WorldManager.Instance.GetWorld(0).HeightMaxCoefficient;
            var dx = hmap.GetLength(0);
            var dz = hmap.GetLength(1);
            var hmapTerrain = new float[dx, dz];
            for (var x = 0; x < dx; x += 1)
                for (var y = 0; y < dz; y += 1)
                    hmapTerrain[x, y] = (float)(hmap[x, y] / heightMaxCoefficient);
            var terrain = new TerrainShape(hmapTerrain, 2.0f, 2.0f);
            var body = new RigidBody(terrain) { IsStatic = true };
            _physWorld.AddBody(body);
        }
        catch (Exception e)
        {
            Logger.Error("{0}\n{1}", e.Message, e.StackTrace);
        }
    }

    public void StartPhysics()
    {
        ThreadRunning = true;
        _thread = new Thread(PhysicsThread);
        _thread.Name = "Physics-" + (SimulationWorld?.Name ?? "???");
        _thread.Start();
    }

    private void PhysicsThread()
    {
        try
        {
            Logger.Debug($"PhysicsThread Start: {Thread.CurrentThread.Name} ({Environment.CurrentManagedThreadId})");
            var simulatedSlaveTypeList = new[]
            {
                SlaveKind.BigSailingShip, SlaveKind.Boat, SlaveKind.Fishboat, SlaveKind.SmallSailingShip,
                SlaveKind.MerchantShip, SlaveKind.Speedboat
            };
            while (ThreadRunning && Thread.CurrentThread.IsAlive)
            {
                Thread.Sleep((int)Math.Floor(1000f / TargetPhysicsTps));
                _physWorld.Step(1f / TargetPhysicsTps, false);
                _tickCount++;

                lock (_slaveListLock)
                {
                    // Not sure if it's better to query it each tick, or track them locally
                    var slaveList = SlaveManager.Instance.GetActiveSlavesByKinds(simulatedSlaveTypeList, SimulationWorld.Id);
                    if (slaveList == null)
                        continue;

                    foreach (var slave in slaveList)
                    {
                        if (slave.Transform.WorldId != SimulationWorld.Id)
                        {
                            Logger.Debug($"Skip {slave.Name}");
                            continue;
                        }

                        // Skip simulation if still summoning
                        if (slave.SpawnTime.AddSeconds(slave.Template.PortalTime) > DateTime.UtcNow)
                            continue;

                        // Skip simulation if no rigidbody applied to slave
                        var slaveRigidBody = slave.RigidBody;
                        if (slaveRigidBody == null)
                            continue;

                        // Note: Y, Z swapped
                        var xDelta = slaveRigidBody.Position.X - slave.Transform.World.Position.X;
                        var yDelta = slaveRigidBody.Position.Z - slave.Transform.World.Position.Y;
                        var zDelta = slaveRigidBody.Position.Y - slave.Transform.World.Position.Z;

                        slave.Transform.Local.Translate(xDelta, yDelta, zDelta);
                        var rot = JQuaternion.CreateFromMatrix(slaveRigidBody.Orientation);
                        slave.Transform.Local.ApplyFromQuaternion(rot.X, rot.Z, rot.Y, rot.W);

                        if (_tickCount % 6 != 0) { continue; }
                        _physWorld.CollisionSystem.Detect(true);
                        BoatPhysicsTick(slave, slaveRigidBody);
                        //Logger.Debug($"{_thread.Name}, slave: {slave.Name} collision check tick");
                    }
                }
            }
            Logger.Debug($"PhysicsThread End: {Thread.CurrentThread.Name} ({Environment.CurrentManagedThreadId})");
        }
        catch (Exception e)
        {
            Logger.Error($"StartPhysics: {e}");
        }
    }

    public void AddShip(Slave slave)
    {
        var shipModel = ModelManager.Instance.GetShipModel(slave.ModelId);
        if (shipModel == null) { return; }
        var slaveBox = new BoxShape(shipModel.MassBoxSizeX, shipModel.MassBoxSizeZ, shipModel.MassBoxSizeY);
        var slaveMaterial = new Material();
        // TODO: Add the center of mass settings into JitterPhysics somehow

        var rigidBody = new RigidBody(slaveBox, slaveMaterial)
        {
            Position = new JVector(slave.Transform.World.Position.X, slave.Transform.World.Position.Z, slave.Transform.World.Position.Y),
            // Mass = shipModel.Mass, // Using the actually defined mass of the DB doesn't really work
            Orientation = JMatrix.CreateRotationY(slave.Transform.World.Rotation.Z)
        };

        _buoyancy.Add(rigidBody, 3);
        _physWorld.AddBody(rigidBody);
        slave.RigidBody = rigidBody;
        Logger.Debug($"AddShip {slave.Name} -> {SimulationWorld.Name}");
    }

    public void RemoveShip(Slave slave)
    {
        if (slave.RigidBody == null) return;
        _buoyancy.Remove(slave.RigidBody);
        _physWorld.RemoveBody(slave.RigidBody);
        Logger.Debug($"RemoveShip {slave.Name} <- {SimulationWorld.Name}");
    }

    private void BoatPhysicsTick(Slave slave, RigidBody rigidBody)
    {
        var moveType = (ShipMoveType)MoveType.GetType(MoveTypeEnum.Ship);
        moveType.UseSlaveBase(slave);
        var shipModel = ModelManager.Instance.GetShipModel(slave.Template.ModelId);

        var velAccel = shipModel.Accel; // 2.0f; //per s
        var rotAccel = shipModel.TurnAccel; // 0.5f; //per s
        var maxVelForward = shipModel.Velocity; // 12.9f //per s
        var maxVelBackward = -shipModel.ReverseVelocity; // -5.0f

        // If no driver, then no steering
        if (!slave.AttachedCharacters.ContainsKey(AttachPointKind.Driver))
        {
            slave.ThrottleRequest = 0;
            slave.SteeringRequest = 0;
        }

        ComputeThrottle(slave, (int)Math.Ceiling(shipModel.Velocity / shipModel.Accel));
        ComputeSteering(slave);//, (int)Math.Ceiling(shipModel.Velocity / shipModel.TurnAccel));
        slave.RigidBody.IsActive = true;

        // Provide minimum speed of 1 when Throttle is used
        if (slave.Throttle > 0 && slave.Speed < 1f)
            slave.Speed = 1f;
        if (slave.Throttle < 0 && slave.Speed > -1f)
            slave.Speed = -1f;

        // Convert sbyte throttle value to use as speed
        slave.Speed += slave.Throttle * 0.00787401575f * (velAccel / 10f);

        // Clamp speed between min and max Velocity
        slave.Speed = Math.Min(slave.Speed, maxVelForward);
        slave.Speed = Math.Max(slave.Speed, maxVelBackward);

        slave.RotSpeed += slave.Steering * 0.00787401575f * (rotAccel / 100f);
        slave.RotSpeed = Math.Min(slave.RotSpeed, 1f);
        slave.RotSpeed = Math.Max(slave.RotSpeed, -1f);

        if (slave.Steering == 0)
        {
            slave.RotSpeed -= slave.RotSpeed / 20;
            if (Math.Abs(slave.RotSpeed) <= 0.01)
                slave.RotSpeed = 0;
        }

        if (slave.Throttle == 0) // this needs to be fixed : ships need to apply a static drag, and slowly ship away at the speed instead of doing it like this
        {
            slave.Speed -= slave.Speed / 20f;
            if (Math.Abs(slave.Speed) < 0.01)
                slave.Speed = 0;
        }
        // Logger.Debug("Slave: {0}, speed: {1}, rotSpeed: {2}", slave.ObjId, slave.Speed, slave.RotSpeed);

        // Calculate some stuff for later
        var boxSize = rigidBody.Shape.BoundingBox.Max - rigidBody.Shape.BoundingBox.Min;
        var tubeVolume = shipModel.TubeLength * shipModel.TubeRadius * MathF.PI;
        var solidVolume = MathF.Abs(rigidBody.Mass - tubeVolume);

        var floor = WorldManager.Instance.GetHeight(slave.Transform); // получим уровень земли // get ground level
        Logger.Debug($"[Height] Z-Pos: {slave.Transform.World.Position.Z} - Floor: {floor}");
        if (floor >= slave.Transform.World.Position.Z - boxSize.Z)
        {
            var damage = _random.Next(500, 750); // damage randomly 500-750
            if (damage > 0)
            {
                slave.DoDamage((int)damage, false, KillReason.Collide);
            }

            Logger.Debug($"Slave: {slave.ObjId}, speed: {slave.Speed}, rotSpeed: {slave.RotSpeed}, floor: {floor}, Z: {slave.Transform.World.Position.Z}, damage: {damage}");

            if (slave.Hp <= 0)
            {
                slave.Speed = 0;
                return;
            }
        }

        var rpy = PhysicsUtil.GetYawPitchRollFromMatrix(rigidBody.Orientation);
        var slaveRotRad = rpy.Item1 + 90 * (MathF.PI / 180.0f);

        var forceThrottle = slave.Speed * 17.25f * slave.MoveSpeedMul; // don't know what this number is, but it's perfect for all ships
        rigidBody.AddForce(new JVector(forceThrottle * rigidBody.Mass * MathF.Cos(slaveRotRad), 0.0f, forceThrottle * rigidBody.Mass * MathF.Sin(slaveRotRad)));

        // Make sure the steering is reversed when going backwards.
        var steer = (float)slave.Steering * shipModel.SteerVel * ((100f + slave.TurnSpeed) / 100f);
        if (forceThrottle < 0)
            steer *= -1;

        // Calculate Steering Force based on bounding box 
        var steerForce = -steer * (solidVolume * boxSize.X * boxSize.Y / 172.5f * 2f); // Totally random value, but it feels right
        rigidBody.AddTorque(new JVector(0, steerForce, 0));

        /*
        if ((slave.Steering != 0) || (slave.Throttle != 0))
            Logger.Debug($"Request: {slave.SteeringRequest}, Steering: {slave.Steering}, steer: {steer}, vol: {solidVolume} mass: {rigidBody.Mass}, force: {steerForce}, torque: {rigidBody.Torque}");
        */

        // Insert new Rotation data into MoveType
        var (rotZ, rotY, rotX) = MathUtil.GetSlaveRotationFromDegrees(rpy.Item1, rpy.Item2, rpy.Item3);
        moveType.RotationX = rotX;
        moveType.RotationY = rotY;
        moveType.RotationZ = rotZ;

        // Fill in the Velocity Data into the MoveType
        moveType.Velocity = new Vector3(rigidBody.LinearVelocity.X, rigidBody.LinearVelocity.Z, rigidBody.LinearVelocity.Y);
        moveType.AngVelX = rigidBody.AngularVelocity.X;
        moveType.AngVelY = rigidBody.AngularVelocity.Z;
        moveType.AngVelZ = rigidBody.AngularVelocity.Y;

        // Seems display the correct speed this way, but what happens if you go over the bounds ?
        moveType.VelX = (short)(rigidBody.LinearVelocity.X * 1024);
        moveType.VelY = (short)(rigidBody.LinearVelocity.Z * 1024);
        moveType.VelZ = (short)(rigidBody.LinearVelocity.Y * 1024);

        // Create virtual offset, this is not a good solution, but it'll have to do for now.
        // This will likely create issues with skill that generate position specified plots likely not having this offset when on the ship

        // Don't know how to handle X/Y for this, if we even should ...
        // moveType.X += shipModel.MassCenterX; 
        // moveType.Y += shipModel.MassCenterY;

        // We can more or less us the model Mass Center Z value to get how much it needs to sink
        // It doesn't actually do this server-side, as wel only modify the packet sent to the players
        // If center of mass is positive rather than negative, we need to ignore it here to prevent the boat from floating
        moveType.Z += (shipModel.MassCenterZ < 0f ? shipModel.MassCenterZ / 2f : 0f) - shipModel.KeelHeight;

        // Do not allow the body to flip
        slave.RigidBody.Orientation = JMatrix.CreateFromYawPitchRoll(rpy.Item1, 0, 0); // TODO: Fix me with proper physics

        // Apply new Location/Rotation to GameObject
        slave.Transform.Local.SetPosition(rigidBody.Position.X, rigidBody.Position.Z, rigidBody.Position.Y);
        var jRot = JQuaternion.CreateFromMatrix(rigidBody.Orientation);
        slave.Transform.Local.ApplyFromQuaternion(jRot.X, jRot.Z, jRot.Y, jRot.W);

        // Send the packet
        slave.BroadcastPacket(new SCOneUnitMovementPacket(slave.ObjId, moveType), false);
        // Logger.Debug("Island: {0}", slave.RigidBody.CollisionIsland.Bodies.Count);

        // Update all to main Slave and it's children 
        slave.Transform.FinalizeTransform();
    }

    internal void Stop()
    {
        ThreadRunning = false;
    }

    private static void ComputeThrottle(Slave slave, int throttleAccel = 6)
    {
        if (slave.ThrottleRequest > slave.Throttle)
        {
            slave.Throttle = (sbyte)Math.Min(sbyte.MaxValue, slave.Throttle + throttleAccel);
        }
        else if (slave.ThrottleRequest < slave.Throttle && slave.ThrottleRequest != 0)
        {
            slave.Throttle = (sbyte)Math.Max(sbyte.MinValue, slave.Throttle - throttleAccel);
        }
        else
        {
            if (slave.Throttle > 0)
            {
                slave.Throttle = (sbyte)Math.Max(slave.Throttle - throttleAccel, 0);
            }
            else if (slave.Throttle < 0)
            {
                slave.Throttle = (sbyte)Math.Min(slave.Throttle + throttleAccel, 0);
            }
        }
    }

    private static void ComputeSteering(Slave slave, int steeringAccel = 6)
    {
        if (slave.SteeringRequest > slave.Steering)
        {
            slave.Steering = (sbyte)Math.Min(sbyte.MaxValue, slave.Steering + steeringAccel);
        }
        else if (slave.SteeringRequest < slave.Steering && slave.SteeringRequest != 0)
        {
            slave.Steering = (sbyte)Math.Max(sbyte.MinValue, slave.Steering - steeringAccel);
        }
        else
        {
            if (slave.Steering > 0)
            {
                slave.Steering = (sbyte)Math.Max(slave.Steering - steeringAccel, 0);
            }
            else if (slave.Steering < 0)
            {
                slave.Steering = (sbyte)Math.Min(slave.Steering + steeringAccel, 0);
            }
        }
    }
}
