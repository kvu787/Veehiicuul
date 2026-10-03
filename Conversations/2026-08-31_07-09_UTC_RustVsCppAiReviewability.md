# Rust versus C++ for AI-driven development

Date: 2026-08-31

Repository: `Simple_DirectX12_3D_Game`

## User

> what are the pros/cons of using rust instead of c++ for this app? keep in mind that development of this app is heavily ai-driven but must be reviewable by humans

## Assistant

Short answer: keep C++ for this app as it exists today. Rust becomes the better bet if the project is expected to grow substantially and most new code will be safe game logic, tools, asset processing, networking, or concurrency rather than more raw DirectX 12 integration.

The current project has about 1,700 lines of hand-written C++/headers, with roughly 1,275 concentrated in `Renderer.cpp`, plus 108 lines of HLSL. It has no automated tests. Its C++ is already fairly disciplined: COM objects use `ComPtr`, HRESULTs are checked, COM initialization is scoped, mapped memory and the Win32 fence handle are explicitly cleaned up, and there is no raw `new`/`delete`. That reduces the marginal safety gain of a rewrite.

| Concern | Rust | C++ | Edge for this app |
| --- | --- | --- | --- |
| AI-generated memory/ownership mistakes | The borrow checker, `Result`/`Option`, exhaustive matching, and thread-safety traits reject many bad changes at compile time. | More mistakes compile and must be caught by review, analysis, or runtime validation. | Rust |
| DirectX 12 integration | Microsoft's `windows` crate exposes Direct3D, DXGI, WIC, and Win32, but calls and ABI details remain heavily `unsafe`. | Directly matches the SDK, documentation, PIX/debug-layer terminology, and most samples. | C++ |
| Human review of the existing project | A full port would be one large semantic rewrite whose parity is hard to prove without tests. | Reviewers can assess small diffs against already-working code. | C++ |
| GPU correctness | Rust does not inherently verify resource states, barriers, fence values, allocator reuse, descriptor offsets, or CPU/HLSL layouts. | Same semantic burden. | Tie |
| Quality tooling | Cargo, rustfmt, Clippy, unit/integration tests, and `Cargo.lock` provide a consistent default workflow. | Equivalent gates are possible, but CMake, formatting, analysis, and testing need more assembly. | Rust |
| Runtime speed and distribution | Native executable with no garbage collector; performance should be comparable when implemented equivalently. | Same. | Tie |
| Portability | Still Windows-only when using Win32/D3D12. Rust alone does not make the renderer portable. | Already Windows-only. | Tie |

Rust's biggest advantage in an AI-heavy workflow is that it moves part of the review burden into the compiler. It is harder for an agent to accidentally leave a dangling reference, race ordinary shared state, ignore an enum case, or muddle nullable ownership in safe Rust. `rustfmt`, Clippy, and `cargo test` also make a uniform quality gate easy to give every agent.

The important limitation is that this app lives close to an unsafe boundary. Microsoft says the `windows` crate covers Direct3D and Win32 directly, and Microsoft maintains a Direct3D 12 Rust sample. That sample nevertheless uses `unsafe` throughout the rendering path, including raw pointer copies, union construction, `transmute_copy`, and Direct3D calls. Rust can isolate and label those obligations, but it cannot prove that a resource barrier or fence protocol is correct. The official Rust FFI guidance likewise describes `unsafe` as a proof obligation owned by the programmer, not a correctness guarantee.

There is also a documentation tax. Microsoft notes that Windows API documentation is mainly written with C/C++ in mind, so Rust developers often have to translate the documented signature and idiom into the `windows` projection. I would expect that to create more opportunities for AI projection mistakes and more cognitive load for a reviewer; that is an inference from the documentation ecosystem, not a measured model benchmark. Rust also adds `rustup` and Cargo, while still requiring Microsoft's C++ build tools on Windows.

For reviewability, the highest-value change is not a language rewrite. It is to split the large renderer into narrow modules, add tests around paint parsing/math/layout, run the D3D12 debug layer in a smoke test, capture a deterministic parity image, enable stricter warning/static-analysis gates, and require small commits with validation evidence. Current Codex workflows explicitly support codebase analysis, refactoring, controlled migrations, and diff review, so either language is workable when those review boundaries are supplied.

If Rust is chosen, use it deliberately:

- use the `windows` crate directly rather than changing to `wgpu` unless cross-platform rendering is a separate goal;
- confine `unsafe` to small Win32/D3D12 modules, require a `// SAFETY:` explanation for every block, and deny `unsafe_op_in_unsafe_fn`;
- forbid `unwrap`/`expect` in runtime paths and reject broad `unsafe` blocks;
- use `#[repr(C)]` plus compile-time size/alignment checks for vertices and constant buffers;
- keep dependencies minimal and commit `Cargo.lock`;
- port in reviewable parity checkpoints rather than as one replacement commit.

A mixed Rust/C++ design would be the worst fit at this size: it adds an FFI boundary and two build ecosystems without removing the unsafe D3D12 core.

Recommendation: retain C++ now and improve structure and verification. Reconsider Rust before the next major expansion if the project is becoming a long-lived game/engine rather than remaining a compact DirectX 12 scene. If starting this larger version from scratch, I would lean Rust, with a thin, rigorously reviewed unsafe renderer boundary.

Sources consulted:

- Microsoft Learn, "Rust for Windows, and the windows crate": https://learn.microsoft.com/en-us/windows/dev-environment/rust/rust-for-windows
- Microsoft's current Rust Direct3D 12 sample: https://github.com/microsoft/windows-rs/blob/master/crates/samples/windows/direct3d12/src/main.rs
- Microsoft Learn, Rust for Windows tutorial and C/C++-oriented API documentation note: https://learn.microsoft.com/en-us/windows/dev-environment/rust/rss-reader-rust-for-windows
- Rustonomicon FFI guidance: https://doc.rust-lang.org/nomicon/ffi.html
- Cargo build scripts: https://doc.rust-lang.org/cargo/reference/build-scripts.html
- Cargo tests: https://doc.rust-lang.org/cargo/commands/cargo-test.html
- Clippy documentation: https://doc.rust-lang.org/stable/clippy/index.html
- Microsoft Learn, Windows Rust setup: https://learn.microsoft.com/en-us/windows/dev-environment/rust/setup
- OpenAI Codex use cases: https://learn.chatgpt.com/use-cases

No images were attached to this conversation. No application source or build files were changed.
