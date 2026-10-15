import bpy
from pathlib import Path
p=Path.cwd();bpy.ops.wm.open_mainfile(filepath=str(p/'ArtSource/DrHale.blend'))
o=bpy.data.objects['DrHaleClothing'];adj={v.index:set() for v in o.data.vertices}
for e in o.data.edges:a,b=e.vertices;adj[a].add(b);adj[b].add(a)
seen=set()
for v in adj:
 if v in seen:continue
 stack=[v];part=set()
 while stack:
  n=stack.pop()
  if n in part:continue
  part.add(n);stack.extend(adj[n]-part)
 seen|=part
 if len(part)>20:
  xyz=[o.data.vertices[i].co for i in part];print('GARMENT',len(part),[(min(v[a] for v in xyz),max(v[a] for v in xyz)) for a in range(3)],flush=True)
