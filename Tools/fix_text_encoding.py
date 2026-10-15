from pathlib import Path
root=Path(__file__).resolve().parents[1]
for name in ['create_map.py','create_gdd.py']:
    path=root/'Tools'/name
    s=path.read_text(encoding='utf8')
    for original in ['•','×','−','…','–','—','→','’','é']:
        try: broken=original.encode('utf8').decode('cp1252')
        except UnicodeError: continue
        s=s.replace(broken,original)
    if name=='create_map.py':
        s=s.replace("'#cedbd8','LABORATORY\\n12 × 12 m'","'#cedbd8'").replace("'#d2d1c9','CUSTOMER APRON\\n12 × 4 m'","'#d2d1c9'")
        s=s.replace("t(*P(4.5,19.8)","t(*P(4.5,21.2)")
    else:
        s=s.replace("f.text='Amr Hazem El Kotit • 58-14399                                          '","f.paragraph_format.tab_stops.add_tab_stop(Inches(6.65),WD_TAB_ALIGNMENT.RIGHT);f.text='Amr Hazem El Kotit • 58-14399\\t'")
        s=s.replace("if path.exists():D.add_picture(str(path),width=Inches(width))","if path.exists():D.add_picture(str(path),width=Inches(min(width,5.3) if name.startswith('Evidence/') else width))")
    path.write_text(s,encoding='utf8')
print('Source text encoding corrected')
