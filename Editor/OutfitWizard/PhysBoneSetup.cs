using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using VRC.SDK3.Avatars.Components;

namespace OutfitWizard
{
    /// <summary>
    /// Utility class for detecting and configuring PhysBones
    /// </summary>
    public static class PhysBoneSetup
    {
        private static Type _physBoneType;
        
        public static Type PhysBoneType
        {
            get
            {
                if (_physBoneType == null)
                {
                    _physBoneType = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(a => a.GetTypes())
                        .FirstOrDefault(t => t.Name == "VRCPhysBone");
                }
                return _physBoneType;
            }
        }
        
        /// <summary>
        /// Preset configurations for common PhysBone use cases
        /// </summary>
        public enum PhysBonePreset
        {
            Hair,
            Skirt,
            Coat,
            Ears,
            Tail,
            Accessory,
            Custom
        }
        
        public class PhysBonePresetConfig
        {
            public float Pull = 0.2f;
            public float Spring = 0.2f;
            public float Stiffness = 0f;
            public float Gravity = 0f;
            public float GravityFalloff = 0f;
            public float Immobile = 0f;
            public float Radius = 0.02f;
            public bool AllowGrabbing = true;
            public bool AllowPosing = false;
        }
        
        public static PhysBonePresetConfig GetPreset(PhysBonePreset preset)
        {
            switch (preset)
            {
                case PhysBonePreset.Hair:
                    return new PhysBonePresetConfig
                    {
                        Pull = 0.2f,
                        Spring = 0.8f,
                        Stiffness = 0.2f,
                        Gravity = 0.1f,
                        GravityFalloff = 1f,
                        Radius = 0.02f,
                        AllowGrabbing = true
                    };
                    
                case PhysBonePreset.Skirt:
                    return new PhysBonePresetConfig
                    {
                        Pull = 0.2f,
                        Spring = 0.5f,
                        Stiffness = 0.5f,
                        Gravity = 0.3f,
                        GravityFalloff = 0.5f,
                        Radius = 0.05f,
                        AllowGrabbing = false
                    };
                    
                case PhysBonePreset.Coat:
                    return new PhysBonePresetConfig
                    {
                        Pull = 0.2f,
                        Spring = 0.3f,
                        Stiffness = 0.7f,
                        Gravity = 0.5f,
                        GravityFalloff = 0.3f,
                        Radius = 0.05f,
                        AllowGrabbing = false
                    };
                    
                case PhysBonePreset.Ears:
                    return new PhysBonePresetConfig
                    {
                        Pull = 0.2f,
                        Spring = 0.9f,
                        Stiffness = 0.3f,
                        Gravity = 0f,
                        Radius = 0.02f,
                        AllowGrabbing = true,
                        AllowPosing = true
                    };
                    
                case PhysBonePreset.Tail:
                    return new PhysBonePresetConfig
                    {
                        Pull = 0.2f,
                        Spring = 0.6f,
                        Stiffness = 0.4f,
                        Gravity = 0.2f,
                        GravityFalloff = 0.8f,
                        Radius = 0.03f,
                        AllowGrabbing = true,
                        AllowPosing = true
                    };
                    
                case PhysBonePreset.Accessory:
                default:
                    return new PhysBonePresetConfig
                    {
                        Pull = 0.2f,
                        Spring = 0.5f,
                        Stiffness = 0.5f,
                        Gravity = 0.1f,
                        Radius = 0.01f,
                        AllowGrabbing = true
                    };
            }
        }
        
        /// <summary>
        /// Find all PhysBones on a GameObject and its children
        /// </summary>
        public static List<Component> FindPhysBones(GameObject root)
        {
            var result = new List<Component>();
            
            if (PhysBoneType == null || root == null)
                return result;
            
            var components = root.GetComponentsInChildren(PhysBoneType, true);
            foreach (var comp in components)
            {
                result.Add(comp as Component);
            }
            
            return result;
        }
        
        /// <summary>
        /// Find all PhysBone Colliders on a GameObject
        /// </summary>
        public static List<Component> FindPhysBoneColliders(GameObject root)
        {
            var result = new List<Component>();
            
            var colliderType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == "VRCPhysBoneCollider");
            
            if (colliderType == null || root == null)
                return result;
            
            var components = root.GetComponentsInChildren(colliderType, true);
            foreach (var comp in components)
            {
                result.Add(comp as Component);
            }
            
            return result;
        }
        
        /// <summary>
        /// Get PhysBone info for display
        /// </summary>
        public static string GetPhysBoneInfo(Component physBone)
        {
            if (physBone == null) return "null";
            
            var rootProp = physBone.GetType().GetProperty("rootTransform");
            var root = rootProp?.GetValue(physBone) as Transform;
            
            string rootName = root != null ? root.name : physBone.gameObject.name;
            
            // Count affected transforms
            int transformCount = 1;
            if (root != null)
            {
                transformCount = root.GetComponentsInChildren<Transform>().Length;
            }
            
            return $"{rootName} ({transformCount} transforms)";
        }
        
        /// <summary>
        /// Apply a preset configuration to a PhysBone
        /// </summary>
        public static void ApplyPreset(Component physBone, PhysBonePreset preset)
        {
            if (physBone == null) return;
            
            var config = GetPreset(preset);
            var type = physBone.GetType();
            
            Undo.RecordObject(physBone, "Apply PhysBone Preset");
            
            SetPropertyValue(physBone, "pull", config.Pull);
            SetPropertyValue(physBone, "spring", config.Spring);
            SetPropertyValue(physBone, "stiffness", config.Stiffness);
            SetPropertyValue(physBone, "gravity", config.Gravity);
            SetPropertyValue(physBone, "gravityFalloff", config.GravityFalloff);
            SetPropertyValue(physBone, "immobile", config.Immobile);
            SetPropertyValue(physBone, "radius", config.Radius);
            SetPropertyValue(physBone, "allowGrabbing", config.AllowGrabbing);
            SetPropertyValue(physBone, "allowPosing", config.AllowPosing);
            
            EditorUtility.SetDirty(physBone);
        }
        
        private static void SetPropertyValue(Component component, string propertyName, object value)
        {
            var prop = component.GetType().GetProperty(propertyName);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(component, value);
            }
            else
            {
                var field = component.GetType().GetField(propertyName);
                if (field != null)
                {
                    field.SetValue(component, value);
                }
            }
        }
        
        /// <summary>
        /// Create a new PhysBone component on a bone
        /// </summary>
        public static Component CreatePhysBone(Transform bone, PhysBonePreset preset = PhysBonePreset.Accessory)
        {
            if (PhysBoneType == null || bone == null)
            {
                Debug.LogError("[OutfitWizard] Cannot create PhysBone - type not found or bone is null");
                return null;
            }
            
            // Check if already has PhysBone
            var existing = bone.GetComponent(PhysBoneType);
            if (existing != null)
            {
                Debug.LogWarning($"[OutfitWizard] {bone.name} already has a PhysBone");
                return existing as Component;
            }
            
            Undo.RecordObject(bone.gameObject, "Add PhysBone");
            var physBone = bone.gameObject.AddComponent(PhysBoneType) as Component;
            
            if (physBone != null)
            {
                ApplyPreset(physBone, preset);
            }
            
            return physBone;
        }
    }
}
