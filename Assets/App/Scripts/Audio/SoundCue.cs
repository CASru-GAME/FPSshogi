namespace App.Audio
{
    public enum AudioBus { Master, Effects, Interface }
    public enum SoundCue
    {
        UiNavigate, UiConfirm, UiBack, UiInvalid, UiOpen,
        PlayerJoin, PlayerReady, PlayerLeave, Disconnect,
        BoardCursor, BoardSelect, BoardCancel, BoardMove, BoardDrop, Promote,
        Countdown, DuelStart, DuelWin, DuelDraw, MatchWin,
        SpearSwing, SwordSwing, NaginataSwing, KnifeSwing, ShortGun, LongGun, BowRelease, BowNock,
        GuardRaise, GuardEnd, AimIn, AimOut,
        Leap, Throw, FlashBurst, Dash, DashStrike, GinShield, KinShield, BuffEnd,
        Ascend, FieldCast, FieldEnter, FieldExit, Command, TurretSpawn, TurretShot, TurretEnd,
        Hit, Block, Down, ArrowImpact, BulletImpact, FootstepSoft, FootstepWood, Land, Climb, SkillReady
    }
}
