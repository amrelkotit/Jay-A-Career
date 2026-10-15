"""Inspect a fresh run; encode only its captured frames, never stale earlier takes."""
from pathlib import Path
import json,shutil,subprocess,sys
from PIL import Image,ImageDraw
r=Path(__file__).resolve().parents[1];source=r/('Deliverables/Windows Labeled 20261015/Evidence/Demo/route-report.json' if '--labeled' in sys.argv else 'Deliverables/Windows/Evidence/Demo/route-report.json' if '--player' in sys.argv else 'Evidence/Demo/route-report.json');report=json.loads(source.read_text(encoding='utf8'))
folder=Path(report['directory']);frames=sorted(folder.glob('frame-*.jpg'))[:report['frames']]
assert report['completed'] and not report['error'] and len(frames)==report['frames']
out=r/'Deliverables/Evidence';out.mkdir(exist_ok=True)
shutil.copy2(folder/'route-report.json',out/'demonstration-route-report.json')
if '--contacts' in sys.argv:
 chosen=list(range(0,len(frames),35))
 for start in range(0,len(chosen),20):
  sheet=Image.new('RGB',(1600,1025),'#dddddd');d=ImageDraw.Draw(sheet)
  for j,index in enumerate(chosen[start:start+20]):
   im=Image.open(frames[index]);im.thumbnail((390,220));x=j%4*400;y=j//4*205
   im.thumbnail((390,180));sheet.paste(im,(x,y+20));d.text((x+8,y+4),str(index),fill='black')
  sheet.save(out/f'demo-contact-{start:03d}.png')
if '--encode' in sys.argv:
 exe=Path('E:/Setups/ffmpeg/bin/ffmpeg.exe');video=r/'Deliverables/Jay A Career - Demonstration.mp4'
 subprocess.run([str(exe),'-y','-framerate','10','-i',str(folder/'frame-%05d.jpg'),'-frames:v',str(len(frames)),'-c:v','libx264','-preset','medium','-crf','20','-pix_fmt','yuv420p','-movflags','+faststart',str(video)],check=True)
 print(video)
print('Fresh run',folder,'frames',len(frames))
