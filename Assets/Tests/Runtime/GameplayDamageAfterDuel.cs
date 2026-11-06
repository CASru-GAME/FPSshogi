#if UNITY_EDITOR || FPS_GAMEPLAY_SMOKE
using App.Main.Player;
using UnityEngine;

// Reproduces another damage source later in the same frame's Update phase.
[DefaultExecutionOrder(1000)]
public sealed class GameplayDamageAfterDuel : MonoBehaviour
{
    public Player target;
    void Update() { if (target != null) { target.playerStatus.TakeDamage(10000); target = null; } }
}
#endif
