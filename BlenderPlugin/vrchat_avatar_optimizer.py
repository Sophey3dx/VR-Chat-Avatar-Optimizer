# VRChat Avatar Optimizer - Blender 5.0 Plugin
# Bridge to Unity for seamless avatar optimization workflow

bl_info = {
    "name": "VRChat Avatar Optimizer",
    "author": "Sophey3dx",
    "version": (2, 0, 0),
    "blender": (5, 0, 0),
    "location": "View3D > Sidebar > VRC Optimizer",
    "description": "Optimize VRChat avatars with Unity bridge support",
    "category": "3D View",
}

import bpy
import os
import json
import time
from bpy.props import (
    FloatProperty, 
    IntProperty, 
    BoolProperty, 
    StringProperty,
    EnumProperty
)
from bpy.types import Operator, Panel, PropertyGroup
from pathlib import Path


# ============================================================================
# BRIDGE DATA HANDLER
# ============================================================================

class BridgeData:
    """Handles communication with Unity via JSON bridge file"""
    
    _instance = None
    _last_modified = 0
    _data = None
    
    @classmethod
    def get_bridge_path(cls):
        """Get the default bridge folder path"""
        documents = Path.home() / "Documents" / "VRChatAvatarOptimizer"
        return documents / "vrchat_optimizer_bridge.json"
    
    @classmethod
    def load(cls, force=False):
        """Load bridge data from JSON file"""
        path = cls.get_bridge_path()
        
        if not path.exists():
            cls._data = None
            return None
        
        # Check if file was modified
        current_mtime = path.stat().st_mtime
        if not force and current_mtime == cls._last_modified and cls._data is not None:
            return cls._data
        
        try:
            with open(path, 'r', encoding='utf-8') as f:
                cls._data = json.load(f)
                cls._last_modified = current_mtime
                return cls._data
        except Exception as e:
            print(f"[VRC Optimizer] Error loading bridge data: {e}")
            return None
    
    @classmethod
    def get_keep_objects(cls):
        """Get list of objects to keep"""
        data = cls.load()
        if data:
            return data.get('keepObjects', [])
        return []
    
    @classmethod
    def get_remove_objects(cls):
        """Get list of objects to remove"""
        data = cls.load()
        if data:
            return data.get('removeObjects', [])
        return []
    
    @classmethod
    def get_settings(cls):
        """Get optimization settings"""
        data = cls.load()
        if data:
            return data.get('settings', {})
        return {}
    
    @classmethod
    def has_new_data(cls):
        """Check if bridge file has been updated"""
        path = cls.get_bridge_path()
        if not path.exists():
            return False
        
        current_mtime = path.stat().st_mtime
        return current_mtime != cls._last_modified


# ============================================================================
# FOLDER WATCH OPERATOR
# ============================================================================

class VRC_OT_folder_watch(Operator):
    """Watch for Unity bridge file updates and auto-apply"""
    bl_idname = "vrc.folder_watch"
    bl_label = "Watch Bridge Folder"
    bl_description = "Automatically detect and apply Unity bridge updates"
    
    _timer = None
    _running = False
    
    def modal(self, context, event):
        if event.type == 'TIMER':
            # Check if still running
            if not context.scene.vrc_optimizer.watch_enabled:
                self.cancel(context)
                return {'CANCELLED'}
            
            # Check for new bridge data
            if BridgeData.has_new_data():
                data = BridgeData.load(force=True)
                if data:
                    self.report({'INFO'}, f"[VRC] New bridge data detected: {data.get('avatarName', 'Unknown')}")
                    
                    # Auto-apply if enabled
                    if context.scene.vrc_optimizer.auto_apply:
                        bpy.ops.vrc.apply_bridge_selection()
                        
                        # Auto-delete if enabled
                        if context.scene.vrc_optimizer.auto_delete:
                            bpy.ops.vrc.delete_unselected()
        
        return {'PASS_THROUGH'}
    
    def execute(self, context):
        if VRC_OT_folder_watch._running:
            self.report({'WARNING'}, "Folder watch already running")
            return {'CANCELLED'}
        
        # Start timer
        wm = context.window_manager
        self._timer = wm.event_timer_add(2.0, window=context.window)  # Check every 2 seconds
        wm.modal_handler_add(self)
        
        VRC_OT_folder_watch._running = True
        context.scene.vrc_optimizer.watch_enabled = True
        
        self.report({'INFO'}, "Started watching bridge folder")
        return {'RUNNING_MODAL'}
    
    def cancel(self, context):
        if self._timer:
            context.window_manager.event_timer_remove(self._timer)
        VRC_OT_folder_watch._running = False
        context.scene.vrc_optimizer.watch_enabled = False


class VRC_OT_stop_watch(Operator):
    """Stop watching the bridge folder"""
    bl_idname = "vrc.stop_watch"
    bl_label = "Stop Watching"
    bl_description = "Stop automatic bridge detection"
    
    def execute(self, context):
        context.scene.vrc_optimizer.watch_enabled = False
        self.report({'INFO'}, "Stopped watching bridge folder")
        return {'FINISHED'}


# ============================================================================
# BRIDGE OPERATORS
# ============================================================================

class VRC_OT_load_bridge(Operator):
    """Load bridge data from Unity"""
    bl_idname = "vrc.load_bridge"
    bl_label = "Load Bridge Data"
    bl_description = "Load object selection from Unity"
    
    def execute(self, context):
        data = BridgeData.load(force=True)
        
        if data is None:
            self.report({'WARNING'}, "No bridge file found. Export from Unity first.")
            return {'CANCELLED'}
        
        props = context.scene.vrc_optimizer
        props.bridge_avatar_name = data.get('avatarName', 'Unknown')
        props.bridge_timestamp = data.get('timestamp', '')
        props.bridge_keep_count = len(data.get('keepObjects', []))
        props.bridge_remove_count = len(data.get('removeObjects', []))
        
        self.report({'INFO'}, f"Loaded bridge data for: {props.bridge_avatar_name}")
        return {'FINISHED'}


class VRC_OT_apply_bridge_selection(Operator):
    """Apply bridge selection to scene objects"""
    bl_idname = "vrc.apply_bridge_selection"
    bl_label = "Apply Selection"
    bl_description = "Select objects to keep, deselect objects to remove"
    
    def execute(self, context):
        keep_objects = BridgeData.get_keep_objects()
        remove_objects = BridgeData.get_remove_objects()
        
        if not keep_objects and not remove_objects:
            self.report({'WARNING'}, "No bridge data loaded")
            return {'CANCELLED'}
        
        # Deselect all first
        bpy.ops.object.select_all(action='DESELECT')
        
        kept = 0
        marked_remove = 0
        
        for obj in bpy.data.objects:
            obj_name = obj.name
            # Also check without .001, .002 suffixes
            base_name = obj_name.rsplit('.', 1)[0] if '.' in obj_name and obj_name.rsplit('.', 1)[1].isdigit() else obj_name
            
            if obj_name in keep_objects or base_name in keep_objects:
                obj.select_set(True)
                obj.hide_set(False)
                kept += 1
            elif obj_name in remove_objects or base_name in remove_objects:
                obj.select_set(False)
                obj.hide_set(True)  # Hide objects marked for removal
                marked_remove += 1
        
        self.report({'INFO'}, f"Applied: {kept} to keep, {marked_remove} to remove")
        return {'FINISHED'}


class VRC_OT_delete_unselected(Operator):
    """Delete all hidden/unselected objects"""
    bl_idname = "vrc.delete_unselected"
    bl_label = "Delete Marked Objects"
    bl_description = "Permanently delete objects marked for removal (hidden objects)"
    bl_options = {'UNDO'}
    
    def invoke(self, context, event):
        return context.window_manager.invoke_confirm(self, event)
    
    def execute(self, context):
        remove_objects = BridgeData.get_remove_objects()
        
        deleted = 0
        to_delete = []
        
        for obj in bpy.data.objects:
            obj_name = obj.name
            base_name = obj_name.rsplit('.', 1)[0] if '.' in obj_name and obj_name.rsplit('.', 1)[1].isdigit() else obj_name
            
            # Delete if in remove list OR if hidden
            if obj_name in remove_objects or base_name in remove_objects or obj.hide_get():
                to_delete.append(obj)
        
        # Delete objects
        for obj in to_delete:
            bpy.data.objects.remove(obj, do_unlink=True)
            deleted += 1
        
        self.report({'INFO'}, f"Deleted {deleted} objects")
        return {'FINISHED'}


# ============================================================================
# OPTIMIZATION OPERATORS
# ============================================================================

class VRC_OT_analyze(Operator):
    """Analyze current scene for VRChat optimization"""
    bl_idname = "vrc.analyze"
    bl_label = "Analyze Scene"
    bl_description = "Analyze meshes, bones, and textures"
    
    def execute(self, context):
        props = context.scene.vrc_optimizer
        
        # Count triangles
        total_tris = 0
        mesh_count = 0
        for obj in bpy.data.objects:
            if obj.type == 'MESH' and not obj.hide_get():
                mesh_count += 1
                if obj.data:
                    total_tris += sum(len(p.vertices) - 2 for p in obj.data.polygons)
        
        # Count bones
        bone_count = 0
        for obj in bpy.data.objects:
            if obj.type == 'ARMATURE' and not obj.hide_get():
                bone_count += len(obj.data.bones)
        
        # Count materials
        material_count = len([m for m in bpy.data.materials if m.users > 0])
        
        props.scene_triangles = total_tris
        props.scene_bones = bone_count
        props.scene_meshes = mesh_count
        props.scene_materials = material_count
        
        self.report({'INFO'}, f"Analysis: {total_tris} tris, {bone_count} bones, {mesh_count} meshes")
        return {'FINISHED'}


class VRC_OT_decimate_mesh(Operator):
    """Decimate selected meshes to reduce triangle count"""
    bl_idname = "vrc.decimate_mesh"
    bl_label = "Decimate Meshes"
    bl_description = "Reduce triangle count of selected meshes"
    bl_options = {'UNDO'}
    
    def execute(self, context):
        props = context.scene.vrc_optimizer
        ratio = props.decimate_ratio
        
        decimated = 0
        for obj in context.selected_objects:
            if obj.type == 'MESH':
                # Add decimate modifier
                mod = obj.modifiers.new(name="VRC_Decimate", type='DECIMATE')
                mod.ratio = ratio
                mod.use_collapse_triangulate = True
                
                # Apply modifier
                context.view_layer.objects.active = obj
                bpy.ops.object.modifier_apply(modifier=mod.name)
                decimated += 1
        
        self.report({'INFO'}, f"Decimated {decimated} meshes to {ratio*100:.0f}%")
        return {'FINISHED'}


class VRC_OT_remove_unused_bones(Operator):
    """Remove bones not used by any vertex groups"""
    bl_idname = "vrc.remove_unused_bones"
    bl_label = "Remove Unused Bones"
    bl_description = "Delete bones that don't affect any mesh"
    bl_options = {'UNDO'}
    
    def execute(self, context):
        armature = None
        for obj in context.selected_objects:
            if obj.type == 'ARMATURE':
                armature = obj
                break
        
        if not armature:
            self.report({'WARNING'}, "Select an armature first")
            return {'CANCELLED'}
        
        # Collect used bones from vertex groups
        used_bones = set()
        for obj in bpy.data.objects:
            if obj.type == 'MESH' and not obj.hide_get():
                for vg in obj.vertex_groups:
                    used_bones.add(vg.name)
        
        # Enter edit mode
        context.view_layer.objects.active = armature
        bpy.ops.object.mode_set(mode='EDIT')
        
        # Find unused bones
        removed = 0
        edit_bones = armature.data.edit_bones
        bones_to_remove = []
        
        for bone in edit_bones:
            if bone.name not in used_bones:
                # Don't remove root bones or bones with used children
                has_used_child = any(child.name in used_bones for child in bone.children_recursive)
                if not has_used_child and bone.parent is not None:
                    bones_to_remove.append(bone.name)
        
        for bone_name in bones_to_remove:
            bone = edit_bones.get(bone_name)
            if bone:
                edit_bones.remove(bone)
                removed += 1
        
        bpy.ops.object.mode_set(mode='OBJECT')
        
        self.report({'INFO'}, f"Removed {removed} unused bones")
        return {'FINISHED'}


class VRC_OT_resize_textures(Operator):
    """Resize all textures to maximum size"""
    bl_idname = "vrc.resize_textures"
    bl_label = "Resize Textures"
    bl_description = "Scale down textures to maximum resolution"
    bl_options = {'UNDO'}
    
    def execute(self, context):
        props = context.scene.vrc_optimizer
        max_size = props.max_texture_size
        
        resized = 0
        for image in bpy.data.images:
            if image.size[0] > max_size or image.size[1] > max_size:
                # Calculate new size maintaining aspect ratio
                ratio = min(max_size / image.size[0], max_size / image.size[1])
                new_width = int(image.size[0] * ratio)
                new_height = int(image.size[1] * ratio)
                
                image.scale(new_width, new_height)
                resized += 1
        
        self.report({'INFO'}, f"Resized {resized} textures to max {max_size}px")
        return {'FINISHED'}


class VRC_OT_open_bridge_folder(Operator):
    """Open the bridge folder in file explorer"""
    bl_idname = "vrc.open_bridge_folder"
    bl_label = "Open Bridge Folder"
    bl_description = "Open the folder where Unity exports bridge data"
    
    def execute(self, context):
        path = BridgeData.get_bridge_path().parent
        path.mkdir(parents=True, exist_ok=True)
        
        import subprocess
        import sys
        
        if sys.platform == 'win32':
            os.startfile(str(path))
        elif sys.platform == 'darwin':
            subprocess.run(['open', str(path)])
        else:
            subprocess.run(['xdg-open', str(path)])
        
        return {'FINISHED'}


# ============================================================================
# PROPERTY GROUP
# ============================================================================

class VRC_OptimizerProperties(PropertyGroup):
    # Bridge properties
    bridge_avatar_name: StringProperty(name="Avatar", default="")
    bridge_timestamp: StringProperty(name="Timestamp", default="")
    bridge_keep_count: IntProperty(name="Keep", default=0)
    bridge_remove_count: IntProperty(name="Remove", default=0)
    
    # Watch settings
    watch_enabled: BoolProperty(name="Watch Enabled", default=False)
    auto_apply: BoolProperty(
        name="Auto Apply", 
        default=True,
        description="Automatically apply selection when bridge file updates"
    )
    auto_delete: BoolProperty(
        name="Auto Delete", 
        default=False,
        description="Automatically delete objects marked for removal"
    )
    
    # Scene stats
    scene_triangles: IntProperty(name="Triangles", default=0)
    scene_bones: IntProperty(name="Bones", default=0)
    scene_meshes: IntProperty(name="Meshes", default=0)
    scene_materials: IntProperty(name="Materials", default=0)
    
    # Optimization settings
    decimate_ratio: FloatProperty(
        name="Decimate Ratio",
        default=0.5,
        min=0.01,
        max=1.0,
        description="Target polygon ratio (0.5 = 50% reduction)"
    )
    
    max_texture_size: IntProperty(
        name="Max Texture Size",
        default=2048,
        min=128,
        max=4096,
        description="Maximum texture resolution in pixels"
    )


# ============================================================================
# PANEL
# ============================================================================

class VRC_PT_optimizer_panel(Panel):
    """Main panel for VRChat Avatar Optimizer"""
    bl_label = "VRChat Avatar Optimizer"
    bl_idname = "VRC_PT_optimizer_panel"
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_category = "VRC Optimizer"
    
    def draw(self, context):
        layout = self.layout
        props = context.scene.vrc_optimizer
        
        # ===== UNITY BRIDGE SECTION =====
        box = layout.box()
        box.label(text="Unity Bridge", icon='LINKED')
        
        # Bridge status
        bridge_path = BridgeData.get_bridge_path()
        if bridge_path.exists():
            box.label(text=f"✓ Bridge file found", icon='CHECKMARK')
            if props.bridge_avatar_name:
                box.label(text=f"Avatar: {props.bridge_avatar_name}")
                row = box.row()
                row.label(text=f"Keep: {props.bridge_keep_count}")
                row.label(text=f"Remove: {props.bridge_remove_count}")
        else:
            box.label(text="✗ No bridge file", icon='ERROR')
            box.label(text="Export from Unity first")
        
        # Bridge buttons
        row = box.row(align=True)
        row.operator("vrc.load_bridge", text="Reload", icon='FILE_REFRESH')
        row.operator("vrc.open_bridge_folder", text="Open Folder", icon='FILE_FOLDER')
        
        box.operator("vrc.apply_bridge_selection", text="Apply Selection", icon='RESTRICT_SELECT_OFF')
        
        # Watch settings
        col = box.column(align=True)
        if not props.watch_enabled:
            col.operator("vrc.folder_watch", text="Start Auto-Watch", icon='PLAY')
        else:
            col.operator("vrc.stop_watch", text="Stop Auto-Watch", icon='PAUSE')
            col.label(text="Watching for changes...", icon='TIME')
        
        row = col.row()
        row.prop(props, "auto_apply", text="Auto Apply")
        row.prop(props, "auto_delete", text="Auto Delete")
        
        # Delete button
        box.separator()
        row = box.row()
        row.alert = True
        row.operator("vrc.delete_unselected", text="DELETE Marked Objects", icon='TRASH')
        
        layout.separator()
        
        # ===== ANALYSIS SECTION =====
        box = layout.box()
        box.label(text="Scene Analysis", icon='INFO')
        
        box.operator("vrc.analyze", text="Analyze Scene", icon='VIEWZOOM')
        
        if props.scene_triangles > 0:
            col = box.column(align=True)
            
            # Triangle status
            tris = props.scene_triangles
            tris_status = "✓" if tris <= 70000 else "⚠" if tris <= 100000 else "✗"
            col.label(text=f"{tris_status} Triangles: {tris:,} / 70,000")
            
            # Bone status
            bones = props.scene_bones
            bone_status = "✓" if bones <= 400 else "⚠" if bones <= 500 else "✗"
            col.label(text=f"{bone_status} Bones: {bones} / 400")
            
            # Mesh status
            col.label(text=f"Meshes: {props.scene_meshes}")
            col.label(text=f"Materials: {props.scene_materials}")
        
        layout.separator()
        
        # ===== OPTIMIZATION SECTION =====
        box = layout.box()
        box.label(text="Optimization Tools", icon='MODIFIER')
        
        # Decimate
        col = box.column(align=True)
        col.prop(props, "decimate_ratio", slider=True)
        col.operator("vrc.decimate_mesh", text="Decimate Selected", icon='MOD_DECIM')
        
        box.separator()
        
        # Bones
        box.operator("vrc.remove_unused_bones", text="Remove Unused Bones", icon='BONE_DATA')
        
        box.separator()
        
        # Textures
        col = box.column(align=True)
        col.prop(props, "max_texture_size")
        col.operator("vrc.resize_textures", text="Resize Textures", icon='TEXTURE')


# ============================================================================
# REGISTRATION
# ============================================================================

classes = (
    VRC_OptimizerProperties,
    VRC_OT_folder_watch,
    VRC_OT_stop_watch,
    VRC_OT_load_bridge,
    VRC_OT_apply_bridge_selection,
    VRC_OT_delete_unselected,
    VRC_OT_analyze,
    VRC_OT_decimate_mesh,
    VRC_OT_remove_unused_bones,
    VRC_OT_resize_textures,
    VRC_OT_open_bridge_folder,
    VRC_PT_optimizer_panel,
)


def register():
    for cls in classes:
        bpy.utils.register_class(cls)
    
    bpy.types.Scene.vrc_optimizer = bpy.props.PointerProperty(type=VRC_OptimizerProperties)
    print("[VRC Optimizer] Registered successfully")


def unregister():
    for cls in reversed(classes):
        bpy.utils.unregister_class(cls)
    
    del bpy.types.Scene.vrc_optimizer
    print("[VRC Optimizer] Unregistered")


if __name__ == "__main__":
    register()
