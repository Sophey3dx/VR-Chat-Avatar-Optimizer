using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using AvatarOutfitOptimizer.Utils;

namespace AvatarOutfitOptimizer.Cleanup
{
    /// <summary>
    /// Cleans up PhysBones: removes PhysBones on deleted objects and unused colliders
    /// </summary>
    public class PhysBoneCleanup
    {
        /// <summary>
        /// Cleans up PhysBones on duplicate avatar
        /// </summary>
        public void Cleanup(
            GameObject duplicate,
            PhysBoneAnalyzer.PhysBoneAnalysisResult analysis)
        {
            if (duplicate == null || analysis == null)
            {
                Debug.LogError("[AvatarOptimizer] PhysBoneCleanup: Invalid parameters");
                return;
            }

            // Remove PhysBones on deleted objects
            RemovePhysBonesOnDeletedObjects(duplicate, analysis);

            // Remove unused colliders
            RemoveUnusedColliders(duplicate, analysis);
        }

        private void RemovePhysBonesOnDeletedObjects(
            GameObject duplicate,
            PhysBoneAnalyzer.PhysBoneAnalysisResult analysis)
        {
            if (analysis.PhysBonesOnDeletedObjects == null || analysis.PhysBonesOnDeletedObjects.Count == 0)
            {
                return;
            }

            int removedCount = 0;

            foreach (var physBone in analysis.PhysBonesOnDeletedObjects)
            {
                if (physBone != null)
                {
                    Undo.DestroyObjectImmediate(physBone);
                    removedCount++;
                }
            }

            if (removedCount > 0)
            {
                Debug.Log($"[AvatarOptimizer] Removed {removedCount} PhysBones on deleted objects");
            }
        }

        private void RemoveUnusedColliders(
            GameObject duplicate,
            PhysBoneAnalyzer.PhysBoneAnalysisResult analysis)
        {
            if (analysis.UnusedColliders == null || analysis.UnusedColliders.Count == 0)
            {
                return;
            }

            // Remove unused colliders
            // Note: We need to be careful - colliders might be used by other systems
            // For now, we only remove colliders that are explicitly marked as unused by PhysBone analysis
            
            int removedCount = 0;

            foreach (var collider in analysis.UnusedColliders)
            {
                if (collider != null)
                {
                    // Check if collider is used by other PhysBones
                    bool isUsedElsewhere = CheckColliderUsage(collider, duplicate);
                    
                    if (!isUsedElsewhere)
                    {
                        Undo.DestroyObjectImmediate(collider);
                        removedCount++;
                    }
                }
            }

            if (removedCount > 0)
            {
                Debug.Log($"[AvatarOptimizer] Removed {removedCount} unused colliders");
            }
        }

        private bool CheckColliderUsage(Component collider, GameObject root)
        {
            // Check if collider is referenced by any other PhysBone
            // This requires reflection to access PhysBone collider arrays
            
            var physBoneType = GetPhysBoneType();
            if (physBoneType == null) return true; // Safe default: assume used

            var allPhysBones = root.GetComponentsInChildren(physBoneType, true);
            
            foreach (var physBone in allPhysBones)
            {
                if (physBone == null) continue;

                try
                {
                    var colliderProperty = physBone.GetType().GetProperty("colliders");
                    if (colliderProperty != null)
                    {
                        var colliders = colliderProperty.GetValue(physBone) as System.Array;
                        if (colliders != null)
                        {
                            foreach (var c in colliders)
                            {
                                if (c == collider)
                                {
                                    return true; // Collider is used
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Reflection failed - assume used to be safe
                    return true;
                }
            }

            return false; // Collider not found in any PhysBone
        }

        private Type GetPhysBoneType()
        {
            var type = Type.GetType("VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone, VRC.SDK3.Dynamics.PhysBone");
            if (type != null) return type;
            
            type = Type.GetType("VRC.Dynamics.VRCPhysBone, VRC.SDK3.Dynamics");
            return type;
        }
    }
}

