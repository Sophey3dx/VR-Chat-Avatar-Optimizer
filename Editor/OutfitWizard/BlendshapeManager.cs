using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using VRC.SDK3.Avatars.Components;

namespace OutfitWizard
{
    /// <summary>
    /// Utility class for detecting and managing blendshapes
    /// </summary>
    public static class BlendshapeManager
    {
        /// <summary>
        /// Common blendshape naming patterns used for clothing/outfit systems
        /// </summary>
        public static readonly string[] CommonPrefixes = 
        {
            "Shrink_",
            "shrink_",
            "Hide_",
            "hide_",
            "Cloth_",
            "cloth_",
            "Outfit_",
            "outfit_",
            "Body_",
            "body_"
        };
        
        public static readonly string[] CommonSuffixes =
        {
            "_Shrink",
            "_shrink",
            "_Hide",
            "_hide",
            "_On",
            "_on",
            "_Off",
            "_off"
        };
        
        public class BlendshapeMatch
        {
            public SkinnedMeshRenderer Renderer;
            public string BlendshapeName;
            public int BlendshapeIndex;
            public float CurrentValue;
            public string MatchedItemName;
        }
        
        /// <summary>
        /// Find all body meshes (meshes that are NOT part of the outfit items)
        /// </summary>
        public static List<SkinnedMeshRenderer> FindBodyMeshes(VRCAvatarDescriptor avatar, List<GameObject> outfitItems)
        {
            var result = new List<SkinnedMeshRenderer>();
            
            if (avatar == null) return result;
            
            var allRenderers = avatar.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            
            foreach (var renderer in allRenderers)
            {
                if (renderer.sharedMesh == null) continue;
                if (renderer.sharedMesh.blendShapeCount == 0) continue;
                
                // Check if this renderer is part of any outfit item
                bool isOutfitRenderer = outfitItems.Any(item => 
                    item != null && renderer.transform.IsChildOf(item.transform));
                
                if (!isOutfitRenderer)
                {
                    result.Add(renderer);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Find blendshapes that match an item name
        /// </summary>
        public static List<BlendshapeMatch> FindMatchingBlendshapes(
            SkinnedMeshRenderer renderer, string itemName)
        {
            var matches = new List<BlendshapeMatch>();
            
            if (renderer == null || renderer.sharedMesh == null || string.IsNullOrEmpty(itemName))
                return matches;
            
            string itemNameLower = itemName.ToLower();
            
            for (int i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
            {
                string shapeName = renderer.sharedMesh.GetBlendShapeName(i);
                string shapeNameLower = shapeName.ToLower();
                
                // Check various matching patterns
                bool isMatch = false;
                
                // Direct match
                if (shapeNameLower.Contains(itemNameLower))
                {
                    isMatch = true;
                }
                
                // Check with common prefixes
                foreach (var prefix in CommonPrefixes)
                {
                    if (shapeNameLower == (prefix + itemNameLower).ToLower())
                    {
                        isMatch = true;
                        break;
                    }
                }
                
                // Check with common suffixes
                foreach (var suffix in CommonSuffixes)
                {
                    if (shapeNameLower == (itemNameLower + suffix).ToLower())
                    {
                        isMatch = true;
                        break;
                    }
                }
                
                if (isMatch)
                {
                    matches.Add(new BlendshapeMatch
                    {
                        Renderer = renderer,
                        BlendshapeName = shapeName,
                        BlendshapeIndex = i,
                        CurrentValue = renderer.GetBlendShapeWeight(i),
                        MatchedItemName = itemName
                    });
                }
            }
            
            return matches;
        }
        
        /// <summary>
        /// Find all blendshapes matching any item in a list
        /// </summary>
        public static List<BlendshapeMatch> FindMatchingBlendshapes(
            VRCAvatarDescriptor avatar, List<GameObject> items)
        {
            var allMatches = new List<BlendshapeMatch>();
            
            if (avatar == null || items == null) return allMatches;
            
            var bodyMeshes = FindBodyMeshes(avatar, items);
            
            foreach (var item in items)
            {
                if (item == null) continue;
                
                foreach (var renderer in bodyMeshes)
                {
                    var matches = FindMatchingBlendshapes(renderer, item.name);
                    allMatches.AddRange(matches);
                }
            }
            
            // Remove duplicates
            return allMatches
                .GroupBy(m => new { m.Renderer, m.BlendshapeIndex })
                .Select(g => g.First())
                .ToList();
        }
        
        /// <summary>
        /// Get all blendshapes on a renderer
        /// </summary>
        public static List<BlendshapeMatch> GetAllBlendshapes(SkinnedMeshRenderer renderer)
        {
            var result = new List<BlendshapeMatch>();
            
            if (renderer == null || renderer.sharedMesh == null)
                return result;
            
            for (int i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
            {
                result.Add(new BlendshapeMatch
                {
                    Renderer = renderer,
                    BlendshapeName = renderer.sharedMesh.GetBlendShapeName(i),
                    BlendshapeIndex = i,
                    CurrentValue = renderer.GetBlendShapeWeight(i)
                });
            }
            
            return result;
        }
        
        /// <summary>
        /// Set a blendshape value
        /// </summary>
        public static void SetBlendshapeValue(SkinnedMeshRenderer renderer, int index, float value)
        {
            if (renderer == null) return;
            
            Undo.RecordObject(renderer, "Set Blendshape");
            renderer.SetBlendShapeWeight(index, Mathf.Clamp(value, 0, 100));
            EditorUtility.SetDirty(renderer);
        }
        
        /// <summary>
        /// Set a blendshape value by name
        /// </summary>
        public static void SetBlendshapeValue(SkinnedMeshRenderer renderer, string blendshapeName, float value)
        {
            if (renderer == null || renderer.sharedMesh == null) return;
            
            int index = renderer.sharedMesh.GetBlendShapeIndex(blendshapeName);
            if (index >= 0)
            {
                SetBlendshapeValue(renderer, index, value);
            }
        }
        
        /// <summary>
        /// Preview blendshape values for an outfit
        /// </summary>
        public static void PreviewOutfitBlendshapes(List<BlendshapeMatch> matches, bool outfitOn)
        {
            float targetValue = outfitOn ? 100f : 0f;
            
            foreach (var match in matches)
            {
                if (match.Renderer != null)
                {
                    match.Renderer.SetBlendShapeWeight(match.BlendshapeIndex, targetValue);
                }
            }
        }
        
        /// <summary>
        /// Reset blendshapes to their original values
        /// </summary>
        public static void ResetBlendshapes(List<BlendshapeMatch> matches)
        {
            foreach (var match in matches)
            {
                if (match.Renderer != null)
                {
                    match.Renderer.SetBlendShapeWeight(match.BlendshapeIndex, match.CurrentValue);
                }
            }
        }
    }
}
