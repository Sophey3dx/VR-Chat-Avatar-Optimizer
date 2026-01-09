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
        private AvatarSnapshot currentSnapshot;
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
            if (currentSnapshot == null)
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
                // Generate analysis
                cleanupAnalysis = CleanupAnalysis.CreateFromResults(
                    animatorAnalysis,
                    menuAnalysis,
                    physBoneAnalysis,
                    currentSnapshot);
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

            if (currentSnapshot != null)
            {
                EditorGUILayout.HelpBox(
                    $"Snapshot captured: {currentSnapshot.MeshCount} meshes, " +
                    $"{currentSnapshot.MaterialCount} materials, " +
                    $"{currentSnapshot.BoneCount} bones",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Please capture a snapshot first.",
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
            GUI.enabled = currentSnapshot != null && selectedAvatar != null;
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

            currentSnapshot = AvatarSnapshot.Capture(selectedAvatar);
            
            if (currentSnapshot != null)
            {
                // Perform analysis
                AnalyzeAvatar();
                
                // Generate cleanup analysis
                cleanupAnalysis = CleanupAnalysis.CreateFromResults(
                    animatorAnalysis,
                    menuAnalysis,
                    physBoneAnalysis,
                    currentSnapshot);
                
                EditorUtility.DisplayDialog(
                    "Snapshot Captured",
                    $"Successfully captured snapshot:\n" +
                    $"- {currentSnapshot.MeshCount} meshes\n" +
                    $"- {currentSnapshot.MaterialCount} materials\n" +
                    $"- {currentSnapshot.BoneCount} bones\n" +
                    $"- {currentSnapshot.PhysBoneCount} PhysBones\n" +
                    $"- {currentSnapshot.ParameterCount} parameters\n\n" +
                    $"Switch to the 'Analysis' tab to see what can be cleaned up.",
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
            if (selectedAvatar == null || currentSnapshot == null) return;

            // Scan usage
            usageScanner = new UsageScanner();
            usageScanner.Scan(selectedAvatar, currentSnapshot);

            // Analyze menu
            var descriptor = AvatarUtils.GetAvatarDescriptor(selectedAvatar);
            if (descriptor != null)
            {
                var menu = AvatarUtils.GetExpressionMenu(descriptor);
                if (menu != null)
                {
                    var menuAnalyzer = new MenuAnalyzer();
                    var validParams = new HashSet<string>(currentSnapshot.ExpressionParameterNames);
                    validParams.UnionWith(currentSnapshot.AnimatorParameterNames);
                    menuAnalysis = menuAnalyzer.Analyze(menu, currentSnapshot, validParams);
                }

                // Analyze animator
                var fxLayer = AvatarUtils.GetFXLayer(descriptor);
                if (fxLayer != null)
                {
                    var animatorAnalyzer = new AnimatorAnalyzer();
                    animatorAnalysis = animatorAnalyzer.Analyze(fxLayer, usageScanner);
                }
            }

            // Analyze PhysBones
            var physBoneAnalyzer = new PhysBoneAnalyzer();
            physBoneAnalysis = physBoneAnalyzer.Analyze(
                selectedAvatar,
                currentSnapshot,
                aggressiveBonePruning);
        }

        private void PerformOptimization()
        {
            if (selectedAvatar == null || currentSnapshot == null)
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
                    currentSnapshot,
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

                // Capture after snapshot if not dry run
                AvatarSnapshot afterSnapshot = null;
                if (!dryRun && optimizedAvatar != null)
                {
                    afterSnapshot = AvatarSnapshot.Capture(optimizedAvatar);
                }
                else if (dryRun)
                {
                    // For dry run, create a simulated after snapshot
                    // This is a simplified version - in production you'd simulate the cleanup
                    afterSnapshot = SimulateAfterSnapshot();
                }

                // Generate report
                if (afterSnapshot != null)
                {
                    var comparison = currentSnapshot.Compare(afterSnapshot);
                    currentReport = OptimizationReport.Create(
                        currentSnapshot,
                        afterSnapshot,
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

        private AvatarSnapshot SimulateAfterSnapshot()
        {
            // Create a simulated snapshot for dry run
            // This estimates what the snapshot would look like after cleanup
            
            if (currentSnapshot == null || selectedAvatar == null) return null;

            // Start with copies of all current paths
            var remainingObjects = new HashSet<string>(currentSnapshot.ActiveGameObjectPaths);
            var remainingRenderers = new HashSet<string>(currentSnapshot.ActiveRendererPaths);
            var remainingBones = new HashSet<string>(currentSnapshot.UsedBonePaths);
            var remainingPhysBones = new HashSet<string>(currentSnapshot.ActivePhysBonePaths);
            var remainingExpressionParams = new HashSet<string>(currentSnapshot.ExpressionParameterNames);
            var remainingAnimatorParams = new HashSet<string>(currentSnapshot.AnimatorParameterNames);
            int remainingMaterials = currentSnapshot.MaterialCount;

            // Object cleanup: Get all objects (including inactive) and determine what would be removed
            if (cleanupObjects)
            {
                // Get ALL objects in the hierarchy
                var allObjectPaths = AvatarUtils.GetAllGameObjectPaths(selectedAvatar);
                var activeObjectPaths = new HashSet<string>(currentSnapshot.ActiveGameObjectPaths);
                
                // Objects to be removed = all objects - active objects
                var objectsToRemove = new HashSet<string>(allObjectPaths.Where(p => !activeObjectPaths.Contains(p)));
                
                // After cleanup, only active objects remain
                remainingObjects = activeObjectPaths;
                
                // Remove renderers that are on inactive objects
                var renderersToRemove = new HashSet<string>();
                foreach (var rendererPath in remainingRenderers)
                {
                    // Check if this renderer's parent object is being removed
                    bool isOnActiveObject = activeObjectPaths.Any(objPath => 
                        rendererPath == objPath || rendererPath.StartsWith(objPath + "/"));
                    
                    if (!isOnActiveObject)
                    {
                        renderersToRemove.Add(rendererPath);
                    }
                }
                remainingRenderers.ExceptWith(renderersToRemove);
                
                // Remove PhysBones on inactive objects
                var physBonesToRemove = new HashSet<string>();
                foreach (var physBonePath in remainingPhysBones)
                {
                    bool isOnActiveObject = activeObjectPaths.Any(objPath => 
                        physBonePath == objPath || physBonePath.StartsWith(objPath + "/"));
                    
                    if (!isOnActiveObject)
                    {
                        physBonesToRemove.Add(physBonePath);
                    }
                }
                remainingPhysBones.ExceptWith(physBonesToRemove);
                
                // Recalculate bones based on remaining renderers
                // In a real scenario, we would need to scan which bones are used by remaining meshes
                // For now, we estimate that bones are reduced proportionally
                if (renderersToRemove.Count > 0 && currentSnapshot.ActiveRendererPaths.Count > 0)
                {
                    // Estimate bone reduction based on mesh reduction ratio
                    float meshRetentionRatio = (float)remainingRenderers.Count / currentSnapshot.ActiveRendererPaths.Count;
                    int estimatedRemainingBones = Mathf.RoundToInt(currentSnapshot.BoneCount * meshRetentionRatio);
                    
                    // Take only the first N bones (simplified estimation)
                    remainingBones = new HashSet<string>(remainingBones.Take(estimatedRemainingBones));
                }
            }

            // PhysBone cleanup from analysis
            if (cleanupPhysBones && physBoneAnalysis != null)
            {
                // Remove PhysBones that were identified as on deleted objects
                if (physBoneAnalysis.PhysBonesOnDeletedObjects != null)
                {
                    foreach (var pb in physBoneAnalysis.PhysBonesOnDeletedObjects)
                    {
                        if (pb != null)
                        {
                            string path = GetRelativePath(selectedAvatar.transform, pb.transform);
                            remainingPhysBones.Remove(path);
                        }
                    }
                }
            }

            // Animator cleanup estimates
            if (cleanupAnimator && animatorAnalysis != null)
            {
                if (animatorAnalysis.UnusedParameters != null)
                {
                    foreach (var param in animatorAnalysis.UnusedParameters)
                    {
                        remainingAnimatorParams.Remove(param);
                    }
                }
            }

            // Bone pruning (aggressive mode only)
            if (aggressiveBonePruning && physBoneAnalysis != null && physBoneAnalysis.PrunableBones != null)
            {
                foreach (var bone in physBoneAnalysis.PrunableBones)
                {
                    if (bone != null)
                    {
                        string path = GetRelativePath(selectedAvatar.transform, bone);
                        remainingBones.Remove(path);
                    }
                }
            }

            // Create simulated snapshot with full path lists
            return AvatarSnapshot.CreateSimulatedWithPaths(
                remainingObjects.ToList(),
                remainingRenderers.ToList(),
                remainingBones.ToList(),
                remainingPhysBones.ToList(),
                remainingExpressionParams.ToList(),
                remainingAnimatorParams.ToList(),
                remainingMaterials,
                currentSnapshot.AvatarFingerprint
            );
        }

        private string GetRelativePath(Transform root, Transform target)
        {
            if (target == null) return "";
            if (target == root) return target.name;

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

