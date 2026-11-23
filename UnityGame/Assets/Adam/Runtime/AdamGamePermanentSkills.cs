using System;
using System.Collections.Generic;
using System.Linq;
using BibleSurvivor.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BibleSurvivor.Unity
{
    public sealed partial class AdamGame
    {
        private enum SkillFocus { Trees, Nodes, Prerequisite, Purchase, Refund, Close }

        private static readonly HashSet<string> PlayablePermanentSkills = new HashSet<string>(StringComparer.Ordinal)
        {
            "attack", "health", "armor", "move", "pickup", "growth", "cooldown", "regen",
            "common.power.02", "common.power.03", "common.power.04", "common.power.05", "common.power.06",
            "common.power.07", "common.power.08", "common.power.09", "common.power.10",
            "common.crit.01", "common.crit.02", "common.crit.03", "common.crit.04", "common.crit.05",
            "common.crit.06", "common.crit.07", "common.crit.08", "common.crit.09", "common.crit.10",
            "common.duration.01", "common.duration.02", "common.duration.03", "common.duration.04", "common.duration.05",
            "common.duration.06", "common.duration.07", "common.duration.08", "common.duration.09", "common.duration.10",
            "common.shot.01", "common.shot.02", "common.shot.03",
            "common.shot.05", "common.shot.06", "common.shot.07", "common.shot.08", "common.shot.09", "common.shot.10",
            "common.tempo.02", "common.tempo.03", "common.tempo.04", "common.tempo.05", "common.tempo.06",
            "common.tempo.07", "common.tempo.08", "common.tempo.09", "common.tempo.10",
            "common.area.01", "common.area.02", "common.area.03", "common.area.04", "common.area.05", "common.area.06",
            "common.area.07", "common.area.08", "common.area.09", "common.area.10",
            "weapon.bible.base.01", "weapon.bible.base.02", "weapon.bible.base.03",
            "weapon.bible.base.04", "weapon.bible.base.05",
            "weapon.bible.a.01", "weapon.bible.a.02", "weapon.bible.a.03", "weapon.bible.a.04", "weapon.bible.a.05",
            "weapon.bible.b.01", "weapon.bible.b.02", "weapon.bible.b.03", "weapon.bible.b.04", "weapon.bible.b.05",
            "weapon.bible.c.01", "weapon.bible.c.02", "weapon.bible.c.03", "weapon.bible.c.04", "weapon.bible.c.05",
            "weapon.bible.d.01", "weapon.bible.d.02", "weapon.bible.d.03", "weapon.bible.d.04", "weapon.bible.d.05",
            "weapon.bible.e.01", "weapon.bible.e.02", "weapon.bible.e.03", "weapon.bible.e.04", "weapon.bible.e.05",
            "weapon.jawbone.base.01", "weapon.jawbone.base.02", "weapon.jawbone.base.03",
            "weapon.jawbone.base.04", "weapon.jawbone.base.05",
            "weapon.jawbone.a.01", "weapon.jawbone.a.02", "weapon.jawbone.a.03", "weapon.jawbone.a.04", "weapon.jawbone.a.05",
            "weapon.jawbone.b.01", "weapon.jawbone.b.02", "weapon.jawbone.b.03", "weapon.jawbone.b.04", "weapon.jawbone.b.05",
            "weapon.jawbone.c.01", "weapon.jawbone.c.02", "weapon.jawbone.c.03", "weapon.jawbone.c.04", "weapon.jawbone.c.05",
            "weapon.jawbone.d.01", "weapon.jawbone.d.02", "weapon.jawbone.d.03", "weapon.jawbone.d.04", "weapon.jawbone.d.05",
            "weapon.jawbone.e.01", "weapon.jawbone.e.02", "weapon.jawbone.e.03", "weapon.jawbone.e.04", "weapon.jawbone.e.05",
            "weapon.sling.base.01", "weapon.sling.base.02", "weapon.sling.base.03",
            "weapon.sling.base.04", "weapon.sling.base.05",
            "weapon.sling.a.01", "weapon.sling.a.02", "weapon.sling.a.03",
            "weapon.sling.a.04", "weapon.sling.a.05",
            "weapon.sling.b.01", "weapon.sling.b.02", "weapon.sling.b.03", "weapon.sling.b.04", "weapon.sling.b.05",
            "weapon.sling.c.01", "weapon.sling.c.02", "weapon.sling.c.03", "weapon.sling.c.04", "weapon.sling.c.05",
            "weapon.sling.d.01", "weapon.sling.d.02", "weapon.sling.d.03", "weapon.sling.d.04", "weapon.sling.d.05",
            "weapon.sling.e.01", "weapon.sling.e.02", "weapon.sling.e.03", "weapon.sling.e.04", "weapon.sling.e.05",
            "common.health.02", "common.health.03", "common.health.04",
            "common.health.05", "common.health.06", "common.health.07",
            "common.health.08", "common.health.09", "common.health.10",
            "common.armor.02", "common.armor.03", "common.armor.04",
            "common.armor.05", "common.armor.06", "common.armor.07",
            "common.armor.08", "common.armor.09", "common.armor.10",
            "common.regen.02", "common.regen.03", "common.regen.04",
            "common.regen.05", "common.regen.06", "common.regen.07",
            "common.regen.08", "common.regen.09", "common.regen.10",
            "common.move.02", "common.move.03", "common.move.04",
            "common.move.05", "common.move.06", "common.move.07",
            "common.move.08", "common.move.09", "common.move.10",
            "common.xp.02", "common.xp.03", "common.xp.04",
            "common.xp.05", "common.xp.06", "common.xp.07",
            "common.xp.08", "common.xp.09", "common.xp.10",
            "common.pickup.02", "common.pickup.03", "common.pickup.04",
            "common.pickup.05", "common.pickup.06", "common.pickup.07",
            "common.pickup.08", "common.pickup.09", "common.pickup.10",
            "weapon.wilderness_voice.base.01", "weapon.wilderness_voice.base.02", "weapon.wilderness_voice.base.03",
            "weapon.wilderness_voice.base.04", "weapon.wilderness_voice.base.05",
            "weapon.wilderness_voice.a.01", "weapon.wilderness_voice.b.01",
            "weapon.wilderness_voice.c.01", "weapon.wilderness_voice.c.02", "weapon.wilderness_voice.c.03", "weapon.wilderness_voice.c.04", "weapon.wilderness_voice.c.05",
            "weapon.wilderness_voice.d.01", "weapon.wilderness_voice.d.02", "weapon.wilderness_voice.d.03", "weapon.wilderness_voice.d.04", "weapon.wilderness_voice.d.05",
            "weapon.wilderness_voice.a.02", "weapon.wilderness_voice.a.03",
            "weapon.wilderness_voice.a.04", "weapon.wilderness_voice.a.05",
            "weapon.wilderness_voice.b.02", "weapon.wilderness_voice.b.03", "weapon.wilderness_voice.b.04", "weapon.wilderness_voice.b.05",
            "weapon.wilderness_voice.e.01", "weapon.wilderness_voice.e.02", "weapon.wilderness_voice.e.03", "weapon.wilderness_voice.e.04", "weapon.wilderness_voice.e.05",
            "weapon.lightning.base.01", "weapon.lightning.base.02", "weapon.lightning.base.03",
            "weapon.lightning.base.04", "weapon.lightning.base.05",
            "weapon.lightning.a.01", "weapon.lightning.a.02", "weapon.lightning.e.01",
            "weapon.lightning.a.03", "weapon.lightning.a.04", "weapon.lightning.a.05",
            "weapon.lightning.b.01", "weapon.lightning.b.02", "weapon.lightning.b.03",
            "weapon.lightning.b.04", "weapon.lightning.b.05",
            "weapon.lightning.c.01", "weapon.lightning.c.02", "weapon.lightning.c.03",
            "weapon.lightning.c.04", "weapon.lightning.c.05",
            "weapon.lightning.d.01", "weapon.lightning.d.02", "weapon.lightning.d.03",
            "weapon.lightning.d.04", "weapon.lightning.d.05",
            "weapon.lightning.e.02", "weapon.lightning.e.03", "weapon.lightning.e.04", "weapon.lightning.e.05",
            "weapon.moses_staff.base.01", "weapon.moses_staff.base.02", "weapon.moses_staff.base.03",
            "weapon.moses_staff.base.04", "weapon.moses_staff.base.05",
            "weapon.moses_staff.a.01", "weapon.moses_staff.a.02", "weapon.moses_staff.a.03", "weapon.moses_staff.b.01",
            "weapon.moses_staff.a.04", "weapon.moses_staff.a.05",
            "weapon.moses_staff.b.02", "weapon.moses_staff.b.03", "weapon.moses_staff.b.04", "weapon.moses_staff.b.05",
            "weapon.moses_staff.c.01", "weapon.moses_staff.c.02", "weapon.moses_staff.c.03", "weapon.moses_staff.c.04", "weapon.moses_staff.c.05",
            "weapon.moses_staff.d.01", "weapon.moses_staff.d.02", "weapon.moses_staff.d.03", "weapon.moses_staff.d.04", "weapon.moses_staff.d.05",
            "weapon.moses_staff.e.01", "weapon.moses_staff.e.02", "weapon.moses_staff.e.03", "weapon.moses_staff.e.04", "weapon.moses_staff.e.05",
            "weapon.fire_pillar.base.01", "weapon.fire_pillar.base.02", "weapon.fire_pillar.base.03",
            "weapon.fire_pillar.base.04", "weapon.fire_pillar.base.05",
            "weapon.fire_pillar.a.01", "weapon.fire_pillar.a.02", "weapon.fire_pillar.b.01", "weapon.fire_pillar.e.01",
            "weapon.fire_pillar.a.03", "weapon.fire_pillar.a.04", "weapon.fire_pillar.b.02",
            "weapon.fire_pillar.a.05", "weapon.fire_pillar.b.03", "weapon.fire_pillar.b.04", "weapon.fire_pillar.b.05",
            "weapon.fire_pillar.c.01", "weapon.fire_pillar.c.02", "weapon.fire_pillar.c.03", "weapon.fire_pillar.c.04", "weapon.fire_pillar.c.05",
            "weapon.fire_pillar.d.01", "weapon.fire_pillar.d.02", "weapon.fire_pillar.d.03", "weapon.fire_pillar.d.04", "weapon.fire_pillar.d.05",
            "weapon.fire_pillar.e.02", "weapon.fire_pillar.e.03", "weapon.fire_pillar.e.04", "weapon.fire_pillar.e.05",
            "weapon.daniel_lion.base.01", "weapon.daniel_lion.base.02", "weapon.daniel_lion.base.03",
            "weapon.daniel_lion.base.04", "weapon.daniel_lion.base.05",
            "weapon.daniel_lion.a.01", "weapon.daniel_lion.a.02", "weapon.daniel_lion.b.01",
            "weapon.daniel_lion.a.03", "weapon.daniel_lion.a.04", "weapon.daniel_lion.a.05",
            "weapon.daniel_lion.b.02", "weapon.daniel_lion.b.03", "weapon.daniel_lion.b.04", "weapon.daniel_lion.b.05",
            "weapon.daniel_lion.e.01",
            "weapon.daniel_lion.c.01", "weapon.daniel_lion.c.02", "weapon.daniel_lion.c.03",
            "weapon.daniel_lion.c.04", "weapon.daniel_lion.c.05", "weapon.daniel_lion.e.02", "weapon.daniel_lion.e.03",
            "weapon.daniel_lion.e.04", "weapon.daniel_lion.e.05",
            "weapon.daniel_lion.d.01", "weapon.daniel_lion.d.02", "weapon.daniel_lion.d.03", "weapon.daniel_lion.d.04", "weapon.daniel_lion.d.05",
            "weapon.cross.base.01", "weapon.cross.base.02", "weapon.cross.base.03",
            "weapon.cross.base.04", "weapon.cross.base.05",
            "weapon.cross.d.01", "weapon.cross.d.02", "weapon.cross.d.03",
            "weapon.cross.d.04", "weapon.cross.d.05",
            "weapon.cross.b.05", "weapon.cross.e.02", "weapon.cross.e.03", "weapon.cross.e.04",
            "weapon.cross.a.05", "weapon.cross.c.05", "weapon.cross.e.05",
            "weapon.cross.a.01", "weapon.cross.a.02", "weapon.cross.b.01",
            "weapon.cross.c.01", "weapon.cross.c.02", "weapon.cross.e.01",
            "weapon.cross.a.03", "weapon.cross.a.04", "weapon.cross.b.02", "weapon.cross.b.03",
            "weapon.cross.b.04", "weapon.cross.c.03", "weapon.cross.c.04",
            "weapon.elijah_chariot.base.01", "weapon.elijah_chariot.base.02", "weapon.elijah_chariot.base.03",
            "weapon.elijah_chariot.base.04", "weapon.elijah_chariot.base.05",
            "weapon.elijah_chariot.a.04", "weapon.elijah_chariot.c.01", "weapon.elijah_chariot.c.02", "weapon.elijah_chariot.c.03",
            "weapon.elijah_chariot.c.04", "weapon.elijah_chariot.c.05",
            "weapon.elijah_chariot.d.02", "weapon.elijah_chariot.d.03", "weapon.elijah_chariot.d.04", "weapon.elijah_chariot.d.05",
            "weapon.elijah_chariot.e.03", "weapon.elijah_chariot.e.04", "weapon.elijah_chariot.e.05",
            "weapon.elijah_chariot.a.01", "weapon.elijah_chariot.b.01", "weapon.elijah_chariot.d.01",
            "weapon.elijah_chariot.e.01", "weapon.elijah_chariot.e.02",
            "weapon.elijah_chariot.a.02", "weapon.elijah_chariot.a.03", "weapon.elijah_chariot.a.05",
            "weapon.elijah_chariot.b.02", "weapon.elijah_chariot.b.03", "weapon.elijah_chariot.b.04", "weapon.elijah_chariot.b.05",
        };

        public static bool OffersPermanentSkillPurchase(string id) => PlayablePermanentSkills.Contains(id);
        private PermanentSkillNode[][] skillTrees;
        private readonly List<PermanentSkillNode> skillHistory = new List<PermanentSkillNode>(4);
        private int skillTree;
        private int skillRow;
        private int skillPrerequisite;
        private SkillFocus skillFocus;
        private bool showSkillTrees;
        private bool skillRevealRow;
        private bool skillRevealDetails;
        private Vector2 skillListScroll;
        private Vector2 skillDetailScroll;
        private Vector2 skillBrowserSize;
        private bool skillEntryFocused;
        private string skillEntryScreen;
        private PermanentSkillCommand skillConfirmation;
        private long skillConfirmedBalance;
        private bool skillConfirmFocused;
        private string skillResult;
        private bool skillResultSuccess;

        private bool CanShowPermanentSkillEntry => Host?.CanOpenPermanentSkills == true && !DeathPresentationVisible;
        private bool PermanentSkillEntryHasFocus => skillEntryFocused && CanShowPermanentSkillEntry;

        private PermanentSkillNode ActiveSkillNode => skillTrees[skillTree][skillRow];
        private ProfileState SkillProfile => Host.PermanentSkillProfile;
        private string SkillEntryScreen => Host.BookComplete ? "책 완료" :
            Host.State.Run != null ? "일시정지" : Host.Session == null ? "시작 화면" : "다시 도전";

        private void DrawPermanentSkillEntry(Rect rect, string origin)
        {
            var text = PermanentSkillEntryHasFocus ? "영구 스킬 (Enter)" : "영구 스킬 (K)";
            if (CanShowPermanentSkillEntry &&
                SkillButton(rect, text, PermanentSkillEntryHasFocus, "open"))
                OpenPermanentSkillMenu(origin);
        }

        private string CompetingEnterHint(string text) =>
            PermanentSkillEntryHasFocus ? text.Replace(" (Enter)", "") : text;

        private void OpenPermanentSkillMenu(string origin)
        {
            if (!CanShowPermanentSkillEntry || commandFrame == Time.frameCount) return;
            commandFrame = Time.frameCount;
            if (!Host.OpenPermanentSkills()) return;
            skillEntryScreen = origin;
            skillEntryFocused = false;
            skillConfirmation = null;
            skillResult = null;
            skillHistory.Clear();
            skillFocus = SkillFocus.Nodes;
            showSkillTrees = false;
            EnsurePermanentSkillCatalog();
            skillRevealRow = true;
            inputReleased = false;
            touchStarted = false;
        }

        private void EnsurePermanentSkillCatalog()
        {
            if (skillTrees != null) return;
            skillTrees = PermanentSkillCatalog.Current.Nodes.GroupBy(node => node.TreeId).Select(tree => tree.ToArray()).ToArray();
            var initial = skillTrees.SelectMany(tree => tree).FirstOrDefault(CanBuyPermanentSkill);
            if (initial != null) SelectPermanentSkill(initial);
        }

        private bool HandlePermanentSkillInput(Keyboard keys)
        {
            if (!Host.PermanentSkillsOpen)
            {
                if (!CanShowPermanentSkillEntry)
                {
                    skillEntryFocused = false;
                    return false;
                }
                if (keys == null || keys.altKey.isPressed) return false;
                if (keys.tabKey.wasPressedThisFrame) skillEntryFocused = !skillEntryFocused;
                if (keys.leftArrowKey.wasPressedThisFrame || keys.rightArrowKey.wasPressedThisFrame ||
                    keys.aKey.wasPressedThisFrame || keys.dKey.wasPressedThisFrame) skillEntryFocused = false;
                if (!keys.kKey.wasPressedThisFrame && !(skillEntryFocused && keys.enterKey.wasPressedThisFrame)) return false;
                OpenPermanentSkillMenu(SkillEntryScreen);
                return true;
            }

            EnsurePermanentSkillCatalog();
            if (keys == null || Host.Suspended || keys.altKey.isPressed || commandFrame == Time.frameCount) return true;
            var enter = keys.enterKey.wasPressedThisFrame || keys.numpadEnterKey.wasPressedThisFrame;
            var escape = keys.escapeKey.wasPressedThisFrame;
            var left = keys.leftArrowKey.wasPressedThisFrame;
            var right = keys.rightArrowKey.wasPressedThisFrame;
            var up = keys.upArrowKey.wasPressedThisFrame;
            var down = keys.downArrowKey.wasPressedThisFrame;
            var tab = keys.tabKey.wasPressedThisFrame;
            if (Host.HasPermanentSkillRetry)
            {
                if (enter) RetrySkillWrite();
            }
            else if (skillResult != null)
            {
                if (enter || escape) skillResult = null;
            }
            else if (skillConfirmation != null)
            {
                if (escape) skillConfirmation = null;
                else if (enter)
                {
                    if (skillConfirmFocused) ConfirmSkillWrite();
                    else skillConfirmation = null;
                }
                else if (tab) skillConfirmFocused = !skillConfirmFocused;
                else if (left || right) skillConfirmFocused = right;
            }
            else if (escape)
            {
                if (showSkillTrees) ToggleSkillTrees();
                else ClosePermanentSkillMenu();
            }
            else if (keys.tKey.wasPressedThisFrame) ToggleSkillTrees();
            else if (keys.qKey.wasPressedThisFrame || keys.eKey.wasPressedThisFrame)
                ChangePermanentSkillTree(keys.eKey.wasPressedThisFrame ? 1 : -1);
            else if (keys.backspaceKey.wasPressedThisFrame) ReturnFromPrerequisite();
            else if (tab && !showSkillTrees)
            {
                const int focusCount = (int)SkillFocus.Close + 1;
                var direction = keys.shiftKey.isPressed ? -1 : 1;
                skillFocus = (SkillFocus)(((int)skillFocus + direction + focusCount) % focusCount);
                skillRevealDetails = true;
            }
            else if (showSkillTrees)
            {
                if (up || down) ChangePermanentSkillTree(down ? 1 : -1);
                else if (enter) ToggleSkillTrees();
            }
            else if (keys.gKey.wasPressedThisFrame)
            {
                var requirements = ActiveSkillNode.Prerequisites.ToArray();
                var firstUnmet = Array.FindIndex(requirements, item => PermanentRules.Rank(SkillProfile.Upgrades, item.Key) < item.Value);
                GoToSkillPrerequisite(firstUnmet < 0 ? 0 : firstUnmet);
            }
            else if (keys.rKey.wasPressedThisFrame) BeginSkillRefund();
            else if (enter)
            {
                switch (skillFocus)
                {
                    case SkillFocus.Trees: ToggleSkillTrees(); break;
                    case SkillFocus.Prerequisite: GoToSkillPrerequisite(skillPrerequisite); break;
                    case SkillFocus.Refund: BeginSkillRefund(); break;
                    case SkillFocus.Close: ClosePermanentSkillMenu(); break;
                    default: BeginSkillPurchase(); break;
                }
            }
            else if (up || down || left || right)
            {
                var direction = up || left ? -1 : 1;
                if (skillFocus == SkillFocus.Trees) ChangePermanentSkillTree(direction);
                else if (skillFocus == SkillFocus.Prerequisite)
                {
                    skillPrerequisite = Mathf.Clamp(skillPrerequisite + direction, 0, Mathf.Max(0, ActiveSkillNode.Prerequisites.Count - 1));
                    skillRevealDetails = true;
                }
                else
                {
                    skillFocus = SkillFocus.Nodes;
                    SelectPermanentSkillRow(Mathf.Clamp(skillRow + direction, 0, skillTrees[skillTree].Length - 1));
                }
            }
            return true;
        }

        private void ClosePermanentSkillMenu()
        {
            if (commandFrame == Time.frameCount || !Host.ClosePermanentSkills()) return;
            commandFrame = Time.frameCount;
            skillEntryFocused = true;
            inputReleased = false;
            touchStarted = false;
        }

        private void ToggleSkillTrees()
        {
            showSkillTrees = !showSkillTrees;
            skillFocus = showSkillTrees ? SkillFocus.Trees : SkillFocus.Nodes;
            skillRevealRow = true;
        }

        private void ChangePermanentSkillTree(int direction) =>
            SelectPermanentSkillTree((skillTree + direction + skillTrees.Length) % skillTrees.Length);

        private void SelectPermanentSkillTree(int tree)
        {
            if (skillTree == tree) return;
            skillTree = tree;
            SelectPermanentSkillRow(0);
        }

        private void SelectPermanentSkillRow(int row)
        {
            skillRow = row;
            skillPrerequisite = 0;
            skillDetailScroll = Vector2.zero;
            skillRevealRow = true;
            skillRevealDetails = false;
        }

        private void SelectPermanentSkill(PermanentSkillNode node)
        {
            var tree = Array.FindIndex(skillTrees, entries => entries[0].TreeId == node.TreeId);
            SelectPermanentSkillTree(tree);
            SelectPermanentSkillRow(Array.IndexOf(skillTrees[tree], node));
        }

        private void GoToSkillPrerequisite(int index)
        {
            if (index >= ActiveSkillNode.Prerequisites.Count) return;
            var id = ActiveSkillNode.Prerequisites.ElementAt(index).Key;
            if (skillHistory.Count == 4) skillHistory.RemoveAt(0);
            skillHistory.Add(ActiveSkillNode);
            SelectPermanentSkill(PermanentSkillCatalog.Current.Find(id));
            skillFocus = SkillFocus.Nodes;
        }

        private void ReturnFromPrerequisite()
        {
            if (skillHistory.Count == 0) return;
            var last = skillHistory.Count - 1;
            SelectPermanentSkill(skillHistory[last]);
            skillHistory.RemoveAt(last);
            skillFocus = SkillFocus.Nodes;
        }

        private PermanentSkillAvailability SkillAvailability(PermanentSkillNode node) =>
            node.Availability(SkillProfile.Upgrades, SkillProfile.Gold, SkillProfile.HighestSettledRound.GetValueOrDefault(),
                SkillProfile.UnlockedWeapons, SkillProfile.EvolvedWeapons);

        private bool CanBuyPermanentSkill(PermanentSkillNode node) =>
            OffersPermanentSkillPurchase(node.Id) && SkillAvailability(node) == PermanentSkillAvailability.Available;

        private void BeginSkillPurchase()
        {
            var node = ActiveSkillNode;
            if (commandFrame == Time.frameCount || !CanBuyPermanentSkill(node)) return;
            commandFrame = Time.frameCount;
            var rank = PermanentRules.Rank(SkillProfile.Upgrades, node.Id);
            var revision = Host.State.Revision;
            skillConfirmation = PermanentSkillCommand.Purchase(PermanentSkillCommand.NewOperationId(revision),
                revision, node.Id, rank, node.Prices[rank]);
            skillConfirmedBalance = SkillProfile.Gold;
            skillConfirmFocused = false;
        }

        private void BeginSkillRefund()
        {
            if (commandFrame == Time.frameCount || SkillProfile.Upgrades.Count == 0) return;
            commandFrame = Time.frameCount;
            var quote = PermanentSkillCommand.QuoteRefund(SkillProfile);
            if (quote.Gold > long.MaxValue - SkillProfile.Gold)
            {
                skillResultSuccess = false;
                skillResult = "환불 후 골드가 저장 가능한 한도를 넘습니다.";
                return;
            }
            var revision = Host.State.Revision;
            skillConfirmation = PermanentSkillCommand.RefundAll(PermanentSkillCommand.NewOperationId(revision),
                revision, quote.Gold, quote.Digest);
            skillConfirmedBalance = SkillProfile.Gold;
            skillConfirmFocused = false;
        }

        private void ConfirmSkillWrite()
        {
            if (commandFrame == Time.frameCount || skillConfirmation == null) return;
            commandFrame = Time.frameCount;
            var command = skillConfirmation;
            skillConfirmation = null;
            FinishSkillWrite(Host.SubmitPermanentSkill(command));
        }

        private void RetrySkillWrite()
        {
            if (commandFrame == Time.frameCount) return;
            commandFrame = Time.frameCount;
            FinishSkillWrite(Host.RetryPermanentSkill());
        }

        private void FinishSkillWrite(bool success)
        {
            if (Host.HasPermanentSkillRetry) return;
            skillResultSuccess = success;
            skillResult = success ? "저장했습니다. 변경은 다음 도전부터 적용됩니다." : Host.PermanentSkillError;
        }

        private void DrawPermanentSkills(float width, float height)
        {
            EnsurePermanentSkillCatalog();
            var modal = skillConfirmation != null || Host.HasPermanentSkillRetry || skillResult != null;
            var enabled = GUI.enabled;
            GUI.enabled = enabled && !modal && !Host.Suspended;
            DrawPermanentSkillBrowser(width, height);
            GUI.enabled = enabled && !Host.Suspended;
            if (modal) DrawPermanentSkillModal(width, height);
            GUI.enabled = enabled;
        }

        private void DrawPermanentSkillBrowser(float width, float height)
        {
            var size = new Vector2(width, height);
            if (skillBrowserSize != size)
            {
                skillBrowserSize = size;
                skillRevealRow = true;
                skillRevealDetails = true;
            }
            var tree = skillTrees[skillTree];
            AdamHud.Fill(new Rect(0, 0, width, 96), AdamHud.Ink1);
            AdamHud.Rule(art, new Rect(0, 95, width, 2), true);
            GUI.Label(new Rect(12, 4, width * .56f - 14, 32), "영구 스킬 · " + (skillEntryScreen ?? SkillEntryScreen),
                HudText(20, AdamHud.Primary));
            GUI.Label(new Rect(width * .56f, 4, width * .44f - 12, 32), $"골드 {SkillProfile.Gold:N0}",
                HudText(20, AdamHud.Gold, false, TextAnchor.MiddleRight));

            var activeTree = skillFocus == SkillFocus.Trees;
            if (SkillButton(new Rect(12, 44, 50, 42), "Q ‹", false, "previous")) ChangePermanentSkillTree(-1);
            var treeTitle = tree[0].TreeId.StartsWith("common.", StringComparison.Ordinal) ? "공통" : "무기";
            var treeButtonWidth = Mathf.Min(240, width - 202);
            if (SkillButton(new Rect(68, 44, treeButtonWidth, 42), $"{treeTitle} · {tree[0].TreeName} (T)", activeTree, "trees")) ToggleSkillTrees();
            if (SkillButton(new Rect(74 + treeButtonWidth, 44, 50, 42), "› E", false, "next")) ChangePermanentSkillTree(1);
            if (SkillButton(new Rect(width - 70, 44, 58, 42), "닫기", skillFocus == SkillFocus.Close, "close"))
            {
                ClosePermanentSkillMenu();
                return;
            }

            var footerY = height - 60;
            var listWidth = Mathf.Clamp(Mathf.Floor(width * .38f), 132, 380);
            var treeListOpen = showSkillTrees;
            var list = new Rect(12, 106, treeListOpen ? width - 24 : listWidth, footerY - 113);
            DrawSkillList(list);
            if (!treeListOpen)
                DrawPermanentSkillDetails(new Rect(list.xMax + 12, list.y, width - list.xMax - 24, list.height));

            AdamHud.Fill(new Rect(0, footerY, width, 60), AdamHud.Ink1);
            AdamHud.Rule(art, new Rect(0, footerY, width, 1), false);
            if (SkillButton(new Rect(12, footerY + 8, 140, 44), "전체 환불 (R)", skillFocus == SkillFocus.Refund,
                "refund", SkillProfile.Upgrades.Count > 0 && !showSkillTrees)) BeginSkillRefund();
            if (skillHistory.Count > 0 && !showSkillTrees &&
                SkillButton(new Rect(160, footerY + 8, 98, 44), "이전 노드", false, "back-node")) ReturnFromPrerequisite();
            var guide = new GUIStyle(HudText(16, AdamHud.Secondary, false, TextAnchor.MiddleRight)) { wordWrap = true };
            var guideX = skillHistory.Count > 0 && !treeListOpen ? 266 : 160;
            GUI.Label(new Rect(guideX, footerY + 6, width - guideX - 12, 48),
                width < 500 ? "다음 도전부터 적용" : "다음 도전부터 적용\n↑↓ 선택 · Tab 이동 · Esc 닫기", guide);
        }

        private void DrawSkillList(Rect rect)
        {
            const int rowHeight = 44;
            var tree = skillTrees[skillTree];
            var count = showSkillTrees ? skillTrees.Length : tree.Length;
            var selected = showSkillTrees ? skillTree : skillRow;
            if (skillRevealRow)
            {
                if (skillListScroll.y > selected * rowHeight) skillListScroll.y = selected * rowHeight;
                if (skillListScroll.y + rect.height < (selected + 1) * rowHeight)
                    skillListScroll.y = (selected + 1) * rowHeight - rect.height;
                skillListScroll.y = Mathf.Clamp(skillListScroll.y, 0, Mathf.Max(0, count * rowHeight - rect.height));
                skillRevealRow = false;
            }
            var rowWidth = rect.width - 18;
            skillListScroll = GUI.BeginScrollView(rect, skillListScroll, new Rect(0, 0, rowWidth, count * rowHeight));
            for (var index = 0; index < count; index++)
            {
                var row = new Rect(0, index * rowHeight, rowWidth, rowHeight - 4);
                if (showSkillTrees)
                {
                    var node = skillTrees[index][0];
                    var common = node.TreeId.StartsWith("common.", StringComparison.Ordinal);
                    var group = common ? "공통" : "무기";
                    var locked = !common && !SkillProfile.UnlockedWeapons.Contains(node.TreeId) ? " · 미해금" : "";
                    if (SkillButton(row, group + " · " + node.TreeName + locked, selected == index, "tree-" + node.TreeId,
                        icon: common ? null : AdamHud.Icon(node.TreeId)))
                    {
                        SelectPermanentSkillTree(index);
                        ToggleSkillTrees();
                        break;
                    }
                }
                else
                {
                    var node = tree[index];
                    var rank = PermanentRules.Rank(SkillProfile.Upgrades, node.Id);
                    var availability = SkillAvailability(node);
                    var nodeState = SkillNodeArtwork(node, availability, rank);
                    if (SkillButton(row, $"{node.Name}  {rank}/{node.Prices.Count}", selected == index, "node-" + node.Id,
                        icon: SkillStateIcon(availability, nodeState), nodeState: nodeState))
                    {
                        SelectPermanentSkillRow(index);
                        skillFocus = SkillFocus.Nodes;
                    }
                }
            }
            GUI.EndScrollView();
        }

        private void DrawPermanentSkillDetails(Rect rect)
        {
            var node = ActiveSkillNode;
            var rank = PermanentRules.Rank(SkillProfile.Upgrades, node.Id);
            var availability = SkillAvailability(node);
            var contentWidth = rect.width - 18;
            var body = new GUIStyle(HudText(16, AdamHud.Secondary)) { wordWrap = true, alignment = TextAnchor.UpperLeft };
            var applicability = node.TreeId.StartsWith("common.", StringComparison.Ordinal) ? "공통" :
                node.TreeName + (node.RequiredEvolution == null ? " 장착 중" : " 진화형 장착 중");
            var detail = $"{applicability}\n{node.EffectDescription}";
            var detailHeight = Mathf.Ceil(body.CalcHeight(new GUIContent(detail), contentWidth - 16)) + 10;
            var needs = SkillNeeds(node, availability);
            var needsHeight = Mathf.Ceil(body.CalcHeight(new GUIContent(needs), contentWidth - 16)) + 10;
            var requirementsY = 94 + detailHeight + needsHeight;
            var purchaseY = requirementsY + 44 * node.Prerequisites.Count + 4;
            var contentHeight = purchaseY + 52;
            if (skillRevealDetails && (skillFocus == SkillFocus.Prerequisite || skillFocus == SkillFocus.Purchase))
            {
                var focusedY = skillFocus == SkillFocus.Prerequisite ? requirementsY + 44 * skillPrerequisite : purchaseY;
                if (skillDetailScroll.y > focusedY) skillDetailScroll.y = focusedY;
                if (skillDetailScroll.y + rect.height < focusedY + 44) skillDetailScroll.y = focusedY + 44 - rect.height;
                skillDetailScroll.y = Mathf.Clamp(skillDetailScroll.y, 0, Mathf.Max(0, contentHeight - rect.height));
            }
            skillRevealDetails = false;
            AdamHud.Fill(rect, AdamHud.Ink1);
            skillDetailScroll = GUI.BeginScrollView(rect, skillDetailScroll, new Rect(0, 0, contentWidth, contentHeight));
            var lineWidth = contentWidth - 16;
            var heading = node.BranchName.Length == 0 ? node.TreeName : node.BranchName;
            GUI.Label(new Rect(8, 4, lineWidth, 24), heading, HudText(16, AdamHud.Lapis));
            GUI.Label(new Rect(8, 28, lineWidth, 30), node.Name, HudText(20, AdamHud.Primary));
            var rankLabel = rank < node.Prices.Count ?
                $"랭크 {rank}/{node.Prices.Count} · 다음 {node.Prices[rank]:N0} 골드" : $"랭크 {rank}/{node.Prices.Count} · 최대";
            GUI.Label(new Rect(8, 60, lineWidth, 30), rankLabel, HudText(16, AdamHud.Gold));
            float y = 94;
            GUI.Label(new Rect(8, y, lineWidth, detailHeight), detail, body);
            y += detailHeight;
            GUI.Label(new Rect(8, y, lineWidth, needsHeight), needs, body);
            y += needsHeight;
            var requirementIndex = 0;
            foreach (var requirement in node.Prerequisites)
            {
                var required = PermanentSkillCatalog.Current.Find(requirement.Key);
                var focusedRequirement = skillFocus == SkillFocus.Prerequisite && skillPrerequisite == requirementIndex;
                if (SkillButton(new Rect(8, y, lineWidth, 40),
                    $"↗ {required.Name} {requirement.Value}랭크 (G)", focusedRequirement, "prerequisite-" + required.Id,
                    icon: "ico_ui_link_static_16"))
                    GoToSkillPrerequisite(requirementIndex);
                requirementIndex++;
                y += 44;
            }
            if (SkillButton(new Rect(8, y + 4, lineWidth, 44), "구매 확인 (Enter)",
                skillFocus == SkillFocus.Purchase, "buy", CanBuyPermanentSkill(node))) BeginSkillPurchase();
            GUI.EndScrollView();
        }

        private static string SkillNeeds(PermanentSkillNode node, PermanentSkillAvailability availability)
        {
            var rules = new List<string>();
            if (!OffersPermanentSkillPurchase(node.Id)) rules.Add("효과 연결 대기 · 구매 불가");
            else if (availability != PermanentSkillAvailability.Available) rules.Add(availability switch
            {
                PermanentSkillAvailability.MaximumRank => "최대 랭크 도달",
                PermanentSkillAvailability.InsufficientGold => "골드 부족",
                PermanentSkillAvailability.RequiresSkill => "선행 스킬 필요",
                PermanentSkillAvailability.RequiresSettledRound => "진행 완료 필요",
                PermanentSkillAvailability.RequiresWeapon => "무기 해금 필요",
                PermanentSkillAvailability.RequiresEvolution => "진화 업적 필요",
                _ => "",
            });
            else rules.Add("구매 가능");

            if (node.RequiredSettledRound > 0) rules.Add($"R{node.RequiredSettledRound:00} 정산");
            if (node.RequiredWeapon != null) rules.Add($"{node.TreeName} 해금");
            if (node.RequiredEvolution != null) rules.Add($"{node.TreeName} 진화 업적");
            return string.Join(" · ", rules);
        }

        private void DrawPermanentSkillModal(float width, float height)
        {
            AdamHud.Fill(new Rect(0, 0, width, height), new Color(0, 0, 0, .75f));
            var panelWidth = Mathf.Min(530, width - 24);
            var panel = new Rect((width - panelWidth) / 2, (height - 256) / 2, panelWidth, 256);
            FillPanel(panel);
            AdamHud.Panel(art, panel);
            var inner = new Rect(panel.x + 16, panel.y + 12, panel.width - 32, 34);
            var textArea = new Rect(inner.x, inner.yMax + 6, inner.width, 126);
            var body = new GUIStyle(HudText(16, AdamHud.Secondary)) { wordWrap = true, alignment = TextAnchor.UpperLeft };
            var actionsY = panel.yMax - 56;
            if (skillConfirmation != null)
            {
                var command = skillConfirmation;
                var purchase = command.NodeId != null;
                var heading = purchase ? "구매 확인" : "전체 환불 확인";
                var subject = purchase ? $"{PermanentSkillCatalog.Current.Find(command.NodeId).Name} {command.DisplayedRank + 1}랭크" : "구매한 랭크 전체 초기화";
                var amount = purchase ? "결제액" : "실제 환불액 · 수수료 없음";
                GUI.Label(inner, heading, title);
                GUI.Label(textArea, $"{subject}\n{amount} {command.DisplayedGold:N0} 골드\n현재 보유 골드 {skillConfirmedBalance:N0}\n다음 도전부터 적용됩니다.", body);
                var actionWidth = (inner.width - 8) / 2;
                if (SkillButton(new Rect(inner.x, actionsY, actionWidth, 44), "취소 (Esc)", !skillConfirmFocused, "cancel")) skillConfirmation = null;
                if (SkillButton(new Rect(inner.x + actionWidth + 8, actionsY, actionWidth, 44),
                    purchase ? "구매 확정" : "환불 확정", skillConfirmFocused, "confirm")) ConfirmSkillWrite();
            }
            else
            {
                var retry = Host.HasPermanentSkillRetry;
                var heading = retry ? "저장 결과 확인 필요" : skillResultSuccess ? "저장 완료" : "다시 선택";
                GUI.Label(inner, heading, title);
                GUI.Label(textArea, retry ? $"{Host.PermanentSkillError}\n마지막 확정 잔액 {SkillProfile.Gold:N0} 골드" : skillResult, body);
                if (SkillButton(new Rect(inner.x, actionsY, inner.width, 44),
                    retry ? "같은 요청 확인 (Enter)" : "확인 (Enter)", true, "notice"))
                {
                    if (retry) RetrySkillWrite();
                    else skillResult = null;
                }
            }
        }

        private static int SkillNodeArtwork(PermanentSkillNode node, PermanentSkillAvailability availability, int rank)
        {
            if (!OffersPermanentSkillPurchase(node.Id)) return 3;
            return availability switch
            {
                PermanentSkillAvailability.MaximumRank => 2,
                PermanentSkillAvailability.Available => rank == 0 ? 0 : 1,
                PermanentSkillAvailability.RequiresWeapon or PermanentSkillAvailability.RequiresEvolution => 4,
                _ => 3,
            };
        }

        private static string SkillStateIcon(PermanentSkillAvailability availability, int state)
        {
            if (state == 2) return "ico_ui_check_static_16";
            if (availability == PermanentSkillAvailability.RequiresSettledRound) return "ico_ui_round_static_16";
            if (state >= 3) return "ico_ui_lock_static_16";
            return null;
        }

        private static void DrawSkillNodeFocus(Rect rect)
        {
            const int inset = 4, leg = 8, thickness = 2;
            var left = rect.x + inset;
            var right = rect.xMax - inset;
            var top = rect.y + inset;
            var bottom = rect.yMax - inset;
            var color = AdamHud.Lapis;
            AdamHud.Fill(new Rect(left, top, leg, thickness), color);
            AdamHud.Fill(new Rect(left, top, thickness, leg), color);
            AdamHud.Fill(new Rect(right - leg, top, leg, thickness), color);
            AdamHud.Fill(new Rect(right - thickness, top, thickness, leg), color);
            AdamHud.Fill(new Rect(left, bottom - thickness, leg, thickness), color);
            AdamHud.Fill(new Rect(left, bottom - leg, thickness, leg), color);
            AdamHud.Fill(new Rect(right - leg, bottom - thickness, leg, thickness), color);
            AdamHud.Fill(new Rect(right - thickness, bottom - leg, thickness, leg), color);
        }

        private bool SkillButton(Rect rect, string text, bool focus, string id, bool available = true,
            string icon = null, int nodeState = -1)
        {
            var enabled = GUI.enabled;
            GUI.enabled = enabled && available;
            GUI.SetNextControlName("adam:skills:" + id);
            var mouse = Event.current.isMouse;
            var hover = rect.Contains(Event.current.mousePosition);
            var clicked = GUI.Button(rect, GUIContent.none, clearButton) && mouse;
            const string nodePanel = "ui_skill-node_9slice_16";
            if (nodeState >= 0 && AdamVisualQualityArt.TryGetClip(nodePanel, out _))
                AdamHud.Panel(art, rect, nodePanel, nodeState);
            else
                AdamHud.Panel(art, rect, "ui_button_9slice_16", AdamHud.SurfaceState(GUI.enabled, focus, hover, hover && GUIUtility.hotControl != 0));
            var leftPadding = 5;
            if (!string.IsNullOrEmpty(icon) && AdamVisualQualityArt.TryGetClip(icon, out var clip))
            {
                var side = clip.width == 32 ? 32 : 16;
                AdamHud.Image(art, icon, new Rect(rect.x + 8, rect.center.y - side / 2, side, side));
                leftPadding = side + 12;
            }
            GUI.Label(new Rect(rect.x + leftPadding, rect.y, rect.width - leftPadding - 5, rect.height), text,
                HudText(16, GUI.enabled ? AdamHud.Primary : AdamHud.Muted, false, TextAnchor.MiddleCenter));
            if (focus && enabled)
            {
                if (nodeState >= 0) DrawSkillNodeFocus(rect);
                else DrawFocusBracket(rect);
            }
            GUI.enabled = enabled;
            return clicked;
        }
    }
}
