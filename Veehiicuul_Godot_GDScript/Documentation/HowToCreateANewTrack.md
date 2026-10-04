# Blender-side steps

- Update scripts in TrackTemplate
  - Copy/paste the latest TrackBuilder.py
  - Update validate and export scripts
- Copy/paste TrackTemplate.blend to a new file in `Blender/Tracks`
- In the Outliner, exclude these collections:
  - ColorBlocks
  - Templates
- After finishing your input track outlines, run TrackBuilder
- Adjust vehicle road, placeholder car, and checkered line objects as desired
- Add decorative objects as desired
  - Don't modify anything in the `*_Defaults` collections other than moving their positions
  - Instead, duplicate (not copy) them into your track
  - When you're done, exclude the `*_Defaults` collections
- Before exporting, do this:
  - Purge unused data
  - Exclude these collections:
    - TrackBuilder/Input
    - `*_Defaults`
  - Run ValidateTrackScene.py and fix any issues
- To export, run ExportToVeehiicuul.py
  - This should create a new folder with the track name that contains a GLB model file and a JSON file with collision data

# Godot-side steps

- Open the import settings for the GLB and set this import script: `Source\Editor\DisableSpecularImport.gd`
- Reimport
- Create a new scene with the same name as the track
  - Use the name `TrackName_Scene`
- Copy/paste an existing track or its nodes to get these nodes:
  - CameraPanAndYaw
  - Sunlight
  - WorldEnvironment
- Drag/drop GLB into the track scene
  - Name its root node `Model`
  - Keep `Model` at an exact identity local transform: zero position and rotation, unit scale
  - `TrackSession` reports an error during initialization if this requirement is violated
  - Imported cars face local +Z; their right side is local -X
- Adjust these camera settings:
  - Pan:
    - CameraPanAndYaw.Position.XZ
  - Yaw:
    - CameraPanAndYaw.Rotation.Y
  - Pitch:
    - CameraPanOffsetAndPitch.Rotation.X
    - This should probably remain -PI/4 radians (-45 degrees in the Godot Inspector)
  - Zoom:
    - Camera.Size
  - Clip:
    - Camera.Near
    - Camera.Far
- Update GDScript:
  - Source/Main.gd: TRACK_NAMES
  - Source/Main.gd: INITIAL_TRACK_INDEX

# Godot GLB import settings

Copy/paste this to the *.glb.import file:

```
[params]

nodes/root_type=""
nodes/root_name=""
nodes/root_script=null
mesh_library/use_node_names_as_mesh_names=false
array_mesh/deduplicate_surfaces=true
nodes/apply_root_scale=true
nodes/root_scale=1.0
nodes/import_as_skeleton_bones=false
nodes/use_name_suffixes=true
nodes/use_node_type_suffixes=true
meshes/ensure_tangents=true
meshes/generate_lods=false
meshes/create_shadow_meshes=false
meshes/light_baking=0
meshes/lightmap_texel_size=0.2
meshes/force_disable_compression=true
skins/use_named_skins=true
animation/import=false
animation/fps=30
animation/trimming=false
animation/remove_immutable_tracks=true
animation/import_rest_as_RESET=false
import_script/path="res://Source/Editor/DisableSpecularImport.gd"
materials/extract=0
materials/extract_format=0
materials/extract_path=""
_subresources={}
gltf/naming_version=2
gltf/embedded_image_handling=1
gltf/texture_map_mode=1
```

# Things to check in the track Blender file

- Ensure required objects are present
- Ensure objects have the correct prefixes
- Ensure objects are in the correct collections
- Ensure all collision objects have geometry-centered origins:
  - Barriers
  - Cars
  - Checkpoints
  - Cones
- Ensure all objects have materials assigned
- Check material slots
- Run validation python script
- Select all and check that face normals are correct
- File > Clean Up > Purge Unused Data
