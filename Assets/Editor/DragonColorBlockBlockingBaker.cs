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
            var found = UnityEngine.Object.FindObjectsOfType<DragonColorBlock>(true);
            if (found == null || found.Length == 0)
            {
                return;
            }

            // Детерминированный порядок (по индексам siblings от корня к листу),
            // чтобы список не "прыгал" между вызовами FindObjectsOfType.
            var blocks = new List<DragonColorBlock>(found);
            blocks.Sort((a, b) => string.CompareOrdinal(
                BuildHierarchyOrderKey(a != null ? a.transform : null),
                BuildHierarchyOrderKey(b != null ? b.transform : null)));

            // 1) Перед бейком заполняем список блоков в GameBootstrap всеми
            // блоками, что есть на сцене.
            var bootstrap = UnityEngine.Object.FindObjectOfType<Playeble.Scripts.GameBootstrap>(true);
            if (bootstrap != null)
            {
                Undo.RecordObject(bootstrap, "Fill GameBootstrap Blocks");
                bootstrap.EditorSetBlocks(blocks.ToArray());
                EditorUtility.SetDirty(bootstrap);
            }

            // Pre-cache bounds once (Collider.bounds or fallback).
            var cols = new Collider[blocks.Count];
            var bounds = new Bounds[blocks.Count];
            for (var i = 0; i < blocks.Count; i++)
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
            for (var i = 0; i < blocks.Count; i++)
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

                // Блокеры ищем вдоль РЕАЛЬНОГО forward (XZ), а не по
                // доминантной оси. Иначе блок, стоящий под углом, движется по
                // диагонали, а детект считает его едущим строго по X или Z и
                // пропускает реальные блокеры на диагонали. Логика совпадает с
                // рантаймом StartBlockMoveOnClickSystem.GetForwardBlockerOrBoundary:
                // t = dot(forward, delta) для "впереди", и боковой отступ по
                // суммарным радиусам вместо AABB-полосы.
                // Боковая ось — перпендикуляр к forward в плоскости XZ (unit).
                var lateralDir = new Vector3(forwardXZ.z, 0f, -forwardXZ.x);
                var selfLateral = ProjectedHalfExtent(cols[i], selfBounds, lateralDir);

                var candidates = new List<Candidate>(16);
                for (var j = 0; j < blocks.Count; j++)
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

                    // In-front test (проекция на forward) + sort key (T).
                    var delta = otherCenter - selfCenter;
                    var deltaXZ = new Vector3(delta.x, 0f, delta.z);
                    var t = Vector3.Dot(forwardXZ, deltaXZ);
                    if (t <= 0f)
                    {
                        continue;
                    }

                    // Боковое отклонение от линии движения. Полупролёты —
                    // проекции ОРИЕНТИРОВАННЫХ коробок на боковую ось, а не
                    // раздутый мировой AABB (иначе повёрнутый блок ловит лишних
                    // блокеров сильно в стороне от пути).
                    var lateralDist = Mathf.Abs(Vector3.Dot(deltaXZ, lateralDir));
                    var otherLateral = ProjectedHalfExtent(cols[j], otherBounds, lateralDir);
                    var lateralLimit = selfLateral + otherLateral + Margin;
                    if (lateralDist > lateralLimit)
                    {
                        continue;
                    }

                    // Вертикальное разделение (разная высота не блокирует).
                    if (!Overlaps1D(selfMin.y - Margin, selfMax.y + Margin, otherMin.y, otherMax.y))
                    {
                        continue;
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

        private static string BuildHierarchyOrderKey(Transform t)
        {
            if (t == null)
            {
                return string.Empty;
            }

            var chain = new List<int>(8);
            var cur = t;
            while (cur != null)
            {
                chain.Add(cur.GetSiblingIndex());
                cur = cur.parent;
            }

            chain.Reverse();
            var sb = new System.Text.StringBuilder(chain.Count * 7);
            for (var i = 0; i < chain.Count; i++)
            {
                sb.Append(chain[i].ToString("D6"));
                sb.Append('/');
            }

            return sb.ToString();
        }

        // Полупролёт коробки коллайдера вдоль оси axis (unit, XZ).
        // Для BoxCollider — точная проекция ориентированной коробки
        // (инвариантна к повороту). Иначе — fallback на мировой AABB.
        private static float ProjectedHalfExtent(Collider col, Bounds fallbackBounds, Vector3 axis)
        {
            var box = col as BoxCollider;
            if (box == null)
            {
                var e = fallbackBounds.extents;
                return Mathf.Abs(axis.x) * e.x + Mathf.Abs(axis.y) * e.y + Mathf.Abs(axis.z) * e.z;
            }

            var trb = box.transform;
            var ls = trb.lossyScale;
            var hx = box.size.x * 0.5f * Mathf.Abs(ls.x);
            var hy = box.size.y * 0.5f * Mathf.Abs(ls.y);
            var hz = box.size.z * 0.5f * Mathf.Abs(ls.z);

            return hx * Mathf.Abs(Vector3.Dot(axis, trb.right))
                 + hy * Mathf.Abs(Vector3.Dot(axis, trb.up))
                 + hz * Mathf.Abs(Vector3.Dot(axis, trb.forward));
        }

        private static bool Overlaps1D(float aMin, float aMax, float bMin, float bMax)
        {
            if (aMax < bMin) return false;
            if (aMin > bMax) return false;
            return true;
        }
    }
}

