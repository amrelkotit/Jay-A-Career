from pathlib import Path
import json
r=Path(__file__).resolve().parents[1];e=r/'Deliverables/Evidence'
route=json.loads((e/'windows-labeled-route.json').read_text(encoding='utf8'))
build=json.loads((e/'labeled-windows-build-20261015.json').read_text(encoding='utf8'))
checks=json.loads((e/'windows-labeled-input-checks.json').read_text(encoding='utf8'))
assert build['result']=='Succeeded' and build['errors']==0 and checks['passed'] and checks['checks']==40 and route['completed']
status=f'''## Current verification status

- Latest player: **Deliverables/Windows Labeled 20261015/Jay A Career.exe**; packaged as **Jay A Career - Windows Labeled.zip**. Earlier players/ZIPs are retained.
- A fresh full Unity BuildPipeline.BuildPlayer build succeeded on 15 October 2026: **{build['errors']} errors, {build['warnings']} warning(s), {build['seconds']:.1f} seconds**. Scene data and scripts were rebuilt together using the project-local Bee response-file launcher and original Unity generator. No DLL transplant or OS security change was used.
- The final build CLI exited successfully with code 0. The complete output copy is recorded in labeled-copy-provenance.json. Earlier failed authoring/shutdown attempts are superseded by this successful final build.
- Yellow handwritten notes identify all four liftable objects and ten task/furniture locations. Liftable prompts show the object name and controls. Tables/desks are labeled fixed furniture.
- Lab ceiling fixtures start **off**. F controls only the flashlight. O at the labeled Lab lights switch controls the four ceiling fixtures. Clear window openings and shadowed daylight fills admit exterior light; low ambient colours make deeper areas subdued and the room dark at night when both light sources are off.
- The preceding player passed the user's physical movement, mouse look, pause/Resume, camera, door, flashlight toggle and Quit checks. The user did not test lifting/placing and requested these labels/lighting changes. **Targeted physical lift/place and lighting feedback is pending.** See MANUAL PLAYER CHECKS.md and Evidence/manual-player-review-20261015.json.
- The labeled player passed **40 automated input/physics checks** and a visible two-day 1920×1080 route, ending at Day 3/$150: **{route['averageFps']:.1f} average FPS / {route['percentile1Fps']:.1f} FPS 1% low** on Ryzen 7 4800H / GTX 1650. Prior pure daily rules passed 72 assertions; prior scene integration passed 61 checks.
- Assignment 2 remains 30 pages (DOCX/PDF), with measured map, updated screenshots, source archive and scripted Windows-player demonstration. The video is silent and does not represent human keyboard play.

Original Assignment 1 files and backups are preserved. No publication or submission has occurred.
'''
p=r/'REALISM_README.md';p.write_text(p.read_text(encoding='utf8').split('## Current verification status')[0]+status,encoding='utf8')
(r/'Deliverables/RELEASE STATUS.md').write_text('# Jay A Career release status\n\n'+status,encoding='utf8')
(r/'Deliverables/FINISH THE BUILD.md').write_text('''# Updated labeled Windows build completed

Run Windows Labeled 20261015/Jay A Career.exe, or extract Jay A Career - Windows Labeled.zip and keep its accompanying files together. Earlier Windows Fresh/Windows Final players are historical versions.

The full build and automated regression/performance checks succeeded. The final build CLI exited successfully with code 0; labeled-copy-provenance.json records the complete output copy. See RELEASE STATUS.md and Evidence/labeled-windows-build-20261015.json. The remaining user check concerns yellow note readability, lifting/placing, and the new lighting. Follow MANUAL PLAYER CHECKS.md.

## Reproduction on this machine

Tools/build_labeled_release.ps1 applies the targeted authoring changes in the cached temporary project and builds a full Windows player using UsabilityLightingUpgrade.ApplyAndBuild. Tools/compile_response_launcher.ps1 compiles the included BeeResponseLauncher.cs source if needed. The launcher uses this machine's Unity 6000.3.24f1 installation path; adapt that path when building elsewhere.

The response-file workaround keeps Unity's original Bee backend/generator and metadata arguments. It addresses the original long-command metadata stall. The normal Build Final Windows Game menu is not independently certified as fixed. The optional Unity Pipeline runtime warning is separate from URP rendering.

No OS security settings were changed; no publication or submission has occurred.
''',encoding='utf8')
(e/'release-status.json').write_text(json.dumps({'release_method':'Fresh full Unity BuildPipeline.BuildPlayer build with Bee response-file launcher','windows_player_folder':'Windows Labeled 20261015','windows_player_checks_passed':40,'visible_route_passed':route['completed'],'average_fps':route['averageFps'],'percentile_1_fps':route['percentile1Fps'],'fresh_full_build_succeeded':True,'build_errors':build['errors'],'build_warnings':build['warnings'],'physical_input_review':'Passed on preceding Windows Fresh player; labeled revision regression checks passed','physical_lift_place_and_lighting_review':'Pending user feedback','original_submission_preserved':True},indent=2),encoding='utf8')
print('Updated release status for labeled full build')
