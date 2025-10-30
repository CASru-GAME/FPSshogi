using System;
using UnityEngine;

namespace App.Main.Player
{
    public class SecondaryGuard : ISecondaryAction
    {
        public string secondaryActionName => "SecondaryGuard";
        public Player player { get; private set; }
        public PlayerStatus playerStatus { get; private set; }

        // 調整可能パラメータ
        public float cooldown = 8f;
        public int defenseIncrease = 999;
        public float buffDuration = 1f;

        // 実行状態
        private float cooldownTimer = 0f;
        private bool buffActive = false;
        private float buffTimer = 0f;

        public void SecondaryAction(Player player, PlayerStatus playerStatus)
        {
            if (player == null || playerStatus == null) return;

            this.player = player;
            this.playerStatus = playerStatus;
            Debug.Log("SecondaryGuard activated.");
            // クールダウン中は何もしない
            if (cooldownTimer > 0f) return;
            Debug.Log("SecondaryGuard not on cooldown.");
            // 既にバフが有効なら重ねない（リフレッシュしたければここを変更）
            if (buffActive) return;

            // 直接 PlayerStatus を変更して防御力を上げる
            try
            {
                playerStatus.DefensePoint.Add(defenseIncrease);
                Debug.Log("SecondaryGuard: Adding defense.");
            }
            catch (Exception)
            {
                // DefensePoint API が異なる場合のフォールバック（可能なら Current を直接操作）
                try
                {
                    var prop = playerStatus.GetType().GetProperty("DefensePoint");
                    if (prop != null)
                    {
                        var dp = prop.GetValue(playerStatus);
                        var addMethod = dp?.GetType().GetMethod("Add");
                        addMethod?.Invoke(dp, new object[] { defenseIncrease });
                        Debug.Log("SecondaryGuard: Used reflection to add defense.");
                    }
                }
                catch { }
            }

            // ステート更新
            buffActive = true;
            buffTimer = buffDuration;
            cooldownTimer = cooldown;
        }

        public void UpdateSecondaryAction()
        {
            // タイマー更新（呼び出し元の Update から定期的に呼ばれる想定）
            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;
                if (cooldownTimer < 0f) cooldownTimer = 0f;
            }

            if (!buffActive) return;

            buffTimer -= Time.deltaTime;
            if (buffTimer <= 0f)
            {
                // バフ解除：加えた分を引く
                try
                {
                    playerStatus.DefensePoint.Add(-defenseIncrease);
                    Debug.Log("SecondaryGuard: Removing defense.");
                }
                catch (Exception)
                {
                    try
                    {
                        var prop = playerStatus.GetType().GetProperty("DefensePoint");
                        if (prop != null)
                        {
                            var dp = prop.GetValue(playerStatus);
                            var addMethod = dp?.GetType().GetMethod("Add");
                            addMethod?.Invoke(dp, new object[] { -defenseIncrease });
                            Debug.Log("SecondaryGuard: Used reflection to remove defense.");
                        }
                    }
                    catch { }
                }

                buffActive = false;
                buffTimer = 0f;
            }
        }
    }
}