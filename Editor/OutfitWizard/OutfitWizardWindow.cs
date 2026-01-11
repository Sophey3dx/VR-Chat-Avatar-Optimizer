using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VRC.SDK3.Avatars.Components;

namespace OutfitWizard
{
    /// <summary>
    /// Main EditorWindow for the Outfit Wizard Tool
    /// Provides two modes: Single Item Toggle and Outfit Set Swap
    /// </summary>
    public class OutfitWizardWindow : EditorWindow
    {
        // Mode selection
        public enum WizardMode
        {
            ModeSelection,
            SingleToggle,
            OutfitSet
        }
        
        private WizardMode currentMode = WizardMode.ModeSelection;
        
        // Avatar reference
        private GameObject selectedAvatar;
        private VRCAvatarDescriptor avatarDescriptor;
        
        // Sub-wizards
        private SingleToggleWizard singleToggleWizard;
        private OutfitSetWizard outfitSetWizard;
        
        // UI State
        private Vector2 scrollPosition;
        
        [MenuItem("Tools/VRChat Outfit Wizard")]
        public static void ShowWindow()
        {
            var window = GetWindow<OutfitWizardWindow>("Outfit Wizard");
            window.minSize = new Vector2(450, 600);
        }
        
        private void OnEnable()
        {
            singleToggleWizard = new SingleToggleWizard();
            outfitSetWizard = new OutfitSetWizard();
        }
        
        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            DrawHeader();
            EditorGUILayout.Space(10);
            
            DrawAvatarSelection();
            EditorGUILayout.Space(10);
            
            if (avatarDescriptor == null)
            {
                EditorGUILayout.HelpBox(
                    "Please select an avatar with a VRC Avatar Descriptor to begin.",
                    MessageType.Info);
            }
            else
            {
                switch (currentMode)
                {
                    case WizardMode.ModeSelection:
                        DrawModeSelection();
                        break;
                    case WizardMode.SingleToggle:
                        singleToggleWizard.DrawWizard(this, avatarDescriptor);
                        break;
                    case WizardMode.OutfitSet:
                        outfitSetWizard.DrawWizard(this, avatarDescriptor);
                        break;
                }
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal();
            
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
            
            EditorGUILayout.LabelField("VRChat Outfit Wizard", titleStyle, GUILayout.Height(30));
            
            EditorGUILayout.EndHorizontal();
            
            // Back button when in wizard mode
            if (currentMode != WizardMode.ModeSelection)
            {
                if (GUILayout.Button("← Back to Mode Selection", GUILayout.Height(25)))
                {
                    currentMode = WizardMode.ModeSelection;
                    singleToggleWizard.Reset();
                    outfitSetWizard.Reset();
                }
            }
        }
        
        private void DrawAvatarSelection()
        {
            EditorGUILayout.LabelField("Avatar", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            selectedAvatar = (GameObject)EditorGUILayout.ObjectField(
                "Avatar Root",
                selectedAvatar,
                typeof(GameObject),
                true);
            
            if (EditorGUI.EndChangeCheck())
            {
                if (selectedAvatar != null)
                {
                    avatarDescriptor = selectedAvatar.GetComponent<VRCAvatarDescriptor>();
                    if (avatarDescriptor == null)
                    {
                        EditorUtility.DisplayDialog(
                            "Invalid Avatar",
                            "Selected GameObject does not have a VRC Avatar Descriptor.",
                            "OK");
                        selectedAvatar = null;
                    }
                }
                else
                {
                    avatarDescriptor = null;
                }
            }
            
            // Auto-detect from selection
            if (selectedAvatar == null && Selection.activeGameObject != null)
            {
                var desc = Selection.activeGameObject.GetComponentInParent<VRCAvatarDescriptor>();
                if (desc != null)
                {
                    if (GUILayout.Button($"Use: {desc.gameObject.name}"))
                    {
                        selectedAvatar = desc.gameObject;
                        avatarDescriptor = desc;
                    }
                }
            }
        }
        
        private void DrawModeSelection()
        {
            EditorGUILayout.LabelField("Select Mode", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);
            
            // Single Toggle Mode
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Single Item Toggle", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Create a simple On/Off toggle for a single clothing item.\n" +
                "Perfect for accessories, hats, glasses, etc.",
                EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(5);
            if (GUILayout.Button("Create Single Toggle", GUILayout.Height(35)))
            {
                currentMode = WizardMode.SingleToggle;
                singleToggleWizard.Reset();
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(15);
            
            // Outfit Set Mode
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Outfit Set Swap", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Create a system to swap between complete outfits.\n" +
                "Define multiple outfits, and switching activates one while hiding others.\n" +
                "Uses Radial Menu or Submenu for outfit selection.",
                EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(5);
            if (GUILayout.Button("Create Outfit Set", GUILayout.Height(35)))
            {
                currentMode = WizardMode.OutfitSet;
                outfitSetWizard.Reset();
            }
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Called by sub-wizards when they complete successfully
        /// </summary>
        public void OnWizardComplete()
        {
            currentMode = WizardMode.ModeSelection;
            EditorUtility.DisplayDialog(
                "Success",
                "Outfit setup completed successfully!\n\n" +
                "Your avatar has been updated with the new toggles/outfits.",
                "OK");
        }
    }
}
