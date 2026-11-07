using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using App.Main.Player;

// Isolated, lit views of the actual imported/generated assets. No scene assets are changed.
[InitializeOnLoad]
public static class ModelIntegrityAudit
{
    const string Trigger = ".utmp/model-repair/audit.txt";
    static readonly List<string> rows = new();
    static readonly List<string> metrics = new();
    static string dir;
    [Serializable] class Geometry { public string name; public Vector3[] vertices; public Part[] parts; }
    [Serializable] class Part { public string material; public int[] triangles; }
    static ModelIntegrityAudit() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Trigger)) return;
        var name = File.ReadAllText(Trigger).Trim(); File.Delete(Trigger);
        dir = "VisualReview/Models-" + name; Directory.CreateDirectory(dir);
        try { if(name=="Compare") CompareWeapons(); else Run(); File.WriteAllText(dir + "/complete.txt", DateTime.Now.ToString("O")); }
        catch (Exception e) { File.WriteAllText(dir + "/error.txt", e.ToString()); Debug.LogException(e); }
    }
    static void CompareWeapons()
    {
        foreach(string name in new[]{"Katana","Naginata_red","Naginata_blue","Gun_short","Gun_long","knife","Spear"})
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/App/Models/Weapon/"+name+".fbx").GetComponentInChildren<Renderer>();
            float length=name=="knife"?1.05f:name=="Gun_long"?2.6f:name=="Gun_short"?1.9f:3;
            var rotation=name.StartsWith("Gun")?Quaternion.Euler(0,90,0):Quaternion.Euler(0,0,-90);
            var mesh=(Mesh)typeof(VisualRepairBuilder).GetMethod("Bake",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{source,rotation,source.bounds.center,length/source.bounds.size.x});
            var go=new GameObject(name);go.AddComponent<MeshFilter>().sharedMesh=mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/App/Art/Weapons/"+name+".prefab").GetComponentInChildren<MeshRenderer>().sharedMaterials;
            Capture("unreduced-"+name,go);UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(mesh);
        }
    }
    static void Run()
    {
        rows.Clear(); metrics.Clear(); rows.Add("id\tasset\tobject\ttriangles\tsize\tmaterials");
        foreach (var path in Directory.GetFiles("Assets/App/Models", "*.fbx", SearchOption.AllDirectories).OrderBy(x => x))
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) Describe(path, r);
            // The room is inspected as individual props below; retain source metrics for all imports.
            if (!path.Contains("Field2") && !path.Contains("仮部屋")) Capture("source-" + Path.GetFileNameWithoutExtension(path), go);
        }
        foreach (var path in Directory.GetFiles("Assets/App/Animations", "*.fbx").OrderBy(x => x))
            foreach (var r in AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<Renderer>(true)) Describe(path, r);
        foreach (var path in Directory.GetFiles("Assets/App/Art/Weapons", "*.prefab").OrderBy(x => x))
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) Describe(path, r);
            Capture("display-" + Path.GetFileNameWithoutExtension(path), go);
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true)) Export(path, r);
        }
        foreach (var name in new[]{"fuhyo","kyosya","keima","gin","kin","kakugyo","hisya","ou","gyoku","kakugyo_black"})
        {
            var path = "Assets/App/Prefabs/" + name + ".prefab";
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) Describe(path,r);
            Capture("piece-" + name,go);
        }
        foreach (var path in new[]{"Assets/App/Prefabs/MainWeapons/Arrow.prefab","Assets/App/Prefabs/SubWeapons/Arrow.prefab","Assets/App/Prefabs/SubWeapons/tetuhau.prefab"})
        {
            var go=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach(var r in go.GetComponentsInChildren<Renderer>(true)) Describe(path,r);
            Capture("projectile-" + Path.GetFileName(Path.GetDirectoryName(path)) + "-" + go.name,go);
        }
        var room = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/App/Prefabs/Field2.prefab");
        foreach (var r in room.GetComponentsInChildren<MeshRenderer>(true))
        {
            Describe("Assets/App/Prefabs/Field2.prefab",r);
            // Preserve the complete imported transform when isolating a room prop.
            var obj = new GameObject(r.name);
            obj.AddComponent<MeshFilter>().sharedMesh=r.GetComponent<MeshFilter>().sharedMesh;
            obj.AddComponent<MeshRenderer>().sharedMaterials=r.sharedMaterials;
            obj.transform.SetPositionAndRotation(r.transform.position,r.transform.rotation); obj.transform.localScale=r.transform.lossyScale;
            Capture("room-" + (r.name.StartsWith("LOD") ? r.transform.parent.name+"-"+r.name : r.name),obj);
            UnityEngine.Object.DestroyImmediate(obj);
        }
        File.WriteAllLines(dir+"/inventory.tsv",rows); File.WriteAllLines(dir+"/geometry.txt",metrics);
    }
    static Mesh MeshOf(Renderer r) => r is SkinnedMeshRenderer skin ? skin.sharedMesh : r.TryGetComponent<MeshFilter>(out var filter) ? filter.sharedMesh : null;
    static void Describe(string path, Renderer r)
    {
        var mesh=MeshOf(r); if(mesh==null)return;
        string id=path+"/"+r.name;
        rows.Add(id+"\t"+path+"\t"+r.name+"\t"+mesh.triangles.Length/3+"\t"+r.bounds.size.ToString("F5")+"\t"+string.Join("|",r.sharedMaterials.Select(m=>m==null?"MISSING":m.name)));
        var v=mesh.vertices; var n=mesh.normals; var t=mesh.triangles;
        int degenerate=0,inverted=0,finite=0;
        float tolerance=Mathf.Pow(mesh.bounds.size.magnitude,2)*1e-12f;
        foreach(var p in v)if(float.IsNaN(p.x)||float.IsInfinity(p.x)||float.IsNaN(p.y)||float.IsInfinity(p.y)||float.IsNaN(p.z)||float.IsInfinity(p.z))finite++;
        for(int i=0;i<t.Length;i+=3)
        {
            var cross=Vector3.Cross(v[t[i+1]]-v[t[i]],v[t[i+2]]-v[t[i]]);
            if(cross.magnitude<tolerance)degenerate++;
            else if(n.Length==v.Length && Vector3.Dot(cross.normalized,(n[t[i]]+n[t[i+1]]+n[t[i+2]]).normalized)<-.2f)inverted++;
        }
        metrics.Add(id+": nonfinite="+finite+" degenerate="+degenerate+" invertedNormals="+inverted+" / "+t.Length/3+" determinant="+r.transform.localToWorldMatrix.determinant+" submeshes="+mesh.subMeshCount+" materials="+r.sharedMaterials.Length);
    }
    static void Export(string path, MeshRenderer renderer)
    {
        var mesh=MeshOf(renderer);if(mesh==null)return;
        var g=new Geometry{name=path+"/"+renderer.name,vertices=mesh.vertices,parts=Enumerable.Range(0,mesh.subMeshCount).Select(i=>new Part{material=i<renderer.sharedMaterials.Length?renderer.sharedMaterials[i].name:"MISSING",triangles=mesh.GetTriangles(i)}).ToArray()};
        File.WriteAllText(dir+"/"+Path.GetFileNameWithoutExtension(path)+"-"+renderer.name+".json",JsonUtility.ToJson(g));
    }
    static void Capture(string id, GameObject source)
    {
        var preview=new PreviewRenderUtility();
        try
        {
            var go=UnityEngine.Object.Instantiate(source);go.name=source.name;
            foreach(var b in go.GetComponentsInChildren<MonoBehaviour>(true))b.enabled=false;
            foreach(var a in go.GetComponentsInChildren<Animator>(true))a.enabled=false;
            foreach(var g in go.GetComponentsInChildren<LODGroup>(true))g.ForceLOD(0);
            preview.AddSingleGO(go);
            var renderers=go.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled && r.gameObject.activeInHierarchy).ToArray();if(renderers.Length==0)return;
            var bounds=renderers[0].bounds; foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
            var camera=preview.camera; camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.16f,.18f,.20f);camera.nearClipPlane=.001f;camera.farClipPlane=10000;
            preview.ambientColor=new Color(.45f,.45f,.45f);
            preview.lights[0].intensity=1.6f;preview.lights[0].transform.rotation=Quaternion.Euler(40,35,0);
            preview.lights[1].intensity=1.1f;preview.lights[1].transform.rotation=Quaternion.Euler(330,215,0);
            var angles=new[]{new Vector3(0,0,-1),new Vector3(1,0,0),new Vector3(0,0,1),new Vector3(.7f,.45f,-1),new Vector3(0,1,.1f),new Vector3(0,-1,-.1f)};
            for(int i=0;i<angles.Length;i++)
            {
                camera.transform.position=bounds.center+angles[i].normalized*Mathf.Max(1,bounds.size.magnitude*2);
                camera.transform.LookAt(bounds.center);
                float extent=0;foreach(float x in new[]{-1f,1f})foreach(float y in new[]{-1f,1f})foreach(float z in new[]{-1f,1f})
                {var p=camera.transform.InverseTransformPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z)));extent=Mathf.Max(extent,Mathf.Abs(p.x),Mathf.Abs(p.y));}
                camera.orthographicSize=Mathf.Max(.01f,extent*1.1f);
                preview.BeginStaticPreview(new Rect(0,0,640,640));preview.Render(true,false);var image=preview.EndStaticPreview();
                File.WriteAllBytes(dir+"/"+id+"-"+i+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
            }
            File.WriteAllText(dir+"/progress.txt",id);
        }
        finally { preview.Cleanup(); }
    }
}
