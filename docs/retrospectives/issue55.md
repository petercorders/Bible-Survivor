# #55 desktop display startup retrospective

The baseline macOS Player reproduced Unity's reset of a previously saved 1280×800 window. The approved follow-up verified a native startup change on the same build route and with the same fixtures. The existing baseline Player served as the regression control.

An initial callback used Unity Editor APIs that were unavailable under the pinned version. The retry used the installed API documentation and the built application's own deployment target. App verification then caught the Galmuri notice being copied after signing. Copying those files before signing resolved the packaging failure.

The main lesson is to check engine-owned preference precedence and verify the finished app signature when changing a native default. Reuse the same player for profile and argument cases so that each case isolates the launch condition. Windows Unity and Steam still need their target environments. [Verification record](../harness/desktop-display-startup-55.md)
