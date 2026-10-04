extends SceneTree


func _initialize() -> void:
	call_deferred("capture_views")


func capture_views() -> void:
	var scene := load("res://Main.tscn") as PackedScene
	var main := scene.instantiate()
	root.add_child(main)
	if not main.initialized:
		quit(1)
		return
	var output_directory := ""
	for argument: String in OS.get_cmdline_user_args():
		if argument.begins_with("--capture-directory="):
			output_directory = argument.trim_prefix("--capture-directory=")
	if output_directory.is_empty():
		printerr("Pass --capture-directory=<existing directory>.")
		quit(1)
		return
	for frame in range(30):
		await RenderingServer.frame_post_draw
	var result := root.get_texture().get_image().save_png(output_directory.path_join("FixedCamera.png"))
	if result != OK:
		quit(1)
		return
	main.camera.follows_car = true
	main.camera.size = main.camera.follow_size
	main.camera.apply_to(main.session, main.vehicle)
	for frame in range(10):
		await RenderingServer.frame_post_draw
	result = root.get_texture().get_image().save_png(output_directory.path_join("FollowCamera.png"))
	print("Visual capture result: ", error_string(result))
	quit(0 if result == OK else 1)
