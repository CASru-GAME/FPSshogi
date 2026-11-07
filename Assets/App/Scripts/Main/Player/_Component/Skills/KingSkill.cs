using App.Audio;
using UnityEngine;
namespace App.Main.Player
{
    public class KingSkill : ISkill
    {
        public string skillName=>"KingSkill";
        public float cooldownTime=>12f;
        public float cooldownTimer{get;private set;}
        public bool isOnCooldown=>cooldownTimer>0;
        public PlayerStatus playerStatus{get;private set;}
        public Player player{get;private set;}
        public GameObject turretPrefab;
        public GameObject gunbaiPrefab;
        public float skillDuration=3.5f;
        public float turretFireInterval=1f;
        public float turretRange=32f;
        public float turretFalloffStart=14f;
        public float turretMinDamageMul=.5f;
        public float turretDamageMultiplier=.25f;
        public LayerMask turretHitLayers=~0;
        public void UseSkill(Player player,PlayerStatus playerStatus)
        {
            if(isOnCooldown||player==null||playerStatus==null)return;
            this.player=player;this.playerStatus=playerStatus;cooldownTimer=cooldownTime;
            GameAudio.Play(SoundCue.Command,player);
            player.WeaponObject?.GetComponent<WeaponPresentation>()?.PlaySkill(WeaponPresentation.SkillMotion.Command,.85f);
            if(gunbaiPrefab!=null)
            {
                var fan=Object.Instantiate(gunbaiPrefab,player.transform);fan.name="Gunbai_Command";
                var presentation=fan.GetComponent<WeaponPresentation>();presentation?.Bind(player);presentation?.Attack();
                var life=fan.AddComponent<CombatVisualLifetime>();life.owner=player;life.remaining=.85f;
            }
            var proto=turretPrefab!=null?turretPrefab:player.ObjectForKingSkill;if(proto==null)return;
            var offsets=new[]{new Vector3(-3,0,4),new Vector3(3,0,4),new Vector3(0,0,6.5f)};
            foreach(var offset in offsets)
            {
                Vector3 position=player.transform.TransformPoint(offset);
                float floor=player.transform.position.y-1.6f;
                // Look for a clear patch if a nearby board piece would lift a turret into the sightline.
                foreach(var shift in new[]{Vector3.zero,player.transform.forward*2,-player.transform.forward*2,player.transform.right*2,-player.transform.right*2})
                {
                    Vector3 candidate=position+shift;
                    var ground=Physics.RaycastAll(candidate+Vector3.up*4,Vector3.down,20,~0,QueryTriggerInteraction.Ignore);
                    float best=float.MaxValue;float height=floor;
                    foreach(var hit in ground)if(hit.collider.GetComponentInParent<Player>()==null&&hit.distance<best){height=hit.point.y;best=hit.distance;}
                    if(height<=player.transform.position.y-1.5f){position=candidate;floor=height;break;}
                }
                position.y=floor+.02f;
                var turret=Object.Instantiate(proto,position,player.transform.rotation);turret.name="KingTurret";
                GameAudio.Play(SoundCue.TurretSpawn,player,position);
                var life=turret.AddComponent<CombatVisualLifetime>();life.owner=player;life.remaining=skillDuration;
                var script=turret.GetComponent<KingTurret>()??turret.AddComponent<KingTurret>();
                script.Initialize(player,playerStatus,turretFireInterval,turretRange,turretFalloffStart,turretMinDamageMul,turretDamageMultiplier,turretHitLayers,"");
            }
        }
        public void UpdateSkill(){cooldownTimer=Mathf.Max(0,cooldownTimer-Time.deltaTime);}
    }
}
