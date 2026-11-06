#if UNITY_EDITOR || FPS_GAMEPLAY_SMOKE
using App.Main.Player;
using UnityEngine;

// Reproduces a physics/early-Update lethal hit before ordinary duel management.
[DefaultExecutionOrder(-1000)]
public sealed class GameplayDamageBeforeDuel : MonoBehaviour
{
    public Player target;
    void Update() { if (target != null) { target.playerStatus.TakeDamage(10000); target = null; } }
}
#endif
