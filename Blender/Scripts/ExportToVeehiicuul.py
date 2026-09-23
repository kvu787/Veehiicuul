# This is meant to be run from a script panel from Blender opened on the .blend file you want to export.

import json
import math
from pathlib import Path

import bpy


REQUIRED_EXCLUDED_COLLECTION_PATHS = (
    "Collection/Camera",
    "Collection/Templates",
    "Collection/TrackBuilder/Input/Outlines",
    "Collection/TrackBuilder/Output/OutlineMeshes",
)


def _require_collections_excluded_from_view_layer() -> None:
    def find_collection(
        layer_collection: bpy.types.LayerCollection,
        path_parts: list[str],
        collection_path: str,
    ) -> bpy.types.LayerCollection:
        if not path_parts:
            return layer_collection

        child = layer_collection.children.get(path_parts[0])
        if child is None:
            raise RuntimeError(f"Collection '{collection_path}' does not exist")

        return find_collection(child, path_parts[1:], collection_path)

    for collection_path in REQUIRED_EXCLUDED_COLLECTION_PATHS:
        collection = find_collection(
            bpy.context.view_layer.layer_collection,
            collection_path.split("/"),
            collection_path,
        )
        if not collection.exclude:
            raise RuntimeError(
                f"Collection '{collection_path}' must be excluded from view "
                f"layer '{bpy.context.view_layer.name}' before exporting"
            )


def _direct_child_collection(
    parent: bpy.types.Collection,
    name: str,
) -> bpy.types.Collection:
    collection = parent.children.get(name)
    if collection is None:
        raise RuntimeError(
            f"Required collection '{parent.name}/{name}' does not exist"
        )
    return collection


def _validated_world_xy_vertices(obj: bpy.types.Object) -> list[dict[str, float]]:
    if obj.type != "MESH":
        raise RuntimeError(f"Outline object '{obj.name}' is not a mesh")

    mesh = obj.data
    vertex_count = len(mesh.vertices)
    if vertex_count < 3:
        raise RuntimeError(
            f"Outline mesh '{obj.name}' must contain at least three vertices"
        )
    if len(mesh.polygons) != 0:
        raise RuntimeError(
            f"Outline mesh '{obj.name}' must be an edge-only mesh with no faces"
        )

    expected_edges = {
        tuple(sorted((vertex_index, (vertex_index + 1) % vertex_count)))
        for vertex_index in range(vertex_count)
    }
    actual_edge_list = [
        tuple(sorted((int(edge.vertices[0]), int(edge.vertices[1]))))
        for edge in mesh.edges
    ]
    actual_edges = set(actual_edge_list)
    if len(actual_edge_list) != vertex_count or actual_edges != expected_edges:
        raise RuntimeError(
            f"Outline mesh '{obj.name}' must be one closed loop whose edges follow "
            "the mesh vertex order"
        )

    world_positions = [obj.matrix_world @ vertex.co for vertex in mesh.vertices]
    for vertex_index, position in enumerate(world_positions):
        if not all(math.isfinite(float(component)) for component in position):
            raise RuntimeError(
                f"Outline mesh '{obj.name}' vertex {vertex_index} has a non-finite "
                "world-space coordinate"
            )

    plane_z = float(world_positions[0].z)
    for vertex_index, position in enumerate(world_positions[1:], start=1):
        if float(position.z) != plane_z:
            raise RuntimeError(
                f"Outline mesh '{obj.name}' is not planar in Blender world XY; "
                f"vertex 0 has Z={plane_z!r} and vertex {vertex_index} has "
                f"Z={float(position.z)!r}"
            )

    vertices = []
    for vertex_index, position in enumerate(world_positions):
        next_position = world_positions[(vertex_index + 1) % vertex_count]
        x = float(position.x)
        y = float(position.y)
        next_x = float(next_position.x)
        next_y = float(next_position.y)
        if x == next_x and y == next_y:
            raise RuntimeError(
                f"Outline mesh '{obj.name}' edge {vertex_index} has zero length "
                "in Blender world XY"
            )
        vertices.append({"X": x, "Y": y})

    return vertices


def export_collider_data(filepath: Path) -> None:
    track_builder = bpy.data.collections.get("TrackBuilder")
    if track_builder is None:
        raise RuntimeError("Required collection 'TrackBuilder' does not exist")

    output = _direct_child_collection(track_builder, "Output")
    outline_meshes = _direct_child_collection(output, "OutlineMeshes")

    outer_outlines = []
    inner_outlines = []
    for obj in outline_meshes.objects:
        role = obj.get("track_builder_role")
        if role == "outer_outline":
            outer_outlines.append(obj)
        elif role == "inner_outline":
            inner_outlines.append(obj)
        else:
            raise RuntimeError(
                f"Object '{obj.name}' in '{outline_meshes.name}' has unexpected "
                f"track_builder_role {role!r}"
            )

    if len(outer_outlines) != 1:
        raise RuntimeError(
            f"Expected one outer outline in '{outline_meshes.name}', "
            f"found {len(outer_outlines)}"
        )

    outline_objects = outer_outlines + sorted(inner_outlines, key=lambda obj: obj.name)
    outlines = []
    for obj in outline_objects:
        outlines.append({"Vertices": _validated_world_xy_vertices(obj)})

    collider_data = {
        "Outlines": outlines,
    }
    filepath.parent.mkdir(parents=True, exist_ok=True)
    with filepath.open("w", encoding="utf-8", newline="\n") as output_file:
        json.dump(collider_data, output_file, indent=4, allow_nan=False)
        output_file.write("\n")

    print(f"Exported collider data: {filepath}")


def main() -> None:
    _require_collections_excluded_from_view_layer()

    track_name = Path(bpy.data.filepath).stem
    folder_path = Path("C:/") / "Users" / "k" / "Repository" / "Veehiicuul" / "Veehiicuul_Godot_CSharp" / "Testyo"
    export_collider_data(folder_path / f"{track_name}_ColliderData.json")

    # filepath = Path("C:/") / "Users" / "k" / "Repository" / "Veehiicuul" / "Veehiicuul_Godot_CSharp" / f"{track_name}.fbx"
    # filepath.parent.mkdir(parents=True, exist_ok=True)

    # OUTPUT = "//export.glb"

    # output = Path(bpy.path.abspath(OUTPUT))
    # output.parent.mkdir(parents=True, exist_ok=True)

    result = bpy.ops.export_scene.gltf(
        filepath=str(folder_path / f"{track_name}.glb"),
        export_format="GLB",
        export_copyright="",
        will_save_settings=False,

        # Include
        use_selection=False,
        use_visible=True,
        use_renderable=False,
        use_active_collection=False,
        use_active_scene=False,
        collection="",
        export_extras=False,
        export_cameras=False,
        export_lights=False,

        # Transform
        export_yup=True,

        # Scene Graph
        export_gn_mesh=False,
        export_gpu_instances=False,
        export_hierarchy_flatten_objs=False,
        export_hierarchy_full_collections=False,

        # Mesh
        export_apply=True,
        export_texcoords=True,
        export_normals=True,
        export_tangents=False,
        export_attributes=False,
        use_mesh_edges=False,
        use_mesh_vertices=False,
        export_shared_accessors=False,

        # Vertex Colors
        export_vertex_color="MATERIAL",
        export_all_vertex_colors=True,
        export_active_vertex_color_when_no_material=True,

        # Material
        export_materials="EXPORT",
        export_image_format="NONE",
        export_image_add_webp=False,
        export_image_webp_fallback=False,
        export_unused_images=False,
        export_unused_textures=False,

        # Shape Keys / Skinning
        export_morph=False,
        export_skins=False,

        # Lighting: Standard
        export_import_convert_lighting_mode="SPEC",

        # Compression
        export_draco_mesh_compression_enable=False,
        export_use_gltfpack=False,

        # Animation
        export_animations=False,
        export_current_frame=False,
    )

    if result != {"FINISHED"}:
        raise RuntimeError(f"glTF export failed: {result}")


if __name__ == "__main__":
    main()
