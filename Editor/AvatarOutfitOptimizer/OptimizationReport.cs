using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AvatarOutfitOptimizer
{
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

            // Metrics comparison
            sb.AppendLine("Performance Metrics:");
            sb.AppendLine($"  Meshes:      {BeforeMeshCount} → {AfterMeshCount} ({(AfterMeshCount - BeforeMeshCount):+0;-#})");
            sb.AppendLine($"  Materials:   {BeforeMaterialCount} → {AfterMaterialCount} ({(AfterMaterialCount - BeforeMaterialCount):+0;-#})");
            sb.AppendLine($"  Bones:        {BeforeBoneCount} → {AfterBoneCount} ({(AfterBoneCount - BeforeBoneCount):+0;-#})");
            sb.AppendLine($"  PhysBones:    {BeforePhysBoneCount} → {AfterPhysBoneCount} ({(AfterPhysBoneCount - BeforePhysBoneCount):+0;-#})");
            sb.AppendLine($"  Parameters:   {BeforeParameterCount} → {AfterParameterCount} ({(AfterParameterCount - BeforeParameterCount):+0;-#})");
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
        /// Estimates performance tier based on metrics
        /// Uses heuristics - NOT guaranteed VRChat rank
        /// </summary>
        public static EstimatedPerformanceTier EstimatePerformanceTier(
            int meshCount,
            int materialCount,
            int boneCount,
            int physBoneCount,
            int parameterCount)
        {
            // Heuristic-based estimation
            // These thresholds are approximate and may not match actual VRChat ranking
            
            int score = 0;

            // Mesh scoring (lower is better)
            if (meshCount <= 1) score += 3;
            else if (meshCount <= 2) score += 2;
            else if (meshCount <= 3) score += 1;

            // Material scoring
            if (materialCount <= 1) score += 3;
            else if (materialCount <= 2) score += 2;
            else if (materialCount <= 3) score += 1;

            // Bone scoring
            if (boneCount <= 75) score += 3;
            else if (boneCount <= 150) score += 2;
            else if (boneCount <= 300) score += 1;

            // PhysBone scoring
            if (physBoneCount <= 4) score += 2;
            else if (physBoneCount <= 8) score += 1;

            // Parameter scoring (memory usage)
            int parameterMemory = parameterCount * 4; // Approximate bytes per parameter
            if (parameterMemory <= 64) score += 2;
            else if (parameterMemory <= 128) score += 1;

            // Convert score to tier
            if (score >= 12) return EstimatedPerformanceTier.Excellent;
            if (score >= 8) return EstimatedPerformanceTier.Good;
            if (score >= 4) return EstimatedPerformanceTier.Medium;
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

            // Warn if still high bone count
            if (report.AfterBoneCount > 300)
            {
                report.Warnings.Add($"High bone count ({report.AfterBoneCount}). Consider reducing mesh complexity.");
            }

            // Warn if still high PhysBone count
            if (report.AfterPhysBoneCount > 8)
            {
                report.Warnings.Add($"High PhysBone count ({report.AfterPhysBoneCount}). Consider consolidating PhysBones.");
            }

            // Warn if parameter count is high
            if (report.AfterParameterCount > 32)
            {
                report.Warnings.Add($"High parameter count ({report.AfterParameterCount}). Consider reducing Expression Parameters.");
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

            // Recommendations based on current state
            if (report.AfterMeshCount > 1)
            {
                report.Recommendations.Add("Consider merging meshes to reduce draw calls.");
            }

            if (report.AfterMaterialCount > 1)
            {
                report.Recommendations.Add("Consider using texture atlasing to reduce material count.");
            }

            if (report.AfterBoneCount > 150)
            {
                report.Recommendations.Add("Consider using mesh decimation or LODs to reduce bone count.");
            }

            if (report.AfterTier == EstimatedPerformanceTier.Excellent)
            {
                report.Recommendations.Add("Avatar is well optimized! Test in VRChat to verify actual rank.");
            }
            else
            {
                report.Recommendations.Add("Test optimized avatar in VRChat to verify actual performance rank.");
            }
        }
    }
}

