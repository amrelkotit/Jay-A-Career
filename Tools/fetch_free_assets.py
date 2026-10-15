"""Download only official CC0 assets and portable authoring tools; preserve source metadata."""
from pathlib import Path
import urllib.request, json, re, zipfile, hashlib, concurrent.futures

ROOT = Path(__file__).resolve().parents[1]
CACHE = ROOT / 'Tools' / 'Downloads'
CACHE.mkdir(parents=True, exist_ok=True)
HEADERS = {'User-Agent': 'JayACareer-StudentProject/1.0 (educational asset integration)'}
def get(url):
    return urllib.request.urlopen(urllib.request.Request(url, headers=HEADERS), timeout=120).read()
def download(url, path, md5=None):
    path.parent.mkdir(parents=True, exist_ok=True)
    if path.exists() and (not md5 or hashlib.md5(path.read_bytes()).hexdigest()==md5):
        return path
    data = get(url)
    if md5 and hashlib.md5(data).hexdigest()!=md5: raise ValueError('Hash mismatch: '+url)
    path.write_bytes(data)
    print('Downloaded', path.name, len(data), flush=True)
    return path
def texture(asset):
    metadata=json.loads(get('https://api.polyhaven.com/files/'+asset))
    info=json.loads(get('https://api.polyhaven.com/info/'+asset))
    target=ROOT/'Assets/JayACareer/Textures/PolyHaven'/asset
    target.mkdir(parents=True,exist_ok=True)
    (target/'source.json').write_text(json.dumps({'source':'https://polyhaven.com/a/'+asset,'license':'CC0-1.0','info':info,'files':metadata},indent=2))
    for key in ['Diffuse','diff','nor_gl','Rough','rough','AO','Metal','metal']:
        if key not in metadata: continue
        sizes=metadata[key]
        if '1k' not in sizes: continue
        options=sizes['1k']; fmt='png' if key=='nor_gl' and 'png' in options else 'jpg' if 'jpg' in options else 'png'
        item=options[fmt]
        download(item['url'],target/Path(item['url']).name,item.get('md5'))
def model(asset):
    metadata=json.loads(get('https://api.polyhaven.com/files/'+asset))
    info=json.loads(get('https://api.polyhaven.com/info/'+asset))
    target=CACHE/'PolyHaven'/asset; target.mkdir(parents=True,exist_ok=True)
    (target/'source.json').write_text(json.dumps({'source':'https://polyhaven.com/a/'+asset,'license':'CC0-1.0','info':info,'files':metadata},indent=2))
    options=metadata.get('gltf',{}).get('1k',{})
    if not options: raise ValueError('No glTF 1k for '+asset)
    item=next(iter(options.values())) if 'url' not in options else options
    download(item['url'],target/Path(item['url']).name,item.get('md5'))
    for rel,data in item.get('include',{}).items(): download(data['url'],target/rel,data.get('md5'))
def blender():
    index=get('https://download.blender.org/release/Blender4.5/').decode()
    versions=re.findall(r'href="(blender-(4\.5\.\d+)-windows-x64\.zip)"',index)
    name,version=max(versions,key=lambda v:tuple(map(int,v[1].split('.'))))
    p=download('https://download.blender.org/release/Blender4.5/'+name,CACHE/name)
    dest=ROOT/'Tools/Blender'
    if not (dest/name[:-4]/'blender.exe').exists():
        with zipfile.ZipFile(p) as z:z.extractall(dest)
    print('BLENDER',str(dest/name[:-4]/'blender.exe'),flush=True)
def mpfb():
    p=download('https://github.com/makehumancommunity/mpfb2/archive/refs/tags/v2.0.18.zip',CACHE/'mpfb2-v2.0.18.zip')
    with zipfile.ZipFile(p) as z:z.extractall(ROOT/'Tools/MPFB')
    for pack in ['makehuman_system_assets','skins02','hair01','shirts01','pants01','shoes01']:
        page=get('https://static.makehumancommunity.org/assets/assetpacks/'+pack+'.html').decode()
        urls=re.findall(r'href="(https://files2.makehumancommunity.org/[^\"]+_cc0.zip)"',page)
        if not urls: raise ValueError('CC0 pack link not found: '+pack)
        p=download(urls[0],CACHE/Path(urls[0]).name)
        with zipfile.ZipFile(p) as z:z.extractall(ROOT/'Tools/MPFB/Assets')
def safe(task):
    try: task()
    except Exception as e: print('ERROR',str(e),flush=True)
if __name__=='__main__':
    import sys
    if '--audio' in sys.argv:
        for pack in ['impact-sounds','interface-sounds']:
            page=get('https://kenney.nl/assets/'+pack).decode()
            links=re.findall(r'(https://kenney.nl/media/[^\"\s<>]+\.zip)',page)
            if not links:raise ValueError('Official audio download not found '+pack)
            p=download(links[0],CACHE/Path(links[0]).name)
            with zipfile.ZipFile(p) as z:z.extractall(ROOT/'Assets/JayACareer/Audio/Kenney'/pack)
        raise SystemExit()
    if '--textures-only' in sys.argv:
        for a in ['asphalt_03','concrete_floor','sand_03','rusty_painted_metal']:safe(lambda a=a:texture(a))
        raise SystemExit()
    tasks=[blender,mpfb]
    tasks += [lambda a=a:texture(a) for a in ['asphalt_03','concrete_floor','sand_03','rusty_painted_metal']]
    tasks += [lambda a=a:model(a) for a in ['steel_frame_shelves_01','Barrel_02','cardboard_box_01','street_lamp_01','utility_box_02','modular_chainlink_fence','modular_electricity_poles','exterior_aircon_unit']]
    with concurrent.futures.ThreadPoolExecutor(max_workers=3) as pool:list(pool.map(safe,tasks))
