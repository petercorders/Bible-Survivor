# Art03 + outline-font independent Player UI review

2026-11-14. Reviewer `unity_ui_review`, independent of implementation and of the separate art/code reviewers. Input: `docs/progress/unity-art.md`, current built `UnityGame/Builds/Adam.app`, Art03/outline-font integration. Only this report and ignored evidence were written; no source/build/Editor changes. Initial result: FAIL (death digit bypass). Follow-up: **ART-UI-01 CLOSED for the corrected command boundary**, based on the source/actual InputSystem regression evidence below; precise native GUI clip timing and post-clip interaction remain separately limited. Previous UI-01/02 remain resolved in their exercised scope.

## ART-UI-01 — MEDIUM: digit input bypasses the death presentation

Use the producer's `gallery/death-contact.json` checkpoint (normal Core-created enemies with QA-arranged positions, HP1/time150). Bring the Player forward, resume with Return, then immediately press1. The second screenshot shows a new Bible challenge at time0/level1 while the saved run ID has changed. This matches the separate code review's finding that `AdamGame.SelectSlot` can call `Host.Start` while the death buttons are hidden: the visual0.75-second guard does not guard digit commands. The reviewer reported this immediately to root before changing or closing the Player.

Evidence: `artdeathbefore.json`, `artdeathframe.json/png` (HP1/time150 just after Return), `artdeathkey1.json/png` (new Bible challenge time0), `after-death-key1.json` (new run ID). The Return/key1 commands were issued consecutively in the same exec call. GUI provider metadata is synthetic/unverified; success is based on actual rendered state and saved run identity, not the tool's action return. This review does not claim a frame-accurate0.75-second timing measurement or independent verification of all six death frames; the captured transition and unguarded command path reproduce the bypass. The producer's frame/mutation tests are separate evidence.

Requested correction: guard every start-new-challenge route during death presentation, including numeric selection, and retain legitimate modal/keyboard selection afterward. Verify through actual InputSystem/Player behavior, not only hidden button geometry. Retest same death-contact fixture and confirm immediate input cannot replace the terminal state, death remains visible, and post-animation selection remains usable.

## Actual independent results

| Path | Status and evidence |
| --- | --- |
|660×440 outline-font cards | PASS: all three Korean names, category,0→1/5 and actual effects readable without clipping. Pixel strokes stay crisp; no HUD-title fragments outside modal. `art660modal.png`. |
|1100×720 cards | PASS: same decision content readable; panel opaque; no scene bleed into text. `art1100modal.png`. |
|Keyboard growth choice | PASS: at660, digit1 commits only crown1, clears choices; `660-after-choice.json`. |
|Mouse growth choice | PASS: at1100, card2 click commits only underwear1, clears choices; `1100-after-choice.json`. |
|Pause/resume controls | PASS:660 modal button is visible, actual foreground click resumes; Escape/Return/Escape restore/resume/re-pause.1100 Return/Escape also works. Actual committed world time43.2167→44.2833 at660. `art660resume`, `art660pause`, `art660enter`, `art660repaused`, `art1100enter`, `art1100paused`. |
|Save + restart | PASS: normal close/relaunch without replacing the checkpoint preserves entire Run and Profile equality at43.5667s; visible paused world reconstructed. `restart-check.json` has RunSame/ProfileSame true; `before-restart.json`, `after-restart.json`, `art1100restart.png`. |
|Three enemy shapes and animated world | PASS for visual distinction in staged gallery: low snake, winged moth/demon silhouette, thorned cluster are distinct in form and position. Actual foreground screenshots change pose/location as150.6→150.8s. Player layers, terrain/XP and rotated stone are visible; `artgalleryforeground.png`, `artgalleryframe.png`. This is a QA fixture, not natural spawn/balance evidence. |
|660 battle readability | PASS, narrow: actual post-resume field has distinct player, snake enemies, XP diamonds, terrain and HUD. World integer scale1 is visibly smaller than1100 scale2; no full combat balance judgement. `art660resume.png`. |
|Death presentation | FAIL keyboard guard as above. Full six-frame0.75s visual timing NOT_RUN in this reviewer capture; no claim that the developer's tests substitute for independent animation viewing. |
|Full manual3-minute round / sustained held movement | NOT_RUN; Orca press-key lacks hold duration/down-up controls. No repeated attempts to simulate held hardware input. |
|Mobile/touch device, interruption durability, performance, grayscale/accessibility measurement | NOT_RUN; no desktop-only substitution. |

The exact Korean cards were reopened from a naturally obtained prior level2 checkpoint; this session did not independently replay progression to level2. The gallery/death fixtures use real Core IDs/session state with arranged positions/health and are labeled throughout. Temporary progression/fixtures were restored afterward.

## Evidence and restoration

Ignored root: `evidence/unity-art-ui-review-20261114/`. Five actual Player exec sessions—modal660, modal1100, restart1100, gallery1100, death1100—all exited0 after normal window closure. Their logs and corresponding `art*.json/png` are preserved. Native close actions sometimes return focus errors after the process has already exited; only actual exec exit was used to confirm termination, without repeated close retries.

Both legacy/current save directories' primary/backup/lock existence and bytes were preserved before testing. After all five Players exited, existing originals were restored byte-for-byte; originally absent files were moved to ignored evidence. `restoration.json` is true for all six states. Build/source ownership returned to root for the death-input fix. Independent follow-up must use the rebuilt artifact; this report's evidence describes the pre-fix build.

## Death digit follow-up — ART-UI-01 CLOSED with explicit evidence boundaries

The corrected `AdamGame.SelectSlot` now returns on `DeathPresentationVisible` before commandFrame, Host.Start or Host.Choose. Both visible pointer controls and Update's digit route enter this boundary. The reviewer read the new actual InputSystem tests: normal Core contact death, terminal Session identity/Run null/gold/time invariance on individual digit1/2/3 and simultaneous digits, lifecycle interruption and held-key boundary, then fresh release/press starting the intended weapon afterward. These are enabled-component InputSystem updates, not calls to the guarded method alone. Only the presentation clock is frozen during the rejected-input assertions to avoid a timing-dependent test.

The reviewer parsed the recorded actual Unity XML, independently of the worker's prose: `evidence/unity-death-digit-20261114/fixed-PlayMode/results.xml` is13 passed/0 failed/0 skipped; removing this exact common guard gives `mutant-results.xml`0 passed/3 failed with `Digit input must not bypass the visible death clip` Session identity failures; `restored-results.xml` is3 passed/0 failed. The reviewer did not rerun these Editor suites while owning the Player. All231 source/build files in the producer's final SHA manifest match the files present during this follow-up (`source-build-match.json`:231/231).

### Actual follow-up Player observations and limits

-660×440, same Core contact-death fixture: after foreground resume, the actual screenshot shows the unobscured character death pose and `사망 · 이번 도전이 끝났습니다`. Immediate digit1 no longer produced a new Bible run in this attempt; saved Run remainsnull. However native focus loss also opened the pause modal, so this observation alone does **not** isolate the0.75-second guard or prove frame-accurate rejection. Subsequent resume/post-clip digit attempts were not conclusively delivered; no native post-clip success is claimed.
-1100 follow-up is **NOT_RUN/contaminated**: a prior restore-enabled close observation had reopened the same bundle without arguments through LaunchServices. The unexpected660 window was selected by bundle ID while the explicitly launched1100 process also existed. The shared save then correctly stopped on stale revision; this is not treated as an art/gameplay regression or as death-boundary evidence. Exact-pid observations and the stale-revision screenshot are preserved, not counted as PASS.
-No repeated GUI timing loop was run. The rejection/hold/post-clip boundary closure rests on the corrected common guard plus actual engine InputSystem assertions and the exact original-structure mutation, while the visible death pose is an independent Player observation. Full six-frame native viewing/precise0.75-second timing, fresh native post-clip selection and additional card input on this final rebuild remain NOT_RUN here. The preceding review's actual two-resolution cards/pause/save results are retained as preceding-build evidence; only the death command guard and corresponding tests changed for this fix.

This combination closes the original command bypass without claiming unrestricted native input certification. Future supported-device/full-play QA remains open as before. No new HIGH/MEDIUM was identified in the corrected source/recorded regression evidence.

### Follow-up cleanup and GC evidence

Ignored root: `evidence/unity-art-ui-death-followup-20261114/`. `death660-return.png` shows the visible death pose; `death660-digit.png`/`660-after-immediate-digit.json` show the interrupted/null-run state. Raw `death1100-*` preserve the contaminated attempt and stale-save stop. `source-build-match.json`, `player-stop.json`, `restoration.json` record artifact/cleanup facts.

The explicit660 process exited0. Restore-enabled close returned window-not-found around03:35:01 and reopened an argument-free Player PID55600/PPID1, one second before the explicit1100 process PID55626. Root confirmed no other worker/user-requested launch was in progress; the project retrospective already documents the same restore-close relaunch behavior. Bundle selectors and shared-save concurrency therefore invalidate the1100 timing attempt. The reviewer preserved all six shared save states before cleanup. The explicit1100 process ignored background native close attempts and was reclaimed with SIGTERM, managed exec exit143 (not0). The QA-reopened PID55600 disappeared after restore-free Cmd+Q; the tool returned an AX permission error and no exit code was recoverable, so its exit isnull. No further restore/relaunch retry was made.

`pgrep` confirms no Player process remains. All six original save-file existence/bytes were restored (`restoration.json` alltrue); generated files were moved into ignored evidence. No source/build/Editor change was made. The concrete GC improvement is a PID-bound probe with restore-free shutdown and a strict single-Player assertion before fixture writes, rather than more GUI timing retries. Broader full manual round, sustained hardware hold, mobile and performance gates remain NOT_RUN.
