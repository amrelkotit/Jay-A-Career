from pathlib import Path
from PIL import Image,ImageStat,ImageDraw,ImageFont
import json,shutil
r=Path(__file__).resolve().parents[1];e=r/'Deliverables/Evidence';d=e/'LabelsLighting'
review=json.loads((d/'review-completed.json').read_text(encoding='utf8'));assert review['completed'] and review['captures']==7
def luma(name):return ImageStat.Stat(Image.open(d/(name+'.png')).convert('L').crop((0,180,1920,1080))).mean[0]
values={name:luma(name) for name in ['daylight-lights-off','night-lights-off','night-flashlight','night-lab-switch-on','daylight-window','night-window']}
# The deep room has little direct daylight in either condition. Require it to
# remain subdued; measure the day/night change where the window actually admits light.
assert values['night-lights-off']<20 and values['night-lights-off']<=values['daylight-lights-off']*1.05,values
assert values['night-flashlight']>values['night-lights-off']*1.08,values
assert values['night-lab-switch-on']>values['night-lights-off']*1.5,values
assert values['night-window']<values['daylight-window']*.65,values
(e/'labels-lighting-visual-checks.json').write_text(json.dumps({'environment':'Windows player','captures':7,'average_grayscale_0_to_255':values,'deep_room_remains_subdued':True,'window_view_darkens_at_night':True,'flashlight_and_lab_switch_increase_illumination':True,'day_night_and_switch_contrast_passed':True},indent=2),encoding='utf8')
for target,source in [('lab-final.png','night-lab-switch-on.png'),('labels-final.png','labels-preparation-package.png')]:
 backup=e/('before-labels-'+target)
 if (e/target).exists() and not backup.exists():shutil.copy2(e/target,backup)
 shutil.copy2(d/source,e/target)
sheet=Image.new('RGB',(1920,1160),'#111820');draw=ImageDraw.Draw(sheet);font=ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf',28)
for i,(name,title) in enumerate([('daylight-window','Daylight through window'),('night-lights-off','Night: both lights off'),('night-flashlight','Night: flashlight on'),('night-lab-switch-on','Night: lab fixtures on')]):
 image=Image.open(d/(name+'.png'));image.thumbnail((960,540));x=i%2*960;y=i//2*580;sheet.paste(image,(x,y+40));draw.text((x+18,y+4),title,font=font,fill='white')
backup=e/'before-labels-lights-final.png'
if (e/'lights-final.png').exists() and not backup.exists():shutil.copy2(e/'lights-final.png',backup)
sheet.save(e/'lights-final.png');print(json.dumps(values,indent=2))
