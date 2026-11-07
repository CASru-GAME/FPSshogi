using UnityEngine;
namespace App.Main.Player
{
    public sealed class CombatVisualLifetime : MonoBehaviour
    {
        public Player owner;
        public float remaining;
        void Update()
        {
            remaining-=Time.deltaTime;
            if(owner==null||remaining<=0)
            {
                if(owner!=null&&remaining<=0&&TryGetComponent<KingTurret>(out _))
                    App.Audio.GameAudio.Play(App.Audio.SoundCue.TurretEnd,owner,transform.position);
                Destroy(gameObject);
            }
        }
    }
}
