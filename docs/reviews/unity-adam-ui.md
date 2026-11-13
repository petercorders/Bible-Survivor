# Unity Adam independent UI review

2026-11-14 KST. Reviewer: coordinator subagent `unity_ui_review`, independent of implementation. Scope: existing macOS Development Player and visible player input only. Art03 is absent; placeholder shapes are expected. Source/build files were not changed. Initial result: FAIL (UI-01). First follow-up: UI-01 CLOSED, UI-02 found. Final targeted follow-up: **PASS — UI-01 and UI-02 CLOSED; no open HIGH/MEDIUM in the exercised scope**. This is not full manual-round/mobile certification. Detailed follow-ups below.

## Finding UI-01 — MEDIUM, level-up choice has no usable effect information

At a naturally reached level2 modal (world43.216666666665496s, HP72), the three buttons show only `1. crown`, `2. underwear`, `3. east_wind`. They expose stable internal IDs without item name, passive/weapon category, current/next level or the effect being purchased. The player cannot compare the upgrades. This affects an actual required choice, independently of placeholder art.

Location: `UnityGame/Assets/Adam/Runtime/AdamGame.cs`, `OnGUI` PendingChoices button loop. Requested fix: present an understandable item name and category, current→next level and the actual next-level effect. Keep the stable IDs for commands, not as the only product label. Do not invent effects or change balance; derive display from canonical implemented rules. Rerun the same natural modal and select using both keyboard and mouse. Evidence: `evidence/unity-ui-review-20261114/after-movement.png` and `.json`; selection result `mouse-choice.png`, `after-choice-save.json`.

## Actual observed paths

| Path | Result and limit |
| --- | --- |
| Resume existing checkpoint with Enter | PASS: pause modal disappears and clock resumes from21.8s. |
| Short real Player movement | PASS, narrow: ten Right taps change saved PlayerX from0 to0.05, facing right. These are genuine Player InputSystem events, not test injection. |
| Sustained movement | NOT_RUN: Orca press-key has no key-down/key-up/hold-duration interface. Repeating100 taps does not establish sustained movement. This limitation is not evidence that physical held keys fail. PlayMode movement results were not reused as Player proof. |
| Natural XP and level-up | PASS: no boundary or XP injection. After resuming automatic sling combat, level2 at43.2167s, HP72, three choices. |
| Mouse choice | PASS: foreground click on option2 clears choices and writes passive `underwear=1`. Clock remains43.2167s because Esc had additionally paused the host. |
| Pause | PASS: Esc opens pause; selection while paused preserves paused state; manual Enter and mouse Resume work. |
| Restart | PASS: normal window close, Player exit0 and relaunch restore43.2s/HP72/LV2/gold7 and arena positions, with explicit pause. |
| Window sizes | Visible1100×720 and660×440 content sizes: no clipped HUD, modal or instruction lines observed. At660×440 labels shrink to approximately11px; minimum supported PC window size remains unspecified, so this is a readability advisory rather than a certification. |
| Keyboard growth choice / new starting-weapon mouse choice | NOT_RUN in this review; the active saved challenge was preserved. Prior implementer checks are not counted as independent review. |
| Full3-minute manual survival / death / completion | NOT_RUN. No claim of a complete manual round. |
| Touch/mobile, physical interruption, performance | NOT_RUN. Desktop GUI does not prove mobile behavior. |

Mouse background input did not activate Resume. One foreground retry did, with visible button label changing to Pause and clock advancing35.3s. This is expected for a game receiving hardware pointer input and is not recorded as a game defect. Orca action metadata remains synthetic/unverified; PASS above is based on observed screenshots and saved state changes. Accessibility tree exposes only the native window, so controls were located from current screenshot coordinates (scale1).

## Evidence and preservation

Ignored local root: `evidence/unity-ui-review-20261114/`. JSON and PNG: `state`, `return`, `pause`, `mouse-resume`, `mouse-resume-foreground`, `after-movement`, `mouse-choice`, `small-restart`; save snapshots: `after-right-taps.json`, `after-choice-save.json`; actual Player logs: `player.log`, `player-small.log`. Native executable launched with `-screen-fullscreen 0 -screen-width 1100 -screen-height 720` and660×440 on restart. Both managed exec sessions returned0 after normal window closure. GUI close commands sometimes returned background/focus errors after the process had already exited; no extra retries or invented GUI verification were counted. No managed exception matched the reviewed logs.

The existing local primary, backup and lock were copied before review. After both Players exited, their final files were preserved as review evidence and all three originals restored byte-for-byte. `restoration.json` confirms all three hashes match. No original gameplay progress is lost; review progress is isolated in ignored evidence. No user account or settings were changed.

Re-review UI-01 after the implementation fix; keep sustained physical movement, keyboard growth selection and full-round/manual device gates explicitly open until separately demonstrated.

## Follow-up — UI-01 CLOSED, UI-02 OPEN

The newly built Player was independently reopened at the same naturally obtained level2 checkpoint (43.2167s, HP72, choices crown/underwear/east_wind). This follow-up reused the producer's saved natural modal to reproduce the exact three choices; it did not inject XP or claim a second independently played natural progression.

At1100×720 and660×440, each card clearly shows Korean name (`가시 면류관`, `요한의 팬티`, `동풍`), category `보조`, `신규 0 → 1/5` and effect. The displayed effects are respectively retaliation damage0→5, move-speed bonus0→10%, stone-projectile speed bonus0→10%, matching the implemented Core rules and the new `AdamChoiceText.Describe` source. Opaque modal fill prevents enemies/drops from bleeding through text. UI-01 is **CLOSED** for this specific level2 reproduction and reviewed display source; not a claim of every item/level/device visual coverage.

Actual input: at1100×720, key1 commits only `crown=1` and clears the choices. A second key1 leaves crown at1 and other passives absent. At660×440, a foreground mouse click on card2 commits only `underwear=1` and clears the choices. Both return to paused state at the exact43.2167s checkpoint, with no second choice applied. Keyboard growth selection is therefore PASS for this follow-up, superseding the initial NOT_RUN for that narrow path.

### UI-02 — MEDIUM: pause overlay hides the Resume control at660×440

After choosing at660×440, the pause panel spans almost the entire content height. The HUD `재개` button is painted first at y54..104; the subsequent620×340 pause panel begins at y50 and covers it. Only a few pixels along the top of the button remain visible, with no readable label. The instruction tells the player to click Resume, but the control is hidden. Enter remains an alternative; this finding concerns visible mouse/touch access and does not claim keyboard resume is broken. The hidden button may still receive a click at its former coordinates; that does not provide a discoverable control.

Location: `AdamGame.OnGUI` HUD button and final paused branch calling `Modal`. Reproduce: launch at660×440, resume or select an upgrade, then pause. Suggested fix: render a labeled Resume button inside the pause modal or reserve a visible control outside its bounds, keeping minimum-size and safe-area behavior. Retest small and large windows with an actual mouse click, verifying clock resumes and can pause again. Evidence: `follow660mouse2.png` and source geometry. New finding was sent immediately to coordinator; no source fix was made by reviewer.

### Follow-up evidence and preservation

Ignored root `evidence/unity-ui-review-followup-20261114/`: `follow1100modal`, `follow1100key1`, `follow1100duplicate`, `follow660modal`, `follow660mouse2` JSON/PNG; `1100-after-key-1.json`, `1100-after-duplicate.json`, `660-after-click.json` save evidence; `player1100.log`, `player660.log`. Both actual Player exec sessions exited0 after normal close. Close GUI calls again reported a focus error after the process had exited; no repeat/retry was counted as independent success.

Before reproduction, both legacy and current product save locations were preserved (six primary/backup/lock states). After both Players exited, existing files were restored byte-for-byte and originally absent generated files were moved into ignored evidence. `restoration.json` is true for all six states. No source, build or original save progress was changed. Sustained held movement, full manual round, physical mobile and performance remain NOT_RUN; they were not retried indefinitely.

## Final targeted follow-up — UI-02 CLOSED

The new actual macOS Player was independently run at660×440 and1100×720 with the previously observed paused level2 checkpoint. At both sizes the modal contains a fully visible `재개 (Enter)` button inside its opaque panel, with readable instructions and no underlying HUD title/button fragments. The arena remains visible around the modal as intended. The reviewer clicked that visible button in each actual window: the modal disappeared, the HUD returned and combat time advanced. Esc restored the pause modal, Enter resumed again, and a final Esc paused again. The two required input paths therefore work at both sizes; no new HIGH/MEDIUM was observed in these paths.

At660×440, the initial saved clock43.2167s advanced to44.0167s after mouse resume/Esc, then44.2833s after Enter/Esc. At1100×720, the restored44.2833s checkpoint resumed via mouse (screenshot44.9s), then the Enter/Esc sequence saved45.3333s. Real HP/enemy changes during those short resumed intervals confirm simulation was running rather than merely changing the button label. No held movement or full survival claim is inferred from these checks.

Evidence root: `evidence/unity-ui-review-ui02-final-20261114/`. `ui02-660pause`, `ui02-660click`, `ui02-660escape`, `ui02-660enter`, `ui02-660repaused` and equivalent `ui02-1100*` JSON/PNG show the actual paths. `660-after-mouse-pause.json`, `660-after-enter-pause.json`, `1100-after-enter-pause.json` preserve actual committed clocks. `player660.log` and `player1100.log` belong to the two real Player launches. Both managed exec sessions returned0 after normal window closure; post-close focus-error responses were not taken as additional GUI verification and were not repeatedly retried.

The same six original save-file states were preserved and restored after both Players exited, with `restoration.json` all true. This final review did not modify source/build, launch Editor or rerun the producer's Unity tests. UI-02 closure is based on independent visible Player interaction, not the producer's PlayMode regression result. UI-01 remains closed from the preceding actual keyboard/mouse card re-review. Sustained held movement, full manual three-minute round, physical touch/mobile interruption and performance measurements remain **NOT_RUN**.
