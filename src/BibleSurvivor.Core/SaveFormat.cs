using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Xml;

namespace BibleSurvivor.Core
{
    // Version probing deliberately does not construct RunState. A newer writer may have a
    // completely different Run representation; that must not be mistaken for recoverable corruption.
    public static class SaveFormat
    {
        public const int CurrentVersion = 2;
        private const int CurrentWorldVersion = 1;
        internal const int MaximumBytes = 4 * 1024 * 1024;
        private static readonly HashSet<string> EnvelopeFields = new HashSet<string> {
            "SchemaVersion", "Revision", "LastOperationId", "Profile", "Run" };
        private static readonly HashSet<string> RunFields = new HashSet<string> {
            "RunId", "RoundNumber", "Stage", "Health", "MaxHealth", "Level", "Experience", "KillRemainder",
            "LastHitTime", "PermanentSnapshot", "Weapons", "Passives", "Wave", "Drops", "PendingLevels",
            "ChoiceSerial", "Choices", "World", "RoundFingerprint" };
        // Explicit serialized shapes avoid reflection/code generation in the Unity/IL2CPP reader.
        // Dictionary keys are data inside Key/Value entries, not names of DTO members.
        private sealed class ContentShape
        {
            internal Dictionary<string, ContentShape>? Members;
            internal ContentShape? Item;
        }
        private static readonly ContentShape Value = new ContentShape();
        private static readonly ContentShape SupportedContent = BuildSupportedContent();

        private static ContentShape Object(string values, params (string Name, ContentShape Shape)[] children)
        {
            var members = new Dictionary<string, ContentShape>(StringComparer.Ordinal);
            foreach (var name in values.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)) members.Add(name, Value);
            foreach (var child in children) members[child.Name] = child.Shape;
            return new ContentShape { Members = members };
        }
        private static ContentShape Array(ContentShape item) => new ContentShape { Item = item };
        private static ContentShape BuildSupportedContent()
        {
            var integerMap = Array(Object("Key Value"));
            var profile = Object("Gold UnlockedWeapons", ("Upgrades", integerMap));
            var wave = Object("Elapsed Cursor Paused Expired", ("Active", Array(Object("Id MonsterId Health ContactDamage RawExperience"))));
            var entity = Object("Id X Y");
            var projectile = Object("Id X Y VelocityX VelocityY Lifetime Damage RemainingPierce Radius HitIds");
            var weapon = Object("WeaponId NextCycle ActiveUntil ActiveSince NextBurst BurstsRemaining PendingBursts",
                ("OrbitHits", Array(Object("Id At"))));
            var tuning = Object("PlayerSpeed CameraHalfWidth CameraHalfHeight SpawnMargin PlayerRadius EnemyRadius BibleOrbitRadius BookRadius JawboneRange SlingSpeed SlingLifetime PickupRadius MaxProjectiles");
            var world = Object("SchemaVersion Time Remainder PlayerX PlayerY FacingX FacingY RandomState NextProjectileId ShotsFired Hits Kills Collected DamageTaken",
                ("Tuning", tuning), ("Enemies", Array(entity)), ("Drops", Array(entity)),
                ("Projectiles", Array(projectile)), ("Weapons", Array(weapon)),
                ("MonsterSpeeds", Array(Object("MonsterId Ratio"))));
            var run = Object(string.Join(" ", RunFields), ("PermanentSnapshot", integerMap),
                ("Weapons", integerMap), ("Passives", integerMap), ("Drops", integerMap), ("Wave", wave), ("World", world));
            return Object(string.Join(" ", EnvelopeFields), ("Profile", profile), ("Run", run));
        }

        private static void CheckSupportedContent(XmlElement element, ContentShape shape)
        {
            // Leave null/primitive type validation and required-member semantics to the typed DTO
            // and Validate; this gate prevents silent loss of unknown members on supported DTOs.
            if (shape.Members != null && element.GetAttribute("type") == "object")
            {
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var member in element.ChildNodes.OfType<XmlElement>())
                {
                    if (!names.Add(member.Name)) throw new InvalidDataException("Duplicate nested save field");
                    if (!shape.Members.TryGetValue(member.Name, out var child)) throw new UnsupportedSaveVersionException();
                    CheckSupportedContent(member, child);
                }
            }
            else if (shape.Item != null && element.GetAttribute("type") == "array")
                foreach (var item in element.ChildNodes.OfType<XmlElement>()) CheckSupportedContent(item, shape.Item);
        }

        private static void CheckWorldVersion(XmlElement? world)
        {
            // World has an independent version domain. Protect its future payload in either
            // supported envelope version before typed DTO validation can recover an old backup.
            if (world == null || world.GetAttribute("type") != "object") return;
            var versions = world.ChildNodes.OfType<XmlElement>().Where(x => x.Name == "SchemaVersion").ToArray();
            if (versions.Length != 1 || versions[0].GetAttribute("type") != "number")
                throw new InvalidDataException("World requires exactly one positive integer SchemaVersion");
            var number = versions[0].InnerText;
            if (number.Length == 0 || number.Any(c => c < '0' || c > '9') || number.All(c => c == '0'))
                throw new InvalidDataException("World requires exactly one positive integer SchemaVersion");
            if (!int.TryParse(number, NumberStyles.None, CultureInfo.InvariantCulture, out var worldVersion) || worldVersion > CurrentWorldVersion)
                throw new UnsupportedSaveVersionException();
        }

        internal static int Probe(byte[] bytes)
        {
            if (bytes.Length > MaximumBytes) throw new InvalidDataException("Save too large");
            var document = new XmlDocument { XmlResolver = null };
            try
            {
                using var reader = JsonReaderWriterFactory.CreateJsonReader(bytes, XmlDictionaryReaderQuotas.Max);
                document.Load(reader);
            }
            catch (Exception error) when (error is XmlException || error is SerializationException || error is ArgumentException)
            { throw new InvalidDataException("Invalid save JSON header", error); }
            var root = document.DocumentElement;
            if (root == null || root.GetAttribute("type") != "object") throw new InvalidDataException("Save envelope must be an object");
            var fields = root.ChildNodes.OfType<XmlElement>().ToArray();
            var versions = fields.Where(x => x.Name == "SchemaVersion").ToArray();
            if (versions.Length != 1 || versions[0].GetAttribute("type") != "number")
                throw new InvalidDataException("Save requires exactly one positive integer SchemaVersion");
            var number = versions[0].InnerText;
            if (number.Length == 0 || number.Any(c => c < '0' || c > '9') || number.All(c => c == '0'))
                throw new InvalidDataException("Save requires exactly one positive integer SchemaVersion");
            // A positive integer larger than Int32 is still a future version, not corrupt data.
            // Never turn overflow into permission to recover and overwrite an older backup.
            if (!int.TryParse(number, NumberStyles.None, CultureInfo.InvariantCulture, out var version) || version > CurrentVersion)
                throw new UnsupportedSaveVersionException();
            if (fields.Select(x => x.Name).Distinct().Count() != fields.Length)
                throw new InvalidDataException("Duplicate save envelope field");
            var run = fields.SingleOrDefault(x => x.Name == "Run");
            if (run != null && run.GetAttribute("type") == "object")
            {
                var members = run.ChildNodes.OfType<XmlElement>().ToArray();
                if (members.Select(x => x.Name).Distinct().Count() != members.Length)
                    throw new InvalidDataException("Duplicate run field for this save format");
                CheckWorldVersion(members.SingleOrDefault(x => x.Name == "World"));
                if (members.Any(x => !RunFields.Contains(x.Name))) RejectUnsupportedContent(version, "Unknown run field for this save format");
                // This preparatory writer knows schema2 but not R03 yet. Future boss fields/stages
                // must be preserved as unsupported content, never recovered from an older backup.
                if (version == CurrentVersion)
                {
                    var stage = members.SingleOrDefault(x => x.Name == "Stage");
                    if (stage != null && stage.GetAttribute("type") == "string" && stage.InnerText != "normal" && stage.InnerText != "between_rounds")
                        throw new UnsupportedSaveVersionException();
                    var round = members.SingleOrDefault(x => x.Name == "RoundNumber");
                    if (stage != null && round != null && round.GetAttribute("type") == "number" &&
                        round.InnerText.Length > 0 && round.InnerText.All(c => c >= '0' && c <= '9') &&
                        (!int.TryParse(round.InnerText, NumberStyles.None, CultureInfo.InvariantCulture, out var numberValue) ||
                         (stage.InnerText == "normal" && numberValue > 2) || (stage.InnerText == "between_rounds" && numberValue > 3)))
                        throw new UnsupportedSaveVersionException();
                }
            }
            if (fields.Any(x => !EnvelopeFields.Contains(x.Name))) RejectUnsupportedContent(version, "Unknown save envelope field");
            if (version == CurrentVersion) CheckSupportedContent(root, SupportedContent);
            return version;
        }

        private static void RejectUnsupportedContent(int version, string message)
        {
            if (version == CurrentVersion) throw new UnsupportedSaveVersionException();
            throw new InvalidDataException(message);
        }
    }
}
