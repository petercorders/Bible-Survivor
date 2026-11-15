#!/usr/bin/env python3
"""R03 reward/evolution regressions; disposable copies and assertion failures only."""
from pathlib import Path
from datetime import datetime, timezone
import json
import shutil
import subprocess
import tempfile
import sys
ROOT=Path(__file__).resolve().parents[1]
CASES=[
    ('evolution_guarantee_removed',[('BossRewards.cs','if(evolutions.Count>0)','if(evolutions.Count<0)')],'R03 production reward guarantees evolution'),
    ('completion_receipt_ignored',[('AdamSession.cs','||run.Stage!="boss_settlement"',''),('AdamSession.cs','||run.Boss.CompletionApplied)return false;',')return false;')],'R03 settlement waits for levels'),
    ('volley_focus_lost_on_restore',[('BattleWorld.cs','foreach(var clock in snapshot.Weapons)world.weapons.Add(clock.WeaponId,clock.Copy());','foreach(var clock in snapshot.Weapons){var restoredClock=clock.Copy();restoredClock.FocusTargetId=0;world.weapons.Add(clock.WeaponId,restoredClock);}')],'R03 sling fivefold preserves six shots'),
]
def main():
    output=ROOT/'evidence'/('r03-reward-mutations-'+datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S%fZ'));output.mkdir(parents=True)
    records=[]
    with tempfile.TemporaryDirectory(prefix='bible-reward-mutants-') as directory:
        temp=Path(directory)
        for name in ('src','tests','content','tools/BibleSurvivor.Sim'):
            shutil.copytree(ROOT/name,temp/name,ignore=shutil.ignore_patterns('bin','obj'))
        shutil.copy2(ROOT/'global.json',temp/'global.json')
        command=['bash',str(ROOT/'tools/dotnet.sh'),'run','--project',str(temp/'tests/BibleSurvivor.Core.Tests'),'--','--reward-only']
        def run(name):
            result=subprocess.run(command,cwd=temp,text=True,capture_output=True);(output/(name+'.log')).write_text(result.stdout+result.stderr);return result
        baseline=run('baseline')
        if baseline.returncode: print('FAIL mutation baseline');return 1
        for name,edits,required in CASES:
            originals={}
            try:
                for file,before,after in edits:
                    path=temp/'src/BibleSurvivor.Core'/file;original=path.read_text();originals.setdefault(path,original)
                    if original.count(before)!=1:raise RuntimeError('Changed mutation anchor: '+name)
                    path.write_text(original.replace(before,after))
                result=run(name);detected=result.returncode==1 and 'FAIL '+required in result.stderr and 'error CS' not in result.stdout+result.stderr
                records.append(dict(name=name,exit_code=result.returncode,detected=detected,assertion=required));print(('PASS detected ' if detected else 'FAIL undetected ')+name)
            finally:
                for path,original in originals.items():path.write_text(original)
        restored=run('restored')
    passed=all(r['detected'] for r in records) and restored.returncode==0
    (output/'summary.json').write_text(json.dumps(dict(status='PASS' if passed else 'FAIL',command=command,baseline_exit=baseline.returncode,regressions=records,restored_exit=restored.returncode,unity='NOT_RUN'),indent=2)+'\n')
    print(output/'summary.json');return 0 if passed else 1
if __name__=='__main__':sys.exit(main())
