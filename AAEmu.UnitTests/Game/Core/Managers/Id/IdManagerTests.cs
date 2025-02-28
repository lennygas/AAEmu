﻿using AAEmu.Game.Core.Managers.Id;
using Xunit;

namespace AAEmu.UnitTests.Game.Core.Managers.Id;

public class IdManagerTests
{
    [Fact]
    public void ObjectIdManagerGetsNextId()
    {
        ObjectIdManager.Instance.Initialize(true);
        var firstId = 0x00000100u;
        var id = ObjectIdManager.Instance.GetNextId();
        Assert.Equal(firstId, id);
        id = ObjectIdManager.Instance.GetNextId();
        Assert.Equal(firstId + 1, id);
        id = ObjectIdManager.Instance.GetNextId();
        Assert.Equal(firstId + 2, id);
    }

    [Fact]
    public void ObjectIdManagerReleasesId()
    {
        ObjectIdManager.Instance.Initialize(true);
        var firstId = 0x00000100u;
        var id = ObjectIdManager.Instance.GetNextId();
        Assert.Equal(firstId, id);
        id = ObjectIdManager.Instance.GetNextId();
        Assert.Equal(firstId + 1, id);

        ObjectIdManager.Instance.ReleaseId(id);

        id = ObjectIdManager.Instance.GetNextId();
        // We get the next ID and THEN release
        Assert.Equal(firstId + 1, id);
    }

    [Fact]
    public void ObjectIdManagerGetMultipleIds()
    {
        ObjectIdManager.Instance.Initialize(true);

        var firstId = 0x00000100u;
        var ids = ObjectIdManager.Instance.GetNextId(10);
        Assert.Equal(new uint[] { firstId, firstId + 1, firstId + 2, firstId + 3, firstId + 4, firstId + 5, firstId + 6, firstId + 7, firstId + 8, firstId + 9 }, ids);
    }
}
