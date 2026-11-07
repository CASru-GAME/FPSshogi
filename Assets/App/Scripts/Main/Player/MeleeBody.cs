using UnityEngine;

namespace App.Main.Player
{
    [DisallowMultipleComponent]
    public sealed class MeleeBody : MonoBehaviour
    {
        static Vector3[] vertices;
        static int[] triangles;
        static Bounds hullBounds;
        Renderer display;
        Matrix4x4 hullToDisplay;
        Vector3[] boxVertices;
        // A triangle clipped against six planes can gain at most six vertices.
        readonly Vector3[] polygon = new Vector3[9];
        readonly Vector3[] clipped = new Vector3[9];

        bool HasModel => display != null && display.gameObject.activeInHierarchy;
        public int Layer { get; private set; }
        public Bounds WorldBounds => HasModel ? display.bounds : default;

        public static MeleeBody For(Player player)
        {
            if (player == null || !player.TryGetComponent<MeleeBody>(out var body)) return null;
            return body.HasModel ? body : null;
        }

        public static void Bind(Player player, GameObject model)
        {
            if (!player.TryGetComponent<MeleeBody>(out var body)) body = player.gameObject.AddComponent<MeleeBody>();
            var filter = model.GetComponentInChildren<MeshFilter>();
            body.display = filter != null ? filter.GetComponent<MeshRenderer>() : null;
            if (body.display == null) return;
            body.Layer = filter.gameObject.layer;

            if (vertices == null)
            {
                // All nine piece FBXs share these 12 wooden outline vertices.
                var mesh = Resources.Load<Mesh>("Combat/ShogiPieceBody");
                vertices = mesh.vertices;
                triangles = mesh.triangles;
                hullBounds = mesh.bounds;
            }
            var displayBounds = filter.sharedMesh.bounds;
            var scale = new Vector3(displayBounds.size.x / hullBounds.size.x,
                displayBounds.size.y / hullBounds.size.y, displayBounds.size.z / hullBounds.size.z);
            body.hullToDisplay = Matrix4x4.TRS(displayBounds.center - Vector3.Scale(scale, hullBounds.center), Quaternion.identity, scale);
            body.boxVertices ??= new Vector3[vertices.Length];
        }

        // Clip the wooden faces against the blade box to obtain a contact on both shapes.
        // Only explicit melee queries use this geometry, so physics, projectiles and fields keep the root capsule.
        public bool Intersects(Vector3 center, Quaternion rotation, Vector3 halfExtents, out Vector3 contact)
        {
            contact = default;
            if (!HasModel) return false;
            var toBox = Matrix4x4.TRS(center, rotation, Vector3.one).inverse * display.localToWorldMatrix * hullToDisplay;
            Vector3 min = boxVertices[0] = toBox.MultiplyPoint3x4(vertices[0]);
            Vector3 max = min;
            for (int i = 1; i < vertices.Length; i++)
            {
                Vector3 point = boxVertices[i] = toBox.MultiplyPoint3x4(vertices[i]);
                min = Vector3.Min(min, point);
                max = Vector3.Max(max, point);
            }
            if (min.x > halfExtents.x || max.x < -halfExtents.x || min.y > halfExtents.y ||
                max.y < -halfExtents.y || min.z > halfExtents.z || max.z < -halfExtents.z)
                return false;
            bool mirrored = toBox.determinant < 0f;
            bool centerInside = true;
            float nearestPatch = float.PositiveInfinity, nearestPlane = float.PositiveInfinity;
            Vector3 patch = default, insideContact = default;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 a = boxVertices[triangles[i]], b = boxVertices[triangles[i + 1]], c = boxVertices[triangles[i + 2]];
                if (centerInside)
                {
                    Vector3 normal = Vector3.Cross(b - a, c - a);
                    float offset = Vector3.Dot(normal, a);
                    if (mirrored ? offset > 0f : offset < 0f) centerInside = false;
                    else if (normal.sqrMagnitude > 0f)
                    {
                        // An enclosed box touches no face. Its nearest supporting plane supplies the cover-ray endpoint.
                        Vector3 projection = normal * (offset / normal.sqrMagnitude);
                        if (projection.sqrMagnitude < nearestPlane)
                        {
                            nearestPlane = projection.sqrMagnitude;
                            insideContact = projection;
                        }
                    }
                }

                polygon[0] = a;
                polygon[1] = b;
                polygon[2] = c;
                int count = 3;
                for (int axis = 0; axis < 3 && count > 0; axis++)
                {
                    count = Clip(polygon, count, clipped, axis, 1f, halfExtents[axis]);
                    if (count > 0) count = Clip(clipped, count, polygon, axis, -1f, halfExtents[axis]);
                }
                if (count == 0) continue;
                Vector3 point = Vector3.zero;
                for (int j = 0; j < count; j++) point += polygon[j];
                point /= count;
                if (point.sqrMagnitude < nearestPatch)
                {
                    nearestPatch = point.sqrMagnitude;
                    patch = point;
                }
            }

            bool touchesFace = nearestPatch < float.PositiveInfinity;
            if (!touchesFace && !centerInside) return false;
            contact = center + rotation * (touchesFace ? patch : insideContact);
            return true;
        }

        static int Clip(Vector3[] input, int count, Vector3[] output, int axis, float sign, float extent)
        {
            int written = 0;
            Vector3 previous = input[count - 1];
            float previousDistance = extent - sign * previous[axis];
            for (int i = 0; i < count; i++)
            {
                Vector3 next = input[i];
                float distance = extent - sign * next[axis];
                if ((distance >= 0f) != (previousDistance >= 0f))
                    output[written++] = Vector3.LerpUnclamped(previous, next, previousDistance / (previousDistance - distance));
                if (distance >= 0f) output[written++] = next;
                previous = next;
                previousDistance = distance;
            }
            return written;
        }
    }
}
