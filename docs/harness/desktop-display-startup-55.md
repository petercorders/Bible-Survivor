# Desktop display startup · #55

The normal macOS and Windows Player builds use a resizable 1920×1080 window on first launch. Unity fits the window to the available work area. The 660×440 supported minimum remains a separate menu check.

## Update behavior

Unity 6000.6 applies changed build defaults ahead of stored preferences. In the first macOS candidate, a 1280×800 window saved under the prior 1100×720 defaults opened at 1920×1080 and its saved size changed. The approved update runs before Unity reads those preferences. It updates the two recorded default-dimension values when a valid saved size exists. The current size, fullscreen mode, saved data, and other preferences remain under Unity's control. A fresh profile needs no migration. Unity still receives the original launch options and applies their normal precedence. [Unity 6.6 Screen reference](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/screen)

The post-build hook installs the startup code in the normal executable, so Editor Build Profiles, standard Unity batch builds, and `AdamProject.BuildMac` share the path. The hook selects macOS and Windows Player builds using the 1920×1080 build defaults. The isolated 1100×720 evidence builds keep their own entry points.

- macOS builds use Xcode command-line tools. The launcher links the generated player's architecture slices to Unity's native `PlayerMain`, and uses the app bundle identifier for preferences. The hook supplies the Galmuri notice and manifest before ad-hoc signing. Sign and notarize a distribution artifact after this build step.
- Windows builds use a Visual Studio Native Tools environment matching the target architecture. Start Unity from that environment so the hook can use `cl.exe` and `mt.exe`. The launcher keeps the requested executable name and the Unity manifest and loads its adjacent `UnityPlayer.dll`. Unity's Windows sample uses the same `UnityMain` entry point. [Unity Windows native launcher sample](https://github.com/Unity-Technologies/DesktopSamples/blob/master/ScreenSelectorExample/WindowsPlayer/WindowsPlayer/Main.cpp)
- Steam should target the normal generated app/executable. Use a private candidate and a supported host to verify client start/exit and overlay behavior, saved preferences on update, and Steam launch options.

## Verification

Unity 6000.6.9f1, macOS 26.5.1, Apple M4 Pro. The local `evidence/issue55/resume/` folder contains the original app, build command and exit status, executable and data SHA-256, Player logs, native frames, isolated settings, and screenshots. The preserved baseline evidence at `evidence/issue55/` demonstrates the reset without the startup change.

| Check | Result |
|---|---|
| Standard macOS Development build | PASS, `AdamProject.BuildMac` exit 0 and `codesign --verify --strict` exit 0. |
| Fresh macOS profile | PASS, 1920×1080 content and title menu on a temporary 2560×1440 display. The 1920×1080 host fit the same app to 1920×1018 content and a 1920×1050 frame. |
| Upgrade and relaunch | PASS, seeded 1280×800 window retained on both launches. The two saved default dimensions changed to 1920×1080 and the unrelated sentinel retained its value. |
| Saved fullscreen | PASS, mode 1 retained with 1280×800 content and a 2560×1440 presentation frame. |
| CLI and minimum-size menu | PASS, `-screen-width 660 -screen-height 440 -screen-fullscreen 0` opened a 660×440 window from the saved fullscreen profile. Unity saved that explicitly requested size and mode. The menu retained three cards and Right moved selection to the second card. |
| Native Windows launcher | The Windows CI job compiles the actual launcher and checks fresh, update, retry, and argument forwarding against a UnityMain probe. See the PR check for its result. |
| Windows Unity Player | BLOCKED locally. Requires Windows Build Support, a matching MSVC/SDK environment, and a Windows host to confirm the real Unity registry layout, native module compatibility, packaged manifest, mode and desktop fitting. |
| Steam client | BLOCKED locally. Requires a Steam client, private AppID/Build ID, and candidate package/branch access. |

The macOS upgrade fixture used the same Player data with a separate bundle ID and profile. Existing QA evidence was retained. #53 ceded the Unity/GUI slot to #56, which offered a sequential window to #55. #55 returned the slot to #56 after restoring the 1920×1080 host display.
