using UnityEngine;

namespace App.Main.Player
{
    // Ability volumes and the shooter's own colliders are not shot blockers.
    // Choose the nearest remaining solid so filtering never skips a wall.
    public static class ShotPhysics
    {
        public static bool Raycast(Ray ray, float distance, LayerMask layers, Player owner,
            out RaycastHit nearest, Transform projectile = null)
        {
            nearest = default;
            bool found = false;
            float nearestDistance = distance;
            foreach (var hit in Physics.RaycastAll(ray, distance, layers.value, QueryTriggerInteraction.Ignore))
            {
                if (owner != null && hit.collider.transform.IsChildOf(owner.transform)) continue;
                if (projectile != null && hit.collider.transform.IsChildOf(projectile)) continue;
                if (hit.distance > nearestDistance) continue;
                nearest = hit;
                nearestDistance = hit.distance;
                found = true;
            }
            return found;
        }
    }
}
