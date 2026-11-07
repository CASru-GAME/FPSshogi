using System;
using App.Main.Player;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class MeleeBodyChecks
{
    public static void Run(Action<string, bool> check)
    {
        var root = new GameObject("Melee body check");
        try
        {
            root.transform.SetPositionAndRotation(new Vector3(2200f, 220f, 2000f), Quaternion.Euler(0f, 41f, 0f));
            root.transform.localScale = Vector3.one * 2f; // Same as Player.prefab.
            var movement = root.AddComponent<CapsuleCollider>();
            var target = root.AddComponent<Player>();
            target.enabled = false;
            Vector3 half = Vector3.one * .02f;
            check("A player fixture without a model has no wooden melee target", MeleeBody.For(target) == null);

            foreach (var piece in new[] { "fuhyo", "kyosya", "keima", "gin", "kin", "kakugyo", "hisya", "ou", "gyoku" })
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/App/Models/Pieces/{piece}.fbx");
                var model = Object.Instantiate(asset);
                model.transform.SetParent(root.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.layer = 30;
                var filter = model.GetComponentInChildren<MeshFilter>();
                MeleeBody.Bind(target, model);
                var body = MeleeBody.For(target);
                check($"{piece} uses the displayed wood with no physics collider", body != null && body.Layer == 30 && model.GetComponentInChildren<Collider>() == null);
                if (body != null)
                {
                    var bounds = filter.sharedMesh.bounds;
                    Vector3 At(float x, float y, float z) => filter.transform.TransformPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z)));
                    bool Touches(Vector3 point) => body.Intersects(point, root.transform.rotation, half, out _);

                    foreach (float yaw in new[] { 0f, 180f })
                    {
                        model.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                        Physics.SyncTransforms();
                        Vector3 edge = At(.80f, -.30f, 0f);
                        check($"{piece} at {yaw}° registers the visible edge beyond the movement capsule",
                            body.WorldBounds.Contains(edge) && Touches(edge) && Vector3.Distance(edge, movement.ClosestPoint(edge)) > .2f);
                        check($"{piece} at {yaw}° misses the empty corner above the sloping shoulder", !Touches(At(.82f, .88f, 0f)));
                        check($"{piece} at {yaw}° misses a blade in front of the face", !Touches(At(0f, 0f, 1.6f)));
                        Vector3 tip = At(0f, 1f, 0f);
                        Vector3 blade = tip + filter.transform.up * .01f;
                        check($"{piece} at {yaw}° returns a contact at the blade crossing the wooden tip",
                            body.Intersects(blade, root.transform.rotation, half, out Vector3 contact) && Vector3.Distance(tip, contact) < .045f);
                        check($"{piece} at {yaw}° stays out of world overlaps including triggers",
                            !Physics.CheckSphere(edge, .03f, 1 << model.layer, QueryTriggerInteraction.Collide));
                    }
                }
                Object.DestroyImmediate(model);
                check($"{piece} releases its melee surface with the displayed model",
                    MeleeBody.For(target) == null && body != null && !body.Intersects(root.transform.position, Quaternion.identity, half, out _));
            }
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
