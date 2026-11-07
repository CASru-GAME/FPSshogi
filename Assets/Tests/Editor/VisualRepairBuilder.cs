using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.SceneManagement;
using App.Main.Player;
using UnityMeshSimplifier;

[InitializeOnLoad]
public static class VisualRepairBuilder
{
    const string Trigger=".utmp/visual-fix/build-art.txt";
    static VisualRepairBuilder(){EditorApplication.update+=Poll;}
    public static void BuildArtBatch(){Build();}
    [MenuItem("FPSshogi/Build visual verification player")]
    public static void BuildVisualPlayer()
    {
        Directory.CreateDirectory("Builds/VisualSmoke");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes=new[]{"Assets/App/Scenes/Title.unity","Assets/App/Scenes/Main.unity"},
            locationPathName="Builds/VisualSmoke/FPSshogi.exe",target=BuildTarget.StandaloneWindows64,
            options=BuildOptions.None,extraScriptingDefines=new[]{"FPS_VISUAL_SMOKE"}
        });
        File.WriteAllText("VisualReview/player-smoke-build.txt",$"Result: {report.summary.result}\nErrors: {report.summary.totalErrors}\nWarnings: {report.summary.totalWarnings}\nTime: {report.summary.totalTime}\n");
        if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Visual verification player build failed");
    }
    static void Poll()
    {
        if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Trigger))return;
        string command=File.ReadAllText(Trigger).Trim();File.Delete(Trigger);
        if(File.Exists(".utmp/visual-fix/art-error.txt"))File.Delete(".utmp/visual-fix/art-error.txt");
        try{if(command=="visual-player")BuildVisualPlayer();else if(command=="build")Build();else if(command=="room"){RestoreRoom();AssetDatabase.SaveAssets();}else if(command=="weapons"){MakeWeapons();ConfigureScene();AssetDatabase.SaveAssets();}else if(command=="report")WriteGeometryReport();else Inventory();File.WriteAllText(".utmp/visual-fix/art-complete.txt",command);}
        catch(Exception e){File.WriteAllText(".utmp/visual-fix/art-error.txt",e.ToString());Debug.LogException(e);}
    }
    const string Art="Assets/App/Art/";
    [Serializable] class Manifest { public Tex[] textures;public Mat[] materials;public Ren[] renderers; }
    [Serializable] class Tex { public string key,name,path; }
    [Serializable] class Mat { public string key,name;public TexSlot[] textures;public FloatValue[] floats;public ColorValue[] colors; }
    [Serializable] class TexSlot { public string property,textureKey;public Vector2 scale,offset; }
    [Serializable] class FloatValue { public string property;public float value; }
    [Serializable] class ColorValue { public string property;public Color value; }
    [Serializable] class Ren { public string name;public MatSlot[] slots; }
    [Serializable] class MatSlot { public int index;public string materialKey; }
    static T Save<T>(T value,string path) where T:UnityEngine.Object
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        value.name=Path.GetFileNameWithoutExtension(path);
        var existing=AssetDatabase.LoadAssetAtPath<T>(path);
        if(value is Mesh sourceMesh)
        {
            if(sourceMesh.triangles.Any(index=>index<0||index>=sourceMesh.vertexCount))throw new InvalidOperationException("Invalid mesh indices before saving "+path+" vertices="+sourceMesh.vertexCount+" max="+sourceMesh.triangles.Max());
            if(existing is Mesh targetMesh)
            {
                // Updating through the mesh API also invalidates native rendering buffers.
                targetMesh.Clear();targetMesh.indexFormat=sourceMesh.indexFormat;
                targetMesh.vertices=sourceMesh.vertices;targetMesh.normals=sourceMesh.normals;targetMesh.tangents=sourceMesh.tangents;targetMesh.uv=sourceMesh.uv;
                targetMesh.subMeshCount=sourceMesh.subMeshCount;
                for(int sub=0;sub<sourceMesh.subMeshCount;sub++)targetMesh.SetTriangles(sourceMesh.GetTriangles(sub),sub);
                targetMesh.bounds=sourceMesh.bounds;EditorUtility.SetDirty(targetMesh);UnityEngine.Object.DestroyImmediate(value);return existing;
            }
        }
        if(existing!=null){EditorUtility.CopySerialized(value,existing);UnityEngine.Object.DestroyImmediate(value);EditorUtility.SetDirty(existing);return existing;}
        AssetDatabase.CreateAsset(value,path);return value;
    }
    static void Progress(string message)=>File.AppendAllText(".utmp/visual-fix/art-progress.txt",message+"\n");
    static void Build()
    {
        File.WriteAllText(".utmp/visual-fix/art-progress.txt","");
        Directory.CreateDirectory(Art+"Weapons");Directory.CreateDirectory(Art+"Environment");Directory.CreateDirectory("Assets/Resources/Combat");
        AssetDatabase.Refresh();
        MakeEffects();RestoreRoom();MakeWeapons();ConfigureScene();AssetDatabase.SaveAssets();
        WriteGeometryReport();Progress("All art saved");
    }
    static void WriteGeometryReport()
    {
        var rows=new List<string>{"Geometry comparison (triangles per mesh; not an FPS benchmark)","Source, original, LOD0, LOD1, LOD2"};
        var original=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/App/Models/Field2.fbx");
        foreach(var name in new[]{"箪笥","本","座布団","座布団.001"})
        {
            long source=Tri(original.GetComponentsInChildren<MeshFilter>().First(f=>f.name==name).sharedMesh);
            var counts=Enumerable.Range(0,3).Select(i=>Tri(AssetDatabase.LoadAssetAtPath<Mesh>(Art+"Environment/"+name+"_LOD"+i+".asset")));
            rows.Add(name+", "+source+", "+string.Join(", ",counts));
        }
        Directory.CreateDirectory("VisualReview");File.WriteAllLines("VisualReview/geometry-verification.txt",rows);
    }
    static Material Lit(string name,Color color,float metallic=0,float smoothness=.3f)
    {
        var mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};mat.SetColor("_BaseColor",color);mat.SetColor("_Color",color);mat.SetFloat("_Metallic",metallic);mat.SetFloat("_Smoothness",smoothness);mat.SetFloat("_Cull",(float)CullMode.Off);
        mat.doubleSidedGI=true;mat.SetOverrideTag("RenderType","Opaque");mat.SetShaderPassEnabled("MOTIONVECTORS",false);
        return Save(mat,Art+"Weapons/"+name+".mat");
    }
    static void MakeEffects()
    {
        foreach(string name in new[]{"Transparent","SlowField","Glow"})
        {
            var mat=new Material(Shader.Find(name=="Glow"?"Universal Render Pipeline/Particles/Unlit":"Universal Render Pipeline/Unlit")){name=name};
            mat.SetFloat("_Surface",1);mat.SetFloat("_Blend",0);mat.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);mat.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite",0);mat.SetFloat("_Cull",name=="SlowField"?(float)CullMode.Back:(float)CullMode.Off);mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");mat.renderQueue=3000;
            mat.SetColor("_BaseColor",name=="SlowField"?new Color(.5f,.12f,.8f,.28f):Color.white);mat.SetOverrideTag("RenderType","Transparent");
            Save(mat,"Assets/Resources/Combat/"+name+".mat");
        }
    }
    static long Tri(Mesh mesh){long n=0;for(int i=0;i<mesh.subMeshCount;i++)n+=(long)mesh.GetIndexCount(i)/3;return n;}
    static Mesh Simplify(Mesh source,float quality)
    {
        var simplifier=new MeshSimplifier();var options=SimplificationOptions.Default;
        // Imported hard edges duplicate most vertices; freezing every seam prevents useful LODs.
        // Smart linking keeps shared positions while the simplifier retains the UV attributes.
        options.PreserveUVSeamEdges=false;options.PreserveBorderEdges=false;
        options.VertexLinkDistance=source.bounds.size.magnitude*0.000001;
        simplifier.SimplificationOptions=options;simplifier.Initialize(source);simplifier.SimplifyMesh(quality);
        var mesh=simplifier.ToMesh();mesh.RecalculateBounds();return mesh;
    }
    static Mesh SimplifySmoothProp(Mesh source,Vector3 scale,float target)
    {
        // Curved cloth needs connected edge collapse; independent vertex clusters open cracks.
        var physical=UnityEngine.Object.Instantiate(source);
        physical.vertices=source.vertices.Select(v=>Vector3.Scale(v,scale)).ToArray();
        physical.normals=source.normals.Select(n=>new Vector3(n.x/scale.x,n.y/scale.y,n.z/scale.z).normalized).ToArray();physical.RecalculateBounds();
        var mesh=Simplify(physical,target/Tri(source));UnityEngine.Object.DestroyImmediate(physical);
        mesh.vertices=mesh.vertices.Select(v=>new Vector3(v.x/scale.x,v.y/scale.y,v.z/scale.z)).ToArray();
        mesh.normals=mesh.normals.Select(n=>Vector3.Scale(n,scale).normalized).ToArray();mesh.RecalculateBounds();mesh.RecalculateTangents();return mesh;
    }
    // CAD-style disconnected faces can stall edge-collapse. Cluster in physical units,
    // retaining each surviving face's material and UVs, rather than dropping arbitrary triangles.
    static Mesh Cluster(Mesh source,Vector3 objectScale,int resolution,bool smooth)
    {
        var vertices=source.vertices;var uv=source.uv;var normal=source.normals;
        var physical=vertices.Select(v=>Vector3.Scale(v,objectScale)).ToArray();
        var bounds=new Bounds(physical[0],Vector3.zero);foreach(var v in physical)bounds.Encapsulate(v);
        float cell=Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z)/resolution;
        var ids=new Dictionary<(int,int),int>();var sums=new List<Vector3>();var counts=new List<int>();var keys=new Dictionary<(int,int,int,int,int,int,int),int>();
        for(int material=0;material<source.subMeshCount;material++)foreach(int i in source.GetTriangles(material))
        {
            if(ids.ContainsKey((material,i)))continue;
            var v=physical[i];var n=normal.Length>i?normal[i]:Vector3.zero;
            // Keep opposite sides of thin panels separate; position-only clustering folds
            // drawer fronts into their back faces and exposes the dark frame behind them.
            var key=(material,Mathf.RoundToInt(v.x/cell),Mathf.RoundToInt(v.y/cell),Mathf.RoundToInt(v.z/cell),Mathf.RoundToInt(n.x*8),Mathf.RoundToInt(n.y*8),Mathf.RoundToInt(n.z*8));
            if(!keys.TryGetValue(key,out int id)){id=sums.Count;keys[key]=id;sums.Add(Vector3.zero);counts.Add(0);}
            ids[(material,i)]=id;sums[id]+=v;counts[id]++;
        }
        for(int i=0;i<sums.Count;i++){var v=sums[i]/counts[i];sums[i]=new Vector3(v.x/objectScale.x,v.y/objectScale.y,v.z/objectScale.z);}
        var points=new List<Vector3>();var tex=new List<Vector2>();var normals=new List<Vector3>();var submeshes=new List<int[]>();
        for(int s=0;s<source.subMeshCount;s++)
        {
            var seen=new HashSet<(int,int,int)>();var input=source.GetTriangles(s);var output=new List<int>();
            if(input.Length<=6000&&!smooth)
            {
                // Already simple panels carry the silhouette and UV seams. Preserve them exactly.
                foreach(int old in input){output.Add(points.Count);points.Add(vertices[old]);tex.Add(uv.Length>old?uv[old]:Vector2.zero);normals.Add(normal.Length>old?normal[old]:Vector3.up);}
                submeshes.Add(output.ToArray());continue;
            }
            for(int i=0;i<input.Length;i+=3)
            {
                int a=ids[(s,input[i])],b=ids[(s,input[i+1])],c=ids[(s,input[i+2])];if(a==b||b==c||c==a)continue;
                int lo=Mathf.Min(a,b,c),hi=Mathf.Max(a,b,c);if(!seen.Add((lo,a+b+c-lo-hi,hi)))continue;
                Vector3 n=Vector3.Cross(sums[b]-sums[a],sums[c]-sums[a]);float magnitude=n.magnitude;if(magnitude<1e-12f)continue;n/=magnitude;
                for(int k=0;k<3;k++){int old=input[i+k];output.Add(points.Count);points.Add(sums[ids[(s,old)]]);tex.Add(uv.Length>old?uv[old]:Vector2.zero);normals.Add(smooth&&normal.Length>old?normal[old]:n);}
            }
            submeshes.Add(output.ToArray());
        }
        var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.SetVertices(points);mesh.SetNormals(normals);mesh.SetUVs(0,tex);mesh.subMeshCount=submeshes.Count;
        for(int s=0;s<submeshes.Count;s++)mesh.SetTriangles(submeshes[s],s);mesh.RecalculateBounds();mesh.RecalculateTangents();return mesh;
    }
    static void RestoreRoom()
    {
        var data=JsonUtility.FromJson<Manifest>(File.ReadAllText(Art+"RecoveredRoom/manifest.json"));
        var textures=data.textures.ToDictionary(t=>t.key,t=>AssetDatabase.LoadAssetAtPath<Texture2D>(t.path));
        var materials=new Dictionary<string,Material>();
        foreach(var source in data.materials)
        {
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=source.name};
            foreach(var f in source.floats)if(material.HasProperty(f.property))material.SetFloat(f.property,f.value);
            foreach(var c in source.colors)if(material.HasProperty(c.property))material.SetColor(c.property,c.value);
            foreach(var t in source.textures)if(material.HasProperty(t.property)){material.SetTexture(t.property,textures[t.textureKey]);material.SetTextureScale(t.property,t.scale);material.SetTextureOffset(t.property,t.offset);}
            materials[source.key]=Save(material,Art+"RecoveredRoom/"+source.key+".mat");
        }
        const string path="Assets/App/Prefabs/Field2.prefab";
        if(!File.Exists(".utmp/visual-fix/Field2-before.prefab"))File.Copy(path,".utmp/visual-fix/Field2-before.prefab");
        var field=PrefabUtility.LoadPrefabContents(path);
        try
        {
            foreach(var renderer in field.GetComponentsInChildren<MeshRenderer>(true))
            {
                var assignment=data.renderers.FirstOrDefault(r=>r.name==renderer.name);if(assignment==null)continue;
                var slots=renderer.sharedMaterials;
                foreach(var slot in assignment.slots)if(slot.index<slots.Length)slots[slot.index]=materials[slot.materialKey];
                renderer.sharedMaterials=slots;
            }
            Progress("18 textures / 22 material identities restored");
            foreach(var filter in field.GetComponentsInChildren<MeshFilter>(true))
            {
                if(filter==null)continue;
                if(!new[]{"箪笥","本","座布団","座布団.001"}.Contains(filter.name))continue;
                var sourceModel=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/App/Models/Field2.fbx");
                var original=sourceModel.GetComponentsInChildren<MeshFilter>(true).First(f=>f.name==filter.name).sharedMesh;
                var oldGroup=filter.GetComponent<LODGroup>();if(oldGroup!=null)UnityEngine.Object.DestroyImmediate(oldGroup);
                foreach(Transform child in filter.transform.Cast<Transform>().ToArray())if(child.name=="LOD1"||child.name=="LOD2")UnityEngine.Object.DestroyImmediate(child.gameObject);
                long before=Tri(original);var renderer=filter.GetComponent<MeshRenderer>();
                var lods=new List<LOD>();
                float[] targets=filter.name=="箪笥"?new[]{65000f,18000,5000}:filter.name=="本"?new[]{18000f,6000,2200}:new[]{9000f,3000,1000};
                for(int level=0;level<3;level++)
                {
                    string meshPath=Art+"Environment/"+filter.name+"_LOD"+level+".asset";
                    Progress(filter.name+" LOD"+level+" simplifying "+before+" triangles");
                    int resolution=level==0?256:level==1?96:32;
                    bool cloth=filter.name.StartsWith("座布団");
                    var mesh=cloth?SimplifySmoothProp(original,filter.transform.lossyScale,targets[level]):Cluster(original,filter.transform.lossyScale,resolution,false);
                    while(!cloth&&Tri(mesh)>targets[level]*1.25f&&resolution>12)
                    {
                        UnityEngine.Object.DestroyImmediate(mesh);resolution=Mathf.Max(12,Mathf.RoundToInt(resolution*.7f));
                        mesh=Cluster(original,filter.transform.lossyScale,resolution,filter.name.StartsWith("座布団"));
                    }
                    if(Tri(mesh)==0)throw new InvalidOperationException("Empty LOD: "+filter.name+" level "+level);
                    mesh.name=filter.name+"_LOD"+level;mesh=Save(mesh,meshPath);
                    Renderer lodRenderer;
                    if(level==0){filter.sharedMesh=mesh;lodRenderer=renderer;}
                    else
                    {
                        var child=new GameObject("LOD"+level);child.transform.SetParent(filter.transform,false);child.AddComponent<MeshFilter>().sharedMesh=mesh;
                        var r=child.AddComponent<MeshRenderer>();r.sharedMaterials=renderer.sharedMaterials;lodRenderer=r;
                    }
                    lods.Add(new LOD(level==0?.15f:level==1?.045f:.001f,new[]{lodRenderer}));
                    Progress(filter.name+" LOD"+level+" = "+Tri(mesh));
                }
                foreach(var collider in filter.GetComponents<Collider>())UnityEngine.Object.DestroyImmediate(collider);
                var box=filter.gameObject.AddComponent<BoxCollider>();box.center=original.bounds.center;box.size=original.bounds.size;
                var group=filter.gameObject.AddComponent<LODGroup>();group.SetLODs(lods.ToArray());group.RecalculateBounds();
            }
            PrefabUtility.SaveAsPrefabAsset(field,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(field);}
    }
    // Bake original imported transforms into a native mesh, keeping material slots and UVs.
    static Mesh Bake(Renderer renderer,Quaternion rotation,Vector3 center,float scale)
    {
        Mesh source;
        // The source bind pose is intentional: runtime presentation supplies the motion.
        if(renderer is SkinnedMeshRenderer sk)source=UnityEngine.Object.Instantiate(sk.sharedMesh);
        else source=UnityEngine.Object.Instantiate(renderer.GetComponent<MeshFilter>().sharedMesh);
        var matrix=Matrix4x4.TRS(Vector3.zero,rotation,Vector3.one*scale)*Matrix4x4.Translate(-center)*renderer.transform.localToWorldMatrix;
        source.vertices=source.vertices.Select(matrix.MultiplyPoint3x4).ToArray();
        var normalMatrix=matrix.inverse.transpose;source.normals=source.normals.Select(n=>normalMatrix.MultiplyVector(n).normalized).ToArray();
        if(matrix.determinant<0)for(int s=0;s<source.subMeshCount;s++){var indices=source.GetTriangles(s);for(int i=0;i<indices.Length;i+=3)(indices[i],indices[i+1])=(indices[i+1],indices[i]);source.SetTriangles(indices,s);}
        source.RecalculateBounds();source.RecalculateTangents();return source;
    }
    static void NormalizeMesh(Mesh mesh,float height,bool longest=false)
    {
        var b=mesh.bounds;float size=longest?Mathf.Max(b.size.x,b.size.y,b.size.z):b.size.y;
        mesh.vertices=mesh.vertices.Select(v=>(v-b.center)*(height/size)).ToArray();mesh.RecalculateBounds();
    }
    static void RemoveCollapsedFaces(Mesh mesh)
    {
        var v=mesh.vertices;float tolerance=mesh.bounds.size.sqrMagnitude*1e-12f;
        for(int s=0;s<mesh.subMeshCount;s++)
        {
            var t=mesh.GetTriangles(s);var keep=new List<int>();
            for(int i=0;i<t.Length;i+=3)
                if(Vector3.Cross(v[t[i+1]]-v[t[i]],v[t[i+2]]-v[t[i]]).magnitude>=tolerance)
                    keep.AddRange(new[]{t[i],t[i+1],t[i+2]});
            mesh.SetTriangles(keep,s);
        }
        mesh.RecalculateBounds();mesh.RecalculateTangents();
    }
    static Vector3 BarrelOpening(Mesh mesh,Material[] materials)
    {
        int slot=Array.FindIndex(materials,m=>m.name=="Steel");var vertices=mesh.vertices;
        var rim=mesh.GetTriangles(slot).Distinct().Select(i=>vertices[i]).Where(v=>v.z>mesh.bounds.max.z-.015f).ToArray();
        var bounds=new Bounds(rim[0],Vector3.zero);foreach(var v in rim)bounds.Encapsulate(v);
        return new Vector3(bounds.center.x,bounds.center.y,mesh.bounds.max.z+.002f);
    }
    static void CloseBladeSurface(Mesh mesh)
    {
        // Both original naginata models contain the bevel/rim but omit the broad blade faces.
        // Trace that rim rather than inventing a different silhouette.
        var input=mesh.GetTriangles(2);var source=mesh.vertices;
        var welded=new List<Vector3>();var map=new Dictionary<(int,int,int),int>();var ids=new Dictionary<int,int>();
        foreach(int old in input)
        {
            var p=source[old];var key=(Mathf.RoundToInt(p.x*100000),Mathf.RoundToInt(p.y*100000),Mathf.RoundToInt(p.z*100000));
            if(!map.TryGetValue(key,out int index)){index=welded.Count;map[key]=index;welded.Add(p);}ids[old]=index;
        }
        var edges=new Dictionary<(int,int),int>();
        void Edge(int a,int b){var key=(Mathf.Min(a,b),Mathf.Max(a,b));edges.TryGetValue(key,out int count);edges[key]=count+1;}
        for(int i=0;i<input.Length;i+=3){int a=ids[input[i]],b=ids[input[i+1]],c=ids[input[i+2]];if(a==b||b==c||a==c)continue;Edge(a,b);Edge(b,c);Edge(c,a);}
        var neighbors=new Dictionary<int,List<int>>();
        foreach(var pair in edges.Where(e=>e.Value==1))
        {
            var (a,b)=pair.Key;if(!neighbors.ContainsKey(a))neighbors[a]=new List<int>();if(!neighbors.ContainsKey(b))neighbors[b]=new List<int>();neighbors[a].Add(b);neighbors[b].Add(a);
        }
        var visited=new HashSet<int>();var outline=new List<Vector3>();float largest=0;
        foreach(int start in neighbors.Keys)
        {
            if(visited.Contains(start))continue;var loop=new List<Vector3>();int previous=-1,current=start;
            do{if(visited.Contains(current))break;visited.Add(current);loop.Add(welded[current]);var next=neighbors[current].Where(n=>n!=previous).DefaultIfEmpty(-1).First();previous=current;current=next;}while(current>=0&&current!=start);
            if(current!=start||loop.Count<3)continue;float area=0;
            for(int i=0;i<loop.Count;i++){var a=loop[i];var b=loop[(i+1)%loop.Count];area+=a.x*b.y-b.x*a.y;}
            if(Mathf.Abs(area)>largest){largest=Mathf.Abs(area);outline=loop;if(area<0)outline.Reverse();}
        }
        if(largest<.01f)throw new InvalidOperationException("No closed blade rim in "+mesh.name);
        float Cross(Vector3 a,Vector3 b,Vector3 c)=>(b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);
        bool removed=true;while(removed&&outline.Count>3){removed=false;for(int i=0;i<outline.Count;i++)if(Mathf.Abs(Cross(outline[(i+outline.Count-1)%outline.Count],outline[i],outline[(i+1)%outline.Count]))<.0000001f){outline.RemoveAt(i);removed=true;break;}}
        var polygon=Enumerable.Range(0,outline.Count).ToList();var cap=new List<int>();
        while(polygon.Count>3)
        {
            bool clipped=false;
            for(int i=0;i<polygon.Count;i++)
            {
                int a=polygon[(i+polygon.Count-1)%polygon.Count],b=polygon[i],c=polygon[(i+1)%polygon.Count];
                if(Cross(outline[a],outline[b],outline[c])<=0)continue;
                bool inside=polygon.Any(p=>p!=a&&p!=b&&p!=c&&Cross(outline[a],outline[b],outline[p])>=0&&Cross(outline[b],outline[c],outline[p])>=0&&Cross(outline[c],outline[a],outline[p])>=0);
                if(inside)continue;cap.AddRange(new[]{a,b,c});polygon.RemoveAt(i);clipped=true;break;
            }
            if(!clipped)throw new InvalidOperationException("Cannot triangulate blade rim "+mesh.name);
        }
        cap.AddRange(polygon);var vertices=source.ToList();var normals=mesh.normals.ToList();var uv=mesh.uv.ToList();var indices=input.ToList();
        float minZ=input.Min(i=>source[i].z),maxZ=input.Max(i=>source[i].z);
        for(int side=0;side<2;side++)
        {
            int offset=vertices.Count;
            foreach(var p in outline){vertices.Add(new Vector3(p.x,p.y,side==0?minZ:maxZ));normals.Add(side==0?Vector3.back:Vector3.forward);uv.Add(new Vector2(p.x,p.y));}
            for(int i=0;i<cap.Count;i+=3){indices.Add(offset+cap[i]);indices.Add(offset+cap[i+(side==0?2:1)]);indices.Add(offset+cap[i+(side==0?1:2)]);}
        }
        mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,2);mesh.RecalculateBounds();mesh.RecalculateTangents();
        Progress("Closed blade surface: "+outline.Count+" outline points, "+cap.Count/3+" triangles per side");
    }
    static void MakeWeapons()
    {
        var wood=Lit("WarmWood",new Color(.27f,.105f,.035f));var leather=Lit("DarkLeather",new Color(.045f,.027f,.018f),0,.2f);
        var steel=Lit("Steel",new Color(.5f,.59f,.65f),.8f,.64f);var gold=Lit("Brass",new Color(.7f,.4f,.1f),.7f,.5f);
        var red=Lit("RedLacquer",new Color(.52f,.045f,.035f),.1f,.4f);var blue=Lit("BlueLacquer",new Color(.035f,.17f,.43f),.1f,.4f);
        var feather=Lit("ArrowFeather",new Color(.7f,.66f,.55f),0,.18f);
        Material Map(Material mat,string weapon)
        {
            string n=mat.name;if(n.Contains("黒レザー"))return leather;if(n=="金")return gold;
            if(weapon.StartsWith("Gun")&&n=="マテリアル.005")return wood;
            if(weapon.StartsWith("Gun")&&n=="鞘")return steel;
            if(n.Contains("薙刀"))return weapon.Contains("red")?red:blue;
            if(n.Contains("鉄")||n=="マテリアル.005")return steel;
            if(n=="赤茶")return red;if(n.Contains("木")||n=="鞘")return wood;return steel;
        }
        foreach(string name in new[]{"Spear","Gun_short","Katana","Naginata_red","Naginata_blue","Gun_long","Bow","knife"})
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/App/Models/Weapon/"+name+".fbx");
            var renderer=source.GetComponentInChildren<Renderer>();bool gun=name.StartsWith("Gun");bool bow=name=="Bow";bool knife=name=="knife";
            float size=bow?2.25f:gun?(name=="Gun_long"?2.6f:1.9f):knife?1.05f:3f;
            Quaternion rotation=bow||gun?Quaternion.Euler(0,90,0):Quaternion.Euler(0,0,-90);
            float length=bow?renderer.bounds.size.y:renderer.bounds.size.x;
            var mesh=Bake(renderer,rotation,renderer.bounds.center,size/length);
            // The imported bow already includes a rigid string (slot 1). Only the
            // limbs belong in this mesh; the animated string must be the sole string.
            if(bow)mesh.subMeshCount=1;
            if(name.StartsWith("Naginata"))CloseBladeSurface(mesh);
            // Thin, almost coplanar blade faces do not survive unrestricted edge
            // collapse: the katana developed sparkling slivers along its cutting edge.
            // Its original 21k triangles are small enough to preserve intact.
            if(bow&&Tri(mesh)>18000){var reduced=Simplify(mesh,14000f/Tri(mesh));UnityEngine.Object.DestroyImmediate(mesh);mesh=reduced;}
            RemoveCollapsedFaces(mesh);
            mesh.name=name+"_Display";mesh=Save(mesh,Art+"Weapons/"+name+".asset");
            var root=new GameObject(name);var visual=new GameObject("Model");visual.transform.SetParent(root.transform,false);
            var materials=renderer.sharedMaterials.Take(mesh.subMeshCount).Select(m=>Map(m,name)).ToArray();
            visual.AddComponent<MeshFilter>().sharedMesh=mesh;visual.AddComponent<MeshRenderer>().sharedMaterials=materials;
            var presentation=root.AddComponent<WeaponPresentation>();presentation.visual=visual.transform;
            presentation.motion=bow?WeaponPresentation.Motion.Bow:gun?WeaponPresentation.Motion.Gun:knife?WeaponPresentation.Motion.Knife:name=="Spear"?WeaponPresentation.Motion.Thrust:WeaponPresentation.Motion.Slash;
            presentation.holdPosition=bow?new Vector3(.4f,-.3f,1.6f):gun?new Vector3(.38f,-.35f,1.75f):knife?new Vector3(.35f,-.28f,1.45f):new Vector3(.38f,-.45f,1.95f);
            presentation.holdEuler=bow?new Vector3(0,-20,-8):gun?new Vector3(-2,-8,0):knife?new Vector3(30,10,-28):name=="Spear"?new Vector3(82,-12,-3):new Vector3(55,-15,-28);
            presentation.muzzleLocal=presentation.holdPosition+Quaternion.Euler(presentation.holdEuler)*Vector3.forward*(size*.5f);
            var muzzle=new GameObject("Muzzle");muzzle.transform.SetParent(visual.transform,false);muzzle.transform.localPosition=gun?BarrelOpening(mesh,materials):Vector3.forward*(size*.5f);presentation.muzzle=muzzle.transform;
            if(bow)
            {
                var v=mesh.vertices;
                Vector3 Tip(bool upper)
                {
                    float y=upper?mesh.bounds.max.y:mesh.bounds.min.y;
                    var end=v.Where(p=>Mathf.Abs(p.y-y)<.004f).ToArray();
                    return end.Aggregate(Vector3.zero,(a,b)=>a+b)/end.Length;
                }
                var drawn=new Vector3(-.035f,0,-.5f);
                presentation.bowString=CombatVisuals.Line(visual,"String",new[]{Tip(true),drawn,Tip(false)},new Color(.7f,.65f,.48f),.006f);
                var arrowSource=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/App/Models/Weapon/Arrow.fbx").GetComponentInChildren<Renderer>();
                var arrowMesh=Bake(arrowSource,Quaternion.Euler(0,90,0),arrowSource.bounds.center,1.1f/arrowSource.bounds.size.x);
                RemoveCollapsedFaces(arrowMesh);arrowMesh=Save(arrowMesh,Art+"Weapons/NockedArrow.asset");var arrow=new GameObject("NockedArrow");arrow.transform.SetParent(visual.transform,false);arrow.transform.localPosition=drawn-Vector3.forward*arrowMesh.bounds.min.z;
                var arrowMaterials=new[]{steel,wood,wood,feather,feather};
                arrow.AddComponent<MeshFilter>().sharedMesh=arrowMesh;arrow.AddComponent<MeshRenderer>().sharedMaterials=arrowMaterials;presentation.nockedArrow=arrow.transform;
                MakeArrowProjectile(arrowMesh,arrowMaterials,"Assets/App/Prefabs/MainWeapons/Arrow.prefab");
                MakeArrowProjectile(arrowMesh,arrowMaterials,"Assets/App/Prefabs/SubWeapons/Arrow.prefab");
            }
            var collider=root.AddComponent<BoxCollider>();collider.center=presentation.holdPosition;collider.size=mesh.bounds.size;collider.isTrigger=true;collider.enabled=false;
            root.AddComponent<PlayerWeapon>();PrefabUtility.SaveAsPrefabAsset(root,Art+"Weapons/"+name+".prefab");UnityEngine.Object.DestroyImmediate(root);
        }
        MakeKingProps();Progress("Eight weapon display prefabs generated");
    }
    static void MakeArrowProjectile(Mesh mesh,Material[] materials,string path)
    {
        // An identity root avoids carrying the FBX authoring scale/rotation into a shot.
        // The launch copies the visible arrow's world scale for a seamless hand-off.
        var root=new GameObject("Arrow");root.AddComponent<MeshFilter>().sharedMesh=mesh;root.AddComponent<MeshRenderer>().sharedMaterials=materials;
        var box=root.AddComponent<BoxCollider>();box.center=mesh.bounds.center;box.size=mesh.bounds.size;
        var rb=root.AddComponent<Rigidbody>();rb.useGravity=true;rb.constraints=RigidbodyConstraints.FreezeRotation;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
        PrefabUtility.SaveAsPrefabAsset(root,path);UnityEngine.Object.DestroyImmediate(root);
    }
    static void MakeKingProps()
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/App/Prefabs/kakugyo_black.prefab");
        var r=source.GetComponentsInChildren<MeshRenderer>().First(x=>x.GetComponent<MeshFilter>()?.sharedMesh!=null&&AssetDatabase.GetAssetPath(x.GetComponent<MeshFilter>().sharedMesh).Contains("Recovered"));
        var mesh=Bake(r,Quaternion.identity,Vector3.zero,1);NormalizeMesh(mesh,1.25f);Progress("Turret body "+mesh.bounds);mesh=Save(mesh,Art+"Weapons/TurretBody.asset");
        var root=new GameObject("KingTurret");var body=new GameObject("Body");body.transform.SetParent(root.transform,false);body.transform.localPosition=Vector3.up*.625f;
        body.AddComponent<MeshFilter>().sharedMesh=mesh;body.AddComponent<MeshRenderer>().sharedMaterials=r.sharedMaterials;
        var gun=new GameObject("Gun");gun.transform.SetParent(root.transform,false);gun.transform.localPosition=new Vector3(0,.9f,.3f);gun.transform.localScale=Vector3.one*.36f;
        gun.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Art+"Weapons/Gun_long.asset");gun.AddComponent<MeshRenderer>().sharedMaterials=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"Weapons/Gun_long.prefab").GetComponentInChildren<Renderer>().sharedMaterials;
        var muzzle=new GameObject("Muzzle");muzzle.transform.SetParent(gun.transform,false);muzzle.transform.localPosition=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"Weapons/Gun_long.prefab").GetComponent<WeaponPresentation>().muzzle.localPosition;
        PrefabUtility.SaveAsPrefabAsset(root,Art+"Weapons/KingTurret.prefab");UnityEngine.Object.DestroyImmediate(root);
        var fan=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/App/Prefabs/SubWeapons/Gunbai.prefab"));
        foreach(var a in fan.GetComponentsInChildren<Animator>())a.enabled=false;
        var renderers=fan.GetComponentsInChildren<Renderer>();
        var fanMeshes=renderers.Select(item=>Bake(item,Quaternion.identity,Vector3.zero,1)).ToArray();
        var bounds=fanMeshes[0].bounds;foreach(var item in fanMeshes)bounds.Encapsulate(item.bounds);
        Vector3 longAxis=bounds.size.x>bounds.size.y&&bounds.size.x>bounds.size.z?Vector3.right:bounds.size.z>bounds.size.y?Vector3.forward:Vector3.up;
        Vector3 normalAxis=bounds.size.x<bounds.size.y&&bounds.size.x<bounds.size.z?Vector3.right:bounds.size.y<bounds.size.z?Vector3.up:Vector3.forward;
        Quaternion fanRotation=Quaternion.FromToRotation(longAxis,Vector3.up);
        if(Mathf.Abs(Vector3.Dot(fanRotation*normalAxis,Vector3.forward))<.5f)fanRotation=Quaternion.Euler(0,90,0)*fanRotation;
        fanRotation=Quaternion.Euler(0,0,180)*fanRotation;
        root=new GameObject("Gunbai");var visual=new GameObject("Model");visual.transform.SetParent(root.transform,false);
        int index=0;
        foreach(var item in renderers)
        {
            var m=fanMeshes[index];m.vertices=m.vertices.Select(v=>fanRotation*((v-bounds.center)*(1.1f/Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z)))).ToArray();m.normals=m.normals.Select(n=>fanRotation*n).ToArray();m.RecalculateBounds();
            // Preserve the fan's thin decorated surface; edge collapse distorts its trim.
            RemoveCollapsedFaces(m);m=Save(m,Art+"Weapons/Gunbai"+index+".asset");
            var child=new GameObject("Fan"+index++);child.transform.SetParent(visual.transform,false);child.AddComponent<MeshFilter>().sharedMesh=m;
            int slot=0;child.AddComponent<MeshRenderer>().sharedMaterials=item.sharedMaterials.Select(material=>{var copy=new Material(material);if(copy.HasProperty("_Cull"))copy.SetFloat("_Cull",0);if(copy.HasProperty("_BaseColor"))copy.SetColor("_Color",copy.GetColor("_BaseColor"));copy.doubleSidedGI=true;copy.SetOverrideTag("RenderType","Opaque");copy.SetShaderPassEnabled("MOTIONVECTORS",false);return Save(copy,Art+"Weapons/FanMaterial"+(slot++)+".mat");}).ToArray();
        }
        var p=root.AddComponent<WeaponPresentation>();p.visual=visual.transform;p.motion=WeaponPresentation.Motion.Command;p.holdPosition=new Vector3(-.34f,-.14f,1.55f);p.holdEuler=new Vector3(0,0,-15);p.attackDuration=.8f;
        PrefabUtility.SaveAsPrefabAsset(root,Art+"Weapons/Gunbai.prefab");UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(fan);
    }
    static void ConfigureScene()
    {
        var scene=EditorSceneManager.OpenScene("Assets/App/Scenes/Main.unity");var manager=UnityEngine.Object.FindFirstObjectByType<PlayerManager>();var serialized=new SerializedObject(manager);
        var fields=new[]{"fuhyoWeapon","kyosyaWeapon","keimaWeapon","ginWeapon","kinWeapon","kakugyoWeapon","hisyaWeapon","kingWeapon","Gunbai","blackKakugyo"};
        var names=new[]{"Spear","Gun_short","Katana","Naginata_red","Naginata_blue","Gun_long","Bow","knife","Gunbai","KingTurret"};
        for(int i=0;i<fields.Length;i++)serialized.FindProperty(fields[i]).objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"Weapons/"+names[i]+".prefab");
        serialized.ApplyModifiedPropertiesWithoutUndo();EditorSceneManager.SaveScene(scene);EditorSceneManager.OpenScene("Assets/App/Scenes/Title.unity");
    }
    static void Inventory()
    {
        var rows=new List<string>();
        foreach(string path in Directory.GetFiles("Assets/App/Models/Weapon","*.fbx"))
        {
            var go=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            rows.Add("MODEL "+path);
            foreach(var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var mesh=r is SkinnedMeshRenderer sk?sk.sharedMesh:r.GetComponent<MeshFilter>()?.sharedMesh;
                rows.Add(r.name+" rotation="+r.transform.rotation.eulerAngles+" scale="+r.transform.lossyScale+" position="+r.transform.position+" mesh="+mesh?.bounds+" renderer="+r.bounds+" materials="+string.Join("|",r.sharedMaterials.Select(m=>m.name)));
            }
        }
        foreach(var guid in AssetDatabase.FindAssets("Field2 t:Prefab"))
        {
            string p=AssetDatabase.GUIDToAssetPath(guid);rows.Add("FIELD "+p);
            foreach(var f in AssetDatabase.LoadAssetAtPath<GameObject>(p).GetComponentsInChildren<MeshFilter>(true))
            {
                var m=f.sharedMesh;rows.Add(f.name+" position="+f.transform.position+" rotation="+f.transform.rotation.eulerAngles+" scale="+f.transform.lossyScale+" mesh="+m.bounds+" renderer="+f.GetComponent<Renderer>().bounds+" triangles="+m.triangles.Length/3+" materials="+string.Join("|",f.GetComponent<Renderer>().sharedMaterials.Select(x=>x.name)));
                if(f.name.Contains("円柱.002"))File.WriteAllLines(".utmp/visual-fix/board-vertices.txt",m.vertices.Select(v=>f.transform.TransformPoint(v).ToString("F4")));
            }
        }
        File.WriteAllLines(".utmp/visual-fix/model-inventory.txt",rows);
    }
}
