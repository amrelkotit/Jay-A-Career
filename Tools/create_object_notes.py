"""Original raster lettering for physical yellow asset labels; no font files redistributed."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json
r=Path(__file__).resolve().parents[1];out=r/'Assets/JayACareer/Textures/ObjectNotes';out.mkdir(parents=True,exist_ok=True)
labels={
 'PreparationBench':('Preparation bench','O: prepare package','Briefing with Dr. Hale first'),
 'InstrumentBench':('Instrument table','Laboratory instruments','Fixed work surface'),
 'GlasswareBench':('Glassware table','Glassware & test tubes','Fixed work surface'),
 'CleaningSink':('Cleaning station','C: clean workspace','After delivery is resolved'),
 'ReturnShelfSupport':('Return table','Unsold packages only','Place package on return tray'),
 'ReturnShelf':('Return tray','L: return package','After a refused / rejected deal'),
 'CustomerCounter':('Delivery counter','L: place package here','Then T: talk to customer'),
 'BedroomDesk':('Bedroom desk','Personal workspace','Fixed furniture'),
 'BedroomBed':('Bed','O: sleep / next day','Finish cleaning first'),
 'LabLightSwitch':('Lab lights','O: switch ON / OFF','F controls your flashlight'),
 'BedroomChair':('Bedroom chair','O: arrange chair','Enter: confirm / Esc: cancel'),
 'SealedDeliveryPackage':('Delivery package','L: lift / place','R: rotate while carrying'),
 'CarryableCrate':('Supply crate','L: lift / place','R: rotate while carrying'),
 'CarryableEmptyDrum':('Empty drum','L: lift / place','R: rotate while carrying'),
 'SupplyCarton':('Supply carton','L: lift / place','R: rotate while carrying'),
}
for key,lines in labels.items():
 im=Image.new('RGB',(1024,576),(246,216,106));d=ImageDraw.Draw(im)
 d.rectangle((0,0,1023,65),fill=(231,196,77));d.line((45,80,979,80),fill=(202,169,63),width=3)
 d.text((54,12),'JAY A CAREER  /  SITE NOTES',font=ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf',27),fill=(73,60,28))
 for text,y,maximum in [(lines[0],123,65),(lines[1],282,49),(lines[2],405,35)]:
  size=maximum
  while d.textbbox((0,0),text,font=ImageFont.truetype('C:/Windows/Fonts/segoepr.ttf',size))[2]>920:size-=1
  font=ImageFont.truetype('C:/Windows/Fonts/segoepr.ttf',size)
  d.text((52,y),text,font=font,fill=(37,35,24),stroke_width=0)
 im.save(out/(key+'.png'))
(r/'Deliverables/Evidence/object-note-catalogue.json').write_text(json.dumps(labels,indent=2),encoding='utf8')
print('Created',len(labels),'original yellow note textures')
