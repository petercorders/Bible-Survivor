# Unity Adam prototype — independent code review

2026-11-14 KST. Separate reviewer session. Scope: `Assets/Adam/Runtime`, `Editor`, `Tests`, the Unity core sync/provenance checker, mutation-copy runner and implementation handoff. Core/R02 and Unity runtime files were not edited by this reviewer. The only repository output is this report.

## Findings

### MEDIUM U01 — provenance can report PASS while the Core DLL is stale

Locations: `tools/unity_check_project.py` source-hash loop and `tools/unity_sync_core.py` source discovery.

The checker iterates only the paths already listed in `sources_sha256`. It does not compare that list with the current source set. Adding a new top-level Core C# file leaves every recorded hash unchanged, so it reports PASS against a DLL that does not contain the new class.

The producer also discovers sources with `glob('*.cs')`, while the SDK project compiles nested C# files by default. A nested source can therefore be compiled into the DLL but omitted from the generated provenance. Changing it after a successful sync is then undetectable. Relevant MSBuild inputs/imports need an explicit scope as well; a manifest of only root C# files and the csproj is not a complete compile-input inventory.

Independent isolated reproduction, using the existing local SDK and no Editor:

| Action | Actual result |
|---|---|
| Run the unchanged checker in a copied tree | exit 0 / PASS |
| Add valid `src/BibleSurvivor.Core/NewUnsyncedRule.cs`, keeping the original DLL/provenance | exit 0 / PASS — incorrect |
| Add `src/BibleSurvivor.Core/Additional/NestedRule.cs`, run the actual sync script | sync exit 0; nested source absent from generated provenance |
| Change only that nested source after sync | checker exit 0 / PASS — incorrect |

Impact: a future Core file addition or nested-source change can pass the proposed Unity synchronization gate without being reflected in the shipped DLL. This does not invalidate the observed existing Editor tests, but it weakens the gate for subsequent changes and prevents treating its PASS as source/DLL synchronization evidence.

Requested fix: inventory the actual supported compile inputs, compare the current input-path set as well as hashes, include recursive/new sources and declared build configuration/import inputs, and verify the same inventory before and after the build. Reject unsupported inputs rather than silently excluding them. Add the two observed false-PASS inputs as negative checker tests. Independent reproduction logs and copied inputs were handed to the coordinator.

Status after follow-up: **U01 resolved in the tools; independently verified against the original two failing inputs**. Live Unity project re-sync and its actual Editor/build/CI gates remain separate integration work.

### U01 follow-up — original regressions rechecked

The revised `unity_build_inputs.py`, `unity_sync_core.py`, `unity_check_project.py` and provenance fixture tests were reviewed. Schema2 uses fresh MSBuild item/property evaluation and resolved imports, compares the input-path set as well as hashes, and records post-Rebuild compiler/reference/generated inputs. The same evaluated inventory is compared before/after the build. Schema1 manifests fail closed. The documented contract excludes arbitrary undeclared custom-task IO rather than claiming full semantic build analysis.

The reviewer reused the original isolated reproduction tree and the existing SDK. No live Unity files, Editor or Player were changed or launched:

| Same original input, revised tools | Actual result |
|---|---|
| Real sync with an existing nested source | exit 0; schema2 explicitly records the nested source |
| Checker baseline | exit 0 / PASS |
| Add the original top-level `NewUnsyncedRule.cs` without re-sync | exit 1 / FAIL, added build input reported |
| Restore the original root-source set | exit 0 / PASS |
| Change only the original nested source after successful sync | exit 1 / FAIL, changed build input reported |
| Restore nested source content | exit 0 / PASS |

This closes U01 through the same negative inputs that previously returned a false PASS. Logs and `review-cases-schema2.json` were handed to the coordinator. The implementation's broader 35-case fixture result is separate evidence; this reviewer did not claim to rerun all 35 cases.

At handoff, the shared project still intentionally carried schema1 while the UI/Unity owner was working. That expected failure is not a current-project PASS. Fresh checkouts also need the recorded compiler/generated inputs available; final CI must be verified on its actual runner. If CI re-syncs before checking, it must detect tracked DLL/catalog/provenance drift so a stale committed DLL is not hidden by a newly generated working copy. These are integration conditions, not evidence that the two original U01 regressions remain open.

## Runtime code review

The reviewed ordinary adapter paths did not reveal another reproducible HIGH/MEDIUM defect:

- `AdamHost` owns the main-thread session/world and commits both at one checkpoint boundary. Save failure sets Error and pauses play; Tick/Start/Choose cannot advance the uncommitted payload through the UI. Retry keeps the operation ID and expected revision. Successful retry clears the operation, preventing a second application. Reload is an explicit discard/recovery path and remains paused. Future/later-content saves are rejected without automatically starting a new challenge.
- Terminal core states are captured without an obsolete World. Death keeps the permanent profile and clears the run; a successfully committed death therefore loads without resurrection. A user choosing Reload after an uncommitted failure may intentionally return to the last committed checkpoint; that distinction is documented.
- Host pause/suspension gates simulation, clears movement and caps a long frame to 0.1 simulated seconds. The documented 5.1-simulated-second checkpoint bound matches the after-tick timer structure; it is not a mobile power-loss durability claim.
- `AdamGame` rechecks a current choice, guards SelectSlot to one command per frame, uses Input System key edges, and holds movement until keyboard/touch input is released after a modal or interruption. Touch and keyboard feed the same normalized movement vector. Error handling blocks new selections while showing retry/reload controls.
- Existing EditMode tests target actual state/revision/reload boundaries rather than only inspecting the implementation: save failure, pause/focus/long frame, checkpoint rollback, completion/death, natural level-up persistence, future-save protection and backup recovery. PlayMode tests exercise actual keyboard/touch state events and MonoBehaviour updates. Their focus-routing test settings are restored in teardown.

These are source-review conclusions, not an independent manual input or UI/UX pass. The implementation handoff correctly limits its brief GUI smoke and distinguishes boundary injection from a full survival playthrough.

## Tool and build review

- The declared asmdef allowlist and runtime editor-import scan are narrow checks; the handoff does not claim semantic dependency analysis. The current repository checker was independently executed and returned PASS before the isolated negative inputs above.
- The sync tool builds the canonical netstandard2.1 project rather than maintaining a hand-edited Core source fork. U01 concerns incomplete provenance input discovery, not the decision to use a controlled DLL.
- The mutation runner uses a separate project, APFS copy-on-write cache copies rather than hard links, named passing baseline cases, actual failed-test XML and a compile-error exclusion. Its two pause/checkpoint mutants are specific behavioral evidence, not broad mutation coverage. This reviewer did not launch that runner or an Editor against the shared project.
- `AdamProject.BuildMac` rejects a non-successful BuildPipeline result. A successful build alone is not a device/performance/UX result.

## Verification boundary

Independently executed: original provenance checker, isolated new-source checker input, real standalone Core sync with a nested compile input, and post-sync nested-source mutation/checker. Existing SDK only; no new SDK/packages or account/license changes.

Not independently executed here: Unity Editor compile/EditMode/PlayMode, GUI, development Player, Android/iOS, real interruption durability, CPU/GPU/allocations/memory and accessibility interactions. Their implementation evidence remains in `docs/progress/unity-adam.md`; independent UI review is a separate assignment. No screen PASS is inferred from this code review.

Runtime/Editor/tests/tools findings and original-regression evidence were handed to the coordinator for correction and follow-up review before final synchronization-gate approval.

## Final integration follow-up — portable CI gate and choice descriptions

The final `unity_check_portable.py` and the last step in `.github/workflows/core.yml` were independently reviewed. The CI step runs this checker after the existing Core checks/mutations, without re-syncing or rebuilding the committed Unity DLL/catalog/provenance. Thus it does not conceal a stale committed artifact by replacing it before verification. It compares freshly evaluated repository input sets, hashes and stable compiler properties with schema2 provenance, plus the canonical/bundled catalog and committed DLL hashes. SDK/generated-input equivalence and Unity execution are explicitly `NOT_RUN`; this portable gate does not replace the strict local checker or Editor tests.

Independent CLI reproduction used a **new copy without `.git`, `bin` or `obj`**, the existing SDK and an actual `bash tools/dotnet.sh restore src/BibleSurvivor.Core/BibleSurvivor.Core.csproj` (exit 0). No re-sync was performed. The copied schema2 provenance and DLL were used as supplied by the implementation.

| Input in the isolated fresh copy | Actual portable-checker result |
|---|---|
| Restored fresh baseline | exit 0 / PASS |
| Add a new root Core C# file | exit 1 / FAIL |
| Add a new nested Core C# file | exit 1 / FAIL |
| Delete existing `AdamSession.cs` | exit 1 / FAIL |
| Change canonical catalog bytes | exit 1 / FAIL |
| Change bundled catalog bytes | exit 1 / FAIL |
| Change committed DLL bytes | exit 1 / FAIL |
| Change csproj `Nullable` property | exit 1 / FAIL |
| Add an imported `Directory.Build.props` | exit 1 / FAIL |
| Restore each of the eight inputs separately | all eight exit 0 / PASS |

All 17 checker invocations returned the expected status; these are actual subprocess exit codes and structured results, not mocked manifest comparisons. `restore.log`, per-case logs and `review-cases.json` are in the isolated `bible-portable-review-8dob9cfm` tree handed to the coordinator. This proves the requested fresh-copy and original input-regression behavior on this macOS SDK environment. The actual GitHub Ubuntu job remains a separate coordinator check.

The final `AdamChoiceText.cs`, its EditMode tests and its use by `AdamGame` were also read. Stable IDs map to Korean item names; cards show new/upgrade state, current/next/max levels and concrete effects. Weapon changes use `WeaponRules` values; passive descriptions distinguish their contribution from combined weapon/permanent values and test them against actual Core effects. No additional reproducible HIGH/MEDIUM code defect was found in this follow-up. The implementer's 25 EditMode / 6 PlayMode results were not independently rerun by this reviewer, and no visual, font-rendering, layout, Player or mobile PASS is inferred from reading the code. Independent UI review retains that separate scope.

## UI-02 narrow final code follow-up

2026-11-14 KST. Read-only review of the final `AdamGame.DrawGameView`, `DrawPauseOverlay`, modal/HUD conditions and `AdamPlayTests.SmallPauseModalPointerClickResumesBattle`; **no additional HIGH/MEDIUM code defect found**. Only this review document was updated. No Core, runtime, test or tool files were changed and no Editor/Player or full suite was launched.

The real OnGUI path and the new test both call the complete `DrawGameView` stack. Any modal suppresses underlying HUD text and controls. The pause overlay owns its own Resume button rather than relying on a covered HUD control. It uses the existing host pause gate, clears held-input/touch state and disables the button during suspension or a save error. Choice/error/terminal branches retain precedence over the pause overlay.

The Editor-backed PlayMode regression renders a 660×440 utility window, verifies stable paused simulation time, sends MouseDown/MouseUp at the modal's button, requires `Paused` to clear and then requires simulation time to advance. It does not invoke the resume handler directly. The temporary window closes in a `finally` block; Editor-only references are guarded. The implementer's compile03 / 25 EditMode / 7 PlayMode / build results and original Modal-only behavioral mutant failure are separate execution evidence, not reruns by this reviewer. Actual final Player appearance and click usability remain the independent UI reviewer's scope; this code follow-up does not close UI-02 visually or claim mobile validation.
