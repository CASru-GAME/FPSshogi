using UnityEngine;

namespace App.Main.Player
{
    sealed class FlashbangAimPreview
    {
        const int CircleSegments = 64;
        readonly GameObject root;
        readonly LineRenderer trajectory;
        readonly LineRenderer boundary;
        readonly LineRenderer impactMark;
        readonly Vector3[] unitCircle = new Vector3[CircleSegments];
        readonly Vector3[] circle = new Vector3[CircleSegments];
        readonly Vector3[] diamond = new Vector3[4];
        Vector3[] path;

        internal FlashbangAimPreview(Player owner)
        {
            root = new GameObject("Flashbang aim");
            root.transform.SetParent(owner.transform, false);
            // The other duel camera excludes its own body layer. Guides on it are private to this seat.
            root.layer = owner.Opponent != null ? owner.Opponent.BodyLayer : owner.gameObject.layer;
            trajectory = Guide("Trajectory", new Color(1f, .93f, .71f), .065f, false);
            boundary = Guide("Blast radius", new Color(1f, .66f, .24f), .095f, true);
            impactMark = Guide("Impact", new Color(1f, .93f, .71f), .02f, true);
            for (int i = 0; i < unitCircle.Length; i++)
            {
                float angle = i * Mathf.PI * 2f / unitCircle.Length;
                unitCircle[i] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            }
        }

        LineRenderer Guide(string name, Color color, float width, bool loop)
        {
            var line = CombatVisuals.Line(root, name, System.Array.Empty<Vector3>(), color, width, loop);
            line.gameObject.layer = root.layer;
            line.useWorldSpace = true;
            line.numCapVertices = 3;
            line.numCornerVertices = 2;
            return line;
        }

        internal void Draw(Player owner, Vector3 origin, Vector3 velocity, float fuse, float radius)
        {
            float step = Time.fixedDeltaTime;
            int steps = Mathf.Max(1, Mathf.CeilToInt(fuse / step));
            if (path == null || path.Length < steps + 1) path = new Vector3[steps + 1];
            Vector3 position = origin, normal = Vector3.up;
            path[0] = origin;
            int count = 1;
            for (int i = 0; i < steps; i++)
            {
                float delta = Mathf.Min(step, fuse - i * step);
                position = GrenadeProjectile.Step(owner, position, ref velocity, delta, out bool hit, out normal);
                path[count++] = position;
                if (hit) break;
            }
            trajectory.positionCount = count;
            trajectory.SetPositions(path);
            // A fixed world width would cover much of the viewport beside the throwing hand.
            Camera camera = owner.DuelCamera;
            Transform view = camera.transform;
            float worldPerPixel = 2f * Mathf.Tan(camera.fieldOfView * .5f * Mathf.Deg2Rad) / Mathf.Max(1, camera.pixelHeight);
            float startScale = worldPerPixel * Mathf.Max(camera.nearClipPlane, Vector3.Dot(origin - view.position, view.forward));
            float endScale = worldPerPixel * Mathf.Max(camera.nearClipPlane, Vector3.Dot(position - view.position, view.forward));
            trajectory.startWidth = 3f * startScale;
            trajectory.endWidth = 3f * endScale;
            boundary.startWidth = boundary.endWidth = 3f * endScale;
            Quaternion plane = CombatVisuals.FlashbangRadiusPlane(normal);
            for (int i = 0; i < circle.Length; i++) circle[i] = position + plane * (unitCircle[i] * radius);
            boundary.positionCount = circle.Length;
            boundary.SetPositions(circle);
            Vector3 right = view.right * (7f * endScale), up = view.up * (7f * endScale);
            diamond[0] = position + up;
            diamond[1] = position + right;
            diamond[2] = position - up;
            diamond[3] = position - right;
            impactMark.startWidth = impactMark.endWidth = 2f * endScale;
            impactMark.positionCount = diamond.Length;
            impactMark.SetPositions(diamond);
        }

        internal void Remove()
        {
            if (root == null) return;
            root.SetActive(false);
            Object.Destroy(root);
        }
    }
}
