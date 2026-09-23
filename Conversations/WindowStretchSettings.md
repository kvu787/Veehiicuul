# Window stretch settings

## User

```
Lines and columns are 1-based. Columns are StartInclusive:EndExclusive. Columns count UTF-16 code units.
c:\Users\k\Repository\Veehiicuul\InputLatency_Godot\project.godot:23:1-25:35
```
```
window/size/viewport_width=1780
window/size/viewport_height=720
window/stretch/mode="canvas_items"
```
```
Lines and columns are 1-based. Columns are StartInclusive:EndExclusive. Columns count UTF-16 code units.
c:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp\project.godot:20:1-21:31
```
```
window/stretch/mode="canvas_items"
window/stretch/aspect="expand"
```

explain the difference between these setups

## Assistant

I’ll check both projects for the surrounding window settings and verify how Godot applies them.

## Assistant

Both use **`canvas_items`**, so 2D content and UI scale with the window while being rendered at the actual output resolution. The base dimensions are a design reference, not a fixed rendering resolution. [Godot’s stretch-mode documentation](https://docs.godotengine.org/en/4.5/tutorials/rendering/multiple_resolutions.html)

The differences are **the base size** and **what happens when the window’s aspect ratio changes**:

| Setting              | InputLatency       | Veehiicuul          |
| -------------------- | ------------------ | ------------------- |
| Base size            | Explicit: 1780×720 | Default: 1152×648    |
| Base aspect ratio    | About 2.47:1       | 16:9                |
| Stretch aspect       | Default: `keep`    | Explicit: `expand`  |

Omitted settings still have values: Godot defaults to 1152×648 and `keep`. [Godot’s project-setting defaults](https://docs.godotengine.org/en/stable/classes/class_projectsettings.html)

- **InputLatency — `keep`:** Preserves the 1780×720 logical content area and scales it uniformly. If the window has different proportions, Godot adds black bars. In a 1920×1080 window, the content occupies approximately 1920×777, with bars above and below.
- **Veehiicuul — `expand`:** Scales uniformly but expands the logical content area as needed to fill the window without black bars. Wider windows reveal extra horizontal space; taller windows reveal extra vertical space. UI anchors and containers determine how controls use that space. [Godot’s aspect-ratio documentation](https://docs.godotengine.org/en/4.5/tutorials/rendering/multiple_resolutions.html)

For example, **if both used a 1780×720 base** and ran in a 1920×1080 window, `keep` would retain the 1780×720 logical area with bars; `expand` would provide roughly 1780×1001 logical units without bars.

One project-specific detail: [InputLatency’s startup code](C:/Users/k/Repository/Veehiicuul/InputLatency_Godot/Source/InputLatency.cs:85) adjusts the actual window size for monitor DPI and available desktop space. So 1780×720 is its design size, but its initial physical window can be larger or smaller.
