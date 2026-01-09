using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AvatarOutfitOptimizer.Utils;

namespace AvatarOutfitOptimizer
{
    /// <summary>
    /// Analyzes PhysBones for issues and potential cleanup
    /// Bone-Pruning is OFF by default - high risk operation
    /// </summary>
    public class PhysBoneAnalyzer
    {
        public class PhysBoneAnalysisResult
        {
            public List<Component> PhysBonesOnDeletedObjects = new List<Component>();
            public List<Component> UnusedColliders = new List<Component>();
            public List<Transform> PrunableBones = new List<Transform>(); // Only if aggressive mode enabled
            public int TotalPhysBoneCount = 0;
            public int ActivePhysBoneCount = 0;
        }

        /// <summary>
        /// Analyzes PhysBones in the avatar
        /// </summary>
        public PhysBoneAnalysisResult Analyze(
            GameObject avatarRoot,
            AvatarSnapshot snapshot,
            bool aggressiveBonePruning = false)
        {
            var result = new PhysBoneAnalysisResult();
            
            if (avatarRoot == null || snapshot == null)
            {
                return result;
            }

            // Get PhysBone component type
            var physBoneType = GetPhysBoneType();
            if (physBoneType == null)
            {
                Debug.LogWarning("[AvatarOptimizer] VRC PhysBone type not found - PhysBone analysis skipped");
                return result;
            }

            // Find all PhysBones
            var allPhysBones = avatarRoot.GetComponentsInChildren(physBoneType, true)
                .Cast<Component>()
                .ToList();

            result.TotalPhysBoneCount = allPhysBones.Count;

            foreach (var physBone in allPhysBones)
            {
                if (physBone == null) continue;

                var transform = physBone.transform;
                string path = GetGameObjectPath(avatarRoot.transform, transform);

                // Check if PhysBone is on a deleted/inactive object
                if (!snapshot.ActiveGameObjectPaths.Contains(path))
                {
                    result.PhysBonesOnDeletedObjects.Add(physBone);
                }
                else
                {
                    result.ActivePhysBoneCount++;
                }

                // Analyze colliders (if PhysBone has collider references)
                AnalyzePhysBoneColliders(physBone, snapshot, result);

                // Bone pruning analysis (only if aggressive mode)
                if (aggressiveBonePruning)
                {
                    AnalyzeBonePruning(physBone, snapshot, result);
                }
            }

            return result;
        }

        private void AnalyzePhysBoneColliders(Component physBone, AvatarSnapshot snapshot, PhysBoneAnalysisResult result)
        {
            // PhysBones can reference colliders
            // We need to check if referenced colliders are still valid
            // This requires reflection to access PhysBone properties
            
            try
            {
                // Use reflection to get collider references
                var colliderProperty = physBone.GetType().GetProperty("colliders");
                if (colliderProperty != null)
                {
                    var colliders = colliderProperty.GetValue(physBone) as System.Array;
                    if (colliders != null)
                    {
                        foreach (var collider in colliders)
                        {
                            if (collider is Component colliderComponent)
                            {
                                string colliderPath = GetGameObjectPath(
                                    physBone.transform.root,
                                    colliderComponent.transform);
                                
                                if (!snapshot.ActiveGameObjectPaths.Contains(colliderPath))
                                {
                                    result.UnusedColliders.Add(colliderComponent);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // Reflection might fail - that's okay, we continue
                Debug.LogWarning($"[AvatarOptimizer] Could not analyze PhysBone colliders: {e.Message}");
            }
        }

        private void AnalyzeBonePruning(Component physBone, AvatarSnapshot snapshot, PhysBoneAnalysisResult result)
        {
            // Bone pruning is HIGH RISK
            // We only analyze, not automatically prune
            // User must explicitly enable aggressive mode
            
            try
            {
                // Get root transform from PhysBone
                var rootTransformProperty = physBone.GetType().GetProperty("rootTransform");
                if (rootTransformProperty != null)
                {
                    var rootTransform = rootTransformProperty.GetValue(physBone) as Transform;
                    if (rootTransform != null)
                    {
                        // Check if root bone and its children are used
                        AnalyzeBoneHierarchy(rootTransform, snapshot, result);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AvatarOptimizer] Could not analyze bone pruning: {e.Message}");
            }
        }

        private void AnalyzeBoneHierarchy(Transform root, AvatarSnapshot snapshot, PhysBoneAnalysisResult result)
        {
            // Recursively check if bones are used by meshes or other systems
            if (root == null) return;

            string bonePath = GetGameObjectPath(root.root, root);
            
            // Check if bone is used by active meshes
            bool isUsed = snapshot.UsedBonePaths.Contains(bonePath);
            
            if (!isUsed)
            {
                // Check children
                bool hasUsedChildren = false;
                foreach (Transform child in root)
                {
                    AnalyzeBoneHierarchy(child, snapshot, result);
                    string childPath = GetGameObjectPath(root.root, child);
                    if (snapshot.UsedBonePaths.Contains(childPath))
                    {
                        hasUsedChildren = true;
                    }
                }
                
                // Only mark as prunable if not used and no children are used
                if (!hasUsedChildren)
                {
                    result.PrunableBones.Add(root);
                }
            }
        }

        private Type GetPhysBoneType()
        {
            // Try VRC SDK 3 PhysBone type
            var type = Type.GetType("VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone, VRC.SDK3.Dynamics.PhysBone");
            if (type != null) return type;
            
            // Fallback: alternative namespace
            type = Type.GetType("VRC.Dynamics.VRCPhysBone, VRC.SDK3.Dynamics");
            return type;
        }

        private string GetGameObjectPath(Transform root, Transform target)
        {
            if (target == null) return "";
            if (target == root) return root.name;

            var path = new List<string>();
            var current = target;

            while (current != null && current != root)
            {
                path.Add(current.name);
                current = current.parent;
            }

            path.Reverse();
            return string.Join("/", path);
        }
    }
}

