using UnityEngine;

namespace App.Main.Player
{
    public class PlayerStatus
    {
        public Hp Hp { get; private set; }
        public AttackPoint AttackPoint { get; private set; }
        public DefensePoint DefensePoint { get; private set; }
        public MoveSpeed MoveSpeed { get; private set; }
        public EffectList EffectList { get; private set; }
        public int ImpactVersion { get; private set; }
        public Vector3? ImpactSource { get; private set; }
        public bool ImpactBlocked { get; private set; }

        public PlayerStatus(int hpMax, int attackPointDefault, float moveSpeedDefault, Player player, int defensePointDefault = 0)
        {
            Hp = new Hp(hpMax);
            AttackPoint = new AttackPoint(attackPointDefault);
            DefensePoint = new DefensePoint(defensePointDefault);
            MoveSpeed = new MoveSpeed(moveSpeedDefault);
            EffectList = new EffectList(player, this);
        }

        public void TakeDamage(int damage, Vector3? source = null)
        {
            int effectiveDamage = damage - DefensePoint.Current;
            if (effectiveDamage < 0)
            {
                effectiveDamage = 0;
            }
            Hp.Subtract(effectiveDamage);
            ImpactSource=source;ImpactBlocked=effectiveDamage==0;ImpactVersion++;
            Debug.Log($"Player took {effectiveDamage} damage, HP is now {Hp.Current}/{Hp.Max}");
        }

        public void DumpStatus()
        {
            Hp.DumpStatus();
            AttackPoint.DumpStatus();
            DefensePoint.DumpStatus();
            MoveSpeed.DumpStatus();
        }
    }
}
