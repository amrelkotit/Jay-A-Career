from pathlib import Path
import json
r=Path(__file__).resolve().parents[1];e=r/'Deliverables/Evidence'
if (e/'windows-labeled-route.json').exists():
 import runpy,sys
 runpy.run_path(str(Path(__file__).with_name('update_labeled_release_status.py')),run_name='__main__');sys.exit(0)
route=json.loads((e/'windows-fresh-route.json').read_text(encoding='utf8'))
build=json.loads((e/'fresh-windows-build-20261015.json').read_text(encoding='utf8'))
checks=json.loads((e/'windows-fresh-input-checks.json').read_text(encoding='utf8'))
assert build['result']=='Succeeded' and build['errors']==0 and checks['passed'] and checks['checks']==40 and route['completed']
status=f'''## Current verification status

- Use **Deliverables/Windows Fresh 20261015/Jay A Career.exe** or **Jay A Career - Windows Full Build.zip** for the latest tested release. Earlier Windows/Windows Final players and ZIPs are preserved as historical versions.
- A fresh full Unity BuildPipeline.BuildPlayer build succeeded on 15 October 2026: **zero errors, one warning**, approximately six minutes. It rebuilt scene data and scripts together; no DLL transplant was used. See Evidence/fresh-windows-build-20261015.json and fresh-temp-build-cli-provenance-20261015.json.
- The build used the cached temporary project copy and a project-local Bee response-file launcher. It keeps Unity's original generator and arguments while shortening the metadata command line. The normal menu may still need this machine-specific workaround. No OS security settings were changed.
- Daily rules: 72 passed assertions. Scene integration: 61 passed checks. Fresh full-build Windows player: **40 passed input/physics checks**, including Resume mouse capture, contextual keys, payment guards, package docks/recovery, prop placement, doors, arrangement, cameras and lights. Player checks use synthetic Input System events through the actual player controller.
- A visible 1920×1080 two-day route completed at Day 3 with $150 and no reported error: **{route['averageFps']:.1f} average FPS / {route['percentile1Fps']:.1f} FPS 1% low** on Ryzen 7 4800H / GTX 1650. See windows-fresh-route.json.
- The 30-page GDD, measured map, source archive and scripted Unity Editor demonstration are prepared. The video is silent and is not a human keyboard-play recording.
- **Physical keyboard/mouse review remains pending user feedback.** Follow Deliverables/MANUAL PLAYER CHECKS.md using the fresh player above. Automated input checks do not replace this review.

The recoverable pre-upgrade backup and earlier Windows ZIP are in the sibling Jay A Career Backups folder. Original Assignment 1 files are preserved. No publication or course submission has occurred.
'''
p=r/'REALISM_README.md';p.write_text(p.read_text(encoding='utf8').split('## Current verification status')[0]+status,encoding='utf8')
(r/'Deliverables/FINISH THE BUILD.md').write_text('''# Fresh full Windows build completed

On 15 October 2026, Unity 6000.3.24f1 completed a fresh full BuildPipeline.BuildPlayer build: Succeeded, zero errors, one warning, 358.8 seconds. This is a full scene-and-script build, not the earlier Mono DLL update.

## Use the latest player

Run `Windows Fresh 20261015/Jay A Career.exe`, or extract `Jay A Career - Windows Full Build.zip` and keep its accompanying folders and DLLs together. Follow MANUAL PLAYER CHECKS.md. Physical keyboard/mouse feedback is the remaining acceptance check.

Evidence: fresh-windows-build-20261015.json, fresh-temp-build-cli-provenance-20261015.json, windows-fresh-input-checks.json and windows-fresh-route.json in Evidence. The fresh player passed 40 automated checks and the visible two-day 1080p route.

## Build workaround retained in source

The original long Bee metadata command stalled on this machine. ResponseFileFullBuild.Execute temporarily uses Tools/BeeResponseLauncher.exe to pass the unchanged generator arguments through a response file. The original Unity Bee backend and generator still perform the build. This opt-in helper does not run during normal opening or gameplay.

Tools/compile_response_launcher.ps1 compiles the helper from its included C# source. Tools/build_response_file.ps1 records the successful workflow using this machine's existing temporary cached project; it is a machine-specific reproduction script. For another checkout, compile the helper, then invoke Unity with -batchmode -quit -projectPath <project> -executeMethod ResponseFileFullBuild.Execute -buildTarget StandaloneWindows64 -logFile <log>. Install Unity 6000.3.24f1 Windows Build Support and resolve its Packages first. Output is Deliverables/Windows Fresh 20261015.

The original Jay A Career / Build Final Windows Game menu remains available; its original launch context is not independently certified as fixed. Earlier blocked logs and the Windows Final Mono update remain historical evidence. The new full build supersedes their release limitations.

The one warning concerns the optional Unity Pipeline runtime configuration; this editor automation package is disabled in the player. URP rendering is separate and works in the tested player.

No OS security settings were changed. No publication or submission has occurred.
''',encoding='utf8')
(r/'Deliverables/RELEASE STATUS.md').write_text('# Jay A Career release status\n\n'+status.split('## Current verification status\n\n')[1],encoding='utf8')
report={'release_method':'Fresh full Unity BuildPipeline.BuildPlayer build with Bee response-file launcher','windows_player_folder':'Windows Fresh 20261015','windows_player_checks_passed':40,'visible_route_passed':route['completed'],'average_fps':route['averageFps'],'percentile_1_fps':route['percentile1Fps'],'fresh_full_build_succeeded':True,'build_errors':build['errors'],'build_warnings':build['warnings'],'physical_input_review':'Pending user feedback','original_submission_preserved':True}
(e/'release-status.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print('Updated release status for successful fresh full build; manual review pending')
