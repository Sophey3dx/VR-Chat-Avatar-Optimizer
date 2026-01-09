using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace AvatarOutfitOptimizer.Utils
{
    /// <summary>
    /// Helper methods for serialization and hashing
    /// </summary>
    public static class SerializationHelper
    {
        /// <summary>
        /// Generates a hash from meshes, bones, and materials for avatar fingerprinting
        /// </summary>
        public static string GenerateAvatarFingerprint(
            List<SkinnedMeshRenderer> renderers,
            HashSet<Transform> bones,
            HashSet<Material> materials)
        {
            var sb = new StringBuilder();
            
            // Hash meshes
            var meshHashes = new List<string>();
            foreach (var renderer in renderers.OrderBy(r => r.name))
            {
                if (renderer.sharedMesh != null)
                {
                    meshHashes.Add($"{renderer.name}:{renderer.sharedMesh.GetInstanceID()}");
                }
            }
            sb.AppendLine(string.Join("|", meshHashes));
            
            // Hash bones
            var boneHashes = bones.OrderBy(b => b != null ? b.name : "")
                .Select(b => b != null ? $"{b.name}:{b.GetInstanceID()}" : "")
                .Where(s => !string.IsNullOrEmpty(s));
            sb.AppendLine(string.Join("|", boneHashes));
            
            // Hash materials
            var materialHashes = materials.OrderBy(m => m != null ? m.name : "")
                .Select(m => m != null ? $"{m.name}:{m.GetInstanceID()}" : "")
                .Where(s => !string.IsNullOrEmpty(s));
            sb.AppendLine(string.Join("|", materialHashes));
            
            // Generate SHA256 hash
            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
                return Convert.ToBase64String(hashBytes).Substring(0, 16); // Short hash for readability
            }
        }

        /// <summary>
        /// Gets all materials from SkinnedMeshRenderers
        /// </summary>
        public static HashSet<Material> GetMaterialsFromRenderers(List<SkinnedMeshRenderer> renderers)
        {
            var materials = new HashSet<Material>();
            
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMaterials != null)
                {
                    foreach (var material in renderer.sharedMaterials)
                    {
                        if (material != null)
                        {
                            materials.Add(material);
                        }
                    }
                }
            }
            
            return materials;
        }
    }
}

