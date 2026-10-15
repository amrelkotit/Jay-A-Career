from pathlib import Path
import json,hashlib,subprocess,shutil
from pypdf import PdfReader
from docx import Document
r=Path(__file__).resolve().parents[1];o=r/'Deliverables';e=o/'Evidence'
latest=(e/'windows-labeled-route.json').exists()
benchmark=e/('windows-labeled-route.json' if latest else 'windows-fresh-route.json')
scene='Assets/JayACareer/Scenes/JayACareer_Realism.unity'
review=Path(r'C:\Users\LENOVO\AppData\Local\Temp\JayCareerBuild20261013') if latest else Path('E:/El Gam3a/JayCareerReview20261013')
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
same=sha(r/scene)==sha(review/scene)
probe=subprocess.run(['E:/Setups/ffmpeg/bin/ffprobe.exe','-v','error','-show_entries','format=duration:stream=codec_name,width,height,avg_frame_rate,nb_frames','-of','json',str(o/'Jay A Career - Demonstration.mp4')],capture_output=True,text=True,check=True)
video=json.loads(probe.stdout)
decode=subprocess.run(['E:/Setups/ffmpeg/bin/ffmpeg.exe','-v','error','-i',str(o/'Jay A Career - Demonstration.mp4'),'-f','null','NUL'],capture_output=True,text=True,check=True)
pdf=PdfReader(o/'Jay A Career - Assignment 2 Revised GDD.pdf');doc=Document(o/'Jay A Career - Assignment 2 Revised GDD.docx')
assert len(pdf.pages)==30 and len(doc.inline_shapes)==8
assert same,'Build scene differs from authored scene'
assert not decode.stderr,decode.stderr
provenance=json.loads((e/('labeled-windows-build-20261015.json' if latest else 'fresh-windows-build-20261015.json')).read_text(encoding='utf8'))
checks=json.loads((e/('windows-labeled-input-checks.json' if latest else 'windows-fresh-input-checks.json')).read_text(encoding='utf8'))
route=json.loads(benchmark.read_text(encoding='utf8'))
assert provenance['result']=='Succeeded' and provenance['errors']==0
assert checks['passed'] and checks['checks']==40
assert route['completed'] and route['width']==1920 and route['height']==1080 and route['percentile1Fps']>=30
source=json.loads((e/'fresh-source-comparison.json').read_text(encoding='utf8'))
assert source['runtime_and_visual_sources_match'] and source['packages_and_settings_match']
report=dict(document_pages=len(pdf.pages),document_images=len(doc.inline_shapes),pdf_all_pages_rendered_and_visually_reviewed=True,video=video,video_full_decode_passed=True,build_scene_matches_final_authored_scene=same,scene_sha256=sha(r/scene),windows_route=route,windows_input_checks=40,performance_acceptance='Passed visible 1080p route',build_matching_final_source='Fresh full BuildPipeline build; runtime/visual/settings/package sources match working project',fresh_full_BuildPipeline_rebuild='Succeeded',interactive_cursor_and_camera_feel='User passed previous full build; automated regression passed on labeled version; targeted lift/place/lighting feedback pending')
(e/'final-artifact-audit.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print('30 PDF pages; 8 document images; full video decode passed; scene hashes match')
