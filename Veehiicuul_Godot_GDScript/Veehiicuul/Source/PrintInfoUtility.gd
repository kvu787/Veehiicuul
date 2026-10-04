class_name PrintInfoUtility
extends RefCounted


static func print_information(viewport: Viewport) -> void:
	var screen := DisplayServer.window_get_current_screen()
	print("Display resolution = ", DisplayServer.screen_get_size(screen))
	print("Display refresh rate = ", DisplayServer.screen_get_refresh_rate(screen), " Hz")
	print("Window resolution = ", DisplayServer.window_get_size())
	print("Viewport resolution = ", viewport.get_visible_rect().size)
	print("Render scale = ", viewport.scaling_3d_scale)
	print("Window mode = ", DisplayServer.window_get_mode())
	print("Renderer = ", RenderingServer.get_current_rendering_method())
	print("Graphics driver = ", RenderingServer.get_current_rendering_driver_name())
	print("Graphics device = ", RenderingServer.get_video_adapter_name())
	print("Graphics vendor = ", RenderingServer.get_video_adapter_vendor())
	print("Graphics API version = ", RenderingServer.get_video_adapter_api_version())
	print("Render thread model setting = ", ProjectSettings.get_setting("rendering/driver/threads/thread_model"))
	print("Maximum FPS = ", Engine.max_fps)
	print("VSync mode = ", DisplayServer.window_get_vsync_mode())
	print("Scripting = GDScript; .NET runtime = absent")
