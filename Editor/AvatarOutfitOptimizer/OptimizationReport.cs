using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AvatarOutfitOptimizer
{
    /// <summary>
    /// VRChat Performance Rank Limits (approximate values based on VRChat documentation)
    /// These may change - always verify with official VRChat documentation
    /// </summary>
    public static class VRChatLimits
    {
        // Excellent/Very Good limits
        public const int ExcellentMeshes = 1;
        public const int ExcellentMaterials = 4;
        public const int ExcellentBones = 75;
        public const int ExcellentPhysBones = 4;
        public const int ExcellentPhysBoneTransforms = 16;
        
        // Good limits
        public const int GoodMeshes = 16;
        public const int GoodMaterials = 32;
        public const int GoodBones = 400;
        public const int GoodPhysBones = 32;
        public const int GoodPhysBoneTransforms = 256;
        
        // Medium limits
        public const int MediumMeshes = 32;
        public const int MediumMaterials = 64;
        public const int MediumBones = 400;
        public const int MediumPhysBones = 64;
        public const int MediumPhysBoneTransforms = 512;
        
        // Poor = anything above Medium limits
    }

    /// <summary>
    /// Performance tier estimation (NOT guaranteed VRChat rank)
    /// </summary>
    public enum EstimatedPerformanceTier
    {
        Poor,
        Medium,
        Good,
        Excellent
    }

    /// <summary>
    /// Comprehensive optimization report with before/after comparison
    /// </summary>
    [Serializable]
    public class OptimizationReport
    {
        public AvatarSnapshot BeforeSnapshot { get; set; }
        public AvatarSnapshot AfterSnapshot { get; set; }
        public SnapshotComparison Comparison { get; set; }
        public EstimatedPerformanceTier BeforeTier { get; set; }
        public EstimatedPerformanceTier AfterTier { get; set; }
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Recommendations { get; set; } = new List<string>();

        // Performance metrics
        public int BeforeMeshCount { get; set; }
        public int AfterMeshCount { get; set; }
        public int BeforeMaterialCount { get; set; }
        public int AfterMaterialCount { get; set; }
        public int BeforeBoneCount { get; set; }
        public int AfterBoneCount { get; set; }
        public int BeforePhysBoneCount { get; set; }
        public int AfterPhysBoneCount { get; set; }
        public int BeforeParameterCount { get; set; }
        public int AfterParameterCount { get; set; }

        /// <summary>
        /// Generates a human-readable report string
        /// </summary>
        public string GenerateReportText()
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("=== VRChat Avatar Optimization Report ===");
            sb.AppendLine();
            
            // Performance Tier (NOT "Green Rank")
            sb.AppendLine("Estimated Performance Tier:");
            sb.AppendLine($"  Before: {BeforeTier} (Estimated)");
            sb.AppendLine($"  After:  {AfterTier} (Estimated)");
            sb.AppendLine();
            sb.AppendLine("NOTE: This is an ESTIMATE based on heuristics.");
            sb.AppendLine("VRChat ranking is not 100% deterministic.");
            sb.AppendLine("Actual rank may vary. Always test your avatar!");
            sb.AppendLine();

            // Metrics comparison with VRChat limits
            sb.AppendLine("Performance Metrics (Current → After | Good Limit):");
            sb.AppendLine($"  Meshes:      {BeforeMeshCount,3} → {AfterMeshCount,3} ({(AfterMeshCount - BeforeMeshCount):+0;-#}) | Good: {VRChatLimits.GoodMeshes} {GetLimitStatus(AfterMeshCount, VRChatLimits.GoodMeshes)}");
            sb.AppendLine($"  Materials:   {BeforeMaterialCount,3} → {AfterMaterialCount,3} ({(AfterMaterialCount - BeforeMaterialCount):+0;-#}) | Good: {VRChatLimits.GoodMaterials} {GetLimitStatus(AfterMaterialCount, VRChatLimits.GoodMaterials)}");
            sb.AppendLine($"  Bones:       {BeforeBoneCount,3} → {AfterBoneCount,3} ({(AfterBoneCount - BeforeBoneCount):+0;-#}) | Good: {VRChatLimits.GoodBones} {GetLimitStatus(AfterBoneCount, VRChatLimits.GoodBones)}");
            sb.AppendLine($"  PhysBones:   {BeforePhysBoneCount,3} → {AfterPhysBoneCount,3} ({(AfterPhysBoneCount - BeforePhysBoneCount):+0;-#}) | Good: {VRChatLimits.GoodPhysBones} {GetLimitStatus(AfterPhysBoneCount, VRChatLimits.GoodPhysBones)}");
            sb.AppendLine($"  Parameters:  {BeforeParameterCount,3} → {AfterParameterCount,3} ({(AfterParameterCount - BeforeParameterCount):+0;-#})");
            sb.AppendLine();

            // Avatar Fingerprint
            if (BeforeSnapshot != null && AfterSnapshot != null)
            {
                sb.AppendLine("Avatar Fingerprint:");
                sb.AppendLine($"  Before: {BeforeSnapshot.AvatarFingerprint}");
                sb.AppendLine($"  After:  {AfterSnapshot.AvatarFingerprint}");
                sb.AppendLine();
            }

            // Changes summary
            if (Comparison != null)
            {
                sb.AppendLine("Changes Summary:");
                
                if (Comparison.RemovedGameObjects != null && Comparison.RemovedGameObjects.Count > 0)
                {
                    sb.AppendLine($"  Removed {Comparison.RemovedGameObjects.Count} inactive GameObjects");
                }
                
                if (Comparison.RemovedRenderers != null && Comparison.RemovedRenderers.Count > 0)
                {
                    sb.AppendLine($"  Removed {Comparison.RemovedRenderers.Count} unused SkinnedMeshRenderers");
                }
                
                if (Comparison.RemovedBones != null && Comparison.RemovedBones.Count > 0)
                {
                    sb.AppendLine($"  Removed {Comparison.RemovedBones.Count} unused bones");
                }
                
                if (Comparison.RemovedPhysBones != null && Comparison.RemovedPhysBones.Count > 0)
                {
                    sb.AppendLine($"  Removed {Comparison.RemovedPhysBones.Count} PhysBones on deleted objects");
                }
                
                if (Comparison.RemovedParameters != null && Comparison.RemovedParameters.Count > 0)
                {
                    sb.AppendLine($"  Removed {Comparison.RemovedParameters.Count} unused parameters");
                }
                
                sb.AppendLine();
            }

            // Warnings
            if (Warnings != null && Warnings.Count > 0)
            {
                sb.AppendLine("Warnings:");
                foreach (var warning in Warnings)
                {
                    sb.AppendLine($"  ⚠ {warning}");
                }
                sb.AppendLine();
            }

            // Recommendations
            if (Recommendations != null && Recommendations.Count > 0)
            {
                sb.AppendLine("Recommendations:");
                foreach (var recommendation in Recommendations)
                {
                    sb.AppendLine($"  • {recommendation}");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Returns a status indicator comparing value to limit
        /// </summary>
        private static string GetLimitStatus(int value, int limit)
        {
            if (value <= limit)
                return "✓";
            else if (value <= limit * 1.5)
                return "⚠";
            else
                return "✗";
        }

        /// <summary>
        /// Estimates performance tier based on metrics using VRChat limits
        /// Uses heuristics - NOT guaranteed VRChat rank
        /// </summary>
        public static EstimatedPerformanceTier EstimatePerformanceTier(
            int meshCount,
            int materialCount,
            int boneCount,
            int physBoneCount,
            int parameterCount)
        {
            // Check against VRChat limits
            // Avatar rank is determined by the WORST category
            
            // Check for Excellent tier
            bool isExcellent = 
                meshCount <= VRChatLimits.ExcellentMeshes &&
                materialCount <= VRChatLimits.ExcellentMaterials &&
                boneCount <= VRChatLimits.ExcellentBones &&
                physBoneCount <= VRChatLimits.ExcellentPhysBones;
            
            if (isExcellent) return EstimatedPerformanceTier.Excellent;
            
            // Check for Good tier
            bool isGood = 
                meshCount <= VRChatLimits.GoodMeshes &&
                materialCount <= VRChatLimits.GoodMaterials &&
                boneCount <= VRChatLimits.GoodBones &&
                physBoneCount <= VRChatLimits.GoodPhysBones;
            
            if (isGood) return EstimatedPerformanceTier.Good;
            
            // Check for Medium tier
            bool isMedium = 
                meshCount <= VRChatLimits.MediumMeshes &&
                materialCount <= VRChatLimits.MediumMaterials &&
                boneCount <= VRChatLimits.MediumBones &&
                physBoneCount <= VRChatLimits.MediumPhysBones;
            
            if (isMedium) return EstimatedPerformanceTier.Medium;
            
            return EstimatedPerformanceTier.Poor;
        }

        /// <summary>
        /// Creates a report from before/after snapshots
        /// </summary>
        public static OptimizationReport Create(
            AvatarSnapshot before,
            AvatarSnapshot after,
            SnapshotComparison comparison)
        {
            var report = new OptimizationReport
            {
                BeforeSnapshot = before,
                AfterSnapshot = after,
                Comparison = comparison
            };

            if (before != null)
            {
                report.BeforeMeshCount = before.MeshCount;
                report.BeforeMaterialCount = before.MaterialCount;
                report.BeforeBoneCount = before.BoneCount;
                report.BeforePhysBoneCount = before.PhysBoneCount;
                report.BeforeParameterCount = before.ParameterCount;
                
                report.BeforeTier = EstimatePerformanceTier(
                    before.MeshCount,
                    before.MaterialCount,
                    before.BoneCount,
                    before.PhysBoneCount,
                    before.ParameterCount);
            }

            if (after != null)
            {
                report.AfterMeshCount = after.MeshCount;
                report.AfterMaterialCount = after.MaterialCount;
                report.AfterBoneCount = after.BoneCount;
                report.AfterPhysBoneCount = after.PhysBoneCount;
                report.AfterParameterCount = after.ParameterCount;
                
                report.AfterTier = EstimatePerformanceTier(
                    after.MeshCount,
                    after.MaterialCount,
                    after.BoneCount,
                    after.PhysBoneCount,
                    after.ParameterCount);
            }

            // Generate warnings
            GenerateWarnings(report);
            
            // Generate recommendations
            GenerateRecommendations(report);

            return report;
        }

        private static void GenerateWarnings(OptimizationReport report)
        {
            if (report.AfterSnapshot == null) return;

            // Warn if bone count exceeds Good limit
            if (report.AfterBoneCount > VRChatLimits.GoodBones)
            {
                report.Warnings.Add($"Bone count ({report.AfterBoneCount}) exceeds Good limit ({VRChatLimits.GoodBones}). This cannot be automatically reduced - bones are shared across meshes.");
            }

            // Warn if PhysBone count exceeds Good limit
            if (report.AfterPhysBoneCount > VRChatLimits.GoodPhysBones)
            {
                report.Warnings.Add($"PhysBone count ({report.AfterPhysBoneCount}) exceeds Good limit ({VRChatLimits.GoodPhysBones}). Disable unused outfit PhysBones.");
            }

            // Warn if mesh count exceeds Good limit
            if (report.AfterMeshCount > VRChatLimits.GoodMeshes)
            {
                report.Warnings.Add($"Mesh count ({report.AfterMeshCount}) exceeds Good limit ({VRChatLimits.GoodMeshes}). Consider merging meshes.");
            }

            // Warn if material count exceeds Good limit
            if (report.AfterMaterialCount > VRChatLimits.GoodMaterials)
            {
                report.Warnings.Add($"Material count ({report.AfterMaterialCount}) exceeds Good limit ({VRChatLimits.GoodMaterials}). Consider texture atlasing.");
            }

            // Warn if bone count didn't change despite mesh reduction (shared skeleton)
            if (report.BeforeBoneCount == report.AfterBoneCount && 
                report.BeforeMeshCount > report.AfterMeshCount &&
                report.AfterBoneCount > VRChatLimits.GoodBones)
            {
                report.Warnings.Add("Bone count unchanged despite mesh reduction. All meshes share the same skeleton - bone reduction requires external tools like Blender.");
            }

            // Warn if tier didn't improve
            if (report.BeforeTier == report.AfterTier && report.BeforeTier != EstimatedPerformanceTier.Excellent)
            {
                report.Warnings.Add("Performance tier did not improve. Consider more aggressive optimization or manual cleanup.");
            }
        }

        private static void GenerateRecommendations(OptimizationReport report)
        {
            if (report.AfterSnapshot == null) return;

            // Recommendations based on current state vs VRChat limits
            if (report.AfterMeshCount > VRChatLimits.GoodMeshes)
            {
                report.Recommendations.Add($"Merge meshes to get under {VRChatLimits.GoodMeshes} for Good rank.");
            }

            if (report.AfterMaterialCount > VRChatLimits.GoodMaterials)
            {
                report.Recommendations.Add($"Use texture atlasing to get under {VRChatLimits.GoodMaterials} materials for Good rank.");
            }

            if (report.AfterBoneCount > VRChatLimits.GoodBones)
            {
                report.Recommendations.Add($"Bone count ({report.AfterBoneCount}) exceeds Good limit ({VRChatLimits.GoodBones}). Use Blender to remove unused bones from the armature.");
            }

            if (report.AfterPhysBoneCount > VRChatLimits.GoodPhysBones)
            {
                report.Recommendations.Add($"Reduce PhysBones from {report.AfterPhysBoneCount} to under {VRChatLimits.GoodPhysBones} by disabling outfit physics you don't need.");
            }

            if (report.AfterTier == EstimatedPerformanceTier.Excellent)
            {
                report.Recommendations.Add("Avatar meets Excellent tier limits! Test in VRChat to verify actual rank.");
            }
            else if (report.AfterTier == EstimatedPerformanceTier.Good)
            {
                report.Recommendations.Add("Avatar meets Good tier limits. Test in VRChat to verify actual rank.");
            }
            else
            {
                report.Recommendations.Add("Test optimized avatar in VRChat to verify actual performance rank.");
            }
        }
    }
}

