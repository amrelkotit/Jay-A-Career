"""Reproducible Blender authoring and CC0 glTF -> FBX adaptation (metres)."""
import bpy, math, json, shutil
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/JayACareer/Models/Realism';OUT.mkdir(parents=True,exist_ok=True)
TEX=ROOT/'Assets/JayACareer/Textures/Realism';TEX.mkdir(parents=True,exist_ok=True)
SOURCE=ROOT/'ArtSource';SOURCE.mkdir(exist_ok=True)
catalog=[]
def reset():
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
def mat(name,color,metal=0,rough=.5,alpha=1):
    m=bpy.data.materials.get(name) or bpy.data.materials.new(name);m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,alpha)
    p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
    if name=='OpticalGlass':p.inputs['Transmission Weight'].default_value=.8
    if name=='ScreenGlow':p.inputs['Emission Color'].default_value=(*color,1);p.inputs['Emission Strength'].default_value=.6
    return m
steel=mat('BrushedSteel',(.48,.53,.55),.8,.3)
paint=mat('LabEnamel',(.65,.71,.69),.2,.45)
dark=mat('Graphite',(.045,.06,.065),.3,.6)
rubber=mat('RubberBlack',(.015,.02,.02),0,.85)
glass=mat('OpticalGlass',(.55,.78,.8),0,.12,.35)
amber=mat('AmberGlass',(.3,.13,.04),0,.25,.75)
wood=mat('DeskWood',(.22,.12,.06),0,.65)
fabric=mat('BedLinen',(.52,.57,.53),0,.95)
blue=mat('InstrumentBlue',(.04,.18,.22),.25,.4)
orange=mat('SafetyOrange',(.85,.29,.055),.1,.7)
white=mat('LabelPaper',(.9,.91,.85),0,.8)
screen=mat('ScreenGlow',(.03,.24,.28),.1,.3)
def finish(obj,name,material,bevel=0):
    obj.name=name
    if material:obj.data.materials.append(material)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        b=obj.modifiers.new('Manufactured edge radii','BEVEL');b.width=bevel;b.segments=2
        bpy.context.view_layer.objects.active=obj;bpy.ops.object.modifier_apply(modifier=b.name)
    for face in obj.data.polygons:face.use_smooth=False
    return obj
def box(name,pos,size,material,bevel=.008):
    bpy.ops.mesh.primitive_cube_add(size=1,location=pos);obj=bpy.context.object;obj.scale=size
    return finish(obj,name,material,bevel)
def cyl(name,pos,radius,depth,material,rotation=(0,0,0),vertices=24):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=radius,depth=depth,location=pos,rotation=rotation)
    obj=finish(bpy.context.object,name,material,.003)
    return obj
def lathe(name,profile,material):
    count=32;vs=[];faces=[]
    for r,z in profile:
        for i in range(count):vs.append((r*math.cos(i*math.tau/count),r*math.sin(i*math.tau/count),z))
    for j in range(len(profile)-1):
        for i in range(count):a=j*count+i;b=j*count+(i+1)%count;faces.append((a,b,b+count,a+count))
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(vs,[],faces);mesh.update()
    obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj);obj.data.materials.append(material)
    for f in mesh.polygons:f.use_smooth=True
    return obj
def export(name,source='Original Blender authoring',simplify=None):
    for obj in list(bpy.context.scene.objects):
        if obj.type=='MESH' and simplify and len(obj.data.polygons)>simplify:
            bpy.context.view_layer.objects.active=obj
            m=obj.modifiers.new('Game mesh reduction','DECIMATE');m.ratio=simplify/len(obj.data.polygons)
            bpy.ops.object.modifier_apply(modifier=m.name)
    records=[]
    for m in {m for obj in bpy.context.scene.objects if obj.type=='MESH' for m in obj.data.materials if m}:
        p=next((n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None) if m.use_nodes else None
        row={'name':m.name,'color':list(p.inputs['Base Color'].default_value) if p else list(m.diffuse_color),'metallic':p.inputs['Metallic'].default_value if p else 0,'roughness':p.inputs['Roughness'].default_value if p else .5,'textures':{}}
        if p:
            for field in ['Base Color','Normal','Roughness','Metallic','Alpha']:
                if not p.inputs[field].is_linked:continue
                n=p.inputs[field].links[0].from_node
                if n.type in ['NORMAL_MAP','SEPARATE_COLOR','SEPRGB']:
                    linked=next((s.links[0].from_node for s in n.inputs if s.is_linked),None);n=linked or n
                if n.type=='TEX_IMAGE' and n.image:
                    path=Path(bpy.path.abspath(n.image.filepath))
                    if path.exists():
                        dest=TEX/path.name
                        if path.resolve()!=dest.resolve():shutil.copy2(path,dest)
                        row['textures'][field]=dest.name
        records.append(row)
    (OUT/(name+'.materials.json')).write_text(json.dumps(records,indent=2))
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH','ARMATURE'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False,path_mode='AUTO')
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/(name+'.blend')))
    triangles=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in bpy.context.scene.objects if o.type=='MESH')
    catalog.append({'name':name,'source':source,'license':'CC0-1.0' if source.startswith('https:') else 'Original project work','triangles':triangles})
def bench(sink=False):
    box('EnamelWorktop',(0,0,.875),(2.4,.85,.05),steel)
    for x in [-.92,.92]:
        box('CabinetCarcass',(x,0,.43),(.5,.72,.84),paint)
        for z in [.19,.42,.65]:
            box('DrawerFront',(x,-.373,z),(.47,.03,.21),paint)
            box('DrawerPull',(x,-.405,z+.035),(.22,.025,.018),steel,.004)
        for y in [-.26,.26]:cyl('AdjustableFoot',(x,y,.035),.035,.07,rubber)
    if sink:
        box('SinkBasin',(0,0,.86),(.65,.48,.08),dark,.035)
        box('SinkInterior',(0,0,.902),(.53,.36,.012),steel,.025)
        cyl('TapStem',(0,.29,1.02),.022,.28,steel)
        cyl('TapSpout',(0,.20,1.15),.018,.18,steel,(math.pi/2,0,0))
def author():
    reset();bench();export('LabBench')
    reset();bench(True);export('SinkBench')
    reset()
    box('HoodBase',(0,0,.9),(1.45,.85,.08),steel)
    for x in [-.68,.68]:box('SidePanel',(x,.05,1.5),(.06,.8,1.2),paint)
    box('BackPanel',(0,.43,1.5),(1.4,.06,1.2),paint)
    box('ExhaustHousing',(0,0,2.13),(1.5,.9,.18),paint)
    box('SashFrame',(0,-.4,1.85),(1.34,.03,.15),dark)
    box('SashGlass',(0,-.39,1.73),(1.3,.012,.35),glass)
    cyl('ExtractionDuct',(0,.15,2.38),.12,.4,steel)
    for x in [-.57,.57]:box('Support',(x,0,.45),(.06,.6,.9),steel)
    export('FumeHood')
    reset();box('Cabinet',(0,0,.45),(1.2,.36,.9),paint)
    for x in [-.3,.3]:
        box('Door',(x,-.195,.45),(.57,.025,.86),paint)
        box('Handle',(x+(-.2 if x>0 else .2),-.22,.45),(.02,.02,.16),steel)
    export('WallCabinet')
    reset();box('Instrument',(0,0,.12),(.46,.4,.24),paint,.025)
    cyl('Lid',(0,.02,.25),.16,.025,blue)
    box('ControlPanel',(0,-.205,.14),(.32,.02,.09),dark)
    box('Display',(-.06,-.22,.14),(.13,.006,.055),screen)
    for x in [.06,.12]:cyl('Button',(x,-.22,.14),.015,.01,orange,(math.pi/2,0,0))
    export('Centrifuge')
    reset();box('BalanceBase',(0,0,.04),(.32,.24,.08),paint)
    cyl('WeighingPan',(0,.035,.095),.1,.025,steel)
    box('Readout',(0,-.126,.046),(.18,.01,.035),screen)
    export('PrecisionBalance')
    reset();box('RackBase',(0,0,.015),(.36,.12,.03),blue)
    for x in [-.15,.15]:box('RackUpright',(x,0,.075),(.02,.12,.15),blue)
    box('RackTop',(0,0,.12),(.36,.12,.02),blue)
    for x in [-.11,-.055,0,.055,.11]:
        tube=lathe('TestTube',[(.012,.04),(.015,.05),(.015,.21),(.012,.21),(.012,.06)],glass);tube.location.x=x
        cyl('TubeCap',(x,0,.215),.016,.014,orange)
    export('TestTubeRack')
    reset();lathe('Beaker',[(0,0),(.075,0),(.08,.01),(.08,.20),(.073,.20),(.073,.012),(0,.012)],glass)
    for z in [.05,.1,.15]:box('Graduation',(.076,-.02,z),(.002,.035,.002),white,.0)
    export('GraduatedBeaker')
    reset();lathe('ConicalFlask',[(0,0),(.09,0),(.095,.02),(.035,.18),(.028,.2),(.028,.27),(.022,.27),(.022,.2),(.08,.02),(0,.015)],glass)
    export('ConicalFlask')
    reset();lathe('AmberBottle',[(0,0),(.045,0),(.045,.13),(.022,.16),(.022,.20),(0,.20)],amber)
    cyl('SealedCap',(0,0,.21),.026,.025,dark)
    box('BlankSafetyLabel',(0,-.046,.085),(.055,.002,.065),white,.0)
    export('ReagentBottle')
    reset();cyl('Seat',(0,0,.6),.18,.045,rubber)
    cyl('Column',(0,0,.32),.025,.58,steel)
    for angle in range(0,360,90):
        a=math.radians(angle);o=box('Foot',(math.cos(a)*.1,math.sin(a)*.1,.04),(.25,.025,.025),steel);o.rotation_euler.z=a
    export('LabStool')
    reset();box('Frame',(0,0,.24),(1.4,2.1,.22),wood)
    box('Mattress',(0,0,.43),(1.35,2,.22),fabric,.05)
    box('Blanket',(0,-.3,.56),(1.36,1.35,.04),blue,.025)
    for x in [-.35,.35]:box('Pillow',(x,.73,.59),(.57,.38,.13),white,.06)
    box('Headboard',(0,1.03,.56),(1.45,.08,.85),wood)
    export('FinishedBed')
    reset();box('Desktop',(0,0,.75),(1.4,.65,.06),wood)
    for x in [-.61,.61]:
        for y in [-.24,.24]:box('Leg',(x,y,.36),(.045,.045,.72),steel)
    export('BedroomDesk')
    reset();cyl('LampBase',(0,0,.02),.12,.04,dark)
    cyl('LampStem',(0,0,.25),.012,.45,steel)
    lathe('LampShade',[(.13,.4),(.07,.57),(.065,.57),(.125,.4)],paint)
    export('DeskLamp')
    reset();box('Housing',(0,0,.09),(.35,.28,.18),dark,.02)
    box('Monitor',(0,.04,.31),(.42,.045,.27),dark)
    box('Screen',(0,.013,.31),(.37,.008,.22),screen)
    box('Keyboard',(0,-.15,.015),(.4,.12,.03),paint)
    for x in range(10):
        for y in range(3):box('Key',(-.17+x*.037,-.19+y*.037,.034),(.028,.025,.008),dark,.001)
    export('LabTerminal')
    reset();cyl('WasteBin',(0,0,.25),.18,.5,paint)
    cyl('Lid',(0,0,.51),.19,.025,orange)
    export('WasteBin')
    reset();box('Crate',(0,0,.16),(.55,.38,.32),blue)
    for x in [-.23,.23]:box('Handle',(x,0,.28),(.02,.2,.035),dark)
    export('ReturnTray')
    reset();box('Backing',(0,0,.15),(.42,.02,.3),white)
    box('Header',(0,-.013,.24),(.38,.004,.08),orange,.001)
    for z in [.14,.10,.06]:box('PrintedLine',(0,-.014,z),(.3,.002,.009),dark,.0)
    export('SafetySign')
    reset();box('Tray',(0,0,.02),(2,.16,.04),steel)
    for x in [-.9,-.45,0,.45,.9]:box('CableSupport',(x,0,.06),(.035,.16,.06),steel)
    for y in [-.045,0,.045]:cyl('Cable',(0,y,.055),.008,2,rubber,(0,math.pi/2,0))
    export('CableTray')
    reset();box('Stain',(0,0,.001),(.6,.35,.002),mat('WorkspaceStain',(.14,.11,.065),0,.9),.0)
    export('WorkspaceStain')
if __name__=='__main__':
    for directory in sorted((ROOT/'Tools/Downloads/PolyHaven').iterdir()):
        files=list(directory.glob('*.gltf'))
        if not files:continue
        reset();bpy.ops.import_scene.gltf(filepath=str(files[0]))
        export(directory.name,'https://polyhaven.com/a/'+directory.name,12000)
    author()
    (OUT/'catalog.json').write_text(json.dumps(catalog,indent=2))
    print('AUTHORED',len(catalog),'models',flush=True)
