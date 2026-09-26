@tool
extends EditorScenePostImport
## Disables specular on embedded mesh materials before an imported scene is saved.


func _post_import(scene: Node) -> Object:
    _disable_specular(scene)
    return scene


func _disable_specular(node: Node) -> void:
    var mesh_instance := node as MeshInstance3D
    if mesh_instance != null and mesh_instance.mesh != null:
        for surface in range(mesh_instance.mesh.get_surface_count()):
            var material := mesh_instance.get_active_material(surface)
            if material is StandardMaterial3D:
                material.diffuse_mode = BaseMaterial3D.DIFFUSE_LAMBERT
                material.specular_mode = BaseMaterial3D.SPECULAR_DISABLED
                material.disable_fog = true
                material.disable_specular_occlusion = true
                material.metallic_specular = 0.0
                material.disable_receive_shadows = true

    for child in node.get_children():
        _disable_specular(child)
