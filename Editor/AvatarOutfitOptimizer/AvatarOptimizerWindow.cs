using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using VRC.SDK3.Avatars.Components;
using AvatarOutfitOptimizer.Cleanup;
using AvatarOutfitOptimizer.Utils;

namespace AvatarOutfitOptimizer
{
    /// <summary>
    /// Main EditorWindow for VRChat Avatar Optimizer
    /// UI Language: English (all labels, buttons, messages, reports)
    /// </summary>
    public class AvatarOptimizerWindow : EditorWindow
    {
        private GameObject selectedAvatar;
        private AvatarSnapshot fullSnapshot;    // All objects (before optimization)
        private AvatarSnapshot activeSnapshot;  // Active objects only (after optimization)
        private OptimizationReport currentReport;
        
        // Analysis results
        private UsageScanner usageScanner;
        private MenuAnalyzer.MenuAnalysisResult menuAnalysis;
        private AnimatorAnalyzer.AnimatorAnalysisResult animatorAnalysis;
        private PhysBoneAnalyzer.PhysBoneAnalysisResult physBoneAnalysis;
        
        // UI State
        private bool dryRun = true;
        private bool aggressiveAnimatorCleanup = false;
        private bool aggressiveBonePruning = false;
        private Vector2 scrollPosition;
        private Vector2 analysisScrollPosition;
        private string reportText = "";
        private CleanupAnalysis cleanupAnalysis;
        
        // Individual cleanup area toggles
        private bool cleanupObjects = true;
        private bool cleanupAnimator = true;
        private bool cleanupMenu = true;
        private bool cleanupPhysBones = true;
        
        // UI Tabs
        private int selectedTab = 0;
        private readonly string[] tabs = { "Guide", "Analysis", "Optimize" };

        [MenuItem("Window/VRChat Avatar Optimizer")]
        [MenuItem("Tools/VRChat Avatar Optimizer")]
        public static void ShowWindow()
        {
            GetWindow<AvatarOptimizerWindow>("Avatar Optimizer");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            
            // Title
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("VRChat Avatar Optimizer", titleStyle);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.HelpBox(
                "This tool creates optimized duplicate avatars. " +
                "Your original avatar will NEVER be modified.",
                MessageType.Info);
            
            EditorGUILayout.Space(10);

            // Avatar Selection
            EditorGUILayout.LabelField("Avatar Selection", EditorStyles.boldLabel);
            selectedAvatar = (GameObject)EditorGUILayout.ObjectField(
                "Avatar GameObject",
                selectedAvatar,
                typeof(GameObject),
                true);

            if (selectedAvatar != null)
            {
                var descriptor = AvatarUtils.GetAvatarDescriptor(selectedAvatar);
                if (descriptor == null)
                {
                    EditorGUILayout.HelpBox(
                        "Selected GameObject does not have a VRC Avatar Descriptor.",
                        MessageType.Warning);
                }
            }

            EditorGUILayout.Space(10);

            // Tabs
            selectedTab = GUILayout.Toolbar(selectedTab, tabs);

            EditorGUILayout.Space(10);

            // Tab Content
            switch (selectedTab)
            {
                case 0: // Guide
                    DrawGuideTab();
                    break;
                case 1: // Analysis
                    DrawAnalysisTab();
                    break;
                case 2: // Optimize
                    DrawOptimizeTab();
                    break;
            }
        }

        private void DrawGuideTab()
        {
            EditorGUILayout.LabelField("How to Use This Tool", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.HelpBox(
                "Follow these steps to optimize your avatar:",
                MessageType.Info);

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Step 1: Capture Snapshot", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Click 'Capture Snapshot' to analyze your avatar's current state. " +
                "This will scan all components, meshes, bones, and parameters.",
                EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Step 2: Review Analysis", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Switch to the 'Analysis' tab to see what can be cleaned up. " +
                "Each area shows what will be removed and how many items were found.",
                EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Step 3: Select Cleanup Areas", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "In the 'Optimize' tab, you can choose which areas to clean up. " +
                "You can enable/disable each cleanup area individually:",
                EditorStyles.wordWrappedLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("• Object Cleanup: Removes inactive GameObjects and unused renderers");
            EditorGUILayout.LabelField("• Animator Cleanup: Removes unused clips, parameters, and states");
            EditorGUILayout.LabelField("• Menu Cleanup: Removes broken parameter references and empty submenus");
            EditorGUILayout.LabelField("• PhysBone Cleanup: Removes PhysBones on deleted objects");
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Step 4: Optimize", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Click 'Create Optimized Avatar' to create a duplicate with selected optimizations applied. " +
                "Use 'Dry Run' mode first to preview changes without creating the duplicate.",
                EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Important Notes", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("• Always test optimized avatars in VRChat before uploading");
            EditorGUILayout.LabelField("• The tool estimates performance tiers - actual VRChat rank may vary");
            EditorGUILayout.LabelField("• Use aggressive cleanup options only if you understand the risks");
            EditorGUILayout.LabelField("• Your original avatar is never modified");
            EditorGUI.indentLevel--;
        }

        private void DrawAnalysisTab()
        {
            if (fullSnapshot == null || activeSnapshot == null)
            {
                EditorGUILayout.HelpBox(
                    "Please capture a snapshot first to see the analysis.",
                    MessageType.Info);
                
                GUI.enabled = selectedAvatar != null;
                if (GUILayout.Button("Capture Snapshot", GUILayout.Height(30)))
                {
                    CaptureSnapshot();
                }
                GUI.enabled = true;
                return;
            }

            EditorGUILayout.LabelField("Cleanup Analysis", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (cleanupAnalysis == null)
            {
                // Generate analysis using full snapshot for context
                cleanupAnalysis = CleanupAnalysis.CreateFromResults(
                    animatorAnalysis,
                    menuAnalysis,
                    physBoneAnalysis,
                    fullSnapshot);
            }

            analysisScrollPosition = EditorGUILayout.BeginScrollView(analysisScrollPosition);

            // Object Cleanup
            DrawAreaAnalysis(cleanupAnalysis.ObjectCleanup, ref cleanupObjects);

            EditorGUILayout.Space(10);

            // Animator Cleanup
            DrawAreaAnalysis(cleanupAnalysis.AnimatorCleanup, ref cleanupAnimator);

            EditorGUILayout.Space(10);

            // Menu Cleanup
            DrawAreaAnalysis(cleanupAnalysis.MenuCleanup, ref cleanupMenu);

            EditorGUILayout.Space(10);

            // PhysBone Cleanup
            DrawAreaAnalysis(cleanupAnalysis.PhysBoneCleanup, ref cleanupPhysBones);

            EditorGUILayout.EndScrollView();
        }

        private void DrawAreaAnalysis(CleanupAnalysis.AreaAnalysis area, ref bool enabled)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            enabled = EditorGUILayout.Toggle(enabled, GUILayout.Width(20));
            EditorGUILayout.LabelField(area.AreaName, EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.LabelField(area.Description, EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.LabelField($"Items found: {area.ItemsFound}", EditorStyles.miniLabel);
            
            if (area.Details != null && area.Details.Count > 0)
            {
                EditorGUI.indentLevel++;
                foreach (var detail in area.Details)
                {
                    EditorGUILayout.LabelField($"• {detail}", EditorStyles.wordWrappedMiniLabel);
                }
                EditorGUI.indentLevel--;
            }
            
            if (!area.IsSafe && !string.IsNullOrEmpty(area.Warning))
            {
                EditorGUILayout.HelpBox(area.Warning, MessageType.Warning);
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawOptimizeTab()
        {
            // Snapshot Section
            EditorGUILayout.LabelField("Analysis", EditorStyles.boldLabel);
            
            GUI.enabled = selectedAvatar != null;
            if (GUILayout.Button("Capture Snapshot", GUILayout.Height(30)))
            {
                CaptureSnapshot();
            }
            GUI.enabled = true;

            if (fullSnapshot != null && activeSnapshot != null)
            {
                // Show before/after comparison
                EditorGUILayout.HelpBox(
                    $"Full Avatar: {fullSnapshot.MeshCount} meshes, {fullSnapshot.BoneCount} bones, {fullSnapshot.PhysBoneCount} PhysBones\n" +
                    $"Active Only: {activeSnapshot.MeshCount} meshes, {activeSnapshot.BoneCount} bones, {activeSnapshot.PhysBoneCount} PhysBones\n" +
                    $"Potential Reduction: {fullSnapshot.MeshCount - activeSnapshot.MeshCount} meshes, {fullSnapshot.BoneCount - activeSnapshot.BoneCount} bones",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Please capture a snapshot first.\n" +
                    "Make sure to enable only the objects you want to KEEP before capturing.",
                    MessageType.Warning);
            }

            EditorGUILayout.Space(10);

            // Cleanup Area Selection
            EditorGUILayout.LabelField("Select Cleanup Areas", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Choose which areas to optimize. Each area can be enabled/disabled individually.",
                MessageType.Info);
            
            EditorGUI.indentLevel++;
            cleanupObjects = EditorGUILayout.Toggle("Object Cleanup", cleanupObjects);
            cleanupAnimator = EditorGUILayout.Toggle("Animator Cleanup", cleanupAnimator);
            cleanupMenu = EditorGUILayout.Toggle("Menu Cleanup", cleanupMenu);
            cleanupPhysBones = EditorGUILayout.Toggle("PhysBone Cleanup", cleanupPhysBones);
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(10);

            // Options
            EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);
            
            dryRun = EditorGUILayout.Toggle("Dry Run (Preview Only)", dryRun);
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.LabelField("Advanced Options", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            
            aggressiveAnimatorCleanup = EditorGUILayout.Toggle(
                "Aggressive Animator Cleanup",
                aggressiveAnimatorCleanup);
            
            if (aggressiveAnimatorCleanup)
            {
                EditorGUILayout.HelpBox(
                    "Warning: This will remove potentially unused animation clips. " +
                    "Use with caution and test your avatar thoroughly.",
                    MessageType.Warning);
            }
            
            aggressiveBonePruning = EditorGUILayout.Toggle(
                "Aggressive Bone Pruning (Advanced Users)",
                aggressiveBonePruning);
            
            if (aggressiveBonePruning)
            {
                EditorGUILayout.HelpBox(
                    "Warning: Bone pruning is HIGH RISK. " +
                    "May break PhysBones, constraints, and animations. " +
                    "Only use if you understand the risks.",
                    MessageType.Warning);
            }
            
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(10);

            // Optimization Button
            GUI.enabled = fullSnapshot != null && activeSnapshot != null && selectedAvatar != null;
            if (GUILayout.Button(
                dryRun ? "Preview Optimization" : "Create Optimized Avatar",
                GUILayout.Height(40)))
            {
                PerformOptimization();
            }
            GUI.enabled = true;

            EditorGUILayout.Space(10);

            // Report Section
            if (!string.IsNullOrEmpty(reportText))
            {
                EditorGUILayout.LabelField("Optimization Report", EditorStyles.boldLabel);
                
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
                
                EditorGUILayout.TextArea(reportText, GUILayout.ExpandHeight(true));
                
                EditorGUILayout.EndScrollView();
            }
        }

        private void CaptureSnapshot()
        {
            if (selectedAvatar == null)
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    "Please select an avatar GameObject first.",
                    "OK");
                return;
            }

            // Capture FULL snapshot (all objects including inactive) - this is "BEFORE"
            fullSnapshot = AvatarSnapshot.Capture(selectedAvatar, includeInactive: true);
            
            // Capture ACTIVE snapshot (only active objects) - this is "AFTER"
            activeSnapshot = AvatarSnapshot.Capture(selectedAvatar, includeInactive: false);
            
            if (fullSnapshot != null && activeSnapshot != null)
            {
                // Perform analysis using full snapshot
                AnalyzeAvatar();
                
                // Generate cleanup analysis
                cleanupAnalysis = CleanupAnalysis.CreateFromResults(
                    animatorAnalysis,
                    menuAnalysis,
                    physBoneAnalysis,
                    fullSnapshot);
                
                int removedMeshes = fullSnapshot.MeshCount - activeSnapshot.MeshCount;
                int removedBones = fullSnapshot.BoneCount - activeSnapshot.BoneCount;
                int removedPhysBones = fullSnapshot.PhysBoneCount - activeSnapshot.PhysBoneCount;
                
                EditorUtility.DisplayDialog(
                    "Snapshot Captured",
                    $"Full Avatar (Before):\n" +
                    $"- {fullSnapshot.MeshCount} meshes\n" +
                    $"- {fullSnapshot.BoneCount} bones\n" +
                    $"- {fullSnapshot.PhysBoneCount} PhysBones\n\n" +
                    $"Active Only (After Cleanup):\n" +
                    $"- {activeSnapshot.MeshCount} meshes\n" +
                    $"- {activeSnapshot.BoneCount} bones\n" +
                    $"- {activeSnapshot.PhysBoneCount} PhysBones\n\n" +
                    $"Potential Reduction:\n" +
                    $"- {removedMeshes} meshes\n" +
                    $"- {removedBones} bones\n" +
                    $"- {removedPhysBones} PhysBones\n\n" +
                    $"Switch to 'Analysis' or 'Optimize' tab for details.",
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    "Failed to capture snapshot. Check console for details.",
                    "OK");
            }
        }

        private void AnalyzeAvatar()
        {
            if (selectedAvatar == null || fullSnapshot == null) return;

            // Scan usage using full snapshot
            usageScanner = new UsageScanner();
            usageScanner.Scan(selectedAvatar, fullSnapshot);

            // Analyze menu
            var descriptor = AvatarUtils.GetAvatarDescriptor(selectedAvatar);
            if (descriptor != null)
            {
                var menu = AvatarUtils.GetExpressionMenu(descriptor);
                if (menu != null)
                {
                    var menuAnalyzer = new MenuAnalyzer();
                    var validParams = new HashSet<string>(fullSnapshot.ExpressionParameterNames);
                    validParams.UnionWith(fullSnapshot.AnimatorParameterNames);
                    menuAnalysis = menuAnalyzer.Analyze(menu, fullSnapshot, validParams);
                }

                // Analyze animator
                var fxLayer = AvatarUtils.GetFXLayer(descriptor);
                if (fxLayer != null)
                {
                    var animatorAnalyzer = new AnimatorAnalyzer();
                    animatorAnalysis = animatorAnalyzer.Analyze(fxLayer, usageScanner);
                }
            }

            // Analyze PhysBones using full snapshot
            var physBoneAnalyzer = new PhysBoneAnalyzer();
            physBoneAnalysis = physBoneAnalyzer.Analyze(
                selectedAvatar,
                fullSnapshot,
                aggressiveBonePruning);
        }

        private void PerformOptimization()
        {
            if (selectedAvatar == null || fullSnapshot == null || activeSnapshot == null)
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    "Please capture a snapshot first.",
                    "OK");
                return;
            }

            // Ensure analysis is done
            if (usageScanner == null)
            {
                AnalyzeAvatar();
            }

            EditorUtility.DisplayProgressBar(
                "Optimizing Avatar",
                "Creating duplicate and performing cleanup...",
                0.5f);

            try
            {
                // Create cleanup coordinator
                var coordinator = new CleanupCoordinator();

                // Create optimized avatar (or preview in dry run)
                GameObject optimizedAvatar = coordinator.CreateOptimizedAvatar(
                    selectedAvatar,
                    activeSnapshot,  // Use active snapshot as the target state
                    usageScanner,
                    menuAnalysis,
                    animatorAnalysis,
                    physBoneAnalysis,
                    aggressiveAnimatorCleanup,
                    aggressiveBonePruning,
                    dryRun,
                    cleanupObjects,
                    cleanupAnimator,
                    cleanupMenu,
                    cleanupPhysBones);

                // For dry run, use the pre-captured activeSnapshot as the "after" state
                // For actual optimization, capture a new snapshot from the optimized avatar
                AvatarSnapshot afterSnapshot = null;
                if (!dryRun && optimizedAvatar != null)
                {
                    // Capture actual result from optimized avatar
                    afterSnapshot = AvatarSnapshot.Capture(optimizedAvatar, includeInactive: false);
                }
                else if (dryRun)
                {
                    // For dry run, activeSnapshot IS the "after" state
                    afterSnapshot = activeSnapshot;
                }

                // Generate report comparing FULL (before) with ACTIVE/actual (after)
                if (afterSnapshot != null)
                {
                    var comparison = fullSnapshot.Compare(afterSnapshot);
                    currentReport = OptimizationReport.Create(
                        fullSnapshot,   // Before = full avatar
                        afterSnapshot,  // After = active only / optimized
                        comparison);
                    
                    reportText = currentReport.GenerateReportText();
                }

                if (!dryRun && optimizedAvatar != null)
                {
                    EditorUtility.DisplayDialog(
                        "Optimization Complete",
                        $"Optimized avatar created: {optimizedAvatar.name}\n\n" +
                        $"Check the report below for details.",
                        "OK");
                    
                    // Select the new avatar
                    Selection.activeGameObject = optimizedAvatar;
                }
                else if (dryRun)
                {
                    EditorUtility.DisplayDialog(
                        "Dry Run Complete",
                        "Preview generated. Check the report below.\n\n" +
                        "Disable 'Dry Run' to create the optimized avatar.",
                        "OK");
                }
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    $"Optimization failed: {e.Message}\n\nCheck console for details.",
                    "OK");
                Debug.LogError($"[AvatarOptimizer] Optimization error: {e}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

    }
}

