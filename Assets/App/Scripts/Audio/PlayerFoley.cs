using App.Main.Player;
using UnityEngine;

namespace App.Audio
{
    /// <summary>Footsteps depend on distance actually travelled, not held input.</summary>
    public sealed class PlayerFoley : MonoBehaviour
    {
        Player player;
        Collider body;
        Vector3 previous;
        float distance,airTime,lastSkillCooldown,climbDistance,stationaryTime;
        bool initialized,wasGrounded;
        readonly RaycastHit[] groundHits=new RaycastHit[12];
        void Awake(){player=GetComponent<Player>();body=GetComponent<Collider>();}
        public void ResetTracking(){initialized=false;distance=airTime=climbDistance=lastSkillCooldown=stationaryTime=0;}
        void LateUpdate()
        {
            if(player==null||body==null)return;
            GameAudio.Register(player);
            Vector3 position=transform.position;
            if(!player.enabled||player.PrimaryCooldownDuration<=0||GameAudio.CombatPaused||Time.timeScale<=0)
            {previous=position;initialized=false;return;}
            bool grounded=Ground(out var surface);
            if(!initialized){previous=position;wasGrounded=grounded;initialized=true;lastSkillCooldown=player.SkillCooldown;return;}
            Vector3 delta=position-previous;previous=position;
            if(delta.magnitude>4){ResetTracking();return;}
            if(lastSkillCooldown>0&&player.SkillCooldown<=0)GameAudio.Play(SoundCue.SkillReady,player);
            lastSkillCooldown=player.SkillCooldown;
            if(!grounded)airTime+=Time.deltaTime;
            if(grounded&&!wasGrounded)
            {
                if(airTime>.18f)GameAudio.Play(SoundCue.Land,player);
                airTime=0;distance=0;
            }
            if(player.IsClimbing)
            {
                climbDistance+=Mathf.Abs(delta.y);
                if(climbDistance>.9f){GameAudio.Play(SoundCue.Climb,player);climbDistance=0;}
                distance=0;
            }
            else if(grounded)
            {
                climbDistance=0;
                float moved=new Vector2(delta.x,delta.z).magnitude;
                if(moved>.002f){distance+=moved;stationaryTime=0;}
                else{stationaryTime+=Time.deltaTime;if(stationaryTime>.18f)distance=0;}
                if(distance>=1.65f)
                {
                    GameAudio.Play(IsWood(surface)?SoundCue.FootstepWood:SoundCue.FootstepSoft,player);
                    distance%=1.65f;
                }
            }
            else distance=0;
            wasGrounded=grounded;
        }
        bool Ground(out Collider surface)
        {
            surface=null;var bounds=body.bounds;
            var origin=new Vector3(bounds.center.x,bounds.min.y+.14f,bounds.center.z);
            int count=Physics.RaycastNonAlloc(origin,Vector3.down,groundHits,.3f,~0,QueryTriggerInteraction.Ignore);
            float closest=float.MaxValue;
            for(int i=0;i<count;i++)
            {
                var hit=groundHits[i];
                if(hit.collider.transform.IsChildOf(transform)||hit.normal.y<.5f||hit.distance>=closest)continue;
                closest=hit.distance;surface=hit.collider;
            }
            return surface!=null;
        }
        static bool IsWood(Collider surface)
        {
            if(surface==null)return false;
            string name=surface.name.ToLowerInvariant();
            if(name.Contains("board")||name.Contains("wood")||name.Contains("desk")||name.Contains("将棋盤")||name.Contains("箪笥")||name=="掛け軸床"||name=="柱")return true;
            // Field2's board is exported as 円柱.002. Identify it by its board
            // material as well; floor (床) and cushions keep the soft footsteps.
            var renderer=surface.GetComponent<Renderer>();
            return name=="円柱.002"||renderer!=null&&renderer.sharedMaterial!=null&&renderer.sharedMaterial.name.StartsWith("盤");
        }
    }
}
