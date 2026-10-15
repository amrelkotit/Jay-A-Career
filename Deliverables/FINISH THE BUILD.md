# Updated labeled Windows build completed

Run Windows Labeled 20261015/Jay A Career.exe, or extract Jay A Career - Windows Labeled.zip and keep its accompanying files together. Earlier Windows Fresh/Windows Final players are historical versions.

The full build and automated regression/performance checks succeeded. The final build CLI exited successfully with code 0; labeled-copy-provenance.json records the complete output copy. See RELEASE STATUS.md and Evidence/labeled-windows-build-20261015.json. The remaining user check concerns yellow note readability, lifting/placing, and the new lighting. Follow MANUAL PLAYER CHECKS.md.

## Reproduction on this machine

Tools/build_labeled_release.ps1 applies the targeted authoring changes in the cached temporary project and builds a full Windows player using UsabilityLightingUpgrade.ApplyAndBuild. Tools/compile_response_launcher.ps1 compiles the included BeeResponseLauncher.cs source if needed. The launcher uses this machine's Unity 6000.3.24f1 installation path; adapt that path when building elsewhere.

The response-file workaround keeps Unity's original Bee backend/generator and metadata arguments. It addresses the original long-command metadata stall. The normal Build Final Windows Game menu is not independently certified as fixed. The optional Unity Pipeline runtime warning is separate from URP rendering.

No OS security settings were changed; no publication or submission has occurred.
