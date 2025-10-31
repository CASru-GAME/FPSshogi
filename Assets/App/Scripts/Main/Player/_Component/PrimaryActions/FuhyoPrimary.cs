using System;
using System.Collections.Generic;
using UnityEngine;

namespace App.Main.Player
{
    public class FuhyoPrimary : IPrimaryAction
    {
        public string primaryActionName => "FuhyoPrimary";
        public Player player { get; private set; }
        public PlayerStatus playerStatus { get; private set; }

        // クールダウン（秒）
        public float cooldown = 1.0f;
        private float lastUseTime = -999f;

        // 攻撃対象レイヤー等（必要であれば調整）
        public LayerMask hitLayers = ~0;

        // 実行時状態
        private HashSet<Player> hitTargets = new HashSet<Player>();
        private Action<Player> onPlayerHitHandler;

        // Player を引数で受け取る仕様に合わせて処理を簡潔化
        // 呼び出し側: PrimaryAction(player, player.playerStatus)
        public void PrimaryAction(Player player, PlayerStatus playerStatus)
        {
            if (player == null || playerStatus == null) return;

            // クールダウンチェック
            if (Time.time - lastUseTime < cooldown) return;

            // Player（owner）から PlayerWeapon を探す
            PlayerWeapon weapon = player.GetComponentInChildren<PlayerWeapon>();
            if (weapon == null && player.WeaponObject != null)
                weapon = player.WeaponObject.GetComponentInChildren<PlayerWeapon>();

            if (weapon == null)
            {
                Debug.LogWarning("[FuhyoPrimary] PlayerWeapon component not found on owner.");
                return;
            }

            // 所有者をセット
            weapon.SetOwner(player);

            // アニメ再生して再生時間を取得（ヒット有効時間に使う）
            float attackDuration = weapon.PlayAnimation(weapon.attackAnimationName);
            if (attackDuration <= 0f) attackDuration = 0.1f;

            // 初期化
            hitTargets.Clear();
            lastUseTime = Time.time;

            // ヒット通知イベントを購読（ラムダで重複ヒット防止とダメージ適用を行う）
            onPlayerHitHandler = (Player target) =>
            {
                if (target == null) return;
                if (hitTargets.Contains(target)) return;
                hitTargets.Add(target);

                // ダメージは owner の AttackPoint をそのまま使用
                int damage = 0;
                try
                {
                    if (player.playerStatus != null && player.playerStatus.AttackPoint != null)
                        damage = player.playerStatus.AttackPoint.Current;
                }
                catch { damage = 0; }

                var ts = target.playerStatus;
                if (ts != null)
                {
                    try { ts.TakeDamage(damage); }
                    catch (Exception) { }
                }
            };

            weapon.OnPlayerHit += onPlayerHitHandler;

            // 当たり判定を有効化（アニメの長さを有効時間に使用）
            weapon.ActivateHitbox(attackDuration, () =>
            {
                // 終了コールバック：イベント解除
                try { weapon.OnPlayerHit -= onPlayerHitHandler; }
                catch (Exception) { }
                hitTargets.Clear();
            });
        }
    }
}
