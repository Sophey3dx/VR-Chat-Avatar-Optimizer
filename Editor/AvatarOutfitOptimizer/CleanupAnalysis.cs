using System.Collections.Generic;
using System.Text;

namespace AvatarOutfitOptimizer
{
    /// <summary>
    /// Detailed analysis of what can be cleaned up in each area
    /// </summary>
    public class CleanupAnalysis
    {
        public class AreaAnalysis
        {
            public string AreaName { get; set; }
            public string Description { get; set; }
            public int ItemsFound { get; set; }
            public List<string> Details { get; set; } = new List<string>();
            public bool IsSafe { get; set; } = true;
            public string Warning { get; set; } = "";
        }

        public AreaAnalysis ObjectCleanup { get; set; } = new AreaAnalysis
        {
            AreaName = "Object Cleanup",
            Description = "Removes inactive GameObjects and unused SkinnedMeshRenderers"
        };

        public AreaAnalysis AnimatorCleanup { get; set; } = new AreaAnalysis
        {
            AreaName = "Animator Cleanup",
            Description = "Removes unused AnimationClips, parameters, and states"
        };

        public AreaAnalysis MenuCleanup { get; set; } = new AreaAnalysis
        {
            AreaName = "Expression Menu Cleanup",
            Description = "Removes broken parameter references and empty submenus"
        };

        public AreaAnalysis PhysBoneCleanup { get; set; } = new AreaAnalysis
        {
            AreaName = "PhysBone Cleanup",
            Description = "Removes PhysBones on deleted objects and unused colliders"
        };

        /// <summary>
        /// Generates a detailed analysis report
        /// </summary>
        public string GenerateAnalysisReport()
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("=== Cleanup Analysis ===");
            sb.AppendLine();
            sb.AppendLine("The following areas can be optimized:");
            sb.AppendLine();

            // Object Cleanup
            AppendAreaAnalysis(sb, ObjectCleanup);
            
            // Animator Cleanup
            AppendAreaAnalysis(sb, AnimatorCleanup);
            
            // Menu Cleanup
            AppendAreaAnalysis(sb, MenuCleanup);
            
            // PhysBone Cleanup
            AppendAreaAnalysis(sb, PhysBoneCleanup);

            return sb.ToString();
        }

        private void AppendAreaAnalysis(StringBuilder sb, AreaAnalysis area)
        {
            sb.AppendLine($"--- {area.AreaName} ---");
            sb.AppendLine(area.Description);
            sb.AppendLine($"Items found: {area.ItemsFound}");
            
            if (area.Details != null && area.Details.Count > 0)
            {
                sb.AppendLine("Details:");
                foreach (var detail in area.Details)
                {
                    sb.AppendLine($"  • {detail}");
                }
            }
            
            if (!area.IsSafe && !string.IsNullOrEmpty(area.Warning))
            {
                sb.AppendLine($"⚠ Warning: {area.Warning}");
            }
            
            sb.AppendLine();
        }

        /// <summary>
        /// Populates analysis from analyzer results
        /// </summary>
        public static CleanupAnalysis CreateFromResults(
            AnimatorAnalyzer.AnimatorAnalysisResult animatorAnalysis,
            MenuAnalyzer.MenuAnalysisResult menuAnalysis,
            PhysBoneAnalyzer.PhysBoneAnalysisResult physBoneAnalysis,
            AvatarSnapshot snapshot)
        {
            var analysis = new CleanupAnalysis();

            // Object Cleanup Analysis
            // Estimate inactive objects (this would need more detailed scanning)
            analysis.ObjectCleanup.ItemsFound = 0; // Would be calculated from snapshot comparison
            analysis.ObjectCleanup.Details.Add("Inactive GameObjects will be removed");
            analysis.ObjectCleanup.Details.Add("Unused SkinnedMeshRenderers will be removed");
            analysis.ObjectCleanup.IsSafe = true;

            // Animator Cleanup Analysis
            if (animatorAnalysis != null)
            {
                int unusedClips = animatorAnalysis.UnusedClips?.Count ?? 0;
                int potentiallyUnused = animatorAnalysis.PotentiallyUnusedClips?.Count ?? 0;
                int unusedParams = animatorAnalysis.UnusedParameters?.Count ?? 0;

                analysis.AnimatorCleanup.ItemsFound = unusedClips + unusedParams;
                
                if (unusedClips > 0)
                {
                    analysis.AnimatorCleanup.Details.Add($"{unusedClips} unused AnimationClips");
                }
                if (potentiallyUnused > 0)
                {
                    analysis.AnimatorCleanup.Details.Add($"{potentiallyUnused} potentially unused clips (requires aggressive mode)");
                }
                if (unusedParams > 0)
                {
                    analysis.AnimatorCleanup.Details.Add($"{unusedParams} unused Animator parameters");
                }

                if (potentiallyUnused > 0)
                {
                    analysis.AnimatorCleanup.IsSafe = false;
                    analysis.AnimatorCleanup.Warning = "Some clips may be used indirectly. Enable aggressive mode at your own risk.";
                }
            }

            // Menu Cleanup Analysis
            if (menuAnalysis != null)
            {
                int brokenParams = menuAnalysis.BrokenParameterControls?.Count ?? 0;
                int emptySubmenus = menuAnalysis.EmptySubmenus?.Count ?? 0;
                int unreachable = menuAnalysis.UnreachableMenus?.Count ?? 0;

                analysis.MenuCleanup.ItemsFound = brokenParams + emptySubmenus;
                
                if (brokenParams > 0)
                {
                    analysis.MenuCleanup.Details.Add($"{brokenParams} broken parameter references");
                }
                if (emptySubmenus > 0)
                {
                    analysis.MenuCleanup.Details.Add($"{emptySubmenus} empty submenus");
                }
                if (unreachable > 0)
                {
                    analysis.MenuCleanup.Details.Add($"{unreachable} unreachable menus (will be warned, not removed)");
                }
                analysis.MenuCleanup.IsSafe = true;
            }

            // PhysBone Cleanup Analysis
            if (physBoneAnalysis != null)
            {
                int deletedPhysBones = physBoneAnalysis.PhysBonesOnDeletedObjects?.Count ?? 0;
                int unusedColliders = physBoneAnalysis.UnusedColliders?.Count ?? 0;

                analysis.PhysBoneCleanup.ItemsFound = deletedPhysBones + unusedColliders;
                
                if (deletedPhysBones > 0)
                {
                    analysis.PhysBoneCleanup.Details.Add($"{deletedPhysBones} PhysBones on deleted objects");
                }
                if (unusedColliders > 0)
                {
                    analysis.PhysBoneCleanup.Details.Add($"{unusedColliders} unused colliders");
                }
                analysis.PhysBoneCleanup.IsSafe = true;
            }

            return analysis;
        }
    }
}

