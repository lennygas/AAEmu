﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.Id;
using AAEmu.Game.Core.Managers.UnitManagers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Core.Network.Connections;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.GameData;
using AAEmu.Game.Models.Game.AI.AStar;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj.Static;
using AAEmu.Game.Models.Game.Expeditions;
using AAEmu.Game.Models.Game.Formulas;
using AAEmu.Game.Models.Game.Housing;
using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Items.Actions;
using AAEmu.Game.Models.Game.Items.Containers;
using AAEmu.Game.Models.Game.Items.Templates;
using AAEmu.Game.Models.Game.NPChar;
using AAEmu.Game.Models.Game.Quests;
using AAEmu.Game.Models.Game.Skills;
using AAEmu.Game.Models.Game.Skills.Plots.Tree;
using AAEmu.Game.Models.Game.Skills.SkillControllers;
using AAEmu.Game.Models.Game.Static;
using AAEmu.Game.Models.Game.Units.Route;
using AAEmu.Game.Models.Game.Units.Static;
using AAEmu.Game.Models.Tasks;
using AAEmu.Game.Models.Tasks.Skills;
using AAEmu.Game.Utils;

namespace AAEmu.Game.Models.Game.Units
{
    public class Unit : BaseUnit, IUnit
    {
        public virtual UnitTypeFlag TypeFlag { get; } = UnitTypeFlag.None;

        public UnitEvents Events { get; }
        private Task _regenTask;
        public uint ModelId { get; set; }
        public SkillController ActiveSkillController { get; set; }

        public override float ModelSize
        {
            get
            {
                return (ModelManager.Instance.GetActorModel(ModelId)?.Radius ?? 0) * Scale;
            }
        }

        public byte Level { get; set; }
        public int Hp { get; set; }

        #region Attributes

        [UnitAttribute(UnitAttribute.MoveSpeedMul)]
        public virtual float MoveSpeedMul { get => (float)CalculateWithBonuses(1000f, UnitAttribute.MoveSpeedMul) / 1000f; }
        [UnitAttribute(UnitAttribute.GlobalCooldownMul)]
        public virtual float GlobalCooldownMul { get; set; } = 100f;
        [UnitAttribute(UnitAttribute.MaxHealth)]
        public virtual int MaxHp { get; set; }
        [UnitAttribute(UnitAttribute.HealthRegen)]
        public virtual int HpRegen { get; set; }
        [UnitAttribute(UnitAttribute.PersistentHealthRegen)]
        public virtual int PersistentHpRegen { get; set; } = 30;
        public int Mp { get; set; }
        [UnitAttribute(UnitAttribute.MaxMana)]
        public virtual int MaxMp { get; set; }
        [UnitAttribute(UnitAttribute.ManaRegen)]
        public virtual int MpRegen { get; set; }
        [UnitAttribute(UnitAttribute.PersistentManaRegen)]
        public virtual int PersistentMpRegen { get; set; } = 30;
        [UnitAttribute(UnitAttribute.CastingTimeMul)]
        public virtual float CastTimeMul { get; set; } = 1f;
        public virtual float LevelDps { get; set; }
        [UnitAttribute(UnitAttribute.MainhandDps)]
        public virtual int Dps { get; set; }
        [UnitAttribute(UnitAttribute.MeleeDpsInc)]
        public virtual int DpsInc { get; set; }
        [UnitAttribute(UnitAttribute.OffhandDps)]
        public virtual int OffhandDps { get; set; }
        [UnitAttribute(UnitAttribute.RangedDps)]
        public virtual int RangedDps { get; set; }
        [UnitAttribute(UnitAttribute.RangedDpsInc)]
        public virtual int RangedDpsInc { get; set; }
        [UnitAttribute(UnitAttribute.SpellDps)]
        public virtual int MDps { get; set; }
        [UnitAttribute(UnitAttribute.SpellDpsInc)]
        public virtual int MDpsInc { get; set; }
        [UnitAttribute(UnitAttribute.HealDps)]
        public virtual int HDps { get; set; }
        [UnitAttribute(UnitAttribute.HealDpsInc)]
        public virtual int HDpsInc { get; set; }
        [UnitAttribute(UnitAttribute.MeleeAntiMissMul)]
        public virtual float MeleeAccuracy { get; set; } = 100f;
        [UnitAttribute(UnitAttribute.MeleeCritical)]
        public virtual float MeleeCritical { get; set; }
        [UnitAttribute(UnitAttribute.MeleeCriticalBonus)]
        public virtual float MeleeCriticalBonus { get; set; }
        [UnitAttribute(UnitAttribute.MeleeCriticalMul)]
        public virtual float MeleeCriticalMul { get; set; } = 1f;
        [UnitAttribute(UnitAttribute.RangedAntiMiss)]
        public virtual float RangedAccuracy { get; set; } = 100f;
        [UnitAttribute(UnitAttribute.RangedCritical)]
        public virtual float RangedCritical { get; set; }
        [UnitAttribute(UnitAttribute.RangedCriticalBonus)]
        public virtual float RangedCriticalBonus { get; set; }
        [UnitAttribute(UnitAttribute.RangedCriticalMul)]
        public virtual float RangedCriticalMul { get; set; } = 1f;
        [UnitAttribute(UnitAttribute.SpellAntiMiss)]
        public virtual float SpellAccuracy { get; set; } = 100f;
        [UnitAttribute(UnitAttribute.SpellCritical)]
        public virtual float SpellCritical { get; set; }
        [UnitAttribute(UnitAttribute.SpellCriticalBonus)]
        public virtual float SpellCriticalBonus { get; set; }
        [UnitAttribute(UnitAttribute.SpellCriticalMul)]
        public virtual float SpellCriticalMul { get; set; } = 1f;
        [UnitAttribute(UnitAttribute.HealCritical)]
        public virtual float HealCritical { get; set; }
        [UnitAttribute(UnitAttribute.HealCriticalBonus)]
        public virtual float HealCriticalBonus { get; set; }
        [UnitAttribute(UnitAttribute.HealCriticalMul)]
        public virtual float HealCriticalMul { get; set; }
        [UnitAttribute(UnitAttribute.Armor)]
        public virtual int Armor { get; set; }
        [UnitAttribute(UnitAttribute.MagicResist)]
        public virtual int MagicResistance { get; set; }
        [UnitAttribute(UnitAttribute.IgnoreArmor)]
        public virtual int DefensePenetration { get; set; }
        [UnitAttribute(UnitAttribute.MagicPenetration)]
        public virtual int MagicPenetration { get; set; }
        [UnitAttribute(UnitAttribute.Dodge)]
        public virtual float DodgeRate { get; set; }
        [UnitAttribute(UnitAttribute.MeleeParry)]
        public virtual float MeleeParryRate { get; set; }
        [UnitAttribute(UnitAttribute.RangedParry)]
        public virtual float RangedParryRate { get; set; }
        [UnitAttribute(UnitAttribute.Block)]
        public virtual float BlockRate { get; set; }
        [UnitAttribute(UnitAttribute.BattleResist)]
        public virtual int BattleResist { get; set; }
        [UnitAttribute(UnitAttribute.BullsEye)]
        public virtual int BullsEye { get; set; }
        [UnitAttribute(UnitAttribute.Flexibility)]
        public virtual int Flexibility { get; set; }
        [UnitAttribute(UnitAttribute.Facets)]
        public virtual int Facets { get; set; }
        [UnitAttribute(UnitAttribute.MeleeDamageMul)]
        public virtual float MeleeDamageMul { get; set; } = 1.0f;
        [UnitAttribute(UnitAttribute.RangedDamageMul)]
        public virtual float RangedDamageMul { get; set; } = 1.0f;
        [UnitAttribute(UnitAttribute.SpellDamageMul)]
        public virtual float SpellDamageMul { get; set; } = 1.0f;

        [UnitAttribute(UnitAttribute.IncomingHealMul)]
        public virtual float IncomingHealMul { get; set; } = 1.0f;
        [UnitAttribute(UnitAttribute.HealMul)]
        public virtual float HealMul { get; set; } = 1.0f;
        [UnitAttribute(UnitAttribute.IncomingDamageMul)]
        public virtual float IncomingDamageMul { get; set; } = 1f;
        [UnitAttribute(UnitAttribute.IncomingMeleeDamageMul)]
        public virtual float IncomingMeleeDamageMul { get; set; } = 1f;
        [UnitAttribute(UnitAttribute.IncomingRangedDamageMul)]
        public virtual float IncomingRangedDamageMul { get; set; } = 1f;
        [UnitAttribute(UnitAttribute.IncomingSpellDamageMul)]
        public virtual float IncomingSpellDamageMul { get; set; } = 1f;
        [UnitAttribute(UnitAttribute.AggroMul)]
        public float AggroMul
        {
            get => (float)CalculateWithBonuses(100d, UnitAttribute.AggroMul);
        }
        [UnitAttribute(UnitAttribute.IncomingAggroMul)]

        #endregion Attributes

        public float IncomingAggroMul
        {
            get => (float)CalculateWithBonuses(100d, UnitAttribute.IncomingAggroMul);
        }
        public BaseUnit CurrentTarget { get; set; }
        public BaseUnit CurrentInteractionObject { get; set; }
        public virtual byte RaceGender => 0;
        public virtual UnitCustomModelParams ModelParams { get; set; }
        public byte ActiveWeapon { get; set; }
        public bool IdleStatus { get; set; }
        public bool ForceAttack { get; set; }
        public bool Invisible { get; set; }
        public uint OwnerId { get; set; }
        public SkillTask SkillTask { get; set; }
        public SkillTask AutoAttackTask { get; set; }
        public DateTime GlobalCooldown { get; set; }
        public bool IsGlobalCooldowned => GlobalCooldown > DateTime.UtcNow;
        public object GCDLock { get; set; }
        public DateTime SkillLastUsed { get; set; }
        public PlotState ActivePlotState { get; set; }
        public Dictionary<uint, List<Bonus>> Bonuses { get; set; }
        public UnitCooldowns Cooldowns { get; set; }
        public Expedition Expedition { get; set; }
        public bool IsInBattle { get; set; }
        public bool IsInDuel { get; set; }
        public bool IsInPatrol { get; set; } // so as not to run the route a second time
        public int SummarizeDamage { get; set; }
        public bool IsAutoAttack = false;
        public uint SkillId;
        public ushort TlId { get; set; }
        public ItemContainer Equipment { get; set; }
        public GameConnection Connection { get; set; }

        /// <summary>
        /// Unit巡逻
        /// Unit patrol
        /// 指明Unit巡逻路线及速度、是否正在执行巡逻等行为
        /// Indicates the route and speed of the Unit patrol, whether it is performing patrols, etc.
        /// </summary>
        public Patrol Patrol { get; set; }
        public Simulation Simulation { get; set; }

        public UnitProcs Procs { get; set; }

        public bool ConditionChance { get; set; }

        public Unit()
        {
            Events = new UnitEvents();
            GCDLock = new object();
            Bonuses = new Dictionary<uint, List<Bonus>>();
            IsInBattle = false;
            Equipment = new EquipmentContainer(0, SlotType.Equipment, true, false);
            ChargeLock = new object();
            Cooldowns = new UnitCooldowns();
        }

        public void SetPosition(float x, float y, float z, sbyte rotationX, sbyte rotationY, sbyte rotationZ)
        {
            SetPosition(x, y, z, (float)MathUtil.ConvertDirectionToRadian(rotationX), (float)MathUtil.ConvertDirectionToRadian(rotationY), (float)MathUtil.ConvertDirectionToRadian(rotationZ));
        }

        public override void SetPosition(float x, float y, float z, float rotationX, float rotationY, float rotationZ)
        {
            var moved = !Transform.World.Position.X.Equals(x) || !Transform.World.Position.Y.Equals(y) || !Transform.World.Position.Z.Equals(z);
            if (moved)
            {
                Events.OnMovement(this, new OnMovementArgs());
            }
            base.SetPosition(x, y, z, rotationX, rotationY, rotationZ);
        }

        public bool CheckMovedPosition(Vector3 oldPosition)
        {
            var moved = !Transform.World.Position.X.Equals(oldPosition.X) || !Transform.World.Position.Y.Equals(oldPosition.Y) || !Transform.World.Position.Z.Equals(oldPosition.Z);
            if (moved)
            {
                Events.OnMovement(this, new OnMovementArgs());
            }
            if (DisabledSetPosition)
                return moved;

            WorldManager.Instance.AddVisibleObject(this);
            // base.SetPosition(x, y, z, rotationX, rotationY, rotationZ);
            return moved;
        }

        /// <summary>
        /// Make unit take value amount of damage, if the unit dies, killReason is used as a reason
        /// </summary>
        /// <param name="attacker"></param>
        /// <param name="value"></param>
        /// <param name="killReason"></param>
        public virtual void ReduceCurrentHp(BaseUnit attacker, int value, KillReason killReason = KillReason.Damage)
        {
            if (((Unit)attacker).CurrentTarget is Character character)
            {
                if (Hp <= 0 && character.IsInDuel)
                {
                    Hp = 1; // we don't let you die during a duel
                    return;
                }

                if (AppConfiguration.Instance.World.GodMode)
                {
                    _log.Debug("{1}:{0}'s Damage disabled because of GM or Admin flag", character.Name, character.Id);
                    return; // GodMode On : take 0 damage from Npc
                }

            }

            if (Hp <= 0)
                return;

            var absorptionEffects = Buffs.GetAbsorptionEffects().ToList();
            if (absorptionEffects.Count > 0)
            {
                // Handle damage absorb
                foreach (var absorptionEffect in absorptionEffects)
                {
                    value = absorptionEffect.ConsumeCharge(value);
                }
            }

            Hp = Math.Max(Hp - value, 0);

            BroadcastPacket(new SCUnitPointsPacket(ObjId, Hp, Hp > 0 ? Mp : 0), true);

            if (Hp > 0) { return; }
            if (((Unit)attacker).CurrentTarget is Character attacked && attacked.IsInDuel)
            {
                Hp = 1; // we don't let you die during a duel
                return;
            }

            ((Unit)attacker).Events.OnKill(attacker, new OnKillArgs { target = (Unit)attacker });
            DoDie(attacker, killReason);
        }

        public virtual void ReduceCurrentMp(BaseUnit unit, int value)
        {
            if (Hp == 0)
            {
                return;
            }

            Mp = Math.Max(Mp - value, 0);
            //if (Mp == 0)
            //{
            //    StopRegen();
            //}

            //else
            //StartRegen();
            BroadcastPacket(new SCUnitPointsPacket(ObjId, Hp, Mp), true);
        }

        public virtual void DoDie(BaseUnit killer, KillReason killReason)
        {
            InterruptSkills();

            Events.OnDeath(this, new OnDeathArgs { Killer = (Unit)killer, Victim = this });
            Buffs.RemoveEffectsOnDeath();

            if (this is Character character)
            {
                character.StopAutoSkill(character);
                character.IsInBattle = false;
                character.DeadTime = DateTime.UtcNow;
                DespawMate(character);

                if (character.Buffs.CheckBuff((uint)BuffConstants.RebirthTrauma))
                    character.DeadCount += 1;
                else
                    character.DeadCount = 1;

                var resurrection = CharacterManager.Instance.GetResurrection((uint)character.DeadCount);

                if (killer is not Character kchar)
                {
                    var expPerLvl = ExpirienceManager.Instance.GetExpForLevel((byte)(character.Level + 1)) - ExpirienceManager.Instance.GetExpForLevel(character.Level);

                    var formula = FormulaManager.Instance.GetFormula((uint)FormulaKind.PenaltyExp);
                    var lostExp = (int)formula.Evaluate(new Dictionary<string, double>() { ["experience"] = expPerLvl });
                    lostExp = Math.Min(lostExp, character.Expirience - ExpirienceManager.Instance.GetExpForLevel(character.Level));
                    character.AddExp(-lostExp, true);

                    formula = FormulaManager.Instance.GetFormula((uint)FormulaKind.RecoverableExp);
                    var recoverExp = (int)formula.Evaluate(new Dictionary<string, double>() { ["experience"] = expPerLvl });
                    if (lostExp < recoverExp)
                        recoverExp = lostExp;
                    character.RecoverableExp = recoverExp;
                    character.SendPacket(new SCRecoverableExpPacket(character.ObjId, character.RecoverableExp, lostExp, 0));

                    var tasks = new List<ItemTask>();
                    var durabilityLossRatio = ItemManager.Instance.GetDeathDurabilityLossRatio();
                    foreach (var item in character.Inventory.Equipment.Items)
                    {
                        if (item is EquipItem equipItem && item.Template is EquipItemTemplate)
                        {
                            equipItem.Durability = (byte)Math.Floor(equipItem.Durability - (equipItem.Durability * durabilityLossRatio / 100.0));
                        }
                        tasks.Add(new ItemUpdate(item));
                    }
                    character.SendPacket(new SCItemTaskSuccessPacket(ItemTaskType.DurabilityLoss, tasks, new List<ulong>()));
                    character.BroadcastPacket(new SCUnitDeathPacket(ObjId, killReason, (Unit)killer, (int)TimeSpan.FromSeconds(resurrection.WaitingTime).TotalMilliseconds, lostExp, durabilityLossRatio), true);
                }
                else
                {
                    character.BroadcastPacket(new SCUnitDeathPacket(ObjId, killReason, (Unit)killer, (int)TimeSpan.FromSeconds(resurrection.WaitingTime).TotalMilliseconds), true);
                }
            }
            else if (this is Mate mate)
            {
                mate.Hp = 1;
                mate.Mp = 1;
                mate.Buffs.AddBuff((uint)BuffConstants.InjuryMount, mate);
                mate.Buffs.AddBuff((uint)BuffConstants.TrippedMount, mate);

                var riders = mate.Passengers.ToList();
                for (var i = riders.Count - 1; i >= 0; i--)
                {
                    var pos = riders[i].Key;
                    var rider = WorldManager.Instance.GetCharacterByObjId(riders[i].Value._objId);
                    if (rider != null)
                    {
                        MateManager.Instance.UnMountMate(rider, TlId, pos, AttachUnitReason.None);
                    }
                }

                var item = ItemManager.Instance.GetItemByItemId(mate.ItemId);
                if (item is Summon summon)
                {
                    summon.Exp = mate.Experience;
                    summon.NeedRepair = 1;
                    summon.Level = mate.Level;
                }
                mate.BroadcastPacket(new SCItemTaskSuccessPacket(ItemTaskType.MateDeath, new List<ItemTask> { new ItemUpdate(item) }, new List<ulong>()), true);
            }
            else if (this is Slave slave)
            {
                slave.BroadcastPacket(new SCUnitDeathPacket(ObjId, killReason, (Unit)killer), true);
                var riders = slave.AttachedCharacters.ToList();
                for (var i = riders.Count - 1; i >= 0; i--)
                {
                    var rider = WorldManager.Instance.GetCharacterByObjId(riders[i].Value.ObjId);
                    if (rider != null)
                    {
                        SlaveManager.Instance.UnbindSlave(rider, TlId, AttachUnitReason.None);
                    }
                }

                var item = ItemManager.Instance.GetItemByItemId(slave.ItemId);
                if (item is Summon summon)
                {
                    summon.NeedRepair = 1;
                    summon.Level = slave.Level;
                }
                slave.BroadcastPacket(new SCItemTaskSuccessPacket(ItemTaskType.SlaveDeath, new List<ItemTask> { new ItemUpdate(item) }, new List<ulong>()), true);
            }
            else
            {
                var lootDropItems = ItemManager.Instance.CreateLootDropItems(ObjId, killer);
                if (lootDropItems.Count > 0)
                {
                    if (killer is Character charr)
                        charr.SendPacket(new SCLootableStatePacket(ObjId, true));
                }
                BroadcastPacket(new SCUnitDeathPacket(ObjId, killReason, (Unit)killer), true);
            }

            if (killer == this)
                return;

            if (((Unit)killer).CurrentTarget.ObjId == ObjId)
            {
                killer.BroadcastPacket(new SCAiAggroPacket(killer.ObjId, 0), true);
                ((Unit)killer).SummarizeDamage = 0;

                killer.BroadcastPacket(new SCCombatClearedPacket(killer.ObjId), true);
                killer.BroadcastPacket(new SCCombatClearedPacket(ObjId), true);
                killer.BroadcastPacket(new SCTargetChangedPacket(killer.ObjId, 0), true);
                ((Unit)killer).CurrentTarget = null;
            }
        }

        private void DespawMate(Character character)
        {
            // if we died sitting on a horse
            if (character.Hp > 0) { return; }

            var mate = MateManager.Instance.GetActiveMate(character.ObjId);
            if (mate != null)
            {
                character.Mates.DespawnMate(mate.TlId);
            }
        }

        private async void StopAutoSkill(Unit character)
        {
            if (!(character is Character) || character.AutoAttackTask == null)
            {
                return;
            }

            await character.AutoAttackTask.CancelAsync();
            character.AutoAttackTask = null;
            character.IsAutoAttack = false; // turned off auto attack
            character.BroadcastPacket(new SCSkillEndedPacket(character.TlId), true);
            character.BroadcastPacket(new SCSkillStoppedPacket(character.ObjId, character.SkillId), true);
            TlIdManager.Instance.ReleaseId(character.TlId);
        }

        [Obsolete("This method is deprecated", false)]
        public void StartRegen()
        {
            if (_regenTask != null || Hp >= MaxHp && Mp >= MaxMp || Hp == 0)
            {
                return;
            }
            _regenTask = new UnitPointsRegenTask(this);
            TaskManager.Instance.Schedule(_regenTask, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        [Obsolete("This method is deprecated", false)]
        public async void StopRegen()
        {
            if (_regenTask == null)
            {
                return;
            }
            await _regenTask.CancelAsync();
            _regenTask = null;
        }

        public void SetInvisible(bool value)
        {
            Invisible = value;
            BroadcastPacket(new SCUnitInvisiblePacket(ObjId, Invisible), true);
        }

        public void SetGeoDataMode(bool value)
        {
            AppConfiguration.Instance.World.GeoDataMode = value;
        }
        public void SetGodMode(bool value)
        {
            AppConfiguration.Instance.World.GodMode = value;
        }
        public void SetGrowthRate(float value)
        {
            AppConfiguration.Instance.World.GrowthRate = value;
        }
        public void SetLootRate(float value)
        {
            AppConfiguration.Instance.World.LootRate = value;
        }
        public void SetVocationRate(float value)
        {
            AppConfiguration.Instance.World.VocationRate = value;
        }
        public void SetHonorRate(float value)
        {
            AppConfiguration.Instance.World.HonorRate = value;
        }
        public void SetExpRate(float value)
        {
            AppConfiguration.Instance.World.ExpRate = value;
        }
        public void SetAutoSaveInterval(float value)
        {
            AppConfiguration.Instance.World.AutoSaveInterval = value;
        }
        public void SetLogoutMessage(string value)
        {
            AppConfiguration.Instance.World.LogoutMessage = value;
        }
        public void SetMotdMessage(string value)
        {
            AppConfiguration.Instance.World.MOTD = value;
        }

        public void SetCriminalState(bool criminalState)
        {
            if (criminalState)
            {
                var buff = SkillManager.Instance.GetBuffTemplate((uint)BuffConstants.Retribution);
                var casterObj = new SkillCasterUnit(ObjId);
                Buffs.AddBuff(new Buff(this, this, casterObj, buff, null, DateTime.UtcNow));
            }
            else
            {
                Buffs.RemoveBuff((uint)BuffConstants.Retribution);
            }
        }

        public void SetForceAttack(bool value)
        {
            ForceAttack = value;
            if (ForceAttack)
            {
                var buff = SkillManager.Instance.GetBuffTemplate((uint)BuffConstants.Bloodlust);
                var casterObj = new SkillCasterUnit(ObjId);
                Buffs.AddBuff(new Buff(this, this, casterObj, buff, null, DateTime.UtcNow));
            }
            else
            {
                Buffs.RemoveBuff((uint)BuffConstants.Bloodlust);
            }
            BroadcastPacket(new SCForceAttackSetPacket(ObjId, ForceAttack), true);
        }

        public override void AddBonus(uint bonusIndex, Bonus bonus)
        {
            var bonuses = Bonuses.ContainsKey(bonusIndex) ? Bonuses[bonusIndex] : new List<Bonus>();
            bonuses.Add(bonus);
            Bonuses[bonusIndex] = bonuses;
        }

        public override void RemoveBonus(uint bonusIndex, UnitAttribute attribute)
        {
            if (!Bonuses.ContainsKey(bonusIndex))
            {
                return;
            }
            var bonuses = Bonuses[bonusIndex];
            foreach (var bonus in new List<Bonus>(bonuses))
            {
                if (bonus.Template != null && bonus.Template.Attribute == attribute)
                {
                    bonuses.Remove(bonus);
                }
            }
        }

        public List<Bonus> GetBonuses(UnitAttribute attribute)
        {
            var result = new List<Bonus>();
            if (Bonuses == null)
            {
                return result;
            }
            foreach (var bonuses in new List<List<Bonus>>(Bonuses.Values))
            {
                foreach (var bonus in new List<Bonus>(bonuses))
                {
                    if (bonus.Template != null && bonus.Template.Attribute == attribute)
                    {
                        result.Add(bonus);
                    }
                }
            }
            return result;
        }

        protected double CalculateWithBonuses(double value, UnitAttribute attr)
        {
            foreach (var bonus in GetBonuses(attr))
            {
                if (bonus.Template.ModifierType == UnitModifierType.Percent)
                    value += (value * bonus.Value / 100f);
                else
                    value += bonus.Value;
            }
            return value;
        }

        public void SendPacket(GamePacket packet)
        {
            Connection?.SendPacket(packet);
        }

        public void SendErrorMessage(ErrorMessageType type)
        {
            SendPacket(new SCErrorMsgPacket(type, 0, true));
        }

        /// <summary>
        /// Get distance between two units taking into account their model sizes
        /// </summary>
        /// <param name="baseUnit"></param>
        /// <param name="includeZAxis"></param>
        /// <returns></returns>
        public float GetDistanceTo(BaseUnit baseUnit, bool includeZAxis = false)
        {
            if (baseUnit == null)
                return 0.0f;

            if (Transform.World.Position.Equals(baseUnit.Transform.World.Position))
                return 0.0f;

            var rawDist = MathUtil.CalculateDistance(Transform.World.Position, baseUnit.Transform.World.Position, includeZAxis);
            if (baseUnit is Shipyard.Shipyard shipyard)
            {
                // Let's use the build radius for this, as it doesn't really have a easy to grab model to get it from 
                rawDist -= ShipyardManager.Instance._shipyardsTemplate[shipyard.ShipyardData.TemplateId].BuildRadius;
            }
            else
            if (baseUnit is House house)
            {
                // Subtract house radius, this should be fair enough for building
                rawDist -= (house.Template.GardenRadius * house.Scale);
            }
            else
            {
                // If target is a Unit, then use it's model for radius
                if (baseUnit is Unit unit)
                    rawDist -= ModelManager.Instance.GetActorModel(unit.ModelId)?.Radius ?? 0 * unit.Scale;
            }
            // Subtract own radius
            rawDist -= ModelManager.Instance.GetActorModel(ModelId)?.Radius ?? 0 * Scale;

            return Math.Max(rawDist, 0);
        }

        public virtual int GetAbLevel(AbilityType type)
        {
            return Level;
        }

        public string GetAttribute(UnitAttribute attr)
        {
            var props = GetType().GetProperties()
                .Where(o => (o.GetCustomAttributes(typeof(UnitAttributeAttribute), true) as IEnumerable<UnitAttributeAttribute>)
                    .Any(a => a.Attributes.Contains(attr)));

            if (props.Count() > 0)
                return props.ElementAt(0).GetValue(this).ToString();
            else
                return "NotFound";
        }

        public T GetAttribute<T>(UnitAttribute attr, T defaultVal)
        {
            var props = GetType().GetProperties()
                .Where(o => (o.GetCustomAttributes(typeof(UnitAttributeAttribute), true) as IEnumerable<UnitAttributeAttribute>)
                    .Any(a => a.Attributes.Contains(attr)));

            if (props.Any())
            {
                var ElementValue = props.ElementAt(0).GetValue(this);
                if (ElementValue is T ret)
                    return ret;
            }
            return defaultVal;
        }

        public string GetAttribute(uint attr) => GetAttribute((UnitAttribute)attr);

        //Uncomment if you need this
        /*
        public string GetAttribute(string attr)
        {
            if (Enum.TryParse(typeof(UnitAttribute), attr, true, out var result))
            {
                return GetAttribute((UnitAttribute)result);
            }
            return "FailedParse";
        }
        */

        public override void InterruptSkills()
        {
            ActivePlotState?.RequestCancellation();
            if (SkillTask == null)
                return;
            switch (SkillTask)
            {
                case EndChannelingTask ect:
                    ect.Skill.Stop(this, ect._channelDoodad);
                    break;
                default:
                    SkillTask.Skill.Stop(this);
                    break;
            }
        }

        public bool IsDead
        {
            get
            {
                return Hp <= 0;
            }
        }

        public bool NeedsRegen
        {
            get
            {
                return Hp < MaxHp || Mp < MaxMp;
            }
        }

        public virtual void OnSkillEnd(Skill skill)
        {

        }

        /// <summary>
        /// Does fall damage based on velocity 
        /// </summary>
        /// <param name="fallVel">Velocity value from MoveType</param>
        /// <returns>The damage that was dealt</returns>
        public virtual int DoFallDamage(ushort fallVel)
        {
            var fallDmg = Math.Min(MaxHp, (int)(MaxHp * ((fallVel - 8600) / 15000f)));
            var multiplier = CalculateWithBonuses(0d, UnitAttribute.FallDamageMul) / 100d;
            var minHpLeft = MaxHp / 20; //5% of hp 
            var maxDmgLeft = Hp - minHpLeft; // Max damage one can take 

            fallDmg = (int)(fallDmg + (fallDmg * multiplier));

            if (fallVel >= 32000)
            {
                ReduceCurrentHp(this, Hp); // This is instant death so should be first
                // This will also kill anybody riding this if this is a mount
            }
            else
            {
                if (fallDmg < maxDmgLeft)
                {
                    ReduceCurrentHp(this, fallDmg); //If you can take the hit without reaching 5% hp left take it
                }
                else
                {
                    var duration = 500 * (fallDmg / minHpLeft);

                    var buff = SkillManager.Instance.GetBuffTemplate((uint)BuffConstants.FallStun);
                    var casterObj = new SkillCasterUnit(ObjId);
                    Buffs.AddBuff(new Buff(this, this, casterObj, buff, null, DateTime.UtcNow), 0, duration);

                    if (Hp > minHpLeft)
                        ReduceCurrentHp(this, maxDmgLeft); // Leaves you at 5% hp no matter what
                }
            }

            BroadcastPacket(new SCEnvDamagePacket(EnvSource.Falling, ObjId, (uint)fallDmg), true);
            //SendPacket(new SCEnvDamagePacket(EnvSource.Falling, ObjId, (uint)fallDmg));
            // TODO: Maybe adjust formula & need to detect water landing?
            return fallDmg;
        }

        /// <summary>
        /// Set the faction of the owner
        /// </summary>
        /// <param name="factionId"></param>
        public void SetFaction(uint factionId)
        {
            // if (this is Character) { return; } // do not change the faction for the character
            BroadcastPacket(new SCUnitFactionChangedPacket(ObjId, Name, Faction?.Id ?? 0, factionId, false), true);
            Faction = FactionManager.Instance.GetFaction(factionId);

            // TODO added for quest Id=2486
            if (this is not Npc npc) { return; }
            // Npc attacks the character
            var characters = WorldManager.Instance.GetAround<Character>(npc, 5.0f);
            foreach (var character in characters.Where(CanAttack))
            {
                npc.Ai.Owner.AddUnitAggro(AggroKind.Damage, character, 1);
                npc.Ai.OnAggroTargetChanged();
                npc.Ai.GoToCombat();
            }
        }

        public virtual void UseSkill(uint skillId, IUnit target)
        {
            var skill = new Skill(SkillManager.Instance.GetSkillTemplate((uint)skillId));

            var caster = SkillCaster.GetByType(SkillCasterType.Unit);
            caster.ObjId = ObjId;

            var sct = SkillCastTarget.GetByType(SkillCastTargetType.Unit);
            sct.ObjId = target.ObjId;

            skill.Use(this, caster, sct, null, true);
        }

        public void ModelPosture(PacketStream stream, Unit unit, BaseUnitType baseUnitType, ModelPostureType modelPostureType, uint animActionId = 0xFFFFFFFF)
        {
            // TODO added that NPCs can be hunted to move their legs while moving, but if they sit or do anything they will just stand there
            if (baseUnitType == BaseUnitType.Npc) // NPC
            {
                if (unit is Npc npc)
                {
                    // TODO UnitModelPosture
                    if (npc.Faction.GuardHelp)
                    {
                        stream.Write((byte)modelPostureType); // оставим это для того, чтобы NPC могли заниматься своими делами // let's leave it so that the NPCs can go about their business
                        _log.Warn($"baseUnitType={baseUnitType}, modelPostureType={modelPostureType}");
                    }
                    else
                    {
                        modelPostureType = 0; // для NPC на которых можно напасть и чтобы они шевелили ногами (для людей особенно) // for NPCs that can be attacked and that they move their legs (especially for people)
                        stream.Write((byte)modelPostureType);
                        _log.Warn($"baseUnitType={baseUnitType}, modelPostureType={modelPostureType}");
                    }
                }
            }
            else // other
            {
                stream.Write((byte)modelPostureType);
            }

            stream.Write(false); // isLooted

            switch (modelPostureType)
            {
                case ModelPostureType.HouseState: // build
                    for (var i = 0; i < 2; i++)
                    {
                        stream.Write(true); // door
                    }

                    for (var i = 0; i < 6; i++)
                    {
                        stream.Write(true); // window
                    }

                    break;
                case ModelPostureType.ActorModelState: // npc
                    var npc = (Npc)unit;
                    stream.Write(animActionId == 0xFFFFFFFF ? npc.Template.AnimActionId : animActionId); // TODO to check for AnimActionId substitution
                    if (animActionId == 0xFFFFFFFF)
                    {
                        _log.Warn($"npc.Template.AnimActionId={npc.Template.AnimActionId}");
                    }
                    else
                    {
                        _log.Warn($"npc.Template.AnimActionId={animActionId}");
                    }

                    stream.Write(true); // activate
                    break;
                case ModelPostureType.FarmfieldState:
                    stream.Write(0u); // type(id)
                    stream.Write(0f); // growRate
                    stream.Write(0); // randomSeed
                    stream.Write(false); // isWithered
                    stream.Write(false); // isHarvested
                    break;
                case ModelPostureType.TurretState: // slave
                    stream.Write(0f); // pitch
                    stream.Write(0f); // yaw
                    break;
            }
        }
    }
}
