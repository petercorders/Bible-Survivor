using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;

public sealed class AdamGenesisStoryTests
{
    [TestCase(1, "Eden", "창 2:8–10")]
    [TestCase(2, "Field", "창 4:3–8")]
    [TestCase(3, "Ark", "창 6:14–16")]
    [TestCase(4, "Tent", "창 15:5, 18:1")]
    [TestCase(5, "Gate", "창 19:15–26")]
    [TestCase(6, "Well", "창 26:19–22")]
    [TestCase(7, "Bethel", "창 28:10–19")]
    [TestCase(8, "Granary", "창 37:3, 39:20, 41:48–49")]
    [TestCase(9, "Hall", "창 44:12, 18–34")]
    public void RoundProvidesItsOwnMotifAndReading(int round, string motif, string reference)
    {
        var story = AdamGenesisRoundStory.Find(round);
        Assert.That(story.Round, Is.EqualTo(round));
        Assert.That(story.Environment.ToString(), Is.EqualTo(motif));
        Assert.That(story.Reference, Is.EqualTo(reference));
        Assert.That(story.Title, Is.Not.Empty);
        Assert.That(story.Summary, Is.Not.Empty);
    }

    [TestCase(0)]
    [TestCase(10)]
    [TestCase(180)]
    public void RoundsOutsideGenesisKeepTheirOriginalTerrain(int round)
    {
        Assert.That(AdamGenesisRoundStory.Find(round), Is.Null);
    }

    [TestCase(660, 440)]
    [TestCase(1920, 1080)]
    public void PlacementsAreInTheInitialViewOutsideItsCombatCenter(int width, int height)
    {
        var arena = AdamGame.Arena(width, height);
        var fresh = new AdamWorldProjection(arena, Vector2.zero);
        var reentered = new AdamWorldProjection(arena, Vector2.zero);
        var moved = new AdamWorldProjection(arena, Vector2.right);
        for (var round = 1; round <= 9; round++)
        {
            var story = AdamGenesisRoundStory.Find(round);
            var west = AdamGenesisArenaArt.Bounds(fresh, story.West);
            var east = AdamGenesisArenaArt.Bounds(fresh, story.East);
            Assert.That(fresh.TextureRect.Contains(west.min) && fresh.TextureRect.Contains(west.max), Is.True);
            Assert.That(fresh.TextureRect.Contains(east.min) && fresh.TextureRect.Contains(east.max), Is.True);
            Assert.That(west.xMax, Is.LessThan(arena.width / 2 - 64 * fresh.Scale));
            Assert.That(east.xMin, Is.GreaterThan(arena.width / 2 + 64 * fresh.Scale));
            Assert.That(AdamGenesisArenaArt.Bounds(reentered, story.West), Is.EqualTo(west));
            Assert.That(AdamGenesisArenaArt.Bounds(reentered, story.East), Is.EqualTo(east));
            Assert.That(AdamGenesisArenaArt.Bounds(moved, story.West).x, Is.EqualTo(west.x - 32 * fresh.Scale));
        }
    }

    [Test]
    public void ReviewedSpritesCanBeReusedAndUnknownDecorSelectsTheFallback()
    {
        var art = new AdamArt();
        Assert.That(AdamGenesisArenaArt.PropSprite(art, AdamGenesisArenaArt.Rock), Is.Not.Null);
        Assert.That(AdamGenesisArenaArt.PropSprite(art, AdamGenesisArenaArt.Scroll), Is.Not.Null);
        Assert.That(AdamGenesisArenaArt.PropSprite(art, "missing-genesis-decor"), Is.Null);
    }

    [TestCase(AdamGenesisArenaArt.Rock)]
    [TestCase(AdamGenesisArenaArt.Scroll)]
    public void DecorationFallsBackWhenItsSpriteSourceIsUnavailable(string id)
    {
        Assert.That(AdamGenesisArenaArt.PropSprite(null, id), Is.Null);
    }
}
