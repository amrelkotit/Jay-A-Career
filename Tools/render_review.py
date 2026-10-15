from pathlib import Path
import pypdfium2 as pdfium
from PIL import Image,ImageDraw,ImageFont
r=Path.cwd();p=r/'Deliverables/Jay A Career - Assignment 2 Revised GDD.pdf';pdf=pdfium.PdfDocument(str(p));out=r/'Deliverables/Document Review';out.mkdir(exist_ok=True)
for i,page in enumerate(pdf):
 im=page.render(scale=1.5).to_pil();im.save(out/f'page-{i+1:02d}.png')
for start in range(0,len(pdf),6):
 sheet=Image.new('RGB',(1350,1350),'#d9d9d9');d=ImageDraw.Draw(sheet)
 for n in range(start,min(start+6,len(pdf))):
  im=Image.open(out/f'page-{n+1:02d}.png');im.thumbnail((430,625));x=((n-start)%3)*450;y=((n-start)//3)*675;sheet.paste(im,(x,y+25));d.text((x+10,y+5),str(n+1),fill='black')
 sheet.save(out/f'contact-{start+1:02d}.png')
print('Rendered',len(pdf),'pages')
