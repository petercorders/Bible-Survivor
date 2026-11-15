#!/usr/bin/env python3
"""Narrow R03 regressions in disposable copies; compilation failures never count."""
from pathlib import Path
from datetime import datetime, timezone
import json
import shutil
import subprocess
import tempfile
import sys

ROOT = Path(__file__).resolve().parents[1]
CASES = [
    ('normal_uses_old_finish', 'AdamSession.cs', 'if(definition.Number==3)', 'if(definition.Number<0)', 'normal timeout'),
    ('support_threshold_not_retained', 'AdamSession.cs', 'run.Boss.ThresholdUsed=true;run.Boss.Summoned=6;', 'run.Boss.ThresholdUsed=false;run.Boss.Summoned=6;', 'threshold summons six'),
    ('boss_added_to_general_remainder', 'AdamSession.cs', 'private void BossDefeated()\n        {', 'private void BossDefeated()\n        {\n            CreditKill();', 'kill gold excludes boss remainder'),
    ('world_clock_reset_on_restore', 'BattleWorld.cs', 'world.SimulationTime=snapshot.Time;', 'world.SimulationTime=0;', 'telegraph midpoint World and Run'),
]

def main():
    output=ROOT/'evidence'/('r03-boss-mutations-'+datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S%fZ'))
    output.mkdir(parents=True)
    records=[]
    with tempfile.TemporaryDirectory(prefix='bible-boss-mutants-') as folder:
        temp=Path(folder)
        for name in ('src','tests','content','tools/BibleSurvivor.Sim'):
            shutil.copytree(ROOT/name,temp/name,ignore=shutil.ignore_patterns('bin','obj'))
        shutil.copy2(ROOT/'global.json',temp/'global.json')
        sdk=ROOT/'tools/dotnet.sh'
        command=['bash',str(sdk),'run','--project',str(temp/'tests/BibleSurvivor.Core.Tests'),'--','--boss-only']
        def run(name):
            result=subprocess.run(command,cwd=temp,text=True,capture_output=True)
            (output/(name+'.log')).write_text(result.stdout+result.stderr)
            return result
        baseline=run('baseline')
        if baseline.returncode:
            print('FAIL boss mutation baseline');return 1
        for name,file,before,after,required in CASES:
            path=temp/'src/BibleSurvivor.Core'/file;original=path.read_text()
            if original.count(before)!=1:raise RuntimeError('Mutation source changed: '+name)
            try:
                path.write_text(original.replace(before,after));result=run(name)
                detected=result.returncode==1 and 'FAIL R03 '+required in result.stderr and 'error CS' not in result.stdout+result.stderr
                records.append(dict(name=name,exit_code=result.returncode,detected=detected,assertion=required))
                print(('PASS detected ' if detected else 'FAIL undetected ')+name)
            finally:path.write_text(original)
        restored=run('restored')
    passed=all(r['detected'] for r in records) and restored.returncode==0
    (output/'summary.json').write_text(json.dumps(dict(status='PASS' if passed else 'FAIL',baseline_exit=baseline.returncode,
        restored_exit=restored.returncode,command=command,regressions=records,unity='NOT_RUN',mobile='NOT_RUN'),indent=2))
    print(str(output/'summary.json'));return 0 if passed else 1

if __name__=='__main__':sys.exit(main())
