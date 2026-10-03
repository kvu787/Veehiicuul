# PowerShell run wrapper

Date: 2026-08-30

Repository: `Simple_DirectX12_3D_Game`

## User

> reimplement Run.cmd so that it's a minimal wrapper around a powershell script called Run.ps1 so that i can read mostly powershell instead of unpleasant batch

## Assistant

Reimplemented the launcher so `Run.cmd` is a minimal wrapper around `Run.ps1`. The PowerShell script now owns Visual Studio discovery, developer-environment setup, CMake configuration, Ninja compilation, executable launch, and failure reporting. Updated the README and verified the wrapper by running it successfully through configuration, an up-to-date Ninja build, and executable launch.

No images were attached to this conversation.
