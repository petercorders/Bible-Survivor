#!/usr/bin/env python3
"""Record token counters only, never prompts, from one explicitly selected Codex session."""
import argparse
from datetime import datetime, timezone
import fcntl
import json
import os
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
COUNTERS = ('input_tokens', 'cached_input_tokens', 'cache_write_input_tokens',
            'output_tokens', 'reasoning_output_tokens', 'total_tokens')


def collect(path):
    model = None
    latest = None
    with path.open() as source:
        for line in source:
            try:
                event = json.loads(line)
            except json.JSONDecodeError:
                # The running host may not have finished its final JSONL line.
                continue
            payload = event.get('payload') or {}
            if event.get('type') == 'turn_context':
                model = payload.get('model', model)
            if event.get('type') != 'event_msg' or payload.get('type') != 'token_count':
                continue
            info = payload.get('info') or {}
            usage = info.get('total_token_usage')
            if not isinstance(usage, dict):
                continue
            values = {key: usage.get(key) for key in COUNTERS}
            if any(value is not None and (type(value) is not int or value < 0)
                   for value in values.values()):
                raise ValueError('Invalid token counter')
            latest = {'event_timestamp': event.get('timestamp'), 'model_at_event': model,
                      'cumulative_tokens': values,
                      'last_usage_includes_reasoning': info.get('last_usage_includes_reasoning')}
    if latest is None:
        raise ValueError('No token telemetry; do not replace missing usage with zero')
    return latest


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--phase', required=True)
    parser.add_argument('--checkpoint', choices=('start', 'end', 'progress'), required=True)
    parser.add_argument('--session-id', default=os.environ.get('CODEX_THREAD_ID'))
    parser.add_argument('--session-file', type=Path, help='Explicit session or isolated fixture only')
    parser.add_argument('--ledger', type=Path, default=ROOT / 'evidence/model-usage/checkpoints.jsonl')
    parser.add_argument('--result', choices=('PENDING', 'PASS', 'FAIL', 'NOT_RUN'), default='PENDING')
    args = parser.parse_args()
    if not args.session_id:
        parser.error('An explicit session ID is required')
    if not re.fullmatch(r'[A-Za-z0-9_-]+', args.session_id):
        parser.error('Session ID must not contain paths or glob patterns')
    if args.session_file:
        source = args.session_file
    else:
        # Search filenames for this session only; never read unrelated conversations.
        matches = list((Path.home() / '.codex/sessions').rglob('*' + args.session_id + '*.jsonl'))
        if len(matches) != 1:
            raise ValueError('Expected exactly one log for the selected session')
        source = matches[0]
    observed = collect(source)
    record = {'recorded_at': datetime.now(timezone.utc).isoformat(),
              'session_id': args.session_id, 'phase': args.phase,
              'checkpoint': args.checkpoint, 'result': args.result, **observed,
              'actual_billed_cost': None, 'api_equivalent_cost': None,
              'cost_reason': 'No billing data or per-request verified price/tier attribution',
              'scope': 'One session interval; excludes unregistered workers; not project lifetime cost',
              'delta_tokens': None, 'delta_status': 'BASELINE'}
    args.ledger.parent.mkdir(parents=True, exist_ok=True)
    with args.ledger.open('a+') as ledger:
        fcntl.flock(ledger, fcntl.LOCK_EX)
        ledger.seek(0)
        previous = None
        for line in ledger:
            candidate = json.loads(line)
            if candidate.get('session_id') == args.session_id:
                previous = candidate
        if previous:
            delta = {}
            for key, current in observed['cumulative_tokens'].items():
                old = previous['cumulative_tokens'].get(key)
                delta[key] = None if current is None or old is None else current - old
            if any(value is not None and value < 0 for value in delta.values()):
                record['delta_status'] = 'COUNTER_RESET_OR_INCOMPATIBLE'
            else:
                record['delta_tokens'] = delta
                record['delta_status'] = 'MEASURED_SESSION_INTERVAL'
                record['previous_phase'] = previous['phase']
                record['previous_event_timestamp'] = previous['event_timestamp']
        ledger.seek(0, 2)
        ledger.write(json.dumps(record, ensure_ascii=False) + '\n')
        ledger.flush()
    # Reasoning may already be part of output: never sum these counters into a new total.
    print(json.dumps(record, ensure_ascii=False))
    return 0


if __name__ == '__main__':
    try:
        sys.exit(main())
    except (OSError, ValueError, KeyError) as error:
        print('USAGE FAIL: ' + str(error), file=sys.stderr)
        sys.exit(1)
