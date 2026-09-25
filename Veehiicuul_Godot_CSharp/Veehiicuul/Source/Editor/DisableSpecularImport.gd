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
            if material is BaseMaterial3D:
                material.metallic_specular = 0.0

    for child in node.get_children():
        _disable_specular(child)
