using System;
using System.Collections;
using System.IO;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

public sealed class AdamRoundPlayTests
{
    string directory;
    GameObject owner;
    AdamGame game;
    Keyboard keys;
    InputSettings.BackgroundBehavior background;
    InputSettings.EditorInputBehaviorInPlayMode editorInput;
    bool run;
    [UnitySetUp] public IEnumerator Setup()
    {
        directory=Path.Combine(Path.GetTempPath(),"adam-round-play-"+Guid.NewGuid().ToString("N"));
        owner=new GameObject("R02 round regression");owner.SetActive(false);game=owner.AddComponent<AdamGame>();game.AutoInitialize=false;
        background=InputSystem.settings.backgroundBehavior;editorInput=InputSystem.settings.editorInputBehaviorInPlayMode;run=Application.runInBackground;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;Application.runInBackground=true;
        keys=InputSystem.AddDevice<Keyboard>();owner.SetActive(true);
        game.Initialize(new AdamHost(AdamCatalog.Load(),new SaveStore(Path.Combine(directory,"save.json"))));yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        InputSystem.RemoveDevice(keys);InputSystem.settings.backgroundBehavior=background;InputSystem.settings.editorInputBehaviorInPlayMode=editorInput;Application.runInBackground=run;
        UnityEngine.Object.Destroy(owner);yield return null;if(Directory.Exists(directory))Directory.Delete(directory,true);
    }
    void Keys(params Key[] input)=>InputSystem.QueueStateEvent(keys,new KeyboardState(input));
    void CompleteR01()
    {
        var host=game.Host;host.Start("sling");host.Session.Advance(180);host.SetPaused(true);
        while(host.Session.PendingChoices.Count>0)Assert.IsTrue(host.Choose(host.Session.PendingChoices[0]));
        Assert.IsTrue(host.Checkpoint(),host.Error);Assert.IsTrue(host.AwaitingR02);
    }
    [UnityTest] public IEnumerator SmallWindowContinueClickEntersSavedR02()=>ContinueClick(660,440);
    [UnityTest] public IEnumerator LargeWindowContinueClickEntersSavedR02()=>ContinueClick(1100,720);
    IEnumerator ContinueClick(int width,int height)
    {
        CompleteR01();game.enabled=false;game.Initialize(game.Host); // Deterministic presentation, real IMGUI pointer dispatch below.
        var window=ScriptableObject.CreateInstance<PauseModalPointerWindow>();
        try
        {
            window.Game=game;window.minSize=window.maxSize=new Vector2(width,height);window.position=new Rect(40,40,width,height);window.ShowUtility();window.Repaint();yield return null;
            game.SetLifecycle(true,false);var host=game.Host;var id=host.State.Run.RunId;var gold=host.State.Profile.Gold;var old=host.World;
            var point=new Vector2(width/2,height/2+48);
            window.SendEvent(new Event{type=EventType.MouseDown,mousePosition=point,button=0,clickCount=1});
            window.SendEvent(new Event{type=EventType.MouseUp,mousePosition=point,button=0,clickCount=1});
            Assert.AreEqual(2,host.Session.RoundNumber,"visible Continue must enter R02, not offer a new R01 challenge");
            Assert.AreNotSame(old,host.World);Assert.Zero(host.World.SimulationTime);Assert.AreEqual(id,host.State.Run.RunId);Assert.AreEqual(gold,host.State.Profile.Gold);
            var restored=new AdamHost(AdamCatalog.Load(),new SaveStore(Path.Combine(directory,"save.json")));Assert.AreEqual(2,restored.Session.RoundNumber);Assert.Zero(restored.World.SimulationTime);
            game.UpdatePresentation(0);Assert.AreEqual(0,game.ArtWorld.ActiveEffects);Assert.Zero(game.ImpactAudio.ActiveVoices);Assert.Zero(game.ImpactParticles.ParticleCount);
            // Exercise actual b-monster geometry in the product view, with no a-monster fallback.
            for(var i=0;i<10;i++)host.Tick(.1);game.UpdatePresentation(.1f);window.Repaint();yield return null;
            Assert.AreEqual("창세기 / 가인 · 3분 시제품",AdamRoundPresentation.Title(host));
        }
        finally{window.Close();}
    }
    [UnityTest] public IEnumerator HeldMovementAcrossEntryRequiresReleaseAndFreshPress()
    {
        game.Host.Start("sling");Keys();yield return null;yield return null;Keys(Key.D);yield return new WaitForSecondsRealtime(.1f);Assert.Greater(game.Host.World.PlayerPosition.X,0);
        CompleteExisting();var old=game.Host.World;game.Host.SetPaused(false);Assert.AreNotSame(old,game.Host.World);
        for(var i=0;i<4;i++)yield return null;
        Assert.AreEqual(System.Numerics.Vector2.Zero,game.Host.World.PlayerPosition,"old round held movement must be released");
        Keys();yield return null;Keys(Key.D);yield return new WaitForSecondsRealtime(.1f);Assert.Greater(game.Host.World.PlayerPosition.X,0);
        game.SetLifecycle(false,true);var time=game.Host.World.SimulationTime;yield return null;game.SetLifecycle(true,false);yield return null;
        Assert.IsTrue(game.Host.Paused);Assert.AreEqual(time,game.Host.World.SimulationTime);Assert.AreEqual(Vector2.zero,game.Host.Movement);Keys();yield return null;
    }
    void CompleteExisting()
    {
        var host=game.Host;host.Session.Advance(180);host.SetPaused(true);
        while(host.Session.PendingChoices.Count>0)Assert.IsTrue(host.Choose(host.Session.PendingChoices[0]));Assert.IsTrue(host.Checkpoint());
    }
    [UnityTest] public IEnumerator R02ContactDeathBlocksDigitsUntilClipThenStartsOnlyR01()
    {
        CompleteR01();var host=game.Host;host.SetPaused(false);
        for(var i=0;i<30 && host.World.Capture().Enemies.Count==0;i++)host.Tick(.1);
        Assert.IsTrue(host.Checkpoint());var store=new SaveStore(Path.Combine(directory,"save.json"));var state=store.Load();
        state.Run.Health=1;state.Run.World.Enemies[0].X=state.Run.World.PlayerX+.1f;state.Run.World.Enemies[0].Y=state.Run.World.PlayerY;
        store.Commit(state,state.Revision,Guid.NewGuid().ToString("N"));Assert.IsTrue(host.Reload());game.ArtWorld.Update(host,0);host.SetPaused(false);host.Tick(1d/60);game.UpdatePresentation(0);
        Assert.IsTrue(game.DeathPresentationVisible);var dead=host.Session;var gold=host.State.Profile.Gold;host.SetPaused(true);game.SetLifecycle(true,false);
        Keys(Key.Digit2);yield return null;Assert.AreSame(dead,host.Session);Assert.IsNull(host.State.Run);
        Keys();yield return null;host.SetPaused(false);yield return new WaitForSecondsRealtime(.85f);Assert.IsFalse(game.DeathPresentationVisible);
        Keys(Key.Digit2);yield return null;Assert.AreEqual(1,host.Session.RoundNumber);Assert.IsTrue(host.Session.Inventory.Weapons.ContainsKey("jawbone"));Assert.AreEqual(gold,host.State.Profile.Gold);Keys();yield return null;
    }
    [UnityTest] public IEnumerator EntryClearsLiveR01ImpactParticlesAudioAndReactionCaches()
    {
        var host=game.Host;host.Start("jawbone");
        for(var i=0;i<20 && host.World.Capture().Enemies.Count<2;i++)host.Tick(.1);
        host.Checkpoint();var store=new SaveStore(Path.Combine(directory,"save.json"));var state=store.Load();
        foreach(var e in state.Run.World.Enemies){e.X=1;e.Y=0;}state.Run.World.Weapons.Clear();
        store.Commit(state,state.Revision,Guid.NewGuid().ToString("N"));Assert.IsTrue(host.Reload());host.SetPaused(false);
        game.UpdatePresentation(0);host.Tick(1d/60);game.UpdatePresentation(0);
        Assert.Greater(game.ArtWorld.ActiveEffects,0);Assert.Greater(game.ImpactParticles.ParticleCount,0);Assert.Greater(game.ImpactAudio.PlayedCount,0);
        var generation=game.ArtWorld.Generation;CompleteExisting();host.SetPaused(false);game.UpdatePresentation(0);
        Assert.AreEqual(2,host.Session.RoundNumber);Assert.Greater(game.ArtWorld.Generation,generation);Assert.Zero(game.ArtWorld.ActiveEffects);Assert.Zero(game.ImpactParticles.ParticleCount);Assert.Zero(game.ImpactAudio.ActiveVoices);
        Assert.IsFalse(game.DeathPresentationVisible);Assert.AreEqual("idle",game.ArtWorld.PlayerMotion);yield return null;
    }
    [UnityTest] public IEnumerator R02FeedbackOnAndOffPreserveExactRunProfileAndWorld()=>FeedbackEquivalence("sling");
    [UnityTest] public IEnumerator R02BibleFeedbackDoesNotChangeCombat()=>FeedbackEquivalence("bible");
    [UnityTest] public IEnumerator R02JawboneFeedbackDoesNotChangeCombat()=>FeedbackEquivalence("jawbone");
    IEnumerator FeedbackEquivalence(string weapon)
    {
        CompleteR01();var host=game.Host;host.SetPaused(false);
        for(var i=0;i<20 && host.World.Capture().Enemies.Count<2;i++)host.Tick(.1);
        host.Checkpoint();var path=Path.Combine(directory,"save.json");var store=new SaveStore(path);var state=store.Load();
        foreach(var e in state.Run.World.Enemies){var angle=weapon=="bible"?(state.Run.World.Time+1d/60)*2.5:0;e.X=(float)Math.Cos(angle)*1.4f;e.Y=(float)Math.Sin(angle)*1.4f;}
        state.Run.World.Weapons.Clear();
        state.Run.World.Projectiles.Clear();
        state.Run.Weapons.Clear();
        state.Run.Weapons.Add(weapon,1);
        store.Commit(state,state.Revision,Guid.NewGuid().ToString("N"));
        game.enabled=false;string expected=null;
        foreach(var enabled in new[]{false,true})
        {
            var copy=Path.Combine(directory,enabled?"on.json":"off.json");File.Copy(path,copy);
            var replay=new AdamHost(AdamCatalog.Load(),new SaveStore(copy));game.Initialize(replay);game.ArtWorld.FeedbackEnabled=enabled;game.ImpactParticles.Enabled=enabled;game.ImpactAudio.Muted=!enabled;replay.SetPaused(false);
            for(var i=0;i<240 && replay.Session.IsActive && replay.Session.PendingChoices.Count==0;i++){replay.Tick(1d/60);game.UpdatePresentation(1f/60);}
            Assert.Greater(replay.World.Hits,0,"comparison must include actual R02 combat events");Assert.IsTrue(replay.Checkpoint(),replay.Error);
            var actual=Json(replay.State.Run)+Json(replay.State.Profile)+Json(replay.World.Capture());
            if(expected==null)expected=actual;else Assert.AreEqual(expected,actual,"presentation must not change battle or saved progression");
        }
        yield return null;
    }
    AdamHost CompletedGenesisForEntry(string suffix)
    {
        var catalog=AdamCatalog.Load();var state=GameSnapshot.New();var session=new AdamSession(state,catalog.ForRound(1),"sling");
        for(var round=1;round<=9;round++)
        {
            session.Advance(180);
            if(session.Stage=="boss_intro")
            {
                new BattleWorld(session,catalog.Speeds,7).Advance(2,System.Numerics.Vector2.Zero);
                session.Hit(session.BossRules.BossActorId,session.BossRules.MaxHealth);
                if(session.Stage=="boss_reward")Assert.IsTrue(session.ChooseBossReward(session.Boss.Reward.RewardId,session.BossChoices[0].Id));
                while(session.PendingChoices.Count>0)Assert.IsTrue(session.Choose(session.PendingChoices[0]));
                Assert.IsTrue(session.CompleteBossSettlement());
            }
            while(session.PendingChoices.Count>0)Assert.IsTrue(session.Choose(session.PendingChoices[0]));
            if(session.Stage=="round_settlement")Assert.IsTrue(session.CompleteNormalSettlement());
            if(round<9)session=session.StartNextRound(catalog.ForRound(round+1));
        }
        session.Capture();state.Run.World=null;var path=Path.Combine(directory,suffix+".json");new SaveStore(path).Commit(state,0,"technical-Genesis-completion");return new AdamHost(catalog,new SaveStore(path));
    }
    // Technical progression fixture; actual keyboard commands drive the saved round transitions.
    static void SettleForEntry(AdamHost host)
    {
        host.Session.Advance(180);
        if(host.Session.Stage=="boss_intro")
        {
            host.Session.Advance(2);
            host.Session.Hit(host.Session.BossRules.BossActorId,host.Session.BossRules.MaxHealth);
            Assert.IsTrue(host.Checkpoint(),host.Error);
            if(host.Session.Stage=="boss_reward")
                Assert.IsTrue(host.ChooseBossReward(host.Session.Boss.Reward.RewardId,host.Session.BossChoices[0].Id));
            while(host.Session.PendingChoices.Count>0)Assert.IsTrue(host.Choose(host.Session.PendingChoices[0]));
            Assert.IsTrue(host.CompleteBossSettlement(),host.Error);
        }
        else Assert.IsTrue(host.Checkpoint(),host.Error);
    }

    [UnityTest] public IEnumerator CompletedBookRejectsNumberKeysAndCancelsWithoutChangingTheSave()
    {
        var host=CompletedGenesisForEntry("cancel-new");
        var old=host.State.Run.RunId;var path=Path.Combine(directory,"cancel-new.json");
        var bytes=File.ReadAllBytes(path);
        game.Initialize(host);game.SetLifecycle(true,false);
        yield return null;

        Keys(Key.Digit1);yield return null;
        Assert.AreEqual(old,host.State.Run.RunId);
        Keys();yield return null;
        Keys(Key.DownArrow);yield return null;
        Keys(Key.Enter);yield return null;
        Assert.IsTrue(game.NewChallengeOpen);
        Keys(Key.Digit2);yield return null;
        Assert.AreEqual(old,host.State.Run.RunId);
        Keys(Key.Escape);yield return null;
        Assert.IsFalse(game.NewChallengeOpen);
        CollectionAssert.AreEqual(bytes,File.ReadAllBytes(path));

        Keys(Key.Enter);yield return null;
        Assert.AreEqual(10,host.Session.RoundNumber);
        Assert.AreEqual(old,host.State.Run.RunId);
    }

    [UnityTest] public IEnumerator CompletedBookNewChallengeRequiresSeparateConfirmationAndDoesNotRepeat()
    {
        var host=CompletedGenesisForEntry("confirm-new");
        var old=host.State.Run.RunId;
        game.Initialize(host);game.SetLifecycle(true,false);
        yield return null;

        game.OpenNewChallenge();
        game.ConfirmNewChallenge(1);
        Assert.AreEqual(old,host.State.Run.RunId,"the modal-opening frame cannot confirm");
        yield return null;
        game.SelectSlot(1);
        Assert.AreEqual(old,host.State.Run.RunId,"the old growth shortcut cannot confirm a new challenge");
        Keys(Key.DownArrow);yield return null;
        Keys(Key.Enter);yield return null;
        Assert.IsFalse(game.NewChallengeOpen);
        Assert.AreEqual(1,host.Session.RoundNumber);
        Assert.AreNotEqual(old,host.State.Run.RunId);
        Assert.IsTrue(host.Session.Inventory.Weapons.ContainsKey("bible"));
        var committed=host.State.Run.RunId;
        game.ConfirmNewChallenge(2);game.SelectSlot(2);
        yield return null;
        Assert.AreEqual(committed,host.State.Run.RunId);
    }

    [UnityTest] public IEnumerator EnterKeyRespectsSuspensionAndChapterBoundariesThroughR180()
    {
        var host=CompletedGenesisForEntry("enter");
        var run=host.State.Run.RunId;
        game.Initialize(host);game.SetLifecycle(true,false);
        yield return null;
        Assert.AreEqual(9,host.Session.RoundNumber);
        Assert.IsTrue(host.Paused);
        host.SetSuspended(true);
        var revision=host.State.Revision;
        Keys(Key.Enter);yield return null;
        Assert.AreEqual(9,host.Session.RoundNumber);
        Assert.AreEqual(revision,host.State.Revision);
        Keys();yield return null;
        game.SetLifecycle(true,false);
        Keys(Key.Enter);yield return null;
        Assert.AreEqual(10,host.Session.RoundNumber);
        Assert.AreEqual(run,host.State.Run.RunId);
        Assert.AreEqual(revision+1,host.State.Revision);
        for(var round=10;round<=180;round++)
        {
            Assert.AreEqual(round,host.Session.RoundNumber);
            SettleForEntry(host);
            Assert.IsTrue(host.Reload(),host.Error);
            Assert.AreEqual(round==15,host.CanEnterLeviticus);
            Assert.AreEqual(round==21,host.CanEnterNumbers);
            Assert.AreEqual(round==30,host.CanEnterDeuteronomy);
            Assert.AreEqual(round==36,host.CanEnterJoshua);
            Assert.AreEqual(round==42,host.CanEnterJudges);
            Assert.AreEqual(round==51,host.CanEnterRuth);
            Assert.AreEqual(round==54,host.CanEnterFirstSamuel);
            Assert.AreEqual(round is 60 or 66 or 72 or 81 or 84 or 90 or 93 or 96 or 99 or 102 or 111 or 114 or 117 or 120 or 129 or 138 or 141 or 150 or 159,host.CanEnterLaterBook);
            Assert.AreEqual(round==180,host.ContentComplete);
            if(round==99)
            {
                Assert.AreEqual("에스더 완료 · 진행 저장됨",AdamRoundPresentation.CompletionHeading(host));
                Assert.AreEqual(100,AdamRoundPresentation.NextChapter(host));
                Assert.AreEqual("욥기 시작 (Enter)",AdamRoundPresentation.StartCard(100));
            }
            var laterEntry=round switch
            {
                102=>("욥기","시편"),111=>("시편","잠언"),114=>("잠언","전도서"),117=>("전도서","아가"),
                120=>("아가","이사야"),129=>("이사야","예레미야"),138=>("예레미야","예레미야애가"),
                141=>("예레미야애가","에스겔"),150=>("에스겔","다니엘"),159=>("다니엘","열두 예언서"),
                _=>(null,null)
            };
            if(laterEntry.Item1!=null)
            {
                Assert.AreEqual(laterEntry.Item1+" 완료 · 진행 저장됨",AdamRoundPresentation.CompletionHeading(host));
                Assert.AreEqual(round+1,AdamRoundPresentation.NextChapter(host));
                Assert.AreEqual(laterEntry.Item2+" 시작 (Enter)",AdamRoundPresentation.StartCard(round+1));
            }
            Assert.AreEqual(!(round is 15 or 21 or 30 or 36 or 42 or 51 or 54 or 60 or 66 or 72 or 81 or 84 or 90 or 93 or 96 or 99 or 102 or 111 or 114 or 117 or 120 or 129 or 138 or 141 or 150 or 159 or 180),host.AwaitingNextRound);
            revision=host.State.Revision;
            Keys();yield return null;
            Assert.AreEqual(round,host.Session.RoundNumber);
            Assert.AreEqual(revision,host.State.Revision);
            Keys(Key.Enter);yield return null;
            Assert.AreEqual(Math.Min(180,round+1),host.Session.RoundNumber);
            Assert.AreEqual(revision+(round==180?0:1),host.State.Revision);
            Assert.AreEqual(run,host.State.Run.RunId);
        }
        Assert.IsTrue(host.ContentComplete);
        Assert.AreEqual(181,host.State.Run.RoundNumber);
        Assert.AreEqual(run,host.State.OldTestamentClear.RunId);
        Assert.Greater(host.State.OldTestamentClear.CompletedAtUnixMilliseconds,0);
        Assert.AreEqual("구약 완주 · 진행 저장됨",AdamRoundPresentation.CompletionHeading(host));
        StringAssert.Contains("말라기 최종 보스 ‘삼키는 자’ 처치",AdamRoundPresentation.CompletionDetail(host));
        Assert.AreEqual(0,AdamRoundPresentation.NextChapter(host));
        var completedAt=host.State.OldTestamentClear.CompletedAtUnixMilliseconds;
        host.Start("sling");
        Assert.IsTrue(host.Reload(),host.Error);
        Assert.AreEqual(1,host.State.Run.RoundNumber);
        Assert.AreNotEqual(run,host.State.Run.RunId);
        Assert.AreEqual(run,host.State.OldTestamentClear.RunId);
        Assert.AreEqual(completedAt,host.State.OldTestamentClear.CompletedAtUnixMilliseconds);
    }
    [UnityTest]public IEnumerator R11ContinueClickEntersSavedNextRoundWithoutRepaying180()
    {
        var host=CompletedGenesisForEntry("r11-click");Assert.IsTrue(host.TryEnterExodus());host.Session.Advance(180);Assert.IsTrue(host.Checkpoint());Assert.IsTrue(host.Reload());Assert.IsTrue(host.AwaitingR11);var id=host.State.Run.RunId;var gold=host.State.Profile.Gold;var revision=host.State.Revision;
        game.enabled=false;game.Initialize(host);var window=ScriptableObject.CreateInstance<PauseModalPointerWindow>();
        try
        {
            window.Game=game;window.minSize=window.maxSize=new Vector2(660,440);window.position=new Rect(40,40,660,440);window.ShowUtility();window.Repaint();yield return null;game.SetLifecycle(true,false);
            var point=new Vector2(330,268);window.SendEvent(new Event{type=EventType.MouseDown,mousePosition=point,button=0,clickCount=1});window.SendEvent(new Event{type=EventType.MouseUp,mousePosition=point,button=0,clickCount=1});
            Assert.AreEqual(11,host.Session.RoundNumber);Assert.AreEqual(id,host.State.Run.RunId);Assert.AreEqual(gold,host.State.Profile.Gold);Assert.AreEqual(revision+1,host.State.Revision);Assert.Zero(host.World.SimulationTime);Assert.IsTrue(host.Reload());Assert.AreEqual(11,host.Session.RoundNumber);
        }
        finally{window.Close();}
    }
    [UnityTest]public IEnumerator R12ContinueClickEntersSavedNextRoundWithoutRepaying200()
    {
        var host=CompletedGenesisForEntry("r12-click");Assert.IsTrue(host.TryEnterExodus());host.Session.Advance(180);Assert.IsTrue(host.Checkpoint());host.SetPaused(false);Assert.AreEqual(11,host.Session.RoundNumber);host.Session.Advance(180);Assert.IsTrue(host.Checkpoint());Assert.IsTrue(host.Reload());Assert.IsTrue(host.AwaitingR12);var id=host.State.Run.RunId;var gold=host.State.Profile.Gold;var revision=host.State.Revision;
        game.enabled=false;game.Initialize(host);var window=ScriptableObject.CreateInstance<PauseModalPointerWindow>();
        try
        {
            window.Game=game;window.minSize=window.maxSize=new Vector2(660,440);window.position=new Rect(40,40,660,440);window.ShowUtility();window.Repaint();yield return null;game.SetLifecycle(true,false);
            var point=new Vector2(330,268);window.SendEvent(new Event{type=EventType.MouseDown,mousePosition=point,button=0,clickCount=1});window.SendEvent(new Event{type=EventType.MouseUp,mousePosition=point,button=0,clickCount=1});Assert.AreEqual(12,host.Session.RoundNumber);Assert.AreEqual(id,host.State.Run.RunId);Assert.AreEqual(gold,host.State.Profile.Gold);Assert.AreEqual(revision+1,host.State.Revision);Assert.Zero(host.World.SimulationTime);Assert.IsTrue(host.Reload());Assert.AreEqual(12,host.Session.RoundNumber);
        }
        finally{window.Close();}
    }
    [UnityTest]public IEnumerator R13ContinueClickPreservesR12ReceiptWithoutRepayingBossOrCompletionGold()
    {
        var host=CompletedGenesisForEntry("r13-click");Assert.IsTrue(host.TryEnterExodus());foreach(var round in new[]{10,11}){Assert.AreEqual(round,host.Session.RoundNumber);host.Session.Advance(180);Assert.IsTrue(host.Checkpoint());host.SetPaused(false);}Assert.AreEqual(12,host.Session.RoundNumber);host.Session.Advance(180);host.Session.Advance(2);host.Session.Hit(host.Session.BossRules.BossActorId,host.Session.BossRules.MaxHealth);Assert.IsTrue(host.Checkpoint());if(host.Session.Stage=="boss_reward")Assert.IsTrue(host.ChooseBossReward(host.Session.Boss.Reward.RewardId,host.Session.BossChoices[0].Id));while(host.Session.PendingChoices.Count>0)Assert.IsTrue(host.Choose(host.Session.PendingChoices[0]));Assert.IsTrue(host.CompleteBossSettlement());Assert.IsTrue(host.Reload());Assert.IsTrue(host.AwaitingR13);var id=host.State.Run.RunId;var receipt=Json(host.State.Run.Boss);var gold=host.State.Profile.Gold;var revision=host.State.Revision;
        game.enabled=false;game.Initialize(host);var window=ScriptableObject.CreateInstance<PauseModalPointerWindow>();
        try
        {
            window.Game=game;window.minSize=window.maxSize=new Vector2(660,440);window.position=new Rect(40,40,660,440);window.ShowUtility();window.Repaint();yield return null;game.SetLifecycle(true,false);var point=new Vector2(330,268);window.SendEvent(new Event{type=EventType.MouseDown,mousePosition=point,button=0,clickCount=1});window.SendEvent(new Event{type=EventType.MouseUp,mousePosition=point,button=0,clickCount=1});Assert.AreEqual(13,host.Session.RoundNumber);Assert.AreEqual(id,host.State.Run.RunId);Assert.AreEqual(gold,host.State.Profile.Gold);Assert.AreEqual(revision+1,host.State.Revision);Assert.AreEqual(receipt,Json(host.State.Run.CompletedFourthBoss));Assert.IsNull(host.State.Run.Boss);Assert.Zero(host.World.SimulationTime);Assert.IsTrue(host.Reload());Assert.AreEqual(13,host.Session.RoundNumber);
        }
        finally{window.Close();}
    }
    [UnityTest]public IEnumerator R14ContinueClickPreservesFourReceiptsWithoutRepaying240()
    {
        var host=CompletedGenesisForEntry("r14-click");Assert.IsTrue(host.TryEnterExodus());foreach(var round in new[]{10,11}){Assert.AreEqual(round,host.Session.RoundNumber);host.Session.Advance(180);Assert.IsTrue(host.Checkpoint());host.SetPaused(false);}Assert.AreEqual(12,host.Session.RoundNumber);host.Session.Advance(180);host.Session.Advance(2);host.Session.Hit(host.Session.BossRules.BossActorId,host.Session.BossRules.MaxHealth);Assert.IsTrue(host.Checkpoint());if(host.Session.Stage=="boss_reward")Assert.IsTrue(host.ChooseBossReward(host.Session.Boss.Reward.RewardId,host.Session.BossChoices[0].Id));while(host.Session.PendingChoices.Count>0)Assert.IsTrue(host.Choose(host.Session.PendingChoices[0]));Assert.IsTrue(host.CompleteBossSettlement());host.SetPaused(false);Assert.AreEqual(13,host.Session.RoundNumber);host.Session.Advance(180);Assert.IsTrue(host.Checkpoint());Assert.IsTrue(host.Reload());Assert.IsTrue(host.AwaitingR14);var id=host.State.Run.RunId;var receipt=Json(host.State.Run.CompletedFourthBoss);var gold=host.State.Profile.Gold;var revision=host.State.Revision;
        game.enabled=false;game.Initialize(host);var window=ScriptableObject.CreateInstance<PauseModalPointerWindow>();
        try
        {
            window.Game=game;window.minSize=window.maxSize=new Vector2(660,440);window.position=new Rect(40,40,660,440);window.ShowUtility();window.Repaint();yield return null;game.SetLifecycle(true,false);var point=new Vector2(330,268);window.SendEvent(new Event{type=EventType.MouseDown,mousePosition=point,button=0,clickCount=1});window.SendEvent(new Event{type=EventType.MouseUp,mousePosition=point,button=0,clickCount=1});Assert.AreEqual(14,host.Session.RoundNumber);Assert.AreEqual(id,host.State.Run.RunId);Assert.AreEqual(gold,host.State.Profile.Gold);Assert.AreEqual(revision+1,host.State.Revision);Assert.AreEqual(receipt,Json(host.State.Run.CompletedFourthBoss));Assert.IsNull(host.State.Run.Boss);Assert.Zero(host.World.SimulationTime);Assert.IsTrue(host.Reload());Assert.AreEqual(14,host.Session.RoundNumber);
        }
        finally{window.Close();}
    }
    [UnityTest]public IEnumerator R10DigitAndEnterInOneFrameContinueWithoutReset()
    {
        var host=CompletedGenesisForEntry("same-frame");var old=host.State.Run.RunId;var profile=Json(SaveStore.PreviewPermanentProfile(host.State));game.Initialize(host);game.SetLifecycle(true,false);yield return null;
        InputSystem.QueueStateEvent(keys,new KeyboardState(Key.Digit1,Key.Enter));yield return null;
        Assert.AreEqual(10,host.Session.RoundNumber);Assert.AreEqual(old,host.State.Run.RunId);Assert.AreEqual(SaveFormat.CurrentVersion,host.State.SchemaVersion);Assert.AreEqual(profile,Json(host.State.Profile));Assert.IsFalse(host.CanEnterExodus);
    }
    static string Json(object value)
    {using(var stream=new System.IO.MemoryStream()){new System.Runtime.Serialization.Json.DataContractJsonSerializer(value.GetType()).WriteObject(stream,value);return System.Text.Encoding.UTF8.GetString(stream.ToArray());}}
}
