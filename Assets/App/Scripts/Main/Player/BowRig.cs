using UnityEngine;

namespace App.Main.Player
{
    // The bow mesh, string and held arrow share the model's local coordinates.
    // Both ends of the string follow the same bend as the wooden tips.
    internal sealed class BowRig
    {
        readonly LineRenderer bowString;
        readonly Transform arrow;
        readonly Vector3 top, bottom, originalNock, relaxedNock, fullNock, arrowPosition;
        readonly Mesh mesh;
        readonly Vector3[] rest, offsets, vertices, restNormals, normalOffsets, normals;
        readonly float halfHeight;
        float lastDraw = -1f;

        internal BowRig(Transform model, LineRenderer bowString, Transform arrow)
        {
            this.bowString = bowString;
            this.arrow = arrow;
            top = bowString.GetPosition(0);
            originalNock = bowString.GetPosition(1);
            bottom = bowString.GetPosition(2);
            relaxedNock = Vector3.Lerp(bottom, top, Mathf.InverseLerp(bottom.y, top.y, originalNock.y));
            fullNock = originalNock + Vector3.back * .42f;
            if (arrow != null) arrowPosition = arrow.localPosition;

            var filter = model != null ? model.GetComponent<MeshFilter>() : null;
            if (filter == null || filter.sharedMesh == null || !filter.sharedMesh.isReadable) return;
            mesh = filter.mesh;
            mesh.MarkDynamic();
            halfHeight = Mathf.Max(.01f, Mathf.Max(Mathf.Abs(top.y), Mathf.Abs(bottom.y)));
            rest = mesh.vertices;
            restNormals = mesh.normals;
            vertices = new Vector3[rest.Length];
            offsets = new Vector3[rest.Length];
            normals = new Vector3[restNormals.Length];
            normalOffsets = new Vector3[restNormals.Length];
            for (int i = 0; i < rest.Length; i++)
            {
                offsets[i] = Bend(rest[i]);
                if (i >= restNormals.Length) continue;
                // Transform the normals with the slope of the bend, preserving the
                // original normals at the grip and across material seams.
                var n = restNormals[i];
                float height = rest[i].y / halfHeight;
                float ySlope = 1f - .30f * height * height;
                float zSlope = -.44f * height / halfHeight;
                var bent = new Vector3(n.x, (n.y - zSlope * n.z) / ySlope, n.z);
                normalOffsets[i] = bent.normalized - n;
            }
        }

        Vector3 Bend(Vector3 point)
        {
            float height = point.y / halfHeight;
            float weight = height * height;
            return new Vector3(0f, -.10f * point.y * weight, -.22f * weight);
        }

        internal void SetPose(float draw, bool showArrow)
        {
            if (arrow != null && arrow.gameObject.activeSelf != showArrow) arrow.gameObject.SetActive(showArrow);
            if (Mathf.Approximately(draw, lastDraw)) return;
            lastDraw = draw;
            if (mesh != null)
            {
                for (int i = 0; i < vertices.Length; i++) vertices[i] = rest[i] + offsets[i] * draw;
                for (int i = 0; i < normals.Length; i++) normals[i] = restNormals[i] + normalOffsets[i] * draw;
                mesh.SetVertices(vertices);
                mesh.SetNormals(normals);
                bowString.SetPosition(0, top + Bend(top) * draw);
                bowString.SetPosition(2, bottom + Bend(bottom) * draw);
            }
            var nock = Vector3.Lerp(relaxedNock, fullNock, draw);
            bowString.SetPosition(1, nock);
            if (arrow != null) arrow.localPosition = arrowPosition + nock - originalNock;
        }

        internal void Dispose() { if (mesh != null) Object.Destroy(mesh); }
    }
}
