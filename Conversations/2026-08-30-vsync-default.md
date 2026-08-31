# Default VSync setting

Date: 2026-08-30

Repository: `Simple_DirectX12_3D_Game`

## User

> is the unstaged change the correct way to set vsync to false by default

## Assistant

Yes. Changing `m_vsyncEnabled`'s in-class initializer to `false` correctly makes VSync off by default.

The presentation path already handles this properly: `Present` uses sync interval zero, tearing is enabled only when supported, and the swap chain is created with the required tearing flag. No startup code overrides the value.

The README still said the application started with VSync on, so it was outdated.

## User

> update the readme

## Assistant

Updated the README to state that the application starts with VSync off. The borderless-fullscreen instructions now say to press only `F11`, because pressing `V` would turn VSync back on.
