**Godot 4.7.2 .NET persistence on this Windows 11 computer**

This audit uses the actual source at `C:\Users\k\Repository\External\Godot_4-7-2`, tag `4.7.2-stable`, commit `ed1daf0bf001b61586d9930840f2f1394092c079`. The source worktree was clean. Local source links below identify the implementation being discussed, not documentation for another Godot version.

The central finding is that **self-contained editor mode redirects the editor's data, configuration, cache, and editor-temporary directories. It does not confine the editor, exported applications, .NET tooling, or Windows to the portable installation folder.** Project caches, runtime user data, generic temporary files, some .NET export staging, and an exported application's registry entry have separate routing. [Editor path routing][S01], [runtime user-data routing][S03], [.NET export staging][S60], [Windows registry writer][S70].

The accompanying [machine inventory](C:/Users/k/Repository/Veehiicuul/Documentation/GodotPersistenceInventory.json) records individual files, empty directories, sizes, and the fresh-download comparison. Its principal filesystem snapshot was captured on September 25, 2026 at 18:50:31 PDT; related-path checks followed during the same audit. It contains metadata, not the contents of settings, logs, credentials, or crash dumps.

This was a source review and a read-only inspection of the existing installation, baseline, caches, and registry. I did not launch Godot or build the project to provoke new writes. “Observed” below means present on disk; it does not prove which historical process created a file. “Conditional” means the source has a writer for that feature, whether or not its output currently exists.

The scope is Windows 11 x64 development and Windows exports. Cross-platform export implementations are not presented as normal Windows-project activity. Project source already committed to Git and unchanged files supplied in the fresh editor/template downloads are excluded from the unknown-data inventory.

**What is present now**

All **109 installed files having counterparts in the supplied editor download or decompressed export-template archive matched by SHA-256**. No matching stock file had changed. The portable installation has 357 additional files: 355 beneath `editor_data`, plus the empty `_sc_` marker and `Godot 4.7.2 .NET.lnk`. Presence of the shortcut does not establish that Godot created it.

| Location or category                   | Observed files | Observed bytes | Interpretation                                   |
| -------------------------------------- | -------------- | -------------- | ------------------------------------------------ |
| Project .godot                         | 403            | 99,655,212     | Generated data plus editor state; detailed below |
| editor_data, excluding stock templates | 355            | 6,178,326      | Settings, caches, project list, C# build logs    |
| Roaming\\Godot                         | 67             | 1,812,084      | All files are Veehiicuul D3D12 shader caches     |
| Local\\Godot                           | 0              | 0              | Directory absent                                 |
| Local\\Temp\\Godot                     | 0              | 0              | Directory absent                                 |
| Local\\Temp\\godot-publish-dotnet      | 0              | 0              | Empty parent directory exists                    |
| Matching Local\\CrashDumps files       | 7              | 107,872,106    | Godot/editor/application-named diagnostic dumps  |

The full `editor_data` tree occupies 2,001,063,455 bytes, but **1,994,885,129 bytes are the 27 unchanged export-template files you excluded**. Thus the new editor data is about 5.89 MiB, not about 1.86 GiB. The project's `.godot` is about 95.04 MiB; its C# build tree accounts for 94,192,210 bytes.

The Roaming tree also has empty directories for `app_userdata\[unnamed project]`, and for Veehiicuul's `objectdb_snapshots` and `vulkan`. Empty directories are themselves persistent filesystem state, so the inventory includes them.

**The path rules explain the five directories you listed**

For the remainder of this report:

- `Install` means `C:\Users\k\Program\Godot_v4.7.2-stable_mono_win64`.
- `Project` means `C:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp\Veehiicuul`.
- `EditorData` means `Install\editor_data`.
- `UserData` means `C:\Users\k\AppData\Roaming\Godot\app_userdata\Veehiicuul_Godot_CSharp`.
- `Temp` means the operating-system temporary directory, normally `C:\Users\k\AppData\Local\Temp` for this account.

The editor checks for `._sc_` first, then `_sc_`, next to its executable. With the marker present, its data and configuration roots both become `EditorData`; its cache is `EditorData\cache`; its editor-temporary directory is `EditorData\temp`. These are fixed choices in [EditorPaths][S01]. The marker is a mode selector, not a filesystem sandbox.

Without the marker, Windows editor data/configuration go to `%APPDATA%\Godot`, editor cache normally goes to `%LOCALAPPDATA%\Godot`, and editor temporary storage uses the OS temporary directory **directly**. Windows obtains these roots from `APPDATA`, `LOCALAPPDATA`, and `GetTempPathW`, with fallback behavior in the source. The absence of `Local\Godot` is therefore expected for this portable editor. [EditorPaths][S01], [Windows path implementation][S02].

**There is no universal `%TEMP%\Godot` root in this implementation.** Its absence does not mean no temporary files have been written. Editor temporary operations use `EditorData\temp`; generic Godot temporary APIs and several managed-code paths use the OS temp root independently. [Generic file temp][S65], [generic directory temp][S66], [.NET publish temp][S60].

`Project\.godot` is tied to the project, independently of the portable marker. The setting `application/config/use_hidden_project_data_directory=false` changes the general project-data directory to `Project\godot`. The default is true. Some .NET and export paths explicitly contain `.godot`, so this setting is not a universal relocation mechanism for every writer. [Directory selection][S04], [defaults][S92], [C# SDK paths][S42], [export credentials][S39].

`user://` normally resolves to `%APPDATA%\Godot\app_userdata\<safe application name>`. With `application/config/use_custom_user_dir=true`, it instead uses `%APPDATA%\<custom user directory name>`, falling back to the application name if the custom name is empty. An empty application name uses `[unnamed project]`. This code does not consult the portable marker. Consequently, an editor and an exported application for the same project can use the same `UserData`, and projects sharing an application name can collide there. [Common resolution][S03], [Windows prefix][S02].

Your committed [project.godot](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/project.godot:13) names the application `Veehiicuul_Godot_CSharp` and does not override these directory defaults.

**Inside the project's .godot directory**

These names describe families of files. Asset names, path hashes, configurations, shader variants, and installed plugins make the complete set of individual filenames dependent on the project.

- **`imported\*`: converted assets and checksums.** Godot constructs a basename from the source asset's filename and a hash of its resource path. Importers add their own output extensions, such as texture or scene formats, and Godot writes `.md5` metadata for source/destination validation. Your tree contains four files, totaling 1,410,394 bytes. Reimporting recreates these from source assets and import settings. Blend and FBX2glTF imports can also place intermediary conversion output here. [Naming][S24], [import/checksum writer][S25], [Blender][S86], [FBX2glTF][S87].
- **`uid_cache.bin`: resource UID-to-path lookup cache.** This differs from the individual source-adjacent `.uid` files. Regeneration depends on retaining the source resources and their identifiers. [Cache path][S26], [sidecar generation][S93].
- **`global_script_class_cache.cfg`, `scene_groups_cache.cfg`, and conditional `extension_list.cfg`: project indexes.** They record global script classes, scene-group data, and discovered GDExtensions respectively. The first two exist here; no extension-list file was observed. [Script classes][S27], [groups][S28], [extensions][S29].
- **`.gdignore`: excludes the generated-data directory from resource scanning.** Godot creates it if absent. [Constructor writer][S01].
- **`editor\filesystem_cache10` and conditional `filesystem_update4`: filesystem/import indexes.** They speed scanning and communicate incremental changes. The version-looking suffixes are literal names in this source, not user-selected versions. [Filesystem-cache writer][S22], [update journal][S23].
- **`editor\editor_layout.cfg` and `project_metadata.cfg`: project-local editor state.** These retain editor layout/session information and arbitrary named project metadata used by editor components. Clearing them resets state even though source assets remain intact. [Layout][S31], [metadata][S32].
- **`editor\<resource>-editstate-<hash>.cfg` and `<resource>-folding-<hash>.cfg`: scene-editing and inspector-folding state.** Resource names and hashes distinguish scenes/resources. These are not saved game state. [Scene editing][S30], [folding][S33].
- **`editor\script_editor_cache.cfg`, `editor_script_doc_cache.res`, `quick_open_dialog_cache.cfg`: script-editor session state, generated script documentation, and quick-open data.** The first two exist here. [Script state][S34], [documentation][S15], [quick-open][S35].
- **`editor\favorites`, `favorite_properties`, `recent_dirs`, `favorites.<base type>`, `create_recent.<base type>`: selections and history.** For example, node creation uses a base-type-specific file; file-dialog directory history has its own files. These are conditional on usage. [Favorites/history][S09], [creation history][S36].
- **`editor\lib_folding.cfg` and `used_class_cache`: feature-specific state.** The former stores animation-library folding; the latter supports build-profile class detection. [Animation state][S37], [class cache][S38].
- **`export_credentials.cfg`: export secrets, when configured.** The export subsystem separates secret options from `export_presets.cfg` and saves them in this file. Examples can include signing identity/password fields. It was absent in your snapshot. It is **not a reconstructible cache**; removing it loses stored credentials unless separately retained. [Credential split and save][S39], [Windows secret export options](C:/Users/k/Repository/External/Godot_4-7-2/platform/windows/export/export_plugin.cpp:464).
- **`exported\<customization hash>\export-<resource hash>-<name>` and `file_cache`: export conversions.** These cache transformed/binary resources so later exports can reuse them. Shader baking can add `shader_baker\<platform>\<driver>\...` and its own `file_cache` beneath the export cache. Your current export cache contains six files totaling 6,568 bytes. [Export resource cache][S40], [shader baking][S41].
- **`mono\temp\bin\<configuration>\...` and `mono\temp\obj\...`: C# build products and intermediates.** Godot's SDK relocates the usual `bin`/`obj` roots here. These include project/dependency assemblies, optional symbols/XML documentation, runtime/dependency JSON, NuGet asset/restore metadata, generated code, and MSBuild bookkeeping as selected by the SDK and project. Configurations include `Debug`, `ExportDebug`, and `ExportRelease`; framework/runtime-specific descendants depend on the build. Intermediate-path properties can override the defaults. [SDK defaults][S42], [native assembly lookup][S18].
- **`mono\metadata\ide_messaging_meta.txt`: live IDE connection metadata.** The editor writes its loopback port and executable path for IDE messaging, then deletes the file when that messaging server is disposed. A crash can leave it behind. The directory exists here but was empty. [Filename][S97], [writer and cleanup][S43].
- **`shader_cache\...`: the project's editor-rendering shader cache.** Its placement and difference from the runtime cache are described next. Your project copy has 86 files totaling 4,027,412 bytes. [Editor root selection][S01], [renderer root selection][S44].

The current `editor` subdirectory has 54 files totaling 15,844 bytes. This explains why “delete `.godot` to rebuild caches” also discards editing history, folding, layouts, and any export credentials present.

**Shader caches exist at different roots for different processes**

For the Project Manager, the shader-cache base is the shared editor data directory. For a project opened in the editor, it is the project data directory. For a running game, the renderer falls back to `user://` when no editor shader-cache base has been supplied. Each adds `shader_cache`. [Editor selection][S01], [rendering-device selection][S44], [OpenGL selection][S48].

For your installation this means:

| Process/context         | Shader-cache location              |
| ----------------------- | ---------------------------------- |
| Project Manager         | Install\\editor_data\\shader_cache |
| Project editor          | Project\\.godot\\shader_cache      |
| Running Veehiicuul game | UserData\\shader_cache             |

RenderingDevice shader files follow `<shader name>\<SHA-256 group>\<SHA-1 variant>.<graphics API>.cache`. Your Roaming cache files end in `.d3d12.cache`. The OpenGL implementation uses its own `.cache` filename scheme; EGL blob caching, when available, adds a `shader_cache\EGL` family. These are regenerable compiled-shader data. Old variants can coexist; the filename is not a timestamped application log. [RD naming][S45], [RD save](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/shader_rd.cpp:692), [GLES save](C:/Users/k/Repository/External/Godot_4-7-2/drivers/gles3/shader_gles3.cpp:644), [EGL][S49].

**A Vulkan pipeline cache is a separate mechanism.** The shared RenderingDevice code forms `user://vulkan/pipelines.<rendering method>.<normalized device name>[.editor].cache`, loads it, and asks the driver to support pipeline caching. The D3D12 driver's `pipeline_cache_create` returns false in this version. Therefore your D3D12 configuration does not persist that Godot pipeline-cache file. However, the shared load routine creates the `vulkan` parent directory before the driver returns false, explaining an empty `UserData\vulkan` directory even with D3D12. [Shared implementation][S46], [D3D12 implementation][S47].

The editor forces its shader cache on even if the project's `rendering/shader_compiler/shader_cache/enabled` setting is false. That project setting controls the game-side behavior; it is not a complete editor-cache off switch. [Renderer condition][S44].

**Inside editor_data, excluding the original templates**

- **`editor_settings-4.7.tres`: shared editor preferences and shortcuts.** The filename includes major/minor, not patch version. Godot can read an older minor-version settings file if needed, then save using the newest filename. It currently contains 18,222 bytes. [Filename/migration selection][S07], [save](C:/Users/k/Repository/External/Godot_4-7-2/editor/settings/editor_settings.cpp:1515).
- **`projects.cfg`: Project Manager's registered-project data.** It records project paths and associated Project Manager state. It is independent of the projects' own Git repositories. [Path][S08].
- **`favorite_dirs`, `recent_dirs`, and `editor_layouts.cfg`: shared directory preferences/history and named editor layouts.** Project-local counterparts live in `.godot\editor`; the context determines which is used. `recent_dirs` exists here. [History][S09], [layouts][S10].
- **`text_editor_themes\*.tet`, `script_templates\*`, `feature_profiles\*.profile`: customization.** These can contain user-created/imported material, not just regenerable data. The directories are created even when empty. [Directory setup][S01], [theme import/save][S11], [feature profiles][S12], [script-template lookup](C:/Users/k/Repository/External/Godot_4-7-2/editor/settings/editor_settings.cpp:1953).
- **`cache\resthumb-<hash>.png`, `..._small.png`, `...txt`: thumbnails and validation/preview metadata.** The filename hash uses the absolute resource path. Moving a project can therefore create a second family of thumbnails while old ones remain. [Writer][S13], [hash input][S14].
- **`cache\editor_doc_cache-4.7.res`: engine help/documentation cache.** Your copy is 3,267,974 bytes. Per-project script documentation is stored separately in `.godot\editor`. [Both paths][S15].
- **`cache\assetimage_<URL hash>.data`, `...etag`, and `tmp_asset_<asset id>.zip`: Asset Library image/download data.** Conditional on using the Asset Library. These names are not part of the fresh engine download. [Download path][S16], [image cache][S17].
- **`shader_cache\...`: shared Project Manager shaders**, covered above. Your files are from the GLES shader-cache family; their existence does not prove that the current Veehiicuul project uses OpenGL. [Shared root][S01], [OpenGL writer][S48].
- **`mono\build_logs\<solution-path MD5>_<configuration>\msbuild_log.txt` and `msbuild_issues.csv`: editor-driven C# build logs.** They hold build output and parsed warnings/errors. The files are opened afresh for a build, rather than forming a timestamped history. Different solution paths/configurations produce separate directories. Enabling `dotnet/build/create_binary_log` adds `msbuild.binlog`. Your eight directories contain 16 files totaling 651,571 bytes. [Root][S18], [hash naming][S19], [logger][S20], [binary-log option][S21].
- **`temp\...`: editor-operation staging**, detailed below.
- **`keystores\debug.keystore`: conditional Android-export credential.** This is a possible additional editor-data file because the Windows editor includes Android-export support. It is not used by your Windows-only workflow and was not observed. [Path][S89], [Android writer/trigger][S88].
- **`export_templates\<version>\...`: template-manager installations.** Your matching 4.7.2 .NET template contents are excluded. Installing another version or a customized template would add data. The manager also stages downloaded pieces temporarily. [Template extraction](C:/Users/k/Repository/External/Godot_4-7-2/editor/export/export_template_manager.cpp:296), [download staging][S59].

The empty `_sc_` marker is user/deployment setup for the portable mode. Godot reads it; the path-selection constructor does not generate it. Its optional configuration contents can also seed editor settings/projects. [Marker check][S01], [configuration loading](C:/Users/k/Repository/External/Godot_4-7-2/editor/settings/editor_settings.cpp:1373).

**Other data in user://, even with the portable editor**

Beyond shader caches, these built-in writers use the runtime user-data root:

- **`logs\godot.log` and rotated backups.** File logging defaults on for desktop games, but not for the editor or Project Manager. The default maximum is five files including the current log. `--log-file <path>` enables logging at that specified path even for the editor/Project Manager and sets one-file behavior, disabling rotation for that override. The project can also change the configured log path or disable logging. [Defaults/override][S05], [rotation implementation][S06].
- **`.recovery_mode_lock`.** The editor writes an initialization lock to detect crashes during startup; it is skipped in recovery mode and removed after editor initialization or cleanup. It is not a permanent lock held throughout every healthy editor session. [Writer][S50], [startup trigger](C:/Users/k/Repository/External/Godot_4-7-2/main/main.cpp:2221), [delayed removal][S51].
- **`editor_screenshot_<date/time>.png`.** The editor's screenshot action chooses `user://`, even in self-contained mode. This is an image the user requested, not a cache. [Path][S52].
- **`objectdb_snapshots\<date/time>[suffix].odb_snapshot`.** The ObjectDB profiler persists snapshots when received after a capture request. Its UI also creates/opens the containing directory. These are diagnostics worth retaining if needed. [Writer and directory][S53].
- **Arbitrary project/plugin data.** Save games, preferences, recordings, downloads, and other files created by game code or plugins have names selected by that code. Godot does not impose a fixed save filename or serialize the whole game's state automatically. The general file API routes `res://`, `user://`, and ordinary filesystem paths; it is not restricted to the five directories in the question. [File routing][S85], [Windows absolute/relative handling](C:/Users/k/Repository/External/Godot_4-7-2/drivers/windows/file_access_windows.cpp:83).

The engine also creates the user-data directory during setup even if no save file is written. [Setup call][S94]. This, and the component-specific directory creation above, explains why “no files” is not the same as “no persistence.”

**Temporary and auxiliary files outside the five listed directories**

1. **`EditorData\temp` is the portable editor's own staging area.** Concrete Windows-relevant examples are `tmpproject.binary`, `packtmp`, `_tmp.ico`, a package-named folder for ZIP export, `windows\...` for remote Windows deployment, and template-manager download ZIP pieces. Normal successful code paths remove many of these. Early failure/crash can leave partial files. This directory currently exists but is empty. [Project binary][S54], [PCK staging][S55], [icon][S57], [ZIP export][S56], [remote deployment][S58], [template download][S59].

2. **`Temp\godot-publish-dotnet\<process id>-<configuration>-<runtime identifier>\...`.** Windows .NET export publishes a full application into this directory before copying/packing the outputs. A typical suffix is `ExportRelease-win-x64`. Godot calls .NET's `Path.GetTempPath()`, so the portable editor-temp setting does not redirect it. `_ExportEnd` deletes the per-export folders; the parent can remain. Your parent exists and is empty. The same cleanup routine handles a conditional `godot-aot-<process id>` directory if present; that cleanup reference alone does not prove a normal Windows export creates it. [Staging][S60], [Windows temp selection](C:/Users/k/Repository/External/Godot_4-7-2/modules/mono/editor/GodotTools/GodotTools/Export/ExportPlugin.cs:195), [cleanup][S61].

3. **An unbranded random file directly in `Temp` during C# translation extraction.** Godot's C# translation parser has MSBuild write the project's define constants to `Path.GetTempPath() + Path.GetRandomFileName()`. It deletes that file on the successful-build branch. A failure can leave it, and its filename need not contain “Godot.” [Implementation][S62].

4. **`Temp\godot_tmp_<random integer>\...` for virtual drag-and-drop.** This handles data supplied as file descriptors/streams, such as a drag from a source that must materialize the file first. Ordinary path-based file drops take another branch. Cleanup erases the directory's contents, but the helper returns after `erase_contents_recursive()` without removing the top-level temporary directory itself. Thus even a successful virtual drop can leave an empty `godot_tmp_*` directory; interruption can leave contents. None was observed now. [Creation and cleanup helper][S63], [drop completion][S64].

5. **`Temp\export_patch_base-<time-based suffix>\...`.** The patch exporter can extract an Android `.apk`/`.aab` base archive here through the generic temporary-directory API. This is an optional cross-platform export feature available from the editor, not part of the Windows-only export workflow. The export code performs explicit cleanup; the directory is initially created with `keep=true`, so cleanup depends on that surrounding export path. None was observed. [Creation][S67], [Android-only trigger and cleanup](C:/Users/k/Repository/External/Godot_4-7-2/editor/export/editor_export_platform.cpp:334).

6. **Generic temporary-file/directory API outputs.** `FileAccess.create_temp` and `DirAccess.create_temp` use the OS temp root, an optional caller-supplied prefix, and a time-based unique suffix. Retention depends on `keep`, object lifetime, and clean execution. An add-on or script can use these without naming the files “Godot.” [File implementation][S65], [directory implementation][S66].

7. **`%LOCALAPPDATA%\data_<C# project name>_windows_x86_64\...` when running an export with embedded .NET build outputs.** If the exported pack contains `.godot/mono/publish/x86_64`, Godot may extract it into this cache folder so native/.NET binaries can be loaded. A `.dotnet-publish-manifest` determines reuse versus replacement. The extracted cache is retained for later launches. If the publish directory can be used directly, extraction is bypassed. This has no `Godot` parent directory. [Extraction/reuse algorithm][S68], [export embed option][S90].

   Your [export preset](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/export_presets.cfg:22) explicitly sets `dotnet/embed_build_outputs=false`. Your present export therefore uses its adjacent `Build\data_Veehiicuul_Godot_CSharp_windows_x86_64` folder instead. No `data_*` directory was found at the top of Local AppData.

8. **`GodotSharp\Tools\GodotTools.IdeMessaging.CLI.log` next to the IDE messaging CLI assembly, when that helper runs and logs.** Its logger derives the filename from its own assembly location and appends messages. This is an exception to the assumption that everything new in the installation is under `editor_data`. No such log was present in this installation. If another copy of the helper is run elsewhere, its log is beside that copy. [Logger][S69].

9. **Temporary GDExtension copies beside the original DLL.** Windows editor loading can create a hidden `~<library>.dll` and renamed temporary PDBs in the library directory to avoid locking the original build output. Godot removes these on unload/shutdown where possible and attempts to clear leftovers. They are native-extension files, not ordinary C# assembly hot-reload output. [DLL copy][S71], [PDB handling][S72].

10. **Safe-save files beside whichever file is being saved.** With the editor's safe-save option enabled, Windows FileAccess writes `<target filename><numeric id>.tmp` in the target's directory, then replaces/moves it into the final filename on close. Failures can leave the temporary file. There is no central backup directory for this mechanism. [Editor option hookup][S91], [temp creation][S73], [replacement](C:/Users/k/Repository/External/Godot_4-7-2/drivers/windows/file_access_windows.cpp:245).

11. **Source-adjacent generated files not yet committed.** Examples are `<asset>.import`, applicable script/resource `.uid` sidecars, and `<3D source>.unwrap_cache` produced during lightmap-UV unwrapping. Your exclusion covers those already committed; it does not cover newly generated untracked instances. In particular, `.import` includes import choices and references, so it should not be treated like the disposable converted output under `.godot\imported`. [Import sidecars][S25], [UIDs][S93], [scene unwrap][S74], [OBJ unwrap][S75].

12. **Explicit outputs at selected/configured destinations.** Windows exports create EXE/PCK/ZIP and supporting files at the chosen export path. Documentation/API dump commands write XML/JSON/header files; Movie Maker writes its configured video or image/audio sequence; debugger export actions write CSVs. Resource saves, image exports, downloaded assets, custom build-profile exports, and plugin writers can choose other paths. These are operation outputs, not a hidden universal cache tree. [Windows export][S56], [API dumps][S76], [movie configuration][S77], [PNG/WAV output][S96], [debugger CSV][S78], [general file routing][S85].

**Windows registry data written explicitly by Godot**

For an **exported Windows application**, the Windows display-server constructor attempts to write:

```text
HKEY_CURRENT_USER\Software\Classes\Local Settings\
  Software\Microsoft\Windows\Shell\MuiCache

Value name: <absolute executable path>.FriendlyAppName
Value type: REG_SZ
Value data: application/config/name
```

The source uses `HKEY_CURRENT_USER_LOCAL_SETTINGS` and opens the existing `Software\Microsoft\Windows\Shell\MuiCache` key before `RegSetValueExW`. The write is inside `#ifndef TOOLS_ENABLED`, so it applies to exported applications rather than the editor executable's project-run mode. It is also conditional on successfully opening that registry key and initializing this Windows display-server path. [Exact writer][S70].

Your registry currently contains matching FriendlyAppName values for the current Veehiicuul export path, its earlier export path, and InputLatencyGodot. Deleting the export folder or editor caches does not itself remove these values. The source does not put this data under `editor_data`.

The same constructor supplies a Windows AppUserModelID for window/taskbar identity. That call is not by itself evidence of an additional Godot-authored on-disk record. Windows may manage its own shell history separately. [Constructor](C:/Users/k/Repository/External/Godot_4-7-2/platform/windows/display_server_windows.cpp:7975).

**.NET SDK / NuGet / MSBuild persistence**

Godot's source proves the dependency boundary: it locates an external .NET SDK, launches `dotnet build`/`publish`, normally permits restore, and requests `--self-contained true` for exported managed applications. The child process receives an environment; Godot does not use `_sc_` to replace every external tool's profile/cache paths. **Portable Godot editor mode and .NET self-contained application deployment are different settings.** [SDK discovery][S95], [process launch][S79], [restore/publish arguments][S80].

The precise persistence of those tools is implemented outside the Godot repository. The following locations are therefore a separately attributed part of the environment, supported by that Godot call site, local observations, and Microsoft's primary documentation:

| Default Windows location             | Role                                   | Observed here                     |
| ------------------------------------ | -------------------------------------- | --------------------------------- |
| %USERPROFILE%\\.nuget\\packages      | Expanded shared packages               | Yes; four Godot packages at 4.7.2 |
| %LOCALAPPDATA%\\NuGet\\v3-cache      | NuGet HTTP response/package cache      | Yes                               |
| %LOCALAPPDATA%\\NuGet\\plugins-cache | NuGet plugin-operation cache           | No                                |
| %TEMP%\\NuGetScratch                 | NuGet temporary files and coordination | Yes                               |
| %USERPROFILE%\\.dotnet               | CLI state, caches and SDK-related data | Yes                               |

NuGet's first four paths have documented environment/configuration overrides, including `NUGET_PACKAGES`, `NUGET_HTTP_CACHE_PATH`, `NUGET_PLUGINS_CACHE_PATH`, and `NUGET_SCRATCH`. They are shared between projects/tools, and their contents cannot all be attributed to Godot. [Microsoft's NuGet storage documentation](https://learn.microsoft.com/en-us/nuget/consume-packages/managing-the-global-packages-and-cache-folders).

The project's existing `project.assets.json` identifies `C:\Users\k\.nuget\packages\` as its package folder. The observed Godot package directories are `godot.net.sdk`, `godot.sourcegenerators`, `godotsharp`, and `godotsharpeditor`, each with 4.7.2. These restored copies are outside your fresh-download baseline. This observation is recorded in the inventory; the package contents are not copied into the report.

The existing restore metadata references `%APPDATA%\NuGet\NuGet.Config` and Visual Studio's offline NuGet configuration. Those files are inputs used by restore; the Godot source cited above does not establish that Godot created or rewrote them.

Your `.dotnet` directory currently contains `sdk-advertising`, `TelemetryStorageService`, workload-advertising state, first-use/toolpath/certificate sentinels, and machine/container cache filenames. Only the filenames were recorded. `DOTNET_CLI_HOME` affects where CLI support state is stored; other behavior depends on the SDK and environment. These observations do not prove which earlier tool run created each file or that telemetry was transmitted. [Microsoft's .NET environment-variable documentation](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-environment-variables).

There are also 21 `MSBuildTemp*` directories in the OS temp root. These are shared-tool candidates; there is no historical process trace here attributing every one to a Godot build. Additional SDK, NuGet package, analyzer, custom MSBuild target, IDE, or signing-tool output must be assessed from that component's implementation/configuration. Godot's source alone cannot enumerate all of it.

**Windows and graphics-driver persistence**

The observed `%LOCALAPPDATA%\CrashDumps` files include three for `Godot_v4.7.2-stable_mono_win64.exe`, three for `Veehiicuul_Godot_CSharp.exe`, and one for `InputLatencyGodot.exe`, totaling 107,872,106 bytes. Their exact names/sizes are in the inventory.

Godot's Windows crash-handler implementations print diagnostic backtraces. The reviewed handlers do not call a minidump writer. Windows Error Reporting or a debugger can produce dumps separately; `%LOCALAPPDATA%\CrashDumps` is a documented Windows dump location. I have not established the exact producer of each existing dump from its filename alone. [Godot SEH handler][S81], [Godot signal handler](C:/Users/k/Repository/External/Godot_4-7-2/platform/windows/crash_handler_windows_signal.cpp:233), [Microsoft's dump documentation](https://learn.microsoft.com/en-us/windows/win32/wer/collecting-user-mode-dumps).

`%LOCALAPPDATA%\D3DSCache` and `%LOCALAPPDATA%\NVIDIA\DXCache` both exist on this computer. **Their presence does not identify which entries belong to Godot.** Godot delegates graphics work to Direct3D/the driver, so driver-managed persistence is outside its own shader-cache files. The Godot source supports the API boundary, not an exact vendor-cache inventory or retention promise. [Direct3D pipeline creation][S82].

Likewise, Godot invokes native file dialogs and Windows shell operations. The shell can own related state, and sending a file to the Recycle Bin is expressly a Windows operation. Windows-managed histories, diagnostics, caches, and filesystem metadata are not all describable from Godot source. They must not be mislabeled as extra `editor_data` files. [Native dialogs][S83], [Recycle Bin call][S84].

**Your repository's own additional outputs**

The current Veehiicuul project has two generated directories outside `.godot`:

- `Project\Build`: 197 files, 198,717,733 bytes; none tracked by Git at inspection. This is the EXE/PCK and adjacent managed runtime/application data produced by your configured Windows export. [Your preset](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/export_presets.cfg:8), [Godot's Windows exporter][S56], [managed publish handling][S90].
- `Project\MyLogOutput\<timestamp>`: 171 files, 360,808 bytes; none tracked by Git at inspection. Your wrappers choose these directories and pass explicit `--log-file` paths for import/export/run. This explains why application diagnostics need not appear under `UserData\logs`. Your application also chooses a session directory and can write `Stutter.log`. [Build wrapper](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Build.ps1:13), [run wrapper](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Run.ps1:13), [session directory](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/EngineIntegration/SessionLog.cs:16), [stutter writer](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/GameDataAndLogic/StutterLogger.cs:22), [Godot override behavior][S05].

The wrapper-created transcript/console files are your launcher behavior, not an undocumented native Godot default. Their output belongs in this inventory even though the scripts defining it are committed source you already know.

**How to reason about retention and relocation**

The distinction follows from the writers above:

- Imported assets, filesystem/class/UID indexes, compiled shader caches, generated help, thumbnails, converted export resources, and C# build intermediates can normally be regenerated from their retained inputs. Clearing them costs reimport/build/compile time.
- Editor settings, project lists, named layouts, favorites, themes, script templates, feature profiles, and scene/script editing state retain preferences or work context. Regeneration supplies defaults, not the previous user choices.
- Export credentials, custom keys, game save data, screenshots, logs, profiler snapshots, and crash dumps can contain information that cannot be reconstructed after deletion.
- “Temporary” describes intended lifetime, not guaranteed cleanup after a crash. Some parent directories remain after normal cleanup; virtual-drop cleanup specifically leaves its top-level directory.
- Exported build outputs are reproducible when the full inputs/toolchain remain available, but are also the runnable application. They are not an editor cache.
- Shared .NET/NuGet and driver/Windows caches belong to multiple applications. A directory's existence or a suggestive filename is not sufficient ownership evidence.

To redirect storage, the relevant mechanisms are separate: the portable marker for editor-owned roots; project directory/settings for project data; custom-user-directory settings for the Roaming suffix; `--log-file` for a particular log; OS/.NET temp configuration for generic and managed temporary writes; SDK/NuGet variables for their own shared storage; and the export preset for output location/embedded managed outputs. None of these individually confines every writer. [Editor roots][S01], [project name][S04], [user directory][S03], [log override][S05], [native temporary API][S65], [managed temporary API][S60], [embed switch][S90].

The inventory is exhaustive for the files and directories under the explicitly enumerated roots at its snapshot, and the report traces the named storage roots and writer families reviewed in this source. **It is not a claim that an arbitrary Godot session can write only these filenames.** User-selected outputs, project/extension code, third-party build tools, and operating-system components make that set open-ended. Proving every actual write for a chosen workflow would additionally require a process/file/registry trace of that workflow, including child processes and relevant Windows services. No such dynamic trace was performed here.

[S01]: C:/Users/k/Repository/External/Godot_4-7-2/editor/file_system/editor_paths.cpp:128 "EditorPaths constructor"
[S02]: C:/Users/k/Repository/External/Godot_4-7-2/platform/windows/os_windows.cpp:2417 "Windows data/config/cache/temp roots"
[S03]: C:/Users/k/Repository/External/Godot_4-7-2/core/os/os.cpp:338 "user:// resolution"
[S04]: C:/Users/k/Repository/External/Godot_4-7-2/core/config/project_settings.cpp:885 "Project data-directory name"
[S05]: C:/Users/k/Repository/External/Godot_4-7-2/main/main.cpp:2312 "File logging defaults and overrides"
[S06]: C:/Users/k/Repository/External/Godot_4-7-2/core/io/logger.cpp:108 "Log rotation"
[S07]: C:/Users/k/Repository/External/Godot_4-7-2/editor/settings/editor_settings.cpp:1350 "Versioned editor settings"
[S08]: C:/Users/k/Repository/External/Godot_4-7-2/editor/project_manager/project_list.cpp:1728 "Project Manager list"
[S09]: C:/Users/k/Repository/External/Godot_4-7-2/editor/settings/editor_settings.cpp:1729 "Favorites and recent directories"
[S10]: C:/Users/k/Repository/External/Godot_4-7-2/editor/settings/editor_settings.cpp:1974 "Saved editor layouts"
[S11]: C:/Users/k/Repository/External/Godot_4-7-2/editor/script/script_editor_plugin.cpp:850 "Text-editor theme import and save"
[S12]: C:/Users/k/Repository/External/Godot_4-7-2/editor/settings/editor_feature_profile.cpp:499 "Feature profiles"
[S13]: C:/Users/k/Repository/External/Godot_4-7-2/editor/inspector/editor_resource_preview.cpp:256 "Resource-preview files"
[S14]: C:/Users/k/Repository/External/Godot_4-7-2/editor/inspector/editor_resource_preview.cpp:321 "Resource-preview cache names"
[S15]: C:/Users/k/Repository/External/Godot_4-7-2/editor/doc/editor_help.cpp:2957 "Documentation-cache paths"
[S16]: C:/Users/k/Repository/External/Godot_4-7-2/editor/asset_library/asset_library_editor_plugin.cpp:935 "Asset download ZIP"
[S17]: C:/Users/k/Repository/External/Godot_4-7-2/editor/asset_library/asset_library_editor_plugin.cpp:1305 "Asset image data and ETag"
[S18]: C:/Users/k/Repository/External/Godot_4-7-2/modules/mono/godotsharp_dirs.cpp:70 "Mono user storage and build paths"
[S19]: C:/Users/k/Repository/External/Godot_4-7-2/modules/mono/editor/GodotTools/GodotTools/Internals/GodotSharpDirs.cs:179 "Build-output and build-log paths"
[S20]: C:/Users/k/Repository/External/Godot_4-7-2/modules/mono/editor/GodotTools/GodotTools.BuildLogger/GodotBuildLogger.cs:19 "MSBuild text and issues logs"
[S21]: C:/Users/k/Repository/External/Godot_4-7-2/modules/mono/editor/GodotTools/GodotTools/Build/BuildSystem.cs:293 "Optional MSBuild binary log"
[S22]: C:/Users/k/Repository/External/Godot_4-7-2/editor/file_system/editor_file_system.cpp:553 "Filesystem cache writer"
[S23]: C:/Users/k/Repository/External/Godot_4-7-2/editor/file_system/editor_file_system.cpp:2072 "Filesystem update journal"
[S24]: C:/Users/k/Repository/External/Godot_4-7-2/core/io/resource_importer.cpp:540 "Imported-resource filename root"
[S25]: C:/Users/k/Repository/External/Godot_4-7-2/editor/file_system/editor_file_system.cpp:2691 "Import sidecars and checksums"
[S26]: C:/Users/k/Repository/External/Godot_4-7-2/core/io/resource_uid.cpp:47 "UID cache path"
[S27]: C:/Users/k/Repository/External/Godot_4-7-2/core/config/project_settings.cpp:1470 "Global script-class cache"
[S28]: C:/Users/k/Repository/External/Godot_4-7-2/core/config/project_settings.cpp:1565 "Scene-group cache"
[S29]: C:/Users/k/Repository/External/Godot_4-7-2/core/extension/gdextension_manager.cpp:380 "Extension-list writer"
[S30]: C:/Users/k/Repository/External/Godot_4-7-2/editor/editor_node.cpp:2125 "Scene edit-state path"
[S31]: C:/Users/k/Repository/External/Godot_4-7-2/editor/editor_node.cpp:6285 "Project editor layout"
[S32]: C:/Users/k/Repository/External/Godot_4-7-2/editor/settings/editor_settings.cpp:1268 "Project metadata path"
[S33]: C:/Users/k/Repository/External/Godot_4-7-2/editor/settings/editor_folding.cpp:52 "Resource and scene folding"
[S34]: C:/Users/k/Repository/External/Godot_4-7-2/editor/script/script_editor_plugin.cpp:3363 "Script editor state"
[S35]: C:/Users/k/Repository/External/Godot_4-7-2/editor/gui/editor_quick_open_dialog.cpp:1008 "Quick-open cache"
[S36]: C:/Users/k/Repository/External/Godot_4-7-2/editor/gui/create_dialog.cpp:562 "Recent node/resource creation"
[S37]: C:/Users/k/Repository/External/Godot_4-7-2/editor/animation/animation_library_editor.cpp:802 "Animation-library folding"
[S38]: C:/Users/k/Repository/External/Godot_4-7-2/editor/settings/editor_build_profile.cpp:881 "Used-class cache"
[S39]: C:/Users/k/Repository/External/Godot_4-7-2/editor/export/editor_export.cpp:79 "Export secrets separated from presets"
[S40]: C:/Users/k/Repository/External/Godot_4-7-2/editor/export/editor_export_platform.cpp:1481 "Converted export-resource cache"
[S41]: C:/Users/k/Repository/External/Godot_4-7-2/editor/export/shader_baker_export_plugin.cpp:79 "Shader-baker export cache"
[S42]: C:/Users/k/Repository/External/Godot_4-7-2/modules/mono/editor/Godot.NET.Sdk/Godot.NET.Sdk/Sdk/Sdk.props:15 "C# build-output defaults"
[S43]: C:/Users/k/Repository/External/Godot_4-7-2/modules/mono/editor/GodotTools/GodotTools/Ides/MessagingServer.cs:100 "IDE metadata creation and deletion"
[S44]: C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/renderer_compositor_rd.cpp:327 "Renderer shader-cache root"
[S45]: C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/shader_rd.cpp:603 "Shader-cache filenames"
[S46]: C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/rendering_device.cpp:8580 "Pipeline-cache path and loading"
[S47]: C:/Users/k/Repository/External/Godot_4-7-2/drivers/d3d12/rendering_device_driver_d3d12.cpp:4216 "Unimplemented D3D12 pipeline serialization"
[S48]: C:/Users/k/Repository/External/Godot_4-7-2/drivers/gles3/rasterizer_gles3.cpp:339 "OpenGL shader-cache root"
[S49]: C:/Users/k/Repository/External/Godot_4-7-2/drivers/egl/egl_manager.cpp:498 "EGL shader-cache root"
[S50]: C:/Users/k/Repository/External/Godot_4-7-2/core/os/os.cpp:370 "Recovery-mode lock"
[S51]: C:/Users/k/Repository/External/Godot_4-7-2/editor/editor_node.cpp:1542 "Initialization-lock removal"
[S52]: C:/Users/k/Repository/External/Godot_4-7-2/editor/editor_node.cpp:3999 "Editor screenshots"
[S53]: C:/Users/k/Repository/External/Godot_4-7-2/modules/objectdb_profiler/editor/objectdb_profiler_panel.cpp:117 "ObjectDB snapshots"
[S54]: C:/Users/k/Repository/External/Godot_4-7-2/editor/export/editor_export_platform.cpp:1840 "Temporary project.binary"
[S55]: C:/Users/k/Repository/External/Godot_4-7-2/editor/export/editor_export_platform.cpp:2422 "Temporary PCK data"
[S56]: C:/Users/k/Repository/External/Godot_4-7-2/platform/windows/export/export_plugin.cpp:235 "Windows ZIP-export staging"
[S57]: C:/Users/k/Repository/External/Godot_4-7-2/platform/windows/export/export_plugin.cpp:531 "Temporary Windows icon"
[S58]: C:/Users/k/Repository/External/Godot_4-7-2/platform/windows/export/export_plugin.cpp:997 "Remote Windows deployment staging"
[S59]: C:/Users/k/Repository/External/Godot_4-7-2/editor/export/export_template_manager.cpp:1852 "Template-manager download staging"
[S60]: C:/Users/k/Repository/External/Godot_4-7-2/modules/mono/editor/GodotTools/GodotTools/Export/ExportPlugin.cs:265 "OS-temp .NET publish staging"
[S61]: C:/Users/k/Repository/External/Godot_4-7-2/modules/mono/editor/GodotTools/GodotTools/Export/ExportPlugin.cs:517 ".NET export cleanup"
[S62]: C:/Users/k/Repository/External/Godot_4-7-2/modules/mono/editor/GodotTools/GodotTools/CsTranslationParserPlugin.cs:414 "Temporary C# translation build output"
[S63]: C:/Users/k/Repository/External/Godot_4-7-2/platform/windows/drop_target_windows.cpp:42 "Virtual-drop temporary directory"
[S64]: C:/Users/k/Repository/External/Godot_4-7-2/platform/windows/drop_target_windows.cpp:364 "Virtual-drop cleanup"
[S65]: C:/Users/k/Repository/External/Godot_4-7-2/core/io/file_access.cpp:83 "Generic temporary-file API"
[S66]: C:/Users/k/Repository/External/Godot_4-7-2/core/io/dir_access.cpp:330 "Generic temporary-directory API"
[S67]: C:/Users/k/Repository/External/Godot_4-7-2/editor/export/editor_export_platform.cpp:263 "Patch-base extraction"
[S68]: C:/Users/k/Repository/External/Godot_4-7-2/modules/mono/godotsharp_dirs.cpp:180 "Embedded .NET runtime extraction"
[S69]: C:/Users/k/Repository/External/Godot_4-7-2/modules/mono/editor/GodotTools/GodotTools.IdeMessaging.CLI/Program.cs:162 "IDE bridge log next to assembly"
[S70]: C:/Users/k/Repository/External/Godot_4-7-2/platform/windows/display_server_windows.cpp:7993 "Exported Windows application's registry write"
[S71]: C:/Users/k/Repository/External/Godot_4-7-2/platform/windows/os_windows.cpp:490 "Temporary GDExtension DLL"
[S72]: C:/Users/k/Repository/External/Godot_4-7-2/platform/windows/windows_utils.cpp:188 "Temporary PDB handling"
[S73]: C:/Users/k/Repository/External/Godot_4-7-2/drivers/windows/file_access_windows.cpp:197 "Safe-save temporary file"
[S74]: C:/Users/k/Repository/External/Godot_4-7-2/editor/import/3d/resource_importer_scene.cpp:3383 "Scene unwrap cache beside source"
[S75]: C:/Users/k/Repository/External/Godot_4-7-2/editor/import/3d/resource_importer_obj.cpp:679 "OBJ unwrap cache beside source"
[S76]: C:/Users/k/Repository/External/Godot_4-7-2/main/main.cpp:4285 "CLI API dump outputs"
[S77]: C:/Users/k/Repository/External/Godot_4-7-2/servers/movie_writer/movie_writer.cpp:151 "Movie output settings"
[S78]: C:/Users/k/Repository/External/Godot_4-7-2/editor/debugger/script_editor_debugger.cpp:191 "Debugger CSV output"
[S79]: C:/Users/k/Repository/External/Godot_4-7-2/modules/mono/editor/GodotTools/GodotTools/Build/BuildSystem.cs:98 "Launching external .NET tooling"
[S80]: C:/Users/k/Repository/External/Godot_4-7-2/modules/mono/editor/GodotTools/GodotTools/Build/BuildSystem.cs:220 "Restore and self-contained publish"
[S81]: C:/Users/k/Repository/External/Godot_4-7-2/platform/windows/crash_handler_windows_seh.cpp:144 "Windows crash backtrace"
[S82]: C:/Users/k/Repository/External/Godot_4-7-2/drivers/d3d12/rendering_device_driver_d3d12.cpp:5358 "Delegating pipeline creation to Direct3D"
[S83]: C:/Users/k/Repository/External/Godot_4-7-2/platform/windows/display_server_windows.cpp:691 "Native Windows file dialogs"
[S84]: C:/Users/k/Repository/External/Godot_4-7-2/platform/windows/os_windows.cpp:2552 "Windows Recycle Bin operation"
[S85]: C:/Users/k/Repository/External/Godot_4-7-2/core/io/file_access.cpp:64 "FileAccess accepts multiple path roots"
[S86]: C:/Users/k/Repository/External/Godot_4-7-2/modules/gltf/editor/editor_scene_importer_blend.cpp:136 "Blender import intermediate"
[S87]: C:/Users/k/Repository/External/Godot_4-7-2/modules/fbx/editor/editor_scene_importer_fbx2gltf.cpp:67 "FBX2glTF import intermediate"
[S88]: C:/Users/k/Repository/External/Godot_4-7-2/platform/android/export/export_plugin.cpp:941 "Conditional Android debug keystore"
[S89]: C:/Users/k/Repository/External/Godot_4-7-2/editor/file_system/editor_paths.cpp:75 "Editor auxiliary directories"
[S90]: C:/Users/k/Repository/External/Godot_4-7-2/modules/mono/editor/GodotTools/GodotTools/Export/ExportPlugin.cs:245 "Embed-build-output export switch"
[S91]: C:/Users/k/Repository/External/Godot_4-7-2/editor/editor_node.cpp:8428 "Enabling safe-save behavior"
[S92]: C:/Users/k/Repository/External/Godot_4-7-2/core/config/project_settings.cpp:1701 "Directory-option defaults"
[S93]: C:/Users/k/Repository/External/Godot_4-7-2/editor/file_system/editor_file_system.cpp:945 "Generated UID sidecars"
[S94]: C:/Users/k/Repository/External/Godot_4-7-2/main/main.cpp:2260 "Creating the user-data directory"
[S95]: C:/Users/k/Repository/External/Godot_4-7-2/modules/mono/mono_gd/gd_mono.cpp:113 "External .NET SDK discovery"
[S96]: C:/Users/k/Repository/External/Godot_4-7-2/servers/movie_writer/movie_writer_pngwav.cpp:79 "Movie frame and audio files"
[S97]: C:/Users/k/Repository/External/Godot_4-7-2/modules/mono/editor/GodotTools/GodotTools.IdeMessaging/GodotIdeMetadata.cs:10 "IDE metadata filename"
