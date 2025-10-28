namespace App.Main.Player
{
    public class PlayerStatus
    {
        public Hp Hp { get; private set; }
        public AttackPoint AttackPoint { get; private set; }
        public DefensePoint DefensePoint { get; private set; }
        public MoveSpeed MoveSpeed { get; private set; }
        public EffectList EffectList { get; private set; }

        public PlayerStatus(int hpMax, int attackPointDefault, float moveSpeedDefault)
        {
            Hp = new Hp(hpMax);
            AttackPoint = new AttackPoint(attackPointDefault);
            DefensePoint = new DefensePoint();
            MoveSpeed = new MoveSpeed(moveSpeedDefault);
            EffectList = new EffectList(this);
        }

        public void TakeDamage(int damage)
        {
            int effectiveDamage = damage - DefensePoint.Current;
            if (effectiveDamage < 0)
            {
                effectiveDamage = 0;
            }
            Hp.Subtract(effectiveDamage);
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
