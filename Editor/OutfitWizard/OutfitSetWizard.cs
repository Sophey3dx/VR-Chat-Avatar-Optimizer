using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using VRC.SDK3.Avatars.Components;

namespace OutfitWizard
{
    /// <summary>
    /// Wizard for creating outfit set swap systems
    /// Steps: Define Outfits → Shared Items → PhysBones → Blendshapes → Menu Type → Review
    /// </summary>
    public class OutfitSetWizard
    {
        public enum WizardStep
        {
            DefineOutfits,
            SharedItems,
            PhysBones,
            Blendshapes,
            MenuType,
            Review
        }
        
        public enum MenuStyle
        {
            RadialPuppet,
            SubmenuToggles,
            FourAxisPuppet
        }
        
        private WizardStep currentStep = WizardStep.DefineOutfits;
        
        // Outfit definitions
        private List<OutfitDefinition> outfits = new List<OutfitDefinition>();
        private List<GameObject> sharedItems = new List<GameObject>();
        
        // Menu configuration
        private MenuStyle menuStyle = MenuStyle.SubmenuToggles;
        private string menuName = "Outfits";
        private int defaultOutfitIndex = 0;
        private bool saveState = true;
        
        // PhysBones per outfit
        private Dictionary<int, List<PhysBoneEntry>> outfitPhysBones = new Dictionary<int, List<PhysBoneEntry>>();
        
        // Blendshapes per outfit
        private Dictionary<int, List<BlendshapeEntry>> outfitBlendshapes = new Dictionary<int, List<BlendshapeEntry>>();
        
        // Scroll positions
        private Vector2 outfitScrollPos;
        private Vector2 sharedScrollPos;
        private Vector2 physBoneScrollPos;
        private Vector2 blendshapeScrollPos;
        
        // Editing state
        private int editingOutfitIndex = -1;
        
        public class OutfitDefinition
        {
            public string Name = "New Outfit";
            public List<GameObject> Items = new List<GameObject>();
            public bool Expanded = true;
        }
        
        public class PhysBoneEntry
        {
            public Component PhysBone;
            public string Name;
            public bool Include = true;
        }
        
        public class BlendshapeEntry
        {
            public SkinnedMeshRenderer Renderer;
            public string BlendshapeName;
            public int BlendshapeIndex;
            public bool Include = true;
        }
        
        public void Reset()
        {
            currentStep = WizardStep.DefineOutfits;
            outfits.Clear();
            sharedItems.Clear();
            outfitPhysBones.Clear();
            outfitBlendshapes.Clear();
            menuStyle = MenuStyle.SubmenuToggles;
            menuName = "Outfits";
            defaultOutfitIndex = 0;
            editingOutfitIndex = -1;
            
            // Start with one empty outfit
            outfits.Add(new OutfitDefinition { Name = "Outfit 1" });
        }
        
        public void DrawWizard(OutfitWizardWindow parent, VRCAvatarDescriptor avatar)
        {
            DrawProgressBar();
            EditorGUILayout.Space(10);
            
            switch (currentStep)
            {
                case WizardStep.DefineOutfits:
                    DrawDefineOutfitsStep(avatar);
                    break;
                case WizardStep.SharedItems:
                    DrawSharedItemsStep(avatar);
                    break;
                case WizardStep.PhysBones:
                    DrawPhysBonesStep(avatar);
                    break;
                case WizardStep.Blendshapes:
                    DrawBlendshapesStep(avatar);
                    break;
                case WizardStep.MenuType:
                    DrawMenuTypeStep();
                    break;
                case WizardStep.Review:
                    DrawReviewStep(parent, avatar);
                    break;
            }
        }
        
        private void DrawProgressBar()
        {
            string[] steps = { "Outfits", "Shared", "PhysBones", "Blendshapes", "Menu", "Review" };
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
        
        private void DrawDefineOutfitsStep(VRCAvatarDescriptor avatar)
        {
            EditorGUILayout.LabelField("Step 1: Define Outfits", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Define your outfit sets. Each outfit is a group of items that will be shown together.\n" +
                "When one outfit is active, all others will be hidden.",
                MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            outfitScrollPos = EditorGUILayout.BeginScrollView(outfitScrollPos, GUILayout.Height(350));
            
            for (int i = 0; i < outfits.Count; i++)
            {
                DrawOutfitCard(i, avatar);
                EditorGUILayout.Space(5);
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space(10);
            
            if (GUILayout.Button("+ Add Outfit", GUILayout.Height(30)))
            {
                outfits.Add(new OutfitDefinition { Name = $"Outfit {outfits.Count + 1}" });
            }
            
            EditorGUILayout.Space(20);
            
            // Navigation
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            GUI.enabled = outfits.Count >= 2 && outfits.All(o => o.Items.Count > 0);
            if (GUILayout.Button("Next →", GUILayout.Width(100), GUILayout.Height(30)))
            {
                currentStep = WizardStep.SharedItems;
            }
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
            
            if (outfits.Count < 2)
            {
                EditorGUILayout.HelpBox("You need at least 2 outfits to create a swap system.", MessageType.Warning);
            }
            else if (outfits.Any(o => o.Items.Count == 0))
            {
                EditorGUILayout.HelpBox("All outfits must have at least one item.", MessageType.Warning);
            }
        }
        
        private void DrawOutfitCard(int index, VRCAvatarDescriptor avatar)
        {
            var outfit = outfits[index];
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Header
            EditorGUILayout.BeginHorizontal();
            
            outfit.Expanded = EditorGUILayout.Foldout(outfit.Expanded, "", true);
            outfit.Name = EditorGUILayout.TextField(outfit.Name, EditorStyles.boldLabel);
            
            EditorGUILayout.LabelField($"({outfit.Items.Count} items)", GUILayout.Width(80));
            
            if (outfits.Count > 2)
            {
                if (GUILayout.Button("×", GUILayout.Width(25)))
                {
                    outfits.RemoveAt(index);
                    return;
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (outfit.Expanded)
            {
                EditorGUI.indentLevel++;
                
                // Items list
                for (int i = 0; i < outfit.Items.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    outfit.Items[i] = (GameObject)EditorGUILayout.ObjectField(
                        outfit.Items[i],
                        typeof(GameObject),
                        true);
                    
                    if (GUILayout.Button("-", GUILayout.Width(25)))
                    {
                        outfit.Items.RemoveAt(i);
                        i--;
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                // Add item
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("+ Add Item"))
                {
                    outfit.Items.Add(null);
                }
                
                if (Selection.activeGameObject != null && 
                    Selection.activeGameObject.transform.IsChildOf(avatar.transform))
                {
                    if (GUILayout.Button($"+ Add '{Selection.activeGameObject.name}'"))
                    {
                        if (!outfit.Items.Contains(Selection.activeGameObject))
                        {
                            outfit.Items.Add(Selection.activeGameObject);
                        }
                    }
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawSharedItemsStep(VRCAvatarDescriptor avatar)
        {
            EditorGUILayout.LabelField("Step 2: Shared Items", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Items that should stay visible in ALL outfits.\n" +
                "Example: Body, Hair, permanent accessories.",
                MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            sharedScrollPos = EditorGUILayout.BeginScrollView(sharedScrollPos, GUILayout.Height(200));
            
            for (int i = 0; i < sharedItems.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                
                sharedItems[i] = (GameObject)EditorGUILayout.ObjectField(
                    sharedItems[i],
                    typeof(GameObject),
                    true);
                
                if (GUILayout.Button("-", GUILayout.Width(25)))
                {
                    sharedItems.RemoveAt(i);
                    i--;
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("+ Add Shared Item"))
            {
                sharedItems.Add(null);
            }
            
            if (Selection.activeGameObject != null && 
                Selection.activeGameObject.transform.IsChildOf(avatar.transform))
            {
                if (GUILayout.Button($"+ Add '{Selection.activeGameObject.name}'"))
                {
                    if (!sharedItems.Contains(Selection.activeGameObject))
                    {
                        sharedItems.Add(Selection.activeGameObject);
                    }
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(20);
            
            // Navigation
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("← Back", GUILayout.Width(100), GUILayout.Height(30)))
            {
                currentStep = WizardStep.DefineOutfits;
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Next →", GUILayout.Width(100), GUILayout.Height(30)))
            {
                currentStep = WizardStep.PhysBones;
                DetectPhysBonesForOutfits();
            }
            EditorGUILayout.EndHorizontal();
        }
        
        private void DetectPhysBonesForOutfits()
        {
            outfitPhysBones.Clear();
            
            var physBoneType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == "VRCPhysBone");
            
            if (physBoneType == null) return;
            
            for (int i = 0; i < outfits.Count; i++)
            {
                var entries = new List<PhysBoneEntry>();
                
                foreach (var item in outfits[i].Items)
                {
                    if (item == null) continue;
                    
                    var components = item.GetComponentsInChildren(physBoneType, true);
                    foreach (var comp in components)
                    {
                        entries.Add(new PhysBoneEntry
                        {
                            PhysBone = comp as Component,
                            Name = (comp as Component)?.gameObject.name ?? "Unknown",
                            Include = true
                        });
                    }
                }
                
                outfitPhysBones[i] = entries;
            }
        }
        
        private void DrawPhysBonesStep(VRCAvatarDescriptor avatar)
        {
            EditorGUILayout.LabelField("Step 3: PhysBones per Outfit", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "PhysBones will be enabled/disabled with their outfit.\n" +
                "This keeps your PhysBone count low when outfits are hidden.",
                MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            physBoneScrollPos = EditorGUILayout.BeginScrollView(physBoneScrollPos, GUILayout.Height(250));
            
            for (int i = 0; i < outfits.Count; i++)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(outfits[i].Name, EditorStyles.boldLabel);
                
                if (outfitPhysBones.TryGetValue(i, out var entries) && entries.Count > 0)
                {
                    foreach (var entry in entries)
                    {
                        EditorGUILayout.BeginHorizontal();
                        entry.Include = EditorGUILayout.Toggle(entry.Include, GUILayout.Width(20));
                        EditorGUILayout.LabelField(entry.Name);
                        EditorGUILayout.EndHorizontal();
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("No PhysBones found", EditorStyles.miniLabel);
                }
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space(20);
            
            // Navigation
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("← Back", GUILayout.Width(100), GUILayout.Height(30)))
            {
                currentStep = WizardStep.SharedItems;
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Next →", GUILayout.Width(100), GUILayout.Height(30)))
            {
                currentStep = WizardStep.Blendshapes;
                DetectBlendshapesForOutfits(avatar);
            }
            EditorGUILayout.EndHorizontal();
        }
        
        private void DetectBlendshapesForOutfits(VRCAvatarDescriptor avatar)
        {
            outfitBlendshapes.Clear();
            
            var renderers = avatar.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            
            for (int i = 0; i < outfits.Count; i++)
            {
                var entries = new List<BlendshapeEntry>();
                var outfit = outfits[i];
                
                // Get all item names for this outfit
                var itemNames = outfit.Items
                    .Where(item => item != null)
                    .Select(item => item.name.ToLower())
                    .ToList();
                
                foreach (var renderer in renderers)
                {
                    if (renderer.sharedMesh == null) continue;
                    
                    // Skip renderers that are part of outfit items
                    bool isOutfitRenderer = outfit.Items.Any(item => 
                        item != null && renderer.transform.IsChildOf(item.transform));
                    if (isOutfitRenderer) continue;
                    
                    for (int j = 0; j < renderer.sharedMesh.blendShapeCount; j++)
                    {
                        string shapeName = renderer.sharedMesh.GetBlendShapeName(j);
                        string shapeNameLower = shapeName.ToLower();
                        
                        // Check if blendshape matches any item in this outfit
                        foreach (var itemName in itemNames)
                        {
                            if (shapeNameLower.Contains(itemName) ||
                                shapeNameLower.Contains("shrink_" + itemName) ||
                                shapeNameLower.Contains("hide_" + itemName))
                            {
                                entries.Add(new BlendshapeEntry
                                {
                                    Renderer = renderer,
                                    BlendshapeName = shapeName,
                                    BlendshapeIndex = j,
                                    Include = true
                                });
                                break;
                            }
                        }
                    }
                }
                
                outfitBlendshapes[i] = entries;
            }
        }
        
        private void DrawBlendshapesStep(VRCAvatarDescriptor avatar)
        {
            EditorGUILayout.LabelField("Step 4: Blendshapes per Outfit", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Body blendshapes can hide clipping when outfits are active.\n" +
                "These will be animated to 100 when the outfit is on.",
                MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            blendshapeScrollPos = EditorGUILayout.BeginScrollView(blendshapeScrollPos, GUILayout.Height(250));
            
            for (int i = 0; i < outfits.Count; i++)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(outfits[i].Name, EditorStyles.boldLabel);
                
                if (outfitBlendshapes.TryGetValue(i, out var entries) && entries.Count > 0)
                {
                    foreach (var entry in entries)
                    {
                        EditorGUILayout.BeginHorizontal();
                        entry.Include = EditorGUILayout.Toggle(entry.Include, GUILayout.Width(20));
                        EditorGUILayout.LabelField($"{entry.Renderer.name} → {entry.BlendshapeName}");
                        EditorGUILayout.EndHorizontal();
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("No matching blendshapes found", EditorStyles.miniLabel);
                }
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }
            
            EditorGUILayout.EndScrollView();
            
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
                currentStep = WizardStep.MenuType;
            }
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawMenuTypeStep()
        {
            EditorGUILayout.LabelField("Step 5: Menu Configuration", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Choose how you want to select outfits in your Expression Menu.",
                MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            menuName = EditorGUILayout.TextField("Menu Name", menuName);
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("Menu Style", EditorStyles.boldLabel);
            
            // Radial Puppet
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            bool isRadial = menuStyle == MenuStyle.RadialPuppet;
            if (EditorGUILayout.ToggleLeft("Radial Puppet", isRadial, EditorStyles.boldLabel))
            {
                menuStyle = MenuStyle.RadialPuppet;
            }
            EditorGUILayout.LabelField("Rotate the radial menu to switch outfits.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(5);
            
            // Submenu with Toggles
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            bool isSubmenu = menuStyle == MenuStyle.SubmenuToggles;
            if (EditorGUILayout.ToggleLeft("Submenu with Buttons", isSubmenu, EditorStyles.boldLabel))
            {
                menuStyle = MenuStyle.SubmenuToggles;
            }
            EditorGUILayout.LabelField("A submenu with a button for each outfit.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            // Default outfit
            string[] outfitNames = outfits.Select(o => o.Name).ToArray();
            defaultOutfitIndex = EditorGUILayout.Popup("Default Outfit", defaultOutfitIndex, outfitNames);
            
            saveState = EditorGUILayout.Toggle("Save State", saveState);
            
            EditorGUILayout.Space(20);
            
            // Navigation
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("← Back", GUILayout.Width(100), GUILayout.Height(30)))
            {
                currentStep = WizardStep.Blendshapes;
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
            EditorGUILayout.LabelField("Step 6: Review & Apply", EditorStyles.boldLabel);
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Summary", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.LabelField($"Number of Outfits: {outfits.Count}");
            EditorGUILayout.LabelField($"Menu Name: {menuName}");
            EditorGUILayout.LabelField($"Menu Style: {menuStyle}");
            EditorGUILayout.LabelField($"Default Outfit: {outfits[defaultOutfitIndex].Name}");
            EditorGUILayout.LabelField($"Save State: {(saveState ? "Yes" : "No")}");
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("Outfits:", EditorStyles.boldLabel);
            foreach (var outfit in outfits)
            {
                int pbCount = outfitPhysBones.TryGetValue(outfits.IndexOf(outfit), out var pbs) 
                    ? pbs.Count(p => p.Include) : 0;
                int bsCount = outfitBlendshapes.TryGetValue(outfits.IndexOf(outfit), out var bss)
                    ? bss.Count(b => b.Include) : 0;
                
                EditorGUILayout.LabelField(
                    $"  • {outfit.Name}: {outfit.Items.Count} items, {pbCount} PhysBones, {bsCount} blendshapes");
            }
            
            if (sharedItems.Count > 0)
            {
                EditorGUILayout.LabelField($"Shared Items: {sharedItems.Count}");
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("This will create:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("• Expression Parameter (Int)");
            EditorGUILayout.LabelField($"• Animator Layer with {outfits.Count} states");
            EditorGUILayout.LabelField("• Animation clips for each outfit");
            EditorGUILayout.LabelField($"• Expression Menu: {menuStyle}");
            
            EditorGUILayout.Space(20);
            
            // Navigation
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("← Back", GUILayout.Width(100), GUILayout.Height(30)))
            {
                currentStep = WizardStep.MenuType;
            }
            GUILayout.FlexibleSpace();
            
            GUI.backgroundColor = new Color(0.5f, 0.9f, 0.5f);
            if (GUILayout.Button("Apply", GUILayout.Width(120), GUILayout.Height(35)))
            {
                ApplyOutfitSet(avatar);
                parent.OnWizardComplete();
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void ApplyOutfitSet(VRCAvatarDescriptor avatar)
        {
            var config = new OutfitSwapCreator.OutfitSwapConfig
            {
                MenuName = menuName,
                MenuStyle = menuStyle,
                DefaultOutfitIndex = defaultOutfitIndex,
                Saved = saveState,
                Outfits = outfits,
                SharedItems = sharedItems,
                OutfitPhysBones = outfitPhysBones,
                OutfitBlendshapes = outfitBlendshapes
            };
            
            OutfitSwapCreator.CreateOutfitSwap(avatar, config);
        }
    }
}
