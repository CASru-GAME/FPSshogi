import argparse, json, sys
from pathlib import Path
root=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(root/'.utmp/python-packages'))
import UnityPy
parser=argparse.ArgumentParser(description='Restore the original FPSshogi room textures and material identities.')
parser.add_argument('source',type=Path,help='Finished FPSshogi_Data directory')
source=parser.parse_args().source
env=UnityPy.load(*(str(p) for p in source.iterdir() if p.suffix=='.assets' or p.name in ('level0','level1')))
out=root/'Assets/App/Art/RecoveredRoom'
out.mkdir(parents=True,exist_ok=True)
names={'掛け軸和紙','箪笥','本','天井','本4','本5','柱','障子紙','本2','畳','襖枠組み','掛け軸台紙','引き出し (2)','和室壁紙','和柄赤','襖','本3'}
def identity(o):return Path(o.assets_file.name).stem+'_'+str(o.path_id)
textures={}; materials={}; renderers=[]
for o in env.objects:
    if o.type.name=='Texture2D' and o.peek_name() in names:
        t=o.parse_as_object();key=identity(o);path=out/(key+'.png');t.image.save(path)
        textures[key]={'key':key,'name':t.m_Name,'path':path.relative_to(root).as_posix(),'sourceFile':o.assets_file.name,'sourceId':o.path_id}
for o in env.objects:
    if o.type.name!='Material':continue
    m=o.parse_as_object(); tex=[]
    for name,v in m.m_SavedProperties.m_TexEnvs:
        if not v.m_Texture.path_id:continue
        t=v.m_Texture.deref();key=identity(t)
        if key in textures:tex.append({'property':name,'textureKey':key,'scale':{'x':v.m_Scale.x,'y':v.m_Scale.y},'offset':{'x':v.m_Offset.x,'y':v.m_Offset.y}})
    if not tex:continue
    key=identity(o)
    materials[key]={'key':key,'name':m.m_Name,'textures':tex,'floats':[{'property':k,'value':v} for k,v in m.m_SavedProperties.m_Floats],'colors':[{'property':k,'value':{c:getattr(v,c) for c in 'rgba'}} for k,v in m.m_SavedProperties.m_Colors]}
for o in env.objects:
    if o.type.name!='MeshRenderer':continue
    r=o.parse_as_object();slots=[]
    for i,p in enumerate(r.m_Materials):
        if p.path_id:
            key=identity(p.deref())
            if key in materials:slots.append({'index':i,'materialKey':key})
    if slots:renderers.append({'name':r.m_GameObject.deref_parse_as_object().m_Name,'slots':slots})
manifest={'source':str(source),'textures':list(textures.values()),'materials':list(materials.values()),'renderers':renderers}
(out/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({k:len(manifest[k]) for k in ['textures','materials','renderers']}))
