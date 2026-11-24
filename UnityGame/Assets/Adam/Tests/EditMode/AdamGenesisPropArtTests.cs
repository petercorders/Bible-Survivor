using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;

public sealed class AdamGenesisPropArtTests
{
    [Test]
    public void MissingAndChangedManifestUseTheLegacyPath()
    {
        Assert.That(AdamGenesisPropArt.HasReviewedManifest(null), Is.False);
        Assert.That(AdamGenesisPropArt.HasReviewedManifest(new byte[] { 0, 1 }), Is.False);
        Assert.That(AdamGenesisPropArt.ReadManifest(null), Is.Null);
        Assert.That(AdamGenesisPropArt.ReadManifest(new byte[] { 0, 1 }), Is.Null);
    }

    [Test]
    public void CompleteReviewPairsFitTheirSlotsAndMissingLayersRejectThePair()
    {
        var asset = Resources.Load<TextAsset>(AdamGenesisPropArt.Root + "manifest");
        Assert.That(asset, Is.Not.Null);
        var manifest = AdamGenesisPropArt.ReadManifest(asset.bytes);
        Assert.That(manifest, Is.Not.Null);
        Assert.That(manifest.version, Is.EqualTo(32));
        Assert.That(manifest.shadowAlpha, Is.EqualTo(.45f));
        Assert.That(manifest.assets.Length, Is.EqualTo(3));
        foreach (var slot in manifest.assets)
        {
            var body = Resources.Load<Sprite>(AdamGenesisPropArt.Root + slot.id);
            var shadow = Resources.Load<Sprite>(AdamGenesisPropArt.Root + slot.id + AdamGenesisPropArt.ShadowSuffix);
            Assert.That(AdamGenesisPropArt.PairMatches(slot, body, shadow), Is.True, slot.id);
            Assert.That(AdamGenesisPropArt.PairMatches(slot, body, null), Is.False, slot.id);
            Assert.That(AdamGenesisPropArt.PairMatches(slot, null, shadow), Is.False, slot.id);
        }
    }

    [TestCase(0, 0, 16, 16)]
    [TestCase(-34, -20, 68, 38)]
    [TestCase(9, -6, 9, 11)]
    public void SlotPlacementUsesIntegerTopLeftWithoutRoundingSpritePivot(int x, int y, int width, int height)
    {
        var slot = new AdamGenesisPropArt.Slot { x = x, y = y, width = width, height = height, pivotX = width / 2f };
        Assert.That(slot.Destination(new Vector2(100, 80), 1), Is.EqualTo(new Rect(100 + x, 80 + y, width, height)));
        Assert.That(slot.Destination(new Vector2(100, 80), 3), Is.EqualTo(new Rect(100 + 3 * x, 80 + 3 * y, 3 * width, 3 * height)));
    }
}
