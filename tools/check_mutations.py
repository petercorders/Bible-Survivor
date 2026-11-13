#!/usr/bin/env python3
"""Replay concrete regressions in isolated source copies; never mutate the working tree."""
from pathlib import Path
import json
import os
import shutil
import subprocess
import tempfile
import sys
from datetime import datetime, timezone

ROOT = Path(__file__).resolve().parents[1]
CASES = [
    ('xp20_40_bonus_removed', 'Growth.cs', '(level == 20 || level == 40 ? 1 : 0)', '0'),
    ('stone_cap_regressed', 'Growth.cs', 'id == "stones" ? 2 : 5', 'id == "stones" ? 5 : 5'),
    ('spawn_backlog_early_clear', 'Waves.cs', '(cursor == schedule.Count && active.Count == 0)', '((expired || cursor == schedule.Count) && active.Count == 0)'),
    ('enemy_nan_restored', 'Waves.cs', 'double.IsNaN(enemy.Health) || double.IsInfinity(enemy.Health) || ', ''),
    ('weapon_kill_credit_lost', 'AdamSession.cs', 'if(damage>=enemy.Health) Kill(id); else Arena!.Hit(id,damage);', 'Arena!.Hit(id,damage);'),
    ('completion_resume_refused', 'AdamSession.cs', 'if (definition.Number < 1 || definition.Number > 2 ||', 'if (run.Stage != "normal" || definition.Number < 1 || definition.Number > 2 ||'),
    ('stale_revision_overwrite', 'SaveState.cs', 'if (revision != expectedRevision)', 'if (false)'),
    ('projectile_double_hit', 'BattleWorld.cs', 'if(projectile.HitIds.Contains(enemy.Key))continue;', ''),
    ('battle_null_hit_entry', 'BattleWorld.cs', 'w.OrbitHits.Any(x=>x==null)||', ''),
    ('stale_world_clock_saved', 'SaveState.cs', 'Math.Abs(Run.World.Time-Run.Wave.Elapsed)>1e-7', 'false'),
    ('wave_null_entry_saved', 'SaveState.cs', '(Run.Wave.Active==null || Run.Wave.Active.Any(x=>x==null))', '(Run.Wave.Active==null)'),
    ('round_entry_ignores_pending_choices', 'AdamSession.cs', 'run.PendingLevels!=0 ||', 'false ||'),
    ('round_entry_keeps_old_iframe', 'AdamSession.cs', 'run.LastHitTime=-1;', '/* old round hit clock retained */'),
    ('round_completion_uses_r01_unlock', 'AdamSession.cs', 'state.Profile.UnlockedWeapons.Add(definition.UnlockWeaponId);', 'state.Profile.UnlockedWeapons.Add("wilderness_voice");'),
    ('round_completion_uses_r01_gold', 'AdamSession.cs', 'checked(state.Profile.Gold + definition.CompletionGold)', 'checked(state.Profile.Gold + 25)'),
    ('round_catalog_change_accepted', 'AdamSession.cs', 'run.RoundFingerprint != definition.Fingerprint', 'false'),
    ('round_entry_stale_session_active', 'AdamSession.cs', 'run.RoundNumber == definition.Number + (run.Stage=="between_rounds" ? 1 : 0)', 'true'),
    ('round_entry_retains_completed_world', 'AdamSession.cs', 'run.World=null;run.Drops.Clear();run.LastHitTime=-1;', 'run.Drops.Clear();run.LastHitTime=-1;'),
]

def main():
    sdk = os.environ.get('BIBLE_SURVIVOR_DOTNET') or shutil.which('dotnet') or str(Path.home()/'.local/share/bible-survivor/dotnet/dotnet')
    if not Path(sdk).is_file():
        print('BLOCKED: .NET SDK missing', file=sys.stderr)
        return 3
    stamp=datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S%fZ')
    output = ROOT/'evidence'/('r02-mutations-'+stamp)
    output.mkdir(parents=True, exist_ok=False)
    results = []
    with tempfile.TemporaryDirectory(prefix='bible-survivor-mutations-') as directory:
        temp = Path(directory)
        for name in ('src','tests','content','tools/BibleSurvivor.Sim'):
            shutil.copytree(ROOT/name,temp/name,ignore=shutil.ignore_patterns('bin','obj'))
        shutil.copy2(ROOT/'global.json',temp/'global.json')
        env = dict(os.environ, DOTNET_CLI_TELEMETRY_OPTOUT='1',DOTNET_GENERATE_ASPNET_CERTIFICATE='false',DOTNET_NOLOGO='1',DOTNET_CLI_HOME=str(ROOT/'.local/dotnet-home'))
        command=[sdk,'run','--project',str(temp/'tests/BibleSurvivor.Core.Tests')]
        baseline=subprocess.run(command,cwd=temp,text=True,capture_output=True,env=env)
        if baseline.returncode or 'passed, 0 failed' not in baseline.stdout:
            print('FAIL: mutation baseline did not pass',baseline.stdout,baseline.stderr,file=sys.stderr)
            return 1
        for name,file,old,new in CASES:
            target=temp/'src/BibleSurvivor.Core'/file
            original=target.read_text()
            if original.count(old)!=1: raise RuntimeError(f'{name}: original regression location changed')
            target.write_text(original.replace(old,new))
            try:
                result=subprocess.run(command,cwd=temp,text=True,capture_output=True,env=env)
                killed=result.returncode!=0 and 'FAIL ' in result.stderr and 'error CS' not in result.stdout+result.stderr
                results.append({'regression':name,'test_failed':killed,'exit_code':result.returncode})
                (output/(name+'.log')).write_text(result.stdout+result.stderr)
                print(('PASS detected ' if killed else 'FAIL undetected ')+name)
            finally:
                target.write_text(original)
    (output/'summary.json').write_text(json.dumps({'baseline':'PASS','regressions':results},indent=2))
    return 0 if all(r['test_failed'] for r in results) else 1

if __name__=='__main__': sys.exit(main())
