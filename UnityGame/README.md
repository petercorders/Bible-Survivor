# Adam Unity prototype

Use Unity **6000.6.9f1**. Open `Assets/Scenes/Adam.unity`, then Play. Choose a starting weapon with1–3, move with WASD/arrows, pause with Esc and resume with Enter or the visible Resume button inside the pause modal. Touch drag in the lower-left is an Editor-tested prototype, not verified mobile support. Reviewed Art03 pixel sprites and the outline-font sidecar are connected; appearance editing and later bosses are not implemented.

After any original Core or catalog change, run `python3 tools/unity_sync_core.py` from the repository root, then `python3 tools/unity_check_project.py` and the Unity EditMode/PlayMode suites. Generated DLL/catalog provenance is stored under `Assets/Adam/Resources`; never edit these copies. Do not open this project in two Editors at once.

The standalone macOS Development build is available locally at `Builds/Adam.app` (ignored). Rebuild via batch `-executeMethod BibleSurvivor.Unity.Editor.AdamProject.BuildMac`. Saves use `Application.persistentDataPath/adam-prototype.json`; failed saves freeze the battle and offer same-operation retry or explicit reload. Automatic checkpoints are nominally every5 combat seconds, with a host-tick bound of5.1 seconds. Art import and code/UI review passed for their documented scopes. Sustained hardware movement/full manual survival, physical-device support and performance remain unverified.

Prepare reviewed assets from the repository root with `python3 tools/unity_prepare_art.py` and `python3 tools/unity_prepare_fonts.py`, then run the actual Unity importer/tests/build. Preserve `.meta` files. Offline preparation checks are `python3 -B tools/unity_art_tool_tests.py`; they do not run Unity.

Full evidence, limitations and reproduction: `../docs/progress/unity-art.md`; the earlier placeholder baseline is `../docs/progress/unity-adam.md`.
