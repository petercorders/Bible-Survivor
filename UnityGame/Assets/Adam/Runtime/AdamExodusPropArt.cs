using System.Collections.Generic;
using UnityEngine;

namespace BibleSurvivor.Unity
{
    public static class AdamExodusPropArt
    {
        public const string Root = "ExodusArenaProps/";
        internal const string Kiln = "dec_exodus_brick-kiln_static_72x42";
        internal const string Stack = "dec_exodus_brick-stack_static_72x42";
        internal const string Basket = "dec_exodus_reed-basket_static_72x42";
        internal const string Passage = "dec_exodus_sea-passage_static_72x42";
        internal const string Springs = "dec_exodus_elim-springs_static_72x42";
        internal const string Foothill = "dec_exodus_sinai-foothill_static_72x42";
        internal const string Court = "dec_exodus_tabernacle-court_static_72x42";

        // Public build inputs are limited to the reviewed single-frame PNG exports.
        static readonly Dictionary<string, string> Hashes = new Dictionary<string, string>
        {
            [Kiln] = "9bbda5d3a59cb307d2fee721e63b440827c392bc93bd4da3ed0baee47783d6c3",
            [Stack] = "9af9c335a53927e58cf11983e7f1243823c9138cca7bf1edff7b2c05bc185d17",
            [Basket] = "023ab950ded53f2ed3a2dbd1e59e93a859a7a8f06e7da692c6a2c62745fee5db",
            [Passage] = "ca765da735683d03f6fd9d36cc45260348e82651bc5f68277265116ffff1be87",
            [Springs] = "49ad76b597f7eec0026b1dd116b357e5b948993d638a41620279aa4b418b5f01",
            [Foothill] = "27593519e91c239ec1ddd2cb4f4fadf0c8b114deaf8da5a9cb9bcf2e95ceb9c7",
            [Court] = "9412ffc75b81ec8546604d3de14a1bf3fde0edd3e785970b32d2f7644b4c1cdd",
        };

        static readonly AdamGenesisPropArt.Slot Registration = new AdamGenesisPropArt.Slot
        {
            width = 72, height = 42, pivotX = 36, pivotY = 20, x = -36, y = -22
        };

        static readonly Dictionary<string, Sprite> Props = new Dictionary<string, Sprite>();
        static readonly List<Object> OwnedPresentation = new List<Object>();

        static AdamExodusPropArt() { Application.quitting += Release; }

        public static IEnumerable<string> AssetIds => Hashes.Keys;
        public static string ExpectedHash(string id) => Hashes.TryGetValue(id, out var hash) ? hash : null;
        public static Vector2 NormalizedPivot => Registration.NormalizedPivot;
        public static bool Matches(Sprite sprite) => AdamGenesisPropArt.MatchesSlot(sprite, Registration) && sprite.texture.isReadable;

        internal static Rect Destination(Vector2 origin, int scale) => Registration.Destination(origin, scale);

        internal static Sprite Resolve(string id, System.Func<string, Sprite> load)
        {
            if (ExpectedHash(id) == null) return null;
            var sprite = load(Root + id);
            return Matches(sprite) ? sprite : null;
        }

        internal static bool TryDraw(string id, Rect destination)
        {
            if (!Props.TryGetValue(id, out var sprite))
            {
                sprite = Resolve(id, Resources.Load<Sprite>);
                if (sprite != null && QualitySettings.activeColorSpace == ColorSpace.Linear)
                    sprite = AdamVisualQualityArt.PresentationFrames(new[] { sprite }, OwnedPresentation)[0];
                Props.Add(id, sprite);
            }
            if (sprite == null) return false;
            AdamArt.Blit(sprite, destination);
            return true;
        }

        static void Release()
        {
            for (var index = OwnedPresentation.Count - 1; index >= 0; index--)
                Object.Destroy(OwnedPresentation[index]);
            OwnedPresentation.Clear();
            Props.Clear();
        }
    }
}
