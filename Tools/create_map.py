from PIL import Image,ImageDraw,ImageFont
from pathlib import Path
root=Path.cwd();im=Image.new('RGB',(2200,1530),'#f5f2e9');d=ImageDraw.Draw(im)
def font(n,b=False):return ImageFont.truetype('C:/Windows/Fonts/'+('arialbd.ttf' if b else 'arial.ttf'),n)
def t(x,y,s,n=24,c='#203b3b',b=False):d.multiline_text((x,y),s,font=font(n,b),fill=c,spacing=6)
scale=43;ox=130;oy=1360
P=lambda x,z:(ox+(x+22)*scale,oy-(z+3)*scale)
def box(x,z,w,h,c,label=''):
 a=P(x,z+h);bb=P(x+w,z);d.rectangle([a,bb],fill=c,outline='#384849',width=3)
 if label:
  bounds=d.multiline_textbbox((0,0),label,font=font(25,True),spacing=6);t((a[0]+bb[0]-bounds[2])/2,(a[1]+bb[1]-bounds[3])/2,label,25,b=True)
t(130,65,'JAY A CAREER  |  AUTHORED SITE PLAN',46,b=True)
t(130,128,'One Unity unit = one metre   •   Roofs omitted to show the four playable zones',25)
box(-22,-3,44,27,'#ded1b5');box(-1.5,-3,5,27,'#a4acad');box(-13,5,12,12,'#cedbd8');box(7,5.5,7,6,'#e5c9a3','BEDROOM\n7 × 6 m');box(4.5,12.5,12,4,'#d2d1c9');box(13.3,16.5,8,3,'#adb8b3','Warehouse facade');box(-11.5,1.7,10,2,'#d2d1c9','Lab access path')
t(*P(-10.5,19.8),"LABORATORY  12 × 12 m",32,b=True)
t(*P(4.5,21.2),"CUSTOMER APRON  12 × 4 m",30,b=True)
for x,z,label,dx,dy in [(-10.5,14.8,'P  Preparation',-70,-45),(-6.5,12.7,'H  Dr. Hale',-45,-45),(-2.85,11.5,'R  Return shelf',-170,30),(-4,7.1,'C  Cleaning',-100,-42),(9,14,'D  Counter',-70,-30),(12.7,9.4,'B  Bed',-90,-30)]:
 px,pz=P(x,z);d.ellipse((px-8,pz-8,px+8,pz+8),fill='#a65e30');t(px+dx,pz+dy,label,23,b=True)
points=[P(x,z) for x,z in [(-7,11.4),(-7,3.3),(1,2.7),(5.5,3.4),(5.5,12.2),(9,12.5)]];d.line(points,fill='#286d77',width=6)
for x in range(-20,21,5):px,pz=P(x,-3);t(px-20,pz+15,str(x),22)
for z in range(0,25,5):px,pz=P(-22,z);t(px-55,pz-14,str(z),22)
for x,z,w in [(-8.05,5,2),(7,5.5,1.4)]:d.line([P(x,z),P(x+w,z)],fill='#a65e30',width=9)
t(*P(.05,21),'ROAD\n5 m wide',22)
px,pz=P(19,21);t(px-10,pz-100,'N',32,b=True);d.line((px,pz,px,pz-55),fill='#203b3b',width=5);d.polygon([(px,pz-65),(px-10,pz-45),(px+10,pz-45)],fill='#203b3b')
d.line([P(-20,0),P(-15,0)],fill='#203b3b',width=5);t(*P(-20,1),'5 metres',22)
t(130,1430,'Playable bounds: X −22…22 m, Z −3…24 m. Blue route: delivery walk; return follows the reverse route.\nMeasured from JayACareer_Realism authoring coordinates. Orange segments mark entrances.',25)
im.save(root/'Deliverables/Jay A Career - Revised World Map.png');print('Map saved')
