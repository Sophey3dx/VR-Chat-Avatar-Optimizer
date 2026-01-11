using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using VRC.SDK3.Avatars.Components;

namespace OutfitWizard
{
    /// <summary>
    /// Wizard for creating a single item toggle (On/Off)
    /// Steps: Select Item → Configure Toggle → PhysBones → Blendshapes → Review
    /// </summary>
    public class SingleToggleWizard
    {
        public enum WizardStep
        {
            SelectItem,
            ConfigureToggle,
            PhysBones,
            Blendshapes,
            Review
        }
        
        private WizardStep currentStep = WizardStep.SelectItem;
        
        // Step 1: Item Selection
        private GameObject targetObject;
        private List<GameObject> childObjects = new List<GameObject>();
        
        // Step 2: Toggle Configuration
        private string toggleName = "";
        private string menuPath = "Outfits";
        private bool defaultState = true;
        private bool saveState = true;
        
        // Step 3: PhysBones
        private List<PhysBoneInfo> detectedPhysBones = new List<PhysBoneInfo>();
        private bool togglePhysBonesWithItem = true;
        
        // Step 4: Blendshapes
        private List<BlendshapeInfo> detectedBlendshapes = new List<BlendshapeInfo>();
        private bool animateBlendshapes = true;
        
        // Scroll positions
        private Vector2 itemScrollPos;
        private Vector2 physBoneScrollPos;
        private Vector2 blendshapeScrollPos;
        
        public class PhysBoneInfo
        {
            public Component PhysBone;
            public string Name;
            public bool Include = true;
        }
        
        public class BlendshapeInfo
        {
            public SkinnedMeshRenderer Renderer;
            public string BlendshapeName;
            public int BlendshapeIndex;
            public bool Include = true;
        }
        
        public void Reset()
        {
            currentStep = WizardStep.SelectItem;
            targetObject = null;
            childObjects.Clear();
            toggleName = "";
            menuPath = "Outfits";
            defaultState = true;
            saveState = true;
            detectedPhysBones.Clear();
            detectedBlendshapes.Clear();
            togglePhysBonesWithItem = true;
            animateBlendshapes = true;
        }
        
        public void DrawWizard(OutfitWizardWindow parent, VRCAvatarDescriptor avatar)
        {
            // Progress bar
            DrawProgressBar();
            EditorGUILayout.Space(10);
            
            switch (currentStep)
            {
                case WizardStep.SelectItem:
                    DrawSelectItemStep(avatar);
                    break;
                case WizardStep.ConfigureToggle:
                    DrawConfigureToggleStep();
                    break;
                case WizardStep.PhysBones:
                    DrawPhysBonesStep(avatar);
                    break;
                case WizardStep.Blendshapes:
                    DrawBlendshapesStep(avatar);
                    break;
                case WizardStep.Review:
                    DrawReviewStep(parent, avatar);
                    break;
            }
        }
        
        private void DrawProgressBar()
        {
            string[] steps = { "Select Item", "Configure", "PhysBones", "Blendshapes", "Review" };
            int currentIndex = (int)currentStep;
            
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < steps.Length; i++)
            {
                GUIStyle style = new GUIStyle(EditorStyles.miniButton);
                if (i == currentIndex)
                {
                    style.fontStyle = FontStyle.Bold;
                    GUI.backgroundColor = new Color(0.4f, 0.7f, 1f);
                }
                else if (i < currentIndex)
                {
                    GUI.backgroundColor = new Color(0.5f, 0.8f, 0.5f);
                }
                else
                {
                    GUI.backgroundColor = Color.gray;
                }
                
                GUILayout.Button(steps[i], style);
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawSelectItemStep(VRCAvatarDescriptor avatar)
        {
            EditorGUILayout.LabelField("Step 1: Select Item", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Select the GameObject you want to create a toggle for.\n" +
                "This can be a clothing item, accessory, or any other object.",
                MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            EditorGUI.BeginChangeCheck();
            targetObject = (GameObject)EditorGUILayout.ObjectField(
                "Target Object",
                targetObject,
                typeof(GameObject),
                true);
            
            if (EditorGUI.EndChangeCheck() && targetObject != null)
            {
                // Validate it's under the avatar
                if (!targetObject.transform.IsChildOf(avatar.transform))
                {
                    EditorUtility.DisplayDialog("Error", "Object must be a child of the avatar.", "OK");
                    targetObject = null;
                }
                else
                {
                    // Auto-generate toggle name
                    toggleName = targetObject.name.Replace("_", " ");
                    
                    // Collect child objects
                    childObjects.Clear();
                    CollectChildren(targetObject.transform);
                }
            }
            
            if (targetObject != null)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Object Info", EditorStyles.boldLabel);
                
                var renderers = targetObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                var meshRenderers = targetObject.GetComponentsInChildren<MeshRenderer>(true);
                
                EditorGUILayout.LabelField($"Skinned Meshes: {renderers.Length}");
                EditorGUILayout.LabelField($"Static Meshes: {meshRenderers.Length}");
                EditorGUILayout.LabelField($"Child Objects: {childObjects.Count}");
                
                if (childObjects.Count > 0)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Children:", EditorStyles.miniLabel);
                    itemScrollPos = EditorGUILayout.BeginScrollView(itemScrollPos, GUILayout.Height(100));
                    foreach (var child in childObjects.Take(20))
                    {
                        EditorGUILayout.LabelField($"  • {child.name}", EditorStyles.miniLabel);
                    }
                    if (childObjects.Count > 20)
                    {
                        EditorGUILayout.LabelField($"  ... and {childObjects.Count - 20} more", EditorStyles.miniLabel);
                    }
                    EditorGUILayout.EndScrollView();
                }
            }
            
            EditorGUILayout.Space(20);
            
            // Navigation
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            GUI.enabled = targetObject != null;
            if (GUILayout.Button("Next →", GUILayout.Width(100), GUILayout.Height(30)))
            {
                currentStep = WizardStep.ConfigureToggle;
            }
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void CollectChildren(Transform parent)
        {
            foreach (Transform child in parent)
            {
                childObjects.Add(child.gameObject);
                CollectChildren(child);
            }
        }
        
        private void DrawConfigureToggleStep()
        {
            EditorGUILayout.LabelField("Step 2: Configure Toggle", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Configure how the toggle will appear in your Expression Menu.",
                MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            toggleName = EditorGUILayout.TextField("Toggle Name", toggleName);
            menuPath = EditorGUILayout.TextField("Menu Path", menuPath);
            
            EditorGUILayout.HelpBox(
                $"Will create: Menu '{menuPath}' → Toggle '{toggleName}'",
                MessageType.None);
            
            EditorGUILayout.Space(10);
            
            defaultState = EditorGUILayout.Toggle("Default On", defaultState);
            saveState = EditorGUILayout.Toggle("Save State", saveState);
            
            EditorGUILayout.Space(20);
            
            // Navigation
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("← Back", GUILayout.Width(100), GUILayout.Height(30)))
            {
                currentStep = WizardStep.SelectItem;
            }
            GUILayout.FlexibleSpace();
            
            GUI.enabled = !string.IsNullOrEmpty(toggleName);
            if (GUILayout.Button("Next →", GUILayout.Width(100), GUILayout.Height(30)))
            {
                currentStep = WizardStep.PhysBones;
                DetectPhysBones();
            }
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DetectPhysBones()
        {
            detectedPhysBones.Clear();
            
            if (targetObject == null) return;
            
            // Find VRCPhysBone type via reflection
            var physBoneType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == "VRCPhysBone");
            
            if (physBoneType == null) return;
            
            // Get PhysBones on target and children
            var components = targetObject.GetComponentsInChildren(physBoneType, true);
            foreach (var comp in components)
            {
                detectedPhysBones.Add(new PhysBoneInfo
                {
                    PhysBone = comp as Component,
                    Name = (comp as Component)?.gameObject.name ?? "Unknown",
                    Include = true
                });
            }
        }
        
        private void DrawPhysBonesStep(VRCAvatarDescriptor avatar)
        {
            EditorGUILayout.LabelField("Step 3: PhysBones", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "PhysBones found on this item can be toggled with the outfit.\n" +
                "This reduces PhysBone count when the item is hidden.",
                MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            togglePhysBonesWithItem = EditorGUILayout.Toggle("Toggle PhysBones with Item", togglePhysBonesWithItem);
            
            if (detectedPhysBones.Count == 0)
            {
                EditorGUILayout.HelpBox("No PhysBones detected on this item.", MessageType.None);
            }
            else
            {
                EditorGUILayout.LabelField($"Detected PhysBones: {detectedPhysBones.Count}", EditorStyles.boldLabel);
                
                physBoneScrollPos = EditorGUILayout.BeginScrollView(physBoneScrollPos, GUILayout.Height(150));
                foreach (var pb in detectedPhysBones)
                {
                    EditorGUILayout.BeginHorizontal();
                    pb.Include = EditorGUILayout.Toggle(pb.Include, GUILayout.Width(20));
                    EditorGUILayout.LabelField(pb.Name);
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }
            
            EditorGUILayout.Space(20);
            
            // Navigation
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("← Back", GUILayout.Width(100), GUILayout.Height(30)))
            {
                currentStep = WizardStep.ConfigureToggle;
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Next →", GUILayout.Width(100), GUILayout.Height(30)))
            {
                currentStep = WizardStep.Blendshapes;
                DetectBlendshapes(avatar);
            }
            EditorGUILayout.EndHorizontal();
        }
        
        private void DetectBlendshapes(VRCAvatarDescriptor avatar)
        {
            detectedBlendshapes.Clear();
            
            if (targetObject == null) return;
            
            string itemName = targetObject.name.ToLower();
            
            // Search for blendshapes on body meshes
            var renderers = avatar.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMesh == null) continue;
                
                // Skip the target object's own renderers
                if (renderer.transform.IsChildOf(targetObject.transform)) continue;
                
                for (int i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
                {
                    string shapeName = renderer.sharedMesh.GetBlendShapeName(i);
                    string shapeNameLower = shapeName.ToLower();
                    
                    // Check for common patterns
                    if (shapeNameLower.Contains(itemName) ||
                        shapeNameLower.Contains("shrink_" + itemName) ||
                        shapeNameLower.Contains("hide_" + itemName) ||
                        shapeNameLower.Contains(itemName + "_shrink") ||
                        shapeNameLower.Contains(itemName + "_hide"))
                    {
                        detectedBlendshapes.Add(new BlendshapeInfo
                        {
                            Renderer = renderer,
                            BlendshapeName = shapeName,
                            BlendshapeIndex = i,
                            Include = true
                        });
                    }
                }
            }
        }
        
        private void DrawBlendshapesStep(VRCAvatarDescriptor avatar)
        {
            EditorGUILayout.LabelField("Step 4: Blendshapes", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Blendshapes on the body mesh can hide clipping when this item is on.\n" +
                "Common patterns: 'Shrink_ItemName', 'Hide_ItemName'",
                MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            animateBlendshapes = EditorGUILayout.Toggle("Animate Blendshapes", animateBlendshapes);
            
            if (detectedBlendshapes.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    $"No matching blendshapes found for '{targetObject?.name}'.\n" +
                    "You can add them manually later.",
                    MessageType.None);
            }
            else
            {
                EditorGUILayout.LabelField($"Detected Blendshapes: {detectedBlendshapes.Count}", EditorStyles.boldLabel);
                
                blendshapeScrollPos = EditorGUILayout.BeginScrollView(blendshapeScrollPos, GUILayout.Height(150));
                foreach (var bs in detectedBlendshapes)
                {
                    EditorGUILayout.BeginHorizontal();
                    bs.Include = EditorGUILayout.Toggle(bs.Include, GUILayout.Width(20));
                    EditorGUILayout.LabelField($"{bs.Renderer.name} → {bs.BlendshapeName}");
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }
            
            EditorGUILayout.Space(20);
            
            // Navigation
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("← Back", GUILayout.Width(100), GUILayout.Height(30)))
            {
                currentStep = WizardStep.PhysBones;
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Next →", GUILayout.Width(100), GUILayout.Height(30)))
            {
                currentStep = WizardStep.Review;
            }
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawReviewStep(OutfitWizardWindow parent, VRCAvatarDescriptor avatar)
        {
            EditorGUILayout.LabelField("Step 5: Review & Apply", EditorStyles.boldLabel);
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Summary", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.LabelField($"Target: {targetObject?.name}");
            EditorGUILayout.LabelField($"Toggle Name: {toggleName}");
            EditorGUILayout.LabelField($"Menu Path: {menuPath}");
            EditorGUILayout.LabelField($"Default State: {(defaultState ? "On" : "Off")}");
            EditorGUILayout.LabelField($"Save State: {(saveState ? "Yes" : "No")}");
            
            EditorGUILayout.Space(5);
            
            int pbCount = detectedPhysBones.Count(p => p.Include);
            int bsCount = detectedBlendshapes.Count(b => b.Include);
            
            EditorGUILayout.LabelField($"PhysBones to toggle: {pbCount}");
            EditorGUILayout.LabelField($"Blendshapes to animate: {bsCount}");
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("This will create:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("• Expression Parameter (Bool)");
            EditorGUILayout.LabelField("• Animator Layer with On/Off states");
            EditorGUILayout.LabelField("• Animation clips for toggle");
            EditorGUILayout.LabelField("• Expression Menu entry");
            
            EditorGUILayout.Space(20);
            
            // Navigation
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("← Back", GUILayout.Width(100), GUILayout.Height(30)))
            {
                currentStep = WizardStep.Blendshapes;
            }
            GUILayout.FlexibleSpace();
            
            GUI.backgroundColor = new Color(0.5f, 0.9f, 0.5f);
            if (GUILayout.Button("Apply", GUILayout.Width(120), GUILayout.Height(35)))
            {
                ApplyToggle(avatar);
                parent.OnWizardComplete();
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void ApplyToggle(VRCAvatarDescriptor avatar)
        {
            // Create the toggle using ToggleCreator
            var config = new ToggleCreator.ToggleConfig
            {
                TargetObject = targetObject,
                ToggleName = toggleName,
                MenuPath = menuPath,
                DefaultOn = defaultState,
                Saved = saveState,
                PhysBonesToToggle = togglePhysBonesWithItem 
                    ? detectedPhysBones.Where(p => p.Include).Select(p => p.PhysBone).ToList()
                    : new List<Component>(),
                BlendshapesToAnimate = animateBlendshapes
                    ? detectedBlendshapes.Where(b => b.Include).ToList()
                    : new List<BlendshapeInfo>()
            };
            
            ToggleCreator.CreateToggle(avatar, config);
        }
    }
}
