# VRChat Avatar Optimizer for Blender 4.x
# Standalone plugin to optimize VRChat avatars
# Features: Bone removal, Mesh decimation, Texture optimization

bl_info = {
    "name": "VRChat Avatar Optimizer",
    "author": "Sophey3dx",
    "version": (1, 0, 0),
    "blender": (4, 0, 0),
    "location": "View3D > Sidebar > VRC Optimizer",
    "description": "Optimize VRChat avatars by reducing bones, triangles, and texture sizes",
    "category": "3D View",
}

import bpy
import bmesh
from bpy.types import Panel, Operator, PropertyGroup
from bpy.props import IntProperty, FloatProperty, BoolProperty, EnumProperty, PointerProperty
from mathutils import Vector
import os

# ============================================================================
# VRChat Limits (from VRChat SDK)
# ============================================================================
class VRChatLimits:
    # Recommended (Excellent)
    EXCELLENT_TRIANGLES = 32000
    EXCELLENT_BONES = 75
    EXCELLENT_PHYSBONES = 4
    EXCELLENT_PHYSBONE_TRANSFORMS = 16
    EXCELLENT_MATERIALS = 4
    
    # Maximum (Good)
    MAX_TRIANGLES = 70000
    MAX_BONES = 400
    MAX_PHYSBONES = 32
    MAX_PHYSBONE_TRANSFORMS = 256
    MAX_MATERIALS = 32


# ============================================================================
# Properties
# ============================================================================
class VRCOptimizerProperties(PropertyGroup):
    # Target values
    target_triangles: IntProperty(
        name="Target Triangles",
        description="Target triangle count",
        default=70000,
        min=1000,
        max=500000
    )
    
    target_bones: IntProperty(
        name="Target Bones",
        description="Target bone count",
        default=400,
        min=50,
        max=1000
    )
    
    # Options
    preserve_shape_keys: BoolProperty(
        name="Preserve Shape Keys",
        description="Try to preserve shape keys during decimation",
        default=True
    )
    
    preserve_uvs: BoolProperty(
        name="Preserve UVs",
        description="Try to preserve UV seams during decimation",
        default=True
    )
    
    texture_max_size: EnumProperty(
        name="Max Texture Size",
        description="Maximum texture resolution",
        items=[
            ('512', '512x512', 'Reduce to 512x512'),
            ('1024', '1024x1024', 'Reduce to 1024x1024'),
            ('2048', '2048x2048', 'Reduce to 2048x2048'),
            ('4096', '4096x4096', 'Keep up to 4096x4096'),
        ],
        default='2048'
    )
    
    remove_unused_bones: BoolProperty(
        name="Remove Unused Bones",
        description="Remove bones not used by any mesh",
        default=True
    )
    
    merge_by_distance: BoolProperty(
        name="Merge Close Vertices",
        description="Merge vertices that are very close together",
        default=False
    )
    
    merge_distance: FloatProperty(
        name="Merge Distance",
        description="Distance threshold for merging vertices",
        default=0.0001,
        min=0.00001,
        max=0.01
    )


# ============================================================================
# Analysis Functions
# ============================================================================
def get_armature():
    """Get the armature object"""
    for obj in bpy.context.scene.objects:
        if obj.type == 'ARMATURE':
            return obj
    return None

def get_mesh_objects():
    """Get all mesh objects"""
    return [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']

def count_triangles():
    """Count total triangles in all meshes"""
    total = 0
    for obj in get_mesh_objects():
        if obj.data:
            total += len(obj.data.polygons)
    return total

def count_bones():
    """Count bones in armature"""
    armature = get_armature()
    if armature and armature.data:
        return len(armature.data.bones)
    return 0

def get_used_bones():
    """Get set of bones that are actually used by meshes"""
    used_bones = set()
    
    for obj in get_mesh_objects():
        if obj.type != 'MESH':
            continue
            
        # Get bones from vertex groups
        for vg in obj.vertex_groups:
            used_bones.add(vg.name)
        
        # Get bones from armature modifier
        for mod in obj.modifiers:
            if mod.type == 'ARMATURE' and mod.object:
                # All bones in the armature could potentially be used
                pass
    
    return used_bones

def get_unused_bones():
    """Get bones that are not used by any mesh"""
    armature = get_armature()
    if not armature:
        return set()
    
    all_bones = set(bone.name for bone in armature.data.bones)
    used_bones = get_used_bones()
    
    # Also keep bones that are parents of used bones
    bones_to_keep = set()
    for bone_name in used_bones:
        bone = armature.data.bones.get(bone_name)
        while bone:
            bones_to_keep.add(bone.name)
            bone = bone.parent
    
    return all_bones - bones_to_keep

def count_materials():
    """Count unique materials"""
    materials = set()
    for obj in get_mesh_objects():
        for slot in obj.material_slots:
            if slot.material:
                materials.add(slot.material.name)
    return len(materials)

def get_texture_memory_mb():
    """Estimate texture memory usage in MB"""
    total_bytes = 0
    processed = set()
    
    for img in bpy.data.images:
        if img.name in processed:
            continue
        processed.add(img.name)
        
        if img.size[0] > 0 and img.size[1] > 0:
            # Estimate: width * height * 4 bytes (RGBA)
            total_bytes += img.size[0] * img.size[1] * 4
    
    return total_bytes / (1024 * 1024)


# ============================================================================
# Operators
# ============================================================================
class VRC_OT_AnalyzeAvatar(Operator):
    """Analyze avatar and show statistics"""
    bl_idname = "vrc.analyze_avatar"
    bl_label = "Analyze Avatar"
    bl_options = {'REGISTER', 'UNDO'}
    
    def execute(self, context):
        triangles = count_triangles()
        bones = count_bones()
        materials = count_materials()
        texture_mb = get_texture_memory_mb()
        unused_bones = len(get_unused_bones())
        
        # Determine status
        tri_status = "✓" if triangles <= VRChatLimits.MAX_TRIANGLES else "✗"
        bone_status = "✓" if bones <= VRChatLimits.MAX_BONES else "✗"
        mat_status = "✓" if materials <= VRChatLimits.MAX_MATERIALS else "✗"
        
        message = (
            f"=== VRChat Avatar Analysis ===\n"
            f"\n"
            f"Triangles: {triangles:,} / {VRChatLimits.MAX_TRIANGLES:,} {tri_status}\n"
            f"Bones: {bones} / {VRChatLimits.MAX_BONES} {bone_status}\n"
            f"  (Unused: {unused_bones})\n"
            f"Materials: {materials} / {VRChatLimits.MAX_MATERIALS} {mat_status}\n"
            f"Texture Memory: ~{texture_mb:.1f} MB\n"
        )
        
        self.report({'INFO'}, message)
        
        # Also show in popup
        def draw(self, context):
            layout = self.layout
            layout.label(text=f"Triangles: {triangles:,} / {VRChatLimits.MAX_TRIANGLES:,} {tri_status}")
            layout.label(text=f"Bones: {bones} / {VRChatLimits.MAX_BONES} {bone_status}")
            layout.label(text=f"  Unused bones: {unused_bones}")
            layout.label(text=f"Materials: {materials} / {VRChatLimits.MAX_MATERIALS} {mat_status}")
            layout.label(text=f"Texture Memory: ~{texture_mb:.1f} MB")
        
        context.window_manager.popup_menu(draw, title="Avatar Analysis", icon='INFO')
        
        return {'FINISHED'}


class VRC_OT_DecimateAll(Operator):
    """Decimate all meshes to target triangle count"""
    bl_idname = "vrc.decimate_all"
    bl_label = "Decimate Meshes"
    bl_options = {'REGISTER', 'UNDO'}
    
    def execute(self, context):
        props = context.scene.vrc_optimizer
        target = props.target_triangles
        current = count_triangles()
        
        if current <= target:
            self.report({'INFO'}, f"Already at or below target ({current:,} triangles)")
            return {'FINISHED'}
        
        # Calculate ratio
        ratio = target / current
        
        # Apply decimation to each mesh
        for obj in get_mesh_objects():
            if obj.type != 'MESH':
                continue
            
            # Select only this object
            bpy.ops.object.select_all(action='DESELECT')
            obj.select_set(True)
            context.view_layer.objects.active = obj
            
            # Add decimate modifier
            mod = obj.modifiers.new(name="VRC_Decimate", type='DECIMATE')
            mod.decimate_type = 'COLLAPSE'
            mod.ratio = ratio
            
            if props.preserve_uvs:
                mod.use_collapse_triangulate = True
            
            # Apply modifier
            bpy.ops.object.modifier_apply(modifier=mod.name)
        
        new_count = count_triangles()
        self.report({'INFO'}, f"Decimated: {current:,} → {new_count:,} triangles")
        
        return {'FINISHED'}


class VRC_OT_RemoveUnusedBones(Operator):
    """Remove bones not used by any mesh"""
    bl_idname = "vrc.remove_unused_bones"
    bl_label = "Remove Unused Bones"
    bl_options = {'REGISTER', 'UNDO'}
    
    def execute(self, context):
        armature = get_armature()
        if not armature:
            self.report({'ERROR'}, "No armature found")
            return {'CANCELLED'}
        
        unused = get_unused_bones()
        
        if not unused:
            self.report({'INFO'}, "No unused bones found")
            return {'FINISHED'}
        
        # Enter edit mode
        bpy.ops.object.select_all(action='DESELECT')
        armature.select_set(True)
        context.view_layer.objects.active = armature
        bpy.ops.object.mode_set(mode='EDIT')
        
        # Delete unused bones
        for bone_name in unused:
            bone = armature.data.edit_bones.get(bone_name)
            if bone:
                armature.data.edit_bones.remove(bone)
        
        bpy.ops.object.mode_set(mode='OBJECT')
        
        self.report({'INFO'}, f"Removed {len(unused)} unused bones")
        
        return {'FINISHED'}


class VRC_OT_ResizeTextures(Operator):
    """Resize all textures to maximum size"""
    bl_idname = "vrc.resize_textures"
    bl_label = "Resize Textures"
    bl_options = {'REGISTER', 'UNDO'}
    
    def execute(self, context):
        props = context.scene.vrc_optimizer
        max_size = int(props.texture_max_size)
        
        resized = 0
        
        for img in bpy.data.images:
            if img.size[0] <= 0 or img.size[1] <= 0:
                continue
            
            if img.size[0] > max_size or img.size[1] > max_size:
                # Calculate new size maintaining aspect ratio
                aspect = img.size[0] / img.size[1]
                
                if aspect >= 1:
                    new_width = max_size
                    new_height = int(max_size / aspect)
                else:
                    new_height = max_size
                    new_width = int(max_size * aspect)
                
                img.scale(new_width, new_height)
                resized += 1
        
        new_memory = get_texture_memory_mb()
        self.report({'INFO'}, f"Resized {resized} textures. New memory: ~{new_memory:.1f} MB")
        
        return {'FINISHED'}


class VRC_OT_MergeVertices(Operator):
    """Merge vertices by distance"""
    bl_idname = "vrc.merge_vertices"
    bl_label = "Merge Close Vertices"
    bl_options = {'REGISTER', 'UNDO'}
    
    def execute(self, context):
        props = context.scene.vrc_optimizer
        distance = props.merge_distance
        
        total_removed = 0
        
        for obj in get_mesh_objects():
            if obj.type != 'MESH':
                continue
            
            before = len(obj.data.vertices)
            
            # Select object
            bpy.ops.object.select_all(action='DESELECT')
            obj.select_set(True)
            context.view_layer.objects.active = obj
            
            # Enter edit mode and merge
            bpy.ops.object.mode_set(mode='EDIT')
            bpy.ops.mesh.select_all(action='SELECT')
            bpy.ops.mesh.remove_doubles(threshold=distance)
            bpy.ops.object.mode_set(mode='OBJECT')
            
            after = len(obj.data.vertices)
            total_removed += (before - after)
        
        self.report({'INFO'}, f"Merged {total_removed} vertices")
        
        return {'FINISHED'}


class VRC_OT_OptimizeAll(Operator):
    """Run all optimizations"""
    bl_idname = "vrc.optimize_all"
    bl_label = "Optimize All"
    bl_options = {'REGISTER', 'UNDO'}
    
    def execute(self, context):
        props = context.scene.vrc_optimizer
        
        messages = []
        
        # 1. Remove unused bones
        if props.remove_unused_bones:
            bpy.ops.vrc.remove_unused_bones()
            messages.append("Removed unused bones")
        
        # 2. Merge vertices
        if props.merge_by_distance:
            bpy.ops.vrc.merge_vertices()
            messages.append("Merged close vertices")
        
        # 3. Decimate meshes
        if count_triangles() > props.target_triangles:
            bpy.ops.vrc.decimate_all()
            messages.append("Decimated meshes")
        
        # 4. Resize textures
        bpy.ops.vrc.resize_textures()
        messages.append("Resized textures")
        
        # Final analysis
        bpy.ops.vrc.analyze_avatar()
        
        self.report({'INFO'}, f"Optimization complete: {', '.join(messages)}")
        
        return {'FINISHED'}


# ============================================================================
# UI Panel
# ============================================================================
class VRC_PT_OptimizerPanel(Panel):
    """VRChat Avatar Optimizer Panel"""
    bl_label = "VRChat Avatar Optimizer"
    bl_idname = "VRC_PT_optimizer"
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_category = "VRC Optimizer"
    
    def draw(self, context):
        layout = self.layout
        props = context.scene.vrc_optimizer
        
        # Analysis Section
        box = layout.box()
        box.label(text="Analysis", icon='VIEWZOOM')
        
        # Quick stats
        col = box.column(align=True)
        col.label(text=f"Triangles: {count_triangles():,}")
        col.label(text=f"Bones: {count_bones()}")
        col.label(text=f"Materials: {count_materials()}")
        col.label(text=f"Texture Memory: ~{get_texture_memory_mb():.1f} MB")
        
        box.operator("vrc.analyze_avatar", icon='INFO')
        
        # Limits reference
        box2 = layout.box()
        box2.label(text="VRChat Limits (Good Rank)", icon='ERROR')
        col = box2.column(align=True)
        col.label(text=f"Max Triangles: {VRChatLimits.MAX_TRIANGLES:,}")
        col.label(text=f"Max Bones: {VRChatLimits.MAX_BONES}")
        col.label(text=f"Max PhysBones: {VRChatLimits.MAX_PHYSBONES}")
        col.label(text=f"Max Materials: {VRChatLimits.MAX_MATERIALS}")
        
        # Mesh Optimization
        box3 = layout.box()
        box3.label(text="Mesh Optimization", icon='MESH_DATA')
        box3.prop(props, "target_triangles")
        box3.prop(props, "preserve_shape_keys")
        box3.prop(props, "preserve_uvs")
        box3.operator("vrc.decimate_all", icon='MOD_DECIM')
        
        row = box3.row()
        row.prop(props, "merge_by_distance")
        if props.merge_by_distance:
            row.prop(props, "merge_distance")
            box3.operator("vrc.merge_vertices", icon='AUTOMERGE_ON')
        
        # Bone Optimization
        box4 = layout.box()
        box4.label(text="Bone Optimization", icon='BONE_DATA')
        box4.prop(props, "target_bones")
        box4.prop(props, "remove_unused_bones")
        
        unused_count = len(get_unused_bones())
        if unused_count > 0:
            box4.label(text=f"Unused bones found: {unused_count}", icon='ERROR')
        
        box4.operator("vrc.remove_unused_bones", icon='BONE_DATA')
        
        # Texture Optimization
        box5 = layout.box()
        box5.label(text="Texture Optimization", icon='TEXTURE')
        box5.prop(props, "texture_max_size")
        box5.operator("vrc.resize_textures", icon='IMAGE_DATA')
        
        # Optimize All
        layout.separator()
        layout.operator("vrc.optimize_all", icon='PLAY', text="OPTIMIZE ALL")


# ============================================================================
# Registration
# ============================================================================
classes = (
    VRCOptimizerProperties,
    VRC_OT_AnalyzeAvatar,
    VRC_OT_DecimateAll,
    VRC_OT_RemoveUnusedBones,
    VRC_OT_ResizeTextures,
    VRC_OT_MergeVertices,
    VRC_OT_OptimizeAll,
    VRC_PT_OptimizerPanel,
)

def register():
    for cls in classes:
        bpy.utils.register_class(cls)
    bpy.types.Scene.vrc_optimizer = PointerProperty(type=VRCOptimizerProperties)

def unregister():
    for cls in reversed(classes):
        bpy.utils.unregister_class(cls)
    del bpy.types.Scene.vrc_optimizer

if __name__ == "__main__":
    register()
