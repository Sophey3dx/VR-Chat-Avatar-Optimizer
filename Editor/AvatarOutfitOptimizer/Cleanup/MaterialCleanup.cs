using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace AvatarOutfitOptimizer.Cleanup
{
    /// <summary>
    /// Handles cleanup of unused Materials and Textures from removed meshes
    /// WARNING: This deletes project assets - use with caution!
    /// </summary>
    public class MaterialCleanup
    {
        /// <summary>
        /// Result of material/texture analysis
        /// </summary>
        public class MaterialAnalysisResult
        {
            public List<Material> UnusedMaterials { get; set; } = new List<Material>();
            public List<Texture> UnusedTextures { get; set; } = new List<Texture>();
            public int TotalMaterialCount { get; set; }
            public int TotalTextureCount { get; set; }
        }

        /// <summary>
        /// Analyzes which materials and textures are only used by inactive/removed meshes
        /// </summary>
        public MaterialAnalysisResult Analyze(GameObject avatarRoot, AvatarSnapshot fullSnapshot, AvatarSnapshot activeSnapshot)
        {
            var result = new MaterialAnalysisResult();
            
            if (avatarRoot == null) return result;

            // Get all materials from ALL renderers (including inactive)
            var allRenderers = avatarRoot.GetComponentsInChildren<Renderer>(true);
            var allMaterials = new HashSet<Material>();
            var activeMaterials = new HashSet<Material>();

            foreach (var renderer in allRenderers)
            {
                if (renderer == null || renderer.sharedMaterials == null) continue;

                foreach (var mat in renderer.sharedMaterials)
                {
                    if (mat != null)
                    {
                        allMaterials.Add(mat);
                        
                        // Check if this renderer is on an active GameObject
                        if (renderer.gameObject.activeInHierarchy && renderer.enabled)
                        {
                            activeMaterials.Add(mat);
                        }
                    }
                }
            }

            // Materials that are ONLY used by inactive renderers
            var unusedMaterials = allMaterials.Except(activeMaterials).ToList();
            result.UnusedMaterials = unusedMaterials;
            result.TotalMaterialCount = allMaterials.Count;

            // Collect textures from unused materials
            var unusedTextures = new HashSet<Texture>();
            var activeTextures = new HashSet<Texture>();

            // Get textures from unused materials
            foreach (var mat in unusedMaterials)
            {
                CollectTexturesFromMaterial(mat, unusedTextures);
            }

            // Get textures from active materials (to exclude them)
            foreach (var mat in activeMaterials)
            {
                CollectTexturesFromMaterial(mat, activeTextures);
            }

            // Only textures that are NOT used by any active material
            result.UnusedTextures = unusedTextures.Except(activeTextures).ToList();
            result.TotalTextureCount = unusedTextures.Count + activeTextures.Count;

            Debug.Log($"[AvatarOptimizer] Material Analysis: {result.UnusedMaterials.Count} unused materials, {result.UnusedTextures.Count} unused textures");

            return result;
        }

        /// <summary>
        /// Deletes unused materials and textures from the project
        /// WARNING: This is destructive and cannot be undone!
        /// </summary>
        public void DeleteUnusedAssets(MaterialAnalysisResult analysis)
        {
            if (analysis == null) return;

            int deletedMaterials = 0;
            int deletedTextures = 0;

            // Delete unused textures first (before materials that reference them)
            foreach (var texture in analysis.UnusedTextures)
            {
                if (texture == null) continue;
                
                string path = AssetDatabase.GetAssetPath(texture);
                if (!string.IsNullOrEmpty(path) && !path.StartsWith("Packages/"))
                {
                    // Check if this texture is used elsewhere in the project
                    if (!IsAssetUsedElsewhere(path))
                    {
                        Undo.RecordObject(texture, "Delete Unused Texture");
                        AssetDatabase.DeleteAsset(path);
                        deletedTextures++;
                        Debug.Log($"[AvatarOptimizer] Deleted texture: {path}");
                    }
                    else
                    {
                        Debug.Log($"[AvatarOptimizer] Skipped texture (used elsewhere): {path}");
                    }
                }
            }

            // Delete unused materials
            foreach (var material in analysis.UnusedMaterials)
            {
                if (material == null) continue;
                
                string path = AssetDatabase.GetAssetPath(material);
                if (!string.IsNullOrEmpty(path) && !path.StartsWith("Packages/"))
                {
                    // Check if this material is used elsewhere in the project
                    if (!IsAssetUsedElsewhere(path))
                    {
                        Undo.RecordObject(material, "Delete Unused Material");
                        AssetDatabase.DeleteAsset(path);
                        deletedMaterials++;
                        Debug.Log($"[AvatarOptimizer] Deleted material: {path}");
                    }
                    else
                    {
                        Debug.Log($"[AvatarOptimizer] Skipped material (used elsewhere): {path}");
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[AvatarOptimizer] Material cleanup complete: {deletedMaterials} materials, {deletedTextures} textures deleted");
        }

        /// <summary>
        /// Collects all textures referenced by a material
        /// </summary>
        private void CollectTexturesFromMaterial(Material material, HashSet<Texture> textures)
        {
            if (material == null) return;

            // Get all texture property names from the shader
            var shader = material.shader;
            if (shader == null) return;

            int propertyCount = ShaderUtil.GetPropertyCount(shader);
            for (int i = 0; i < propertyCount; i++)
            {
                if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                {
                    string propertyName = ShaderUtil.GetPropertyName(shader, i);
                    var texture = material.GetTexture(propertyName);
                    if (texture != null)
                    {
                        textures.Add(texture);
                    }
                }
            }
        }

        /// <summary>
        /// Checks if an asset is used by other assets in the project
        /// </summary>
        private bool IsAssetUsedElsewhere(string assetPath)
        {
            // Find all assets that reference this asset
            var dependencies = AssetDatabase.GetDependencies(assetPath, false);
            
            // Get all assets that depend on this one
            string[] allAssetPaths = AssetDatabase.GetAllAssetPaths();
            foreach (var path in allAssetPaths)
            {
                if (path == assetPath) continue;
                if (path.StartsWith("Packages/")) continue;
                
                var deps = AssetDatabase.GetDependencies(path, false);
                if (deps.Contains(assetPath))
                {
                    return true; // Found another asset using this one
                }
            }

            return false;
        }

        /// <summary>
        /// Gets a summary of what would be deleted (for preview/dry run)
        /// </summary>
        public string GetCleanupSummary(MaterialAnalysisResult analysis)
        {
            if (analysis == null) return "No analysis available";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Materials to remove: {analysis.UnusedMaterials.Count}");
            
            foreach (var mat in analysis.UnusedMaterials.Take(10))
            {
                sb.AppendLine($"  - {mat.name}");
            }
            if (analysis.UnusedMaterials.Count > 10)
            {
                sb.AppendLine($"  ... and {analysis.UnusedMaterials.Count - 10} more");
            }

            sb.AppendLine();
            sb.AppendLine($"Textures to remove: {analysis.UnusedTextures.Count}");
            
            foreach (var tex in analysis.UnusedTextures.Take(10))
            {
                sb.AppendLine($"  - {tex.name}");
            }
            if (analysis.UnusedTextures.Count > 10)
            {
                sb.AppendLine($"  ... and {analysis.UnusedTextures.Count - 10} more");
            }

            return sb.ToString();
        }
    }
}
