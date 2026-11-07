using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace App.Main.Player
{
    public static class CombatVisuals
    {
        // Assets in Resources keep the URP variants referenced in Windows builds.
        public static Material Transparent => Resources.Load<Material>("Combat/Transparent");
        public static Material Glow => Resources.Load<Material>("Combat/Glow");
        public static GameObject Scope(Player owner,string name,float seconds)
        {
            var go=new GameObject(name);var life=go.AddComponent<CombatVisualLifetime>();life.owner=owner;life.remaining=seconds;return go;
        }
        public static void Clear(Player owner)
        {
            foreach(var effect in Object.FindObjectsByType<CombatVisualLifetime>(FindObjectsSortMode.None))
                if(effect.owner==owner)Object.Destroy(effect.gameObject);
        }
        public static LineRenderer Line(GameObject parent,string name,Vector3[] points,Color color,float width,bool loop=false)
        {
            var go=new GameObject(name);go.transform.SetParent(parent.transform,false);
            var line=go.AddComponent<LineRenderer>();line.useWorldSpace=false;line.sharedMaterial=Glow;
            line.positionCount=points.Length;line.SetPositions(points);line.loop=loop;line.startWidth=line.endWidth=width;
            line.startColor=line.endColor=color;line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;
            return line;
        }
        public static void Ring(GameObject parent,float radius,Color color,float width,bool vertical=false)
        {
            var points=Enumerable.Range(0,64).Select(i=>{float a=i*Mathf.PI*2/64;return vertical?new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0):new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);}).ToArray();
            Line(parent,"Boundary",points,color,width,true);
        }
        public static void Aura(Player player,string name,Color color,float seconds)
        {
            var root=Scope(player,name,seconds);root.transform.SetParent(player.transform,false);
            root.transform.localPosition=new Vector3(0,-.35f,0);
            Ring(root,1.2f,color,.035f,true);Ring(root,1.1f,color,.025f);
            foreach(var node in root.GetComponentsInChildren<Transform>())node.gameObject.layer=player.BodyLayer;
            var weapon=player.WeaponObject?.GetComponent<WeaponPresentation>();weapon?.Guard(seconds);
            // A small border near the grip makes the state visible to its owner too.
            if(weapon!=null)
            {
                var grip=Scope(player,name+"_Grip",seconds);grip.transform.SetParent(weapon.visual,false);
                grip.transform.localPosition=weapon.GripLocal+Vector3.up*.25f;Ring(grip,.22f,color,.018f,true);
            }
        }
        public static void Shot(Player player,Vector3 end,bool impact)
        {
            var view=player.GetComponentInChildren<Camera>();if(view==null)return;
            var weapon=player.WeaponObject?.GetComponent<WeaponPresentation>();
            Vector3 start=weapon!=null?weapon.MuzzlePosition:view.transform.position+view.transform.forward;
            ShotFrom(player,start,end,impact);
        }
        public static void ShotFrom(Player owner,Vector3 start,Vector3 end,bool impact)
        {
            var root=Scope(owner,"ShotFeedback",.12f);root.transform.position=start;
            Line(root,"Tracer",new[]{Vector3.zero,end-start},new Color(1,.73f,.28f,.65f),.018f);
            var flash=GameObject.CreatePrimitive(PrimitiveType.Sphere);flash.name="MuzzleFlash";Object.Destroy(flash.GetComponent<Collider>());
            flash.transform.SetParent(root.transform,false);flash.transform.localScale=Vector3.one*.16f;
            var r=flash.GetComponent<Renderer>();r.sharedMaterial=Glow;var block=new MaterialPropertyBlock();block.SetColor("_BaseColor",new Color(1,.75f,.3f));r.SetPropertyBlock(block);r.shadowCastingMode=ShadowCastingMode.Off;
            if(impact)Impact(owner,end);
        }
        public static void Impact(Player owner,Vector3 point)
        {
            var hit=Scope(owner,"ImpactFeedback",.2f);hit.transform.position=point;
            Line(hit,"Impact",new[]{new Vector3(-.12f,-.12f,0),new Vector3(.12f,.12f,0),Vector3.zero,new Vector3(-.12f,.12f,0),new Vector3(.12f,-.12f,0)},new Color(1,.85f,.4f),.025f);
        }

        internal static Quaternion FlashbangRadiusPlane(Vector3 normal)
        {
            // Show the horizontal reach beside players and walls, and follow walkable surfaces.
            return normal.y >= .85f ? Quaternion.FromToRotation(Vector3.up, normal) : Quaternion.identity;
        }

        public static void FlashbangBurst(Player owner, Vector3 center, Vector3 normal, float radius)
        {
            var burst = Scope(owner, "FlashbangBurst", .28f);
            burst.transform.SetPositionAndRotation(center, FlashbangRadiusPlane(normal));
            Ring(burst, radius, new Color(1f, .77f, .32f, .85f), .09f);

            var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "Flash";
            var collider = core.GetComponent<Collider>();
            collider.enabled = false;
            Object.Destroy(collider);
            core.transform.SetParent(burst.transform, false);
            core.transform.localScale = Vector3.one * 1.4f;
            var renderer = core.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = Glow;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var light = burst.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, .9f, .65f);
            light.range = radius;
            light.intensity = 6f;
            light.shadows = LightShadows.None;
        }
    }
}
