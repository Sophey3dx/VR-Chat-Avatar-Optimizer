# VRChat Avatar Optimizer - Blender 5.0 Plugin

Advanced mesh, bone, and texture optimization for VRChat avatars with **Unity Bridge** support.

## Features

### 🔗 Unity Bridge
- **Automatic Folder Watch** - Blender detects when Unity exports new data
- **Auto Apply Selection** - Automatically mark objects to keep/remove
- **Auto Delete** - Optionally delete unwanted objects automatically
- **Two-way Workflow** - Configure in Unity, execute in Blender

### 🔧 Optimization Tools
- **Mesh Decimation** - Reduce triangle count with adjustable ratio
- **Unused Bone Removal** - Delete bones not used by any vertex groups
- **Texture Resizing** - Scale down textures to target resolution
- **Scene Analysis** - View current stats vs VRChat limits

## Installation

### Blender 5.0+
1. Open Blender
2. Go to `Edit > Preferences > Add-ons`
3. Click `Install from Disk...`
4. Select `vrchat_avatar_optimizer_blender5.zip`
5. Enable the addon by checking the box next to "VRChat Avatar Optimizer"

### Location
After installation, find the panel in:
`View3D > Sidebar (N) > VRC Optimizer`

## Unity Bridge Workflow

### Step 1: Configure in Unity
1. Open your avatar in Unity
2. Open `Tools > VRChat Avatar Optimizer`
3. **Enable only the objects you want to KEEP**
4. Click `Capture Snapshot`
5. Click `Export to Blender`

### Step 2: Import to Blender
1. Export your avatar FBX from Unity
2. Import FBX into Blender
3. In the VRC Optimizer panel, click `Reload` or start `Auto-Watch`
4. Click `Apply Selection` - objects to remove will be hidden

### Step 3: Delete Unwanted Objects
- Click `DELETE Marked Objects` to permanently remove hidden objects
- Or enable `Auto Delete` for automatic removal

### Step 4: Optimize Further
1. Use `Decimate Meshes` to reduce triangle count
2. Use `Remove Unused Bones` to clean up the armature
3. Use `Resize Textures` to reduce texture memory

### Step 5: Export Back
1. Export optimized FBX from Blender
2. Import into Unity
3. Run Unity optimizer for final cleanup

## Bridge File Location

The bridge file is stored in:
```
Documents/VRChatAvatarOptimizer/vrchat_optimizer_bridge.json
```

Click `Open Bridge Folder` to view it.

## VRChat Limits Reference

| Metric | Excellent | Good/Medium | Poor |
|--------|-----------|-------------|------|
| Triangles | 32,000 | 70,000 | 70,000+ |
| Bones | 75 | 400 | 400+ |
| PhysBones | 4 | 32 | 32+ |
| PB Transforms | 16 | 256 | 256+ |

## Requirements

- Blender 5.0 or higher
- (Optional) Unity with VRChat Avatar Optimizer for bridge features

## Troubleshooting

**"No bridge file found"**
- Export from Unity first using `Export to Blender` button

**Objects not being matched**
- Blender may add `.001` suffixes - the plugin handles this
- Check that object names match between Unity and Blender

**Auto-Watch not working**
- Make sure the modal operator is running (green button)
- Check the console for errors

## Version History

- **v2.0.0** - Blender 5.0 support, Unity Bridge, Auto-Watch, Auto-Delete
- **v1.0.0** - Initial release with basic optimization tools
