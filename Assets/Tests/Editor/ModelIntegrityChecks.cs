using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using App.Main.Player;

[InitializeOnLoad]
public static class ModelIntegrityChecks
{
    const string Trigger=".utmp/model-repair/check.txt";
    static readonly List<string> rows=new();
    static ModelIntegrityChecks(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Trigger))return;
        string dir="VisualReview/Models-"+File.ReadAllText(Trigger).Trim();File.Delete(Trigger);Directory.CreateDirectory(dir);rows.Clear();
        try{Run();}catch(Exception e){rows.Add("FAIL "+e);}
        File.WriteAllLines(dir+"/checks.txt",rows);
    }
    static void Check(bool pass,string name)=>rows.Add((pass?"PASS ":"FAIL ")+name);
    static GameObject Weapon(string name)=>AssetDatabase.LoadAssetAtPath<GameObject>("Assets/App/Art/Weapons/"+name+".prefab");
    static void Run()
    {
        var bow=Weapon("Bow").GetComponent<WeaponPresentation>();var mesh=bow.visual.GetComponent<MeshFilter>().sharedMesh;var vertices=mesh.vertices;
        Check(mesh.bounds.size.z>mesh.bounds.size.x*5,"Bow bends in the arrow's vertical flight plane");
        Check(mesh.subMeshCount==1,"Bow has one dynamic string, no baked duplicate");
        foreach(int tip in new[]{0,2})
        {
            var p=bow.bowString.GetPosition(tip);
            Check(vertices.Min(v=>Vector3.Distance(v,p))<.015f,"Bow string attached to limb tip "+tip);
        }
        var arrow=bow.nockedArrow.GetComponent<MeshFilter>().sharedMesh;
        var tail=bow.nockedArrow.localPosition+bow.nockedArrow.localRotation*Vector3.forward*arrow.bounds.min.z;
        Check(Vector3.Distance(tail,bow.bowString.GetPosition(1))<.015f,"Nocked arrow tail meets string");
        foreach(string path in new[]{"Assets/App/Prefabs/MainWeapons/Arrow.prefab","Assets/App/Prefabs/SubWeapons/Arrow.prefab"})
        {
            var projectile=AssetDatabase.LoadAssetAtPath<GameObject>(path);var filter=projectile.GetComponentInChildren<MeshFilter>();
            Check(filter.sharedMesh==arrow,path+" shares the held arrow mesh");
            var rootSpace=projectile.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
            var v=filter.sharedMesh.vertices;var tipIds=filter.sharedMesh.GetTriangles(0);
            var tip=tipIds.Select(i=>rootSpace.MultiplyPoint3x4(v[i])).Aggregate(Vector3.zero,(a,b)=>a+b)/tipIds.Length;
            Check(tip.z>0 && Mathf.Abs(tip.x)<.03f && Mathf.Abs(tip.y)<.03f,path+" points forward on local Z");
            Check(filter.GetComponent<Renderer>().sharedMaterials.SequenceEqual(bow.nockedArrow.GetComponent<Renderer>().sharedMaterials),path+" shares held arrow materials");
        }
        foreach(var name in new[]{"Gun_long","Gun_short"})
        {
            var w=Weapon(name).GetComponent<WeaponPresentation>();var m=w.visual.GetComponent<MeshFilter>().sharedMesh;var v=m.vertices;
            var rim=m.GetTriangles(Array.FindIndex(w.visual.GetComponent<Renderer>().sharedMaterials,m=>m.name=="Steel")).Distinct().Select(i=>v[i]).Where(p=>p.z>m.bounds.max.z-.015f).ToArray();
            var b=new Bounds(rim[0],Vector3.zero);foreach(var p in rim)b.Encapsulate(p);
            Check(Vector2.Distance(new Vector2(w.muzzle.localPosition.x,w.muzzle.localPosition.y),new Vector2(b.center.x,b.center.y))<.01f,name+" muzzle is centered on barrel opening");
        }
        var katana=Weapon("Katana").GetComponentInChildren<MeshFilter>().sharedMesh;
        var original=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/App/Models/Weapon/Katana.fbx").GetComponentInChildren<MeshFilter>();
        float sourceBoundary=BoundaryLength(original.sharedMesh,4,original.transform.localToWorldMatrix);
        sourceBoundary*=3f/original.GetComponent<Renderer>().bounds.size.x;
        float actualBoundary=BoundaryLength(katana,4,Matrix4x4.identity);
        Check(actualBoundary<=sourceBoundary+.02f,"Katana blade has no new open seams ("+actualBoundary.ToString("F3")+" versus source "+sourceBoundary.ToString("F3")+")");
        foreach(string name in new[]{"Spear","Gun_short","Katana","Naginata_red","Naginata_blue","Gun_long","Bow","knife","Gunbai"})
        foreach(var filter in Weapon(name).GetComponentsInChildren<MeshFilter>())
        {
            var m=filter.sharedMesh;var v=m.vertices;var t=m.triangles;int zero=0;
            for(int i=0;i<t.Length;i+=3)if(Vector3.Cross(v[t[i+1]]-v[t[i]],v[t[i+2]]-v[t[i]]).magnitude<Mathf.Pow(m.bounds.size.magnitude,2)*1e-12f)zero++;
            Check(zero==0,name+"/"+filter.name+" has no collapsed triangles ("+zero+")");
            Check(t.All(i=>i>=0&&i<v.Length)&&v.All(p=>float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z)),name+"/"+filter.name+" finite vertices and valid indices");
        }
        foreach(string path in Directory.GetFiles("Assets/App/Prefabs","*.prefab",SearchOption.AllDirectories).Concat(Directory.GetFiles("Assets/App/Art/Weapons","*.prefab")))
        {
            var go=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach(var r in go.GetComponentsInChildren<MeshRenderer>(true))
            {
                if(!r.TryGetComponent<MeshFilter>(out var f))continue;
                Check(f.sharedMesh!=null && f.sharedMesh.vertexCount>0 && r.sharedMaterials.Length>=f.sharedMesh.subMeshCount && r.sharedMaterials.All(m=>m!=null && m.shader!=null),path+"/"+r.name+" mesh/material assignments intact");
            }
        }
    }
    static float BoundaryLength(Mesh mesh,int sub,Matrix4x4 matrix)
    {
        var v=mesh.vertices.Select(matrix.MultiplyPoint3x4).ToArray();float unit=Vector3.Distance(v.Aggregate(Vector3.Min),v.Aggregate(Vector3.Max))*1e-6f;
        var weld=new Dictionary<(int,int,int),int>();var points=new List<Vector3>();var ids=new int[v.Length];
        for(int i=0;i<v.Length;i++){var p=v[i];var key=(Mathf.RoundToInt(p.x/unit),Mathf.RoundToInt(p.y/unit),Mathf.RoundToInt(p.z/unit));if(!weld.TryGetValue(key,out int j)){j=points.Count;points.Add(p);weld[key]=j;}ids[i]=j;}
        var edges=new Dictionary<(int,int),int>();var triangles=mesh.GetTriangles(sub);
        for(int i=0;i<triangles.Length;i+=3){int a=ids[triangles[i]],b=ids[triangles[i+1]],c=ids[triangles[i+2]];if(a==b||b==c||a==c)continue;Edge(a,b);Edge(b,c);Edge(c,a);}
        void Edge(int a,int b){var k=(Mathf.Min(a,b),Mathf.Max(a,b));edges.TryGetValue(k,out int n);edges[k]=n+1;}
        return edges.Where(e=>e.Value==1).Sum(e=>Vector3.Distance(points[e.Key.Item1],points[e.Key.Item2]));
    }
}
