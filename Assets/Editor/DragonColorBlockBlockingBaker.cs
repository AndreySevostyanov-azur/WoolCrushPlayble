using Playeble.Scripts.Gameplay.Dragon;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Playeble.EditorTools
{
    public static class DragonColorBlockBlockingBaker
    {
        private const float Margin = 0.05f;
        private const float DefaultExtent = 0.5f;

        [MenuItem("Tools/Blocks/Bake Blocking Blocks (All)")]
        public static void BakeAll()
        {
            var blocks = UnityEngine.Object.FindObjectsOfType<DragonColorBlock>(true);
            if (blocks == null || blocks.Length == 0)
            {
                return;
            }

            // Pre-cache bounds once (Collider.bounds or fallback).
            var cols = new Collider[blocks.Length];
            var bounds = new Bounds[blocks.Length];
            for (var i = 0; i < blocks.Length; i++)
            {
                var b = blocks[i];
                if (b == null)
                {
                    cols[i] = null;
                    bounds[i] = new Bounds(Vector3.zero, Vector3.one);
                    continue;
                }

                var col = b.GetComponentInChildren<Collider>(true);
                cols[i] = col;
                if (col != null)
                {
                    bounds[i] = col.bounds;
                }
                else
                {
                    var p = b.transform != null ? b.transform.position : Vector3.zero;
                    bounds[i] = new Bounds(p, new Vector3(DefaultExtent * 2f, DefaultExtent * 2f, DefaultExtent * 2f));
                }
            }

            var any = false;
            for (var i = 0; i < blocks.Length; i++)
            {
                var self = blocks[i];
                if (self == null)
                {
                    continue;
                }

                var tr = self.transform;
                if (tr == null)
                {
                    continue;
                }

                var startPos = tr.position;
                var forward = tr.forward;
                var forwardXZ = new Vector3(forward.x, 0f, forward.z);
                if (forwardXZ.sqrMagnitude < 0.0001f)
                {
                    forwardXZ = Vector3.forward;
                }
                forwardXZ.Normalize();
                var selfBounds = bounds[i];
                var selfCenter = selfBounds.center;
                var selfMin = selfBounds.min;
                var selfMax = selfBounds.max;

                // Luna runtime movement is mostly axis-aligned: pick dominant axis.
                var moveAlongX = Mathf.Abs(forwardXZ.x) >= Mathf.Abs(forwardXZ.z);
                var sign = moveAlongX ? (forwardXZ.x >= 0f ? 1f : -1f) : (forwardXZ.z >= 0f ? 1f : -1f);

                var candidates = new List<Candidate>(16);
                for (var j = 0; j < blocks.Length; j++)
                {
                    if (j == i)
                    {
                        continue;
                    }

                    var other = blocks[j];
                    if (other == null)
                    {
                        continue;
                    }

                    var otherBounds = bounds[j];
                    var otherCenter = otherBounds.center;
                    var otherMin = otherBounds.min;
                    var otherMax = otherBounds.max;

                    // In-front test + distance sort key (T).
                    float t;
                    if (moveAlongX)
                    {
                        t = (otherCenter.x - selfCenter.x) * sign;
                        if (t <= 0f)
                        {
                            continue;
                        }

                        // Lane overlap on Z (and Y) using AABB.
                        if (!Overlaps1D(selfMin.z - Margin, selfMax.z + Margin, otherMin.z, otherMax.z))
                        {
                            continue;
                        }
                        if (!Overlaps1D(selfMin.y - Margin, selfMax.y + Margin, otherMin.y, otherMax.y))
                        {
                            continue;
                        }
                    }
                    else
                    {
                        t = (otherCenter.z - selfCenter.z) * sign;
                        if (t <= 0f)
                        {
                            continue;
                        }

                        // Lane overlap on X (and Y) using AABB.
                        if (!Overlaps1D(selfMin.x - Margin, selfMax.x + Margin, otherMin.x, otherMax.x))
                        {
                            continue;
                        }
                        if (!Overlaps1D(selfMin.y - Margin, selfMax.y + Margin, otherMin.y, otherMax.y))
                        {
                            continue;
                        }
                    }

                    candidates.Add(new Candidate { Block = other, T = t });
                }

                candidates.Sort((a, b) => a.T.CompareTo(b.T));

                var baked = new DragonColorBlock[candidates.Count];
                for (var k = 0; k < candidates.Count; k++)
                {
                    baked[k] = candidates[k].Block;
                }

                Undo.RecordObject(self, "Bake Blocking Blocks");
                self.EditorSetBlockingBlocks(baked);
                EditorUtility.SetDirty(self);
                any = true;
            }

            if (any)
            {
                AssetDatabase.SaveAssets();
            }
        }

        [CustomEditor(typeof(DragonColorBlock))]
        private sealed class DragonColorBlockEditor : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();

                EditorGUILayout.Space();
                if (GUILayout.Button("Bake Blocking Blocks (All)"))
                {
                    BakeAll();
                }
            }
        }

        private struct Candidate
        {
            public DragonColorBlock Block;
            public float T;
        }

        private static bool Overlaps1D(float aMin, float aMax, float bMin, float bMax)
        {
            if (aMax < bMin) return false;
            if (aMin > bMax) return false;
            return true;
        }
    }
}

