using UnityEngine;
using UnityEngine.UI;

namespace App.UI
{
    // UI geometry stays sharp at every canvas scale and needs no external icon textures.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UiSymbol : MaskableGraphic
    {
        public enum Kind { Heart, Bow, Gun, Sword, Shield, Zoom, Jump, Dash, Flash, Slow, Turret, Gamepad, Keyboard, Check, Pause, Ring, LeftShade }
        public Kind Shape;
        float fill=1;
        public float Fill { get=>fill;set {value=Mathf.Clamp01(value);if(Mathf.Approximately(fill,value))return;fill=value;SetVerticesDirty();} }
        public void SetShape(Kind shape){if(Shape==shape)return;Shape=shape;SetVerticesDirty();}
        Vector2 Point(Vector2 p) => new Vector2(rectTransform.rect.xMin+p.x*rectTransform.rect.width,rectTransform.rect.yMin+p.y*rectTransform.rect.height);
        void Line(VertexHelper vh,float x1,float y1,float x2,float y2,float width=.055f)
        {
            Vector2 a=Point(new Vector2(x1,y1)),b=Point(new Vector2(x2,y2));
            Vector2 n=new Vector2(-(b-a).y,(b-a).x).normalized*width*Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*.5f;
            int start=vh.currentVertCount;vh.AddVert(a+n,color,Vector2.zero);vh.AddVert(b+n,color,Vector2.zero);vh.AddVert(b-n,color,Vector2.zero);vh.AddVert(a-n,color,Vector2.zero);vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
        }
        void Path(VertexHelper vh,params float[] p){for(int i=0;i+3<p.Length;i+=2)Line(vh,p[i],p[i+1],p[i+2],p[i+3]);}
        void Circle(VertexHelper vh,float x,float y,float r,float amount=1,float width=.05f)
        {
            int count=Mathf.CeilToInt(64*amount);for(int i=0;i<count;i++){
                float a=Mathf.PI*.5f-Mathf.PI*2*i/64f,b=Mathf.PI*.5f-Mathf.PI*2*Mathf.Min((i+1)/64f,amount);
                Line(vh,x+Mathf.Cos(a)*r,y+Mathf.Sin(a)*r,x+Mathf.Cos(b)*r,y+Mathf.Sin(b)*r,width);
            }
        }
        void Arrow(VertexHelper vh,float x,float y,float up=1){Line(vh,x,y-up*.22f,x,y+up*.22f);Path(vh,x-.15f,y+up*.07f,x,y+up*.22f,x+.15f,y+up*.07f);}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            switch(Shape)
            {
                case Kind.LeftShade:
                    int j=vh.currentVertCount;var clear=color;clear.a=0;
                    vh.AddVert(Point(Vector2.zero),color,Vector2.zero);vh.AddVert(Point(Vector2.up),color,Vector2.zero);vh.AddVert(Point(Vector2.one),clear,Vector2.zero);vh.AddVert(Point(Vector2.right),clear,Vector2.zero);vh.AddTriangle(j,j+1,j+2);vh.AddTriangle(j,j+2,j+3);break;
                case Kind.Ring: Circle(vh,.5f,.5f,.45f,fill,.055f);break;
                case Kind.Heart:
                    Vector2 last=Vector2.zero;for(int i=0;i<=64;i++){float t=i*Mathf.PI*2/64f;var p=new Vector2(.5f+Mathf.Pow(Mathf.Sin(t),3)*.39f,.52f+(13*Mathf.Cos(t)-5*Mathf.Cos(2*t)-2*Mathf.Cos(3*t)-Mathf.Cos(4*t))*.026f);if(i>0)Line(vh,last.x,last.y,p.x,p.y);last=p;}break;
                case Kind.Bow: Path(vh,.25f,.12f,.43f,.23f,.58f,.5f,.43f,.77f,.25f,.88f,.25f,.12f);Path(vh,.1f,.5f,.9f,.5f,.74f,.65f);Line(vh,.9f,.5f,.74f,.35f);break;
                case Kind.Gun: Path(vh,.1f,.67f,.85f,.67f,.85f,.48f,.43f,.48f,.32f,.15f,.12f,.15f,.23f,.48f,.1f,.48f,.1f,.67f);Line(vh,.47f,.48f,.47f,.32f);Line(vh,.47f,.32f,.37f,.32f);break;
                case Kind.Sword: Path(vh,.18f,.17f,.69f,.81f,.85f,.85f,.82f,.66f,.31f,.16f);Line(vh,.16f,.4f,.42f,.15f);break;
                case Kind.Shield: Path(vh,.5f,.9f,.83f,.76f,.78f,.4f,.65f,.2f,.5f,.1f,.35f,.2f,.22f,.4f,.17f,.76f,.5f,.9f);break;
                case Kind.Zoom: Circle(vh,.44f,.57f,.27f);Line(vh,.64f,.36f,.88f,.12f,.08f);Line(vh,.3f,.57f,.58f,.57f);Line(vh,.44f,.43f,.44f,.71f);break;
                case Kind.Jump: Arrow(vh,.5f,.55f);Line(vh,.18f,.16f,.82f,.16f);break;
                case Kind.Dash: Path(vh,.24f,.2f,.51f,.5f,.24f,.8f);Path(vh,.55f,.2f,.82f,.5f,.55f,.8f);break;
                case Kind.Flash: Circle(vh,.5f,.5f,.2f);for(int k=0;k<8;k++){float a=k*Mathf.PI/4;Line(vh,.5f+Mathf.Cos(a)*.3f,.5f+Mathf.Sin(a)*.3f,.5f+Mathf.Cos(a)*.44f,.5f+Mathf.Sin(a)*.44f);}break;
                case Kind.Slow: Circle(vh,.5f,.5f,.36f);Arrow(vh,.5f,.5f,-1);break;
                case Kind.Turret: Path(vh,.18f,.18f,.82f,.18f,.68f,.36f,.32f,.36f,.18f,.18f);Path(vh,.36f,.36f,.36f,.64f,.63f,.64f,.63f,.36f);Line(vh,.5f,.64f,.83f,.83f,.1f);break;
                case Kind.Gamepad: Path(vh,.24f,.72f,.76f,.72f,.9f,.25f,.73f,.19f,.61f,.37f,.39f,.37f,.27f,.19f,.1f,.25f,.24f,.72f);Line(vh,.22f,.54f,.43f,.54f);Line(vh,.325f,.44f,.325f,.64f);Circle(vh,.67f,.57f,.035f);Circle(vh,.76f,.46f,.035f);break;
                case Kind.Keyboard: Path(vh,.1f,.72f,.9f,.72f,.9f,.22f,.1f,.22f,.1f,.72f);for(int y=0;y<2;y++)for(int x=0;x<5;x++)Line(vh,.2f+x*.14f,.6f-y*.14f,.24f+x*.14f,.6f-y*.14f,.065f);Line(vh,.3f,.32f,.7f,.32f);break;
                case Kind.Check: Path(vh,.15f,.48f,.4f,.22f,.85f,.8f);break;
                case Kind.Pause: Line(vh,.35f,.18f,.35f,.82f,.12f);Line(vh,.65f,.18f,.65f,.82f,.12f);break;
            }
        }
    }
}
