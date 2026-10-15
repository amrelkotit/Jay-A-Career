"""Read ZIP directories with HTTP byte ranges; obtain selected official CC0 pack assets."""
import io,json,urllib.request,zipfile,sys,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
class RemoteZip(io.RawIOBase):
    def __init__(self,url):
        self.url=url;self.pos=0
        with urllib.request.urlopen(urllib.request.Request(url,method='HEAD'),timeout=30) as r:self.length=int(r.headers['Content-Length'])
    def seekable(self):return True
    def tell(self):return self.pos
    def seek(self,offset,whence=0):
        self.pos=offset if whence==0 else self.pos+offset if whence==1 else self.length+offset
        return self.pos
    def read(self,size=-1):
        if size<0:size=self.length-self.pos
        if size==0:return b''
        end=min(self.length,self.pos+size)-1
        req=urllib.request.Request(self.url,headers={'Range':f'bytes={self.pos}-{end}','User-Agent':'JayACareer-StudentProject/1.0'})
        with urllib.request.urlopen(req,timeout=45) as r:
            if r.status!=206:raise RuntimeError('Server did not support byte-range ZIP access')
            data=r.read()
        self.pos+=len(data);return data
def open_pack(pack):
    page=urllib.request.urlopen('https://static.makehumancommunity.org/assets/assetpacks/'+pack+'.html',timeout=30).read().decode()
    links=re.findall(r'href="(https://files.makehumancommunity.org/[^\"]+_cc0.zip)"',page)
    if not links:raise RuntimeError('Official CC0 link not found '+pack)
    return zipfile.ZipFile(RemoteZip(links[0]))
if __name__=='__main__':
    pack=sys.argv[1] if len(sys.argv)>1 else 'makehuman_system_assets'
    z=open_pack(pack);names=z.namelist()
    path=ROOT/'Tools'/('pack-inventory-'+pack+'.json');path.write_text(json.dumps(names,indent=2))
    print(pack,len(names),flush=True)
    for n in names:
        if n.endswith(('.mhclo','.mhmat')):print(n,flush=True)
    if '--extract' in sys.argv:
        filters=sys.argv[sys.argv.index('--extract')+1:]
        for name in names:
            if not filters or any(f in name for f in filters):
                if name.endswith('/'):continue
                dest=ROOT/'Tools/MPFB/Assets'/name
                if dest.exists() and dest.stat().st_size==z.getinfo(name).file_size:continue
                dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(z.read(name));print('Extracted',name,flush=True)
