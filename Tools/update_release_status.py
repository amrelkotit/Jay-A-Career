from pathlib import Path
import runpy,sys
# The fresh full build supersedes the historical Mono-update status below.
runpy.run_path(str(Path(__file__).with_name('update_fresh_release_status.py')),run_name='__main__')
sys.exit(0)
import json
r=Path(__file__).resolve().parents[1];e=r/'Deliverables/Evidence'
route=json.loads((e/'windows-final-route.json').read_text(encoding='utf8'))
status='''## Current verification status

- Use **Deliverables/Windows Final/Jay A Career.exe** or **Jay A Career - Windows Final.zip** for the tested release. The earlier Windows folder and Windows Build ZIP are preserved.
- Release method: latest Unity-compiled Mono gameplay DLL deployed on the earlier matching scene data. Assembly identity, component field layouts and runtime startup methods match. All 556 scene/visual asset files have matching rendering data; three material differences only synchronize obsolete URP aliases. Evidence: windows-final-provenance.json and mono-update-compatibility.json.
- Daily rules: 72 passed assertions. Scene integration: 61 passed checks. Updated Windows player: **40 passed input/physics checks**, including Resume mouse capture, contextual keys, payment guards, package docks/recovery, prop placement, doors, arrangement, cameras and lights. Windows tests use synthetic Input System events and the actual player controller; no headless world-input fallback runs in the player.
- The visible 1920×1080 two-day route completed at Day 3 with $150, no reported error, **59.4 average FPS / 59.2 FPS 1% low** on Ryzen 7 4800H / GTX 1650. See windows-final-route.json. This is the performance result; Editor video capture and older hidden-window timing are separate historical evidence.
- The 30-page GDD, measured map, source archive and scripted Unity Editor demonstration are prepared. The video is silent and is not a human keyboard-play recording.
- Physical keyboard/mouse feel review remains pending user feedback. Automated desktop key injection did not register during normal play; this is not recorded as a passed physical-input check.
- A fresh full BuildPipeline rebuild still stalls when Bee launches suspended metadata subprocesses on this machine. Hub/menu, normal batch, one-worker and process-group diagnostic attempts were observed; the direct metadata generator succeeds with its original argument formatting. No OS security settings were changed. The tested Mono update is **not** represented as a successful fresh full rebuild. The diagnostic Editor code has been removed from RealismBuildEntry; the normal build menu remains available.

The recoverable pre-upgrade source backup and earlier Windows ZIP are in the sibling Jay A Career Backups folder. Original Assignment 1 submission files are preserved. No publication or course submission has been performed.
'''
p=r/'REALISM_README.md';text=p.read_text(encoding='utf8');p.write_text(text.split('## Current verification status')[0]+status,encoding='utf8')
(r/'Deliverables/FINISH THE BUILD.md').write_text('''# Windows release and full rebuild status

## Tested player

Run `Windows Final/Jay A Career.exe`, or extract `Jay A Career - Windows Final.zip` and keep its accompanying folders/DLLs together. This release passed 40 player input/physics checks and a visible two-day 1080p route at 59.4 average FPS / 59.2 FPS 1% low.

It contains the latest Unity-compiled Mono gameplay DLL and previously built matching scene data. Compatibility and asset comparisons are recorded in Evidence/windows-final-provenance.json and Evidence/mono-update-compatibility.json. The original earlier executable and ZIP remain available.

## Known full rebuild blocker

The Unity Hub/editor menu attempt was performed on 14 October 2026. It stalled at Extracting script serialization layouts, with no error dialog; Cancel also hung. The unresponsive Unity process was ended at the user's request. Subsequent focused diagnostic attempts still encountered suspended Bee child processes. The metadata generator itself succeeds when launched directly with the original argument formatting.

A successful fresh full BuildPipeline rebuild is still pending. Do not treat the earlier Succeeded summary as a new full build report, or the validated Mono update as a successful fresh rebuild. No OS security settings were modified.

For a later full rebuild, open this working project in Unity 6000.3.24f1 and choose Jay A Career / Build Final Windows Game. Output is Windows/Jay A Career.exe and Evidence/windows-build-summary.json. Repeating the same launch context is not a confirmed fix.

Physical keyboard/mouse feel review remains pending user feedback. No publication or submission has occurred.
''',encoding='utf8')
(r/'Deliverables/RELEASE STATUS.md').write_text('''# Jay A Career release status

Prepared: upgraded Unity scene/assets/gameplay, 30-page Assignment 2 GDD (DOCX/PDF), measured map PNG, scripted demonstration MP4, Unity project ZIP and a separately packaged tested Windows Final player.

Windows Final passed 40 player input/physics checks and a visible two-day route, reaching Day 3/$150 at 1920×1080 with 59.4 average FPS and 59.2 FPS 1% low.

This is a validated Mono script update on matching previously built scene data. A fresh full Unity BuildPipeline rebuild remains blocked by suspended metadata subprocesses on this machine. Physical keyboard/mouse feel awaits user feedback. These limits prevent an unconditional claim that every original acceptance item is complete.

Use Windows Final, not the historical Windows Build ZIP, for the updated player. No course submission or publication has occurred.
''',encoding='utf8')
report={'release_method':'validated Mono script update','windows_player_checks_passed':40,'visible_route_passed':route['completed'],'average_fps':route['averageFps'],'percentile_1_fps':route['percentile1Fps'],'fresh_full_build_succeeded':False,'physical_input_review':'Pending user feedback','original_submission_preserved':True}
(e/'release-status.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print('Updated release status and rebuild instructions')
