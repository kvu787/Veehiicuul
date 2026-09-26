@tool
extends EditorScript
## Open this script in Godot's Script editor and use File > Run (Ctrl+Shift+X).
## Overwrites all project .tres StandardMaterial3D files with defaults plus the settings below.


func _run() -> void:
    var filesystem := EditorInterface.get_resource_filesystem()
    if filesystem.is_scanning():
        push_warning("Wait for the FileSystem scan to finish, then run this script again.")
        return

    var paths: Array[String] = []
    _collect_material_paths(filesystem.get_filesystem(), paths)
    var saved_count := 0

    for path in paths:
        var material := ResourceLoader.load(path) as StandardMaterial3D
        if material == null:
            push_error("Could not load StandardMaterial3D: %s" % path)
            continue

        _reset_material(material)
        var error := ResourceSaver.save(material, path)
        if error != OK:
            push_error("Could not save %s: %s" % [path, error_string(error)])
            continue

        material.emit_changed()
        filesystem.update_file(path)
        saved_count += 1
        print("Reset material: %s" % path)

    print("Reset %d of %d StandardMaterial3D .tres files." % [saved_count, paths.size()])


func _collect_material_paths(directory: EditorFileSystemDirectory, paths: Array[String]) -> void:
    for index in range(directory.get_file_count()):
        var path := directory.get_file_path(index)
        if path.get_extension().to_lower() == "tres" and directory.get_file_type(index) == "StandardMaterial3D":
            paths.append(path)

    for index in range(directory.get_subdir_count()):
        _collect_material_paths(directory.get_subdir(index), paths)


func _reset_material(material: StandardMaterial3D) -> void:
    var defaults := StandardMaterial3D.new()
    # ClassDB includes properties hidden by the current material settings.
    # Only copy stored properties, preserving the existing resource path and references.
    for property in ClassDB.class_get_property_list("StandardMaterial3D"):
        if property.usage & PROPERTY_USAGE_STORAGE:
            var default_value: Variant = defaults.get(property.name)
            if material.get(property.name) != default_value:
                material.set(property.name, default_value)

    for metadata_name in material.get_meta_list():
        material.remove_meta(metadata_name)

    material.diffuse_mode = 1
    material.specular_mode = 2
    material.disable_fog = true
    material.disable_specular_occlusion = true
    material.albedo_color = Color(0.15155911, 0.5057727, 0.8741714, 1)
    material.metallic_specular = 0.0
    material.disable_receive_shadows = true
