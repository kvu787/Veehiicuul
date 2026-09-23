def meow():
    OUTPUT = "//export.glb"

    output = Path(bpy.path.abspath(OUTPUT))
    output.parent.mkdir(parents=True, exist_ok=True)

    result = bpy.ops.export_scene.gltf(
        filepath=str(output),
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

    print(f"Exported: {output}")
