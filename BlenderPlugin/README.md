# VRChat Avatar Optimizer - Blender Plugin

A standalone Blender 4.x plugin to optimize VRChat avatars by reducing bones, triangles, and texture sizes.

## Features

- **Avatar Analysis**: Shows current triangle count, bone count, materials, and texture memory
- **Mesh Decimation**: Reduce triangles while preserving shape keys and UVs
- **Bone Removal**: Automatically remove unused bones from the armature
- **Texture Resizing**: Batch resize all textures to reduce memory usage
- **VRChat Limits**: Shows VRChat's performance limits for reference

## Installation

1. Open Blender 4.x
2. Go to `Edit > Preferences > Add-ons`
3. Click `Install...`
4. Select `vrchat_avatar_optimizer.py`
5. Enable the addon by checking the checkbox

## Usage

1. Import your VRChat avatar (FBX)
2. Open the sidebar in 3D View (`N` key)
3. Select the "VRC Optimizer" tab
4. Click **Analyze Avatar** to see current stats
5. Adjust settings as needed
6. Click **OPTIMIZE ALL** or use individual optimization buttons

## VRChat Performance Limits

| Metric | Good Rank | Recommended |
|--------|-----------|-------------|
| Triangles | 70,000 | 32,000 |
| Bones | 400 | 75 |
| PhysBones | 32 | 4 |
| PB Transforms | 256 | 16 |
| Materials | 32 | 4 |

## Options

### Mesh Optimization
- **Target Triangles**: The goal triangle count for decimation
- **Preserve Shape Keys**: Try to maintain shape keys during decimation
- **Preserve UVs**: Maintain UV seam integrity
- **Merge Close Vertices**: Merge vertices within a threshold distance

### Bone Optimization
- **Target Bones**: Goal bone count (for reference)
- **Remove Unused Bones**: Delete bones not used by any mesh

### Texture Optimization
- **Max Texture Size**: Resize textures larger than this resolution

## Tips

1. **Before optimizing**: Make a backup of your model!
2. **Shape keys**: Decimation may affect shape key quality
3. **Bones**: Unused bone removal is safe - it only removes bones not weighted to any mesh
4. **Textures**: Consider using texture atlasing for better material reduction

## Requirements

- Blender 4.0 or higher
- VRChat avatar model (FBX format recommended)

## License

Free to use for personal and commercial projects.
