# Rust DirectX 12 rewrite toolchain

Date: 2026-08-31

Repository: `Simple_DirectX12_3D_Game`

## User

> for learning and experimentation purposes, i am interested in rewriting this app in rust in a way that maximizes both safety and performance (and makes reasonable compromises when those conflict). explain the complete toolchain i would need for this

## Assistant

The recommended stack is stable Rust targeting MSVC, Cargo as the top-level build system, Microsoft's typed `windows` crate for Win32/D3D12, offline HLSL compilation with the Windows SDK's `dxc.exe`, the existing Blender asset pipeline, and Direct3D's validation/profiling tools. CMake and Ninja would no longer be needed for the Rust application itself.

The current machine already has most of the native prerequisites:

- Visual Studio 18.9.2 Community with MSVC;
- Windows SDK 10.0.26100.0;
- the SDK's x64 `dxc.exe`;
- the D3D12 and DXGI debug-layer DLLs;
- Blender 4.5.12 and 5.2.0.

Rust and PIX are not currently installed.

## End-to-end build

```text
Rust source --------------------------> rustc
                                           |
HLSL --> build.rs --> dxc.exe --> DXIL ----+--> MSVC linker --> game.exe + PDB
                                           |
Blender --> GenerateAssets.py --> mesh ----+

PNG + editable CarPaint.ini --> Run.ps1 stages runtime assets beside game.exe
```

Cargo owns dependency resolution, Rust compilation, tests, profiles, and linking. A small `build.rs` owns shader compilation only. It invokes `dxc.exe` directly with `std::process::Command`, writes DXIL to Cargo's `OUT_DIR`, emits `cargo::rerun-if-changed` for the HLSL files, and the Rust program embeds the resulting DXIL using `include_bytes!`. No shader compiler DLL is required at runtime.

The existing `Run.cmd`/`Run.ps1` pattern remains useful. The PowerShell script should locate Visual Studio with `vswhere`, initialize its x64 developer environment, find the Windows SDK's DXC, run `cargo build --profile release --locked`, stage the PNG and editable INI file, and launch `target\release\SimpleDirectX12Game.exe`. Cargo build scripts should not stage files outside `OUT_DIR`; runtime-asset staging belongs in `Run.ps1` or a small Rust `xtask` utility.

## Native and Rust toolchains

Use the `x86_64-pc-windows-msvc` Rust target, not the GNU target. It matches the Windows SDK, native PDB/CodeView debugging, PIX, and the already-installed MSVC linker. Microsoft documents the C++ build tools as a prerequisite for Rust on Windows.

Install rustup, then pin the repository to the current stable Rust 1.98.0 rather than following the moving `stable` alias:

```powershell
winget install Rustlang.Rustup
```

Commit this file:

```toml
# rust-toolchain.toml
[toolchain]
channel = "1.98.0"
profile = "minimal"
components = ["rustfmt", "clippy", "rust-analyzer", "llvm-tools-preview"]
targets = ["x86_64-pc-windows-msvc"]
```

This supplies `rustc`, Cargo, the standard library, formatting, linting, editor language services, and the LLVM coverage tools. Commit `Cargo.lock` as well so dependency resolution is repeatable.

Use stable Rust for all production builds. Install a separately pinned nightly toolchain only for Miri:

```powershell
rustup toolchain install nightly --profile minimal --component miri
```

After finding a nightly that includes Miri and passes the project, replace `nightly` in scripts with its exact date. Miri is valuable for pure-Rust undefined-behavior checks, but it cannot execute the DirectX/Win32 FFI path. This is a reason to put safe, platform-independent logic in a separate `game_core` crate.

## Rust dependencies

Use a deliberately small dependency set:

```toml
[dependencies]
windows = { version = "0.62.2", features = [
    "Win32_Foundation",
    "Win32_Graphics_Direct3D",
    "Win32_Graphics_Direct3D12",
    "Win32_Graphics_Dxgi",
    "Win32_Graphics_Dxgi_Common",
    "Win32_Security",
    "Win32_System_Diagnostics_Debug",
    "Win32_System_LibraryLoader",
    "Win32_System_Threading",
    "Win32_UI_HiDpi",
    "Win32_UI_Input_KeyboardAndMouse",
    "Win32_UI_WindowsAndMessaging",
] }
glam = "0.33"
bytemuck = { version = "1.25", features = ["derive"] }
png = "0.18.1"
```

The precise `windows` feature list should be compiler-checked against the selected release and kept narrow. The choices serve different purposes:

- `windows`, rather than `windows-sys`, provides the more strongly typed projection and automatic COM reference counting. The runtime cost is negligible relative to the API calls; the safety and reviewability improvement is worth it.
- `glam` supplies SIMD-aware game/graphics math. Keep `glam` types on the CPU side and convert matrices to explicit `[[f32; 4]; 4]` arrays in GPU-facing structures so shader layout is obvious.
- `bytemuck` safely exposes plain GPU structures as bytes. Its `Pod` derive rejects implicit padding, which is exactly the failure mode to avoid for vertex and constant-buffer layouts. Use explicit padding and compile-time size/alignment assertions.
- `png` replaces WIC for the one startup image. It removes COM/WIC calls from the unsafe boundary, and its normal decoder remains memory-safe. Do not enable its optional `zlib-rs` feature unless profiling proves startup decoding matters and the added unsafe dependency is reviewed.

Avoid `wgpu`, a game engine, `winit`, or a large convenience framework for this experiment. They are defensible product choices, but they would teach their abstractions rather than this app's Win32/D3D12 design. A small safe `Window` wrapper over the handful of required Win32 operations is the more useful compromise here.

## Safe/unsafe architecture

Use a two-crate workspace:

```text
crates/game_core/    # animation, paint parsing, math policy; forbids unsafe
crates/game_win32/   # executable, Win32 shell, D3D12 backend
```

`game_core` should use `#![forbid(unsafe_code)]`. The Windows crate should use:

```rust
#![deny(unsafe_op_in_unsafe_fn)]
#![deny(clippy::undocumented_unsafe_blocks)]
```

The D3D12 crate should expose safe RAII types and keep every unsafe block narrow. Useful wrappers include:

- an owned fence-event handle that calls `CloseHandle` in `Drop`;
- a mapped-buffer guard that validates the byte length, exposes a bounded slice, and unmaps in `Drop`;
- descriptor-index and frame-index newtypes;
- GPU resource wrappers that track expected states and centralize barrier creation;
- checked conversions and `checked_mul` for all allocation, row-pitch, and upload sizes;
- a window wrapper whose `Drop` destroys the window and whose callback cannot outlive its state.

Do not use `transmute`, broad unsafe functions, or manual `Send`/`Sync` implementations unless an individually documented invariant makes them unavoidable. Do not clone COM interfaces in the render loop; borrow them. Allocate resources at initialization, use fixed arrays for the two frames, keep the constant buffer persistently mapped, and perform no heap allocation in the steady-state render path.

Rust cannot prove the D3D12 state machine. Resource states, descriptor contents, shader bindings, fence ordering, and allocator reuse must still be checked by design plus Direct3D validation.

## Shader and asset tools

Keep the current HLSL and offline DXC model. Use separate shader modes:

- development/validation: `-Od -Zi -Qembed_debug -Ges -WX`;
- profiling: `-O3 -Zi -Qembed_debug -Ges -WX -all_resources_bound`;
- distribution: `-O3 -Ges -WX -all_resources_bound -Qstrip_debug -Qstrip_reflect`.

Keep asset regeneration explicit rather than performing it on every build. Modify `GenerateAssets.py` to emit either a generated Rust module with typed constant arrays or a versioned binary mesh with a fully checked parser. For this small mesh, generated typed Rust arrays are the simplest and safest option. Blender supplies its own Python runtime, so a separate Python installation is not required.

## Build profiles

Use a fast development profile, an optimized release profile, and a symbol-rich profiling profile:

```toml
[profile.release]
opt-level = 3
lto = "thin"
codegen-units = 1
panic = "abort"
overflow-checks = true

[profile.profiling]
inherits = "release"
debug = 2
strip = "none"
```

Thin LTO is a good optimization/build-time compromise, and one codegen unit is reasonable for a project this small. `panic = "abort"` prevents unwinding through Win32 callback/FFI boundaries. Retaining PDB information in the profiling profile makes native CPU debugging and PIX correlation practical.

Start with release overflow checks enabled. If profiling demonstrates a real hot-path cost, disable them globally only after size calculations and every FFI boundary use explicit checked arithmetic. Do not use `target-cpu=native` in normal builds; it makes the binary specific to the build machine. Reserve it for a local benchmark profile if desired.

## Validation and safety tools

Every change should pass:

```powershell
cargo fmt --all -- --check
cargo clippy --workspace --all-targets --all-features -- -D warnings
cargo test --workspace --all-targets --locked
cargo build --profile release --locked
```

Install `cargo-deny` and make dependency-source, license, duplicate-version, yanked-version, and RustSec advisory checks part of review:

```powershell
cargo install --locked cargo-deny
cargo deny init
cargo deny check
```

Optionally install `cargo-llvm-cov` for coverage of `game_core`. Miri should run only the pure core and any isolated unsafe helpers that do not call FFI:

```powershell
cargo +nightly miri test -p game_core
```

The executable should gain deterministic diagnostic flags such as `--warp`, `--validate`, `--gpu-validation`, `--dred`, `--frames N`, and `--screenshot PATH`. That allows repeatable smoke tests, screenshot parity checks against the C++ app, and clean separation between validation and performance runs.

## DirectX debugging and profiling

Use all of these, for different jobs:

- D3D12 debug layer on ordinary development runs. It must be enabled before device creation.
- GPU-based validation on focused validation runs. Microsoft warns that it can be very slow; never use its timings as performance data.
- DRED auto-breadcrumbs and page-fault reporting for device-removal diagnosis. Enable it only in diagnostic modes because it has measurable overhead.
- PIX for GPU captures, shader debugging, resource inspection, and CPU/GPU timing captures. Install the stable build with `winget install Microsoft.PIX`.
- PresentMon optionally for external frame-time, present, latency, and display-level measurements.
- Visual Studio, WinDbg, or VS Code's native Windows debugger for CPU crashes and PDB-backed call stacks; rust-analyzer supplies editing and static feedback.

Never compare performance while the debug layer, GPU validation, DRED, shader `-Od`, or a debugger is changing execution. Use the `profiling` Cargo profile, optimized shaders with symbols, identical window/present settings, and repeated PIX/PresentMon captures.

## AI-driven, human-reviewable workflow

Put the exact gate commands and unsafe policy in `AGENTS.md`. Require every unsafe block to explain:

1. the API contract being relied upon;
2. pointer validity and length;
3. ownership and lifetime;
4. thread assumptions;
5. why the safe wrapper prevents misuse.

Commit `rust-toolchain.toml`, `Cargo.lock`, `deny.toml`, shader flags, and the narrow Windows feature list. Dependency changes should include `cargo tree`/`cargo deny` evidence. Separate the rewrite into reviewable commits: toolchain scaffold, safe core, Win32 window, device/swap chain, shader pipeline, background, mesh/car, resize/synchronization, then parity/performance. Official OpenAI documentation describes Codex workflows for controlled migrations, refactoring, and reviewing diffs; those checkpoints give both AI and humans an objective surface to inspect.

The practical day-one additions are therefore rustup/Rust 1.98.0 and PIX. Everything else native is already present. Cargo, the `windows` crate, DXC, the D3D validation stack, a tiny set of reviewed Rust crates, and strict automated gates form the complete working toolchain.

Sources consulted:

- Rust 1.98.0 release: https://blog.rust-lang.org/2026/08/20/Rust-1.98.0/
- rustup toolchain files and pinning: https://rust-lang.github.io/rustup/overrides.html
- Microsoft Rust-on-Windows setup: https://learn.microsoft.com/en-us/windows/dev-environment/rust/setup
- Microsoft `windows-rs`: https://github.com/microsoft/windows-rs
- Cargo build scripts: https://doc.rust-lang.org/cargo/reference/build-scripts.html
- Cargo profiles: https://doc.rust-lang.org/cargo/reference/profiles.html
- Miri: https://github.com/rust-lang/miri
- glam: https://github.com/bitshifter/glam-rs
- bytemuck `Pod`: https://docs.rs/bytemuck/latest/bytemuck/derive.Pod.html
- Rust PNG decoder: https://github.com/image-rs/image-png
- D3D12 GPU-based validation: https://learn.microsoft.com/en-us/windows/win32/direct3d12/using-d3d12-debug-layer-gpu-based-validation
- DRED: https://learn.microsoft.com/en-us/windows/win32/direct3d12/use-dred
- PIX: https://learn.microsoft.com/en-us/windows/win32/direct3dtools/pix/articles/general/pix-overview
- PresentMon: https://github.com/GameTechDev/PresentMon
- cargo-deny: https://github.com/EmbarkStudios/cargo-deny
- OpenAI Codex use cases: https://learn.chatgpt.com/use-cases

No images were attached to this conversation. No application source or build files were changed.
