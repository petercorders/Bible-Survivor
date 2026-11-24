using System.Linq;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;

public sealed class AdamExodusPropTests
{
    [Test]
    public void AllReviewedPropsImportWithTheSharedTerrainRegistration()
    {
        var ids = AdamExodusPropArt.AssetIds.ToArray();
        Assert.That(ids.Length, Is.EqualTo(7));
        foreach (var id in ids)
        {
            var sprite = AdamExodusPropArt.Resolve(id, Resources.Load<Sprite>);
            Assert.That(sprite, Is.Not.Null, id);
            Assert.That(sprite.GetPhysicsShapeCount(), Is.Zero, id);
        }

        var small = new AdamWorldProjection(new Rect(0, 0, 620, 246), Vector2.zero);
        var fullHd = new AdamWorldProjection(new Rect(0, 0, 1880, 886), Vector2.zero);
        var west = AdamExodusArenaArt.BossWest;
        Assert.That(AdamExodusPropArt.Destination(small.Point(west.x, west.y), small.Scale),
            Is.EqualTo(new Rect(106, 69, 72, 42)));
        Assert.That(AdamExodusPropArt.Destination(fullHd.Point(west.x, west.y), fullHd.Scale),
            Is.EqualTo(new Rect(328, 281, 216, 126)));
    }

    [Test]
    public void MissingUnlistedOrMisregisteredSpriteLeavesTheSlotAvailableForFallback()
    {
        var valid = Resources.Load<Sprite>(AdamExodusPropArt.Root + AdamExodusPropArt.Kiln);
        Assert.That(valid, Is.Not.Null);
        Assert.That(AdamExodusPropArt.Resolve(AdamExodusPropArt.Kiln, _ => null), Is.Null);
        Assert.That(AdamExodusPropArt.Resolve("unlisted", _ => valid), Is.Null);
        var incorrect = Sprite.Create(valid.texture, valid.rect, Vector2.zero, 32);
        try { Assert.That(AdamExodusPropArt.Resolve(AdamExodusPropArt.Kiln, _ => incorrect), Is.Null); }
        finally { Object.DestroyImmediate(incorrect); }
        Assert.That(AdamExodusPropArt.Resolve(AdamExodusPropArt.Stack, Resources.Load<Sprite>), Is.Not.Null);
    }
}
