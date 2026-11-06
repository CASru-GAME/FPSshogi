using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using App.Main.ShogiThings;

namespace App.UI
{
    public sealed class RoomPresentation : MonoBehaviour
    {
        readonly Dictionary<int,Texture2D> portraits=new();
        RoomUiAssets assets;
        GameObject titlePieces;
        void Awake(){assets=RoomUiAssets.Load();}
        public void SetTitleRoom()
        {
            var camera=Camera.main;if(camera!=null){camera.transform.position=new Vector3(-38,65,-70);camera.transform.LookAt(new Vector3(12,30,6));camera.fieldOfView=52;camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;}
            if(assets==null||titlePieces!=null)return;
            titlePieces=new GameObject("Title board pieces");
            PieceType[] back={PieceType.Kyosya,PieceType.Keima,PieceType.Gin,PieceType.Kin,PieceType.King,PieceType.Kin,PieceType.Gin,PieceType.Keima,PieceType.Kyosya};
            for(int p=0;p<2;p++)for(int x=0;x<9;x++){Place(back[x],x,p==0?0:8,p);Place(PieceType.Fuhyo,x,p==0?2:6,p);}
            Place(PieceType.Kakugyo,1,1,0);Place(PieceType.Hisya,7,1,0);Place(PieceType.Hisya,1,7,1);Place(PieceType.Kakugyo,7,7,1);
        }
        static void DisablePhysics(GameObject model)
        {
            foreach(var collider in model.GetComponentsInChildren<Collider>())collider.enabled=false;
            foreach(var body in model.GetComponentsInChildren<Rigidbody>()){body.isKinematic=true;body.detectCollisions=false;}
            foreach(var behaviour in model.GetComponentsInChildren<MonoBehaviour>())behaviour.enabled=false;
        }
        static Bounds BoundsOf(GameObject model){var rs=model.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
        void Place(PieceType type,int x,int y,int player)
        {
            var prefab=assets.Prefab(type);if(prefab==null)return;
            var model=Instantiate(prefab,Vector3.zero,Quaternion.Euler(-90,0,player==0?0:-180),titlePieces.transform);DisablePhysics(model);
            var b=BoundsOf(model);model.transform.localScale*=4.02f/Mathf.Max(b.size.x,b.size.z);b=BoundsOf(model);
            var center=new Vector3(43.34f-(x+.5f)*47.01f/9,29.87f,24.325f-(y+.5f)*47f/9);
            model.transform.position+=center-new Vector3(b.center.x,b.min.y,b.center.z);
        }
        public Texture2D Portrait(PieceType type,bool promoted=false)
        {
            int key=(int)type*2+(promoted?1:0);if(portraits.TryGetValue(key,out var saved))return saved;
            var prefab=assets!=null?assets.Prefab(type):null;if(prefab==null)return null;
            var model=Instantiate(prefab,new Vector3(10000,10000,10000),promoted?Quaternion.Euler(90,-90,90):Quaternion.Euler(-90,0,0));
            DisablePhysics(model);foreach(var t in model.GetComponentsInChildren<Transform>())t.gameObject.layer=31;
            var b=BoundsOf(model);float size=Mathf.Max(b.size.x,b.size.z);
            var go=new GameObject("Piece portrait camera");var camera=go.AddComponent<Camera>();camera.enabled=false;
            camera.orthographic=true;camera.orthographicSize=size*.68f;camera.nearClipPlane=.01f;camera.farClipPlane=size*6;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.cullingMask=1<<31;camera.allowHDR=false;
            camera.transform.position=b.center+new Vector3(0,size*1.6f,size*.65f);camera.transform.LookAt(b.center);
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            var target=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32);var previous=RenderTexture.active;
            Texture2D image=null;
            try {
                target.Create();camera.aspect=1;
                if(GraphicsSettings.currentRenderPipeline!=null)RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
                else {camera.targetTexture=target;camera.Render();}
                RenderTexture.active=target;image=new Texture2D(256,256,TextureFormat.RGBA32,false);image.name="Portrait "+type+(promoted?" promoted":"");
                image.ReadPixels(new Rect(0,0,256,256),0,0);image.Apply();portraits[key]=image;return image;
            }finally{RenderTexture.active=previous;target.Release();Destroy(target);model.SetActive(false);Destroy(model);Destroy(go);}
        }
        void OnDestroy(){foreach(var texture in portraits.Values)if(texture!=null)Destroy(texture);if(titlePieces!=null)Destroy(titlePieces);}
    }
}
