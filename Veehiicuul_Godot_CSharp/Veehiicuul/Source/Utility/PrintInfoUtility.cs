using Godot;

namespace Veehiicuul_Godot_CSharp;

public static class PrintInfoUtility {
    public static void PrintDisplayInfo(Viewport viewport) {
        int screen = DisplayServer.WindowGetCurrentScreen();
        GD.Print($"Display resolution = {DisplayServer.ScreenGetSize(screen)}");
        GD.Print($"Display refresh rate = {DisplayServer.ScreenGetRefreshRate(screen)} Hz");
        GD.Print($"Window resolution = {DisplayServer.WindowGetSize()}");
        GD.Print($"Viewport resolution = {viewport.GetVisibleRect().Size}");
        GD.Print($"Render scale = {viewport.Scaling3DScale}");
        GD.Print($"Window mode = {DisplayServer.WindowGetMode()}");
    }

    public static void PrintGraphicsInfo() {
        GD.Print($"Renderer = {RenderingServer.GetCurrentRenderingMethod()}");
        GD.Print($"Graphics driver = {RenderingServer.GetCurrentRenderingDriverName()}");
        GD.Print($"Graphics device = {RenderingServer.GetVideoAdapterName()}");
        GD.Print($"Graphics vendor = {RenderingServer.GetVideoAdapterVendor()}");
        GD.Print($"Graphics API version = {RenderingServer.GetVideoAdapterApiVersion()}");
        GD.Print($"Render thread model setting = {ProjectSettings.GetSetting("rendering/driver/threads/thread_model")}");
        GD.Print($"Maximum FPS = {Engine.MaxFps}");
        GD.Print($"VSync mode = {DisplayServer.WindowGetVsyncMode()}");
    }
}
