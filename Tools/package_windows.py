from pathlib import Path
import zipfile,hashlib,json,shutil,sys
r=Path(__file__).resolve().parents[1];final='--final' in sys.argv;fresh='--fresh' in sys.argv;labeled='--labeled' in sys.argv
folder=r/('Deliverables/Windows Final' if final else 'Deliverables/Windows');out=r/('Deliverables/Jay A Career - Windows Final.zip' if final else 'Deliverables/Jay A Career - Windows Build.zip')
if fresh:
 folder=r/'Deliverables/Windows Fresh 20261015';out=r/'Deliverables/Jay A Career - Windows Full Build.zip'
if labeled:
 folder=r/'Deliverables/Windows Labeled 20261015';out=r/'Deliverables/Jay A Career - Windows Labeled.zip'
shutil.copy2(r/'REALISM_ASSET_CREDITS.md',folder/'REALISM_ASSET_CREDITS.md')
(folder/'READ ME.txt').write_text('Jay A Career - Realism revision\nRun Jay A Career.exe. Keep all accompanying folders and DLL files together.\n\nWASD/mouse: move/look. T: talk/continue briefing. O/Q: door open/close.\nO: preparation, lab switch or bed according to target. L: carry/place. R: rotate.\nCustomer dialogue: A accepts $50, C counters $75 with 50% chance, R rejects.\nReturn any unsold package to the lab return shelf, C at the sink cleans, then O at bed.\nV: first/third person. F: flashlight. Esc: pause.\n\nThis build completed a two-day scripted gameplay route. Interactive cursor/camera feel and visible 1080p performance acceptance are pending. Final source includes later testing/capture changes; an exact-source rebuild remains pending.\nNo persistent saves, driving or combat. Asset sources and licenses are in the credits.\n',encoding='utf8')
if final:
 (folder/'READ ME.txt').write_text('Jay A Career - Windows Final\nRun Jay A Career.exe. Keep every accompanying folder and DLL together.\n\nWASD/mouse move/look; T talk/continue; O/Q open/close door.\nO prepare/switch lights/sleep according to target; L carry/place; R rotate.\nCustomer: A accepts $50; C counters $75 with 50% acceptance; R rejects.\nReturn an unsold package, C at sink to clean, then O at bed.\nV first/third person; F flashlight; Esc pause; Resume recaptures mouse.\n\nRelease method: latest Unity-compiled Mono gameplay DLL on previously built matching scene data. A fresh full Unity BuildPipeline rebuild remains blocked by suspended metadata subprocesses on this machine. This is not labelled as a successful clean rebuild.\nValidation: 40 player input/physics checks passed; two-day visible 1080p route reached Day 3/$150; average 59.4 FPS, 1% low 59.2 FPS on Ryzen 7 4800H / GTX 1650. Physical keyboard/mouse feel still requires user review.\nNo persistent saves, driving or combat. See REALISM_ASSET_CREDITS.md for licenses.\n',encoding='utf8')
if fresh or labeled:
 route=json.loads((r/('Deliverables/Evidence/windows-labeled-route.json' if labeled else 'Deliverables/Evidence/windows-fresh-route.json')).read_text(encoding='utf8'))
 (folder/'READ ME.txt').write_text(f'''Jay A Career - Fresh Full Windows Build, 15 October 2026
Run Jay A Career.exe. Keep all accompanying folders and DLLs together.

WASD/mouse move/look; T talk/continue; O/Q open/close door.
O prepare/switch lights/sleep according to target; L carry/place; R rotate.
Customer: A accepts $50; C counters $75 with 50% acceptance; R rejects.
Return an unsold package, C at sink to clean, then O at bed.
V first/third person; F flashlight; Esc pause; Resume recaptures mouse.

Fresh full Unity BuildPipeline build succeeded: zero errors, one warning.
Validation: 40 automated player input/physics checks passed; visible two-day
1080p route reached Day 3/$150. Average {route['averageFps']:.1f} FPS, 1% low
{route['percentile1Fps']:.1f} FPS on Ryzen 7 4800H / GTX 1650.
Physical movement/pause/camera/door checks passed on the preceding full build.
The labeled version needs the targeted lift/place and lighting review described
in MANUAL PLAYER CHECKS.md. Yellow notes identify stations and all liftable props.
Lab fixtures start off; O at the labeled switch controls them independently of F.
No persistent saves, driving or combat. Asset licenses are in the credits.
''',encoding='utf8')
 shutil.copy2(r/'Deliverables/MANUAL PLAYER CHECKS.md',folder/'MANUAL PLAYER CHECKS.md')
files=[p for p in folder.rglob('*') if p.is_file() and not any(x in p.relative_to(folder).parts for x in ['Evidence','Deliverables','Jay A Career_BurstDebugInformation_DoNotShip']) and p.suffix!='.log']
with zipfile.ZipFile(out,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as z:
 for p in files:z.write(p,p.relative_to(folder))
with zipfile.ZipFile(out) as z:assert z.testzip() is None
(r/('Deliverables/Evidence/windows-labeled-package-check.json' if labeled else 'Deliverables/Evidence/windows-fresh-package-check.json' if fresh else 'Deliverables/Evidence/windows-final-package-check.json' if final else 'Deliverables/Evidence/windows-package-check.json')).write_text(json.dumps(dict(path=str(out),files=len(files),bytes=out.stat().st_size,sha256=hashlib.sha256(out.read_bytes()).hexdigest(),crc_verified=True),indent=2),encoding='utf8')
print(out,len(files),'files')
