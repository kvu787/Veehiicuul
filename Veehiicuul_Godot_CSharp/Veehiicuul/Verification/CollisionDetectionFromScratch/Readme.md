# Collision detection from scratch

An independent validation and measurement harness for the collision detection
code. It was written without the repository's other verification code, track
data, settings, measurements, or reports. Its findings are in
[the from-scratch report](../../Documentation/CollisionReviewFromScratch20260929/Report.md).

## What it takes from the application

Eight source files, compiled unmodified:

| File                                     | Compiled by                |
| ---------------------------------------- | -------------------------- |
| `ColliderJson.cs`                        | Console program and engine |
| `CoordinateXY.cs`                        | Console program and engine |
| `Guard.cs`                               | Console program and engine |
| `Outline.cs`                             | Console program and engine |
| `TrackCollisionDetector.cs`              | Console program and engine |
| `TrackCollisionDetector.ExpandedGrid.cs` | Console program and engine |
| `VehicleCollisionFootprint.cs`           | Engine only                |
| `CollisionManager.cs`                    | Engine only                |

Everything else is generated or written here. Tracks are computed from
parameters and seeds. Vehicles are boxes built in code. The collision manager
names three application types (`Car`, `CarSwitcher`, `JsonUtility`);
`NativeProject/Source/StandIns.cs` supplies stand-ins with only the members the
manager uses.

## Build and run

Double-click `Build.cmd`, then `Run.cmd`. Both require Windows 11 x64, the
.NET 10 SDK, and the portable Godot 4.7.2 .NET installation with export
templates at `%UserProfile%\Program\Godot_v4.7.2-stable_mono_win64`.

`Build.cmd` compiles the console program in Release configuration and exports
`NativeProject` as a release executable with Godot's command line.
`Run.cmd` stops if either build is missing. Each run writes to a new
`MyLogOutput/yyyy-MM-dd_HH-mm-ss` folder. The complete run takes about forty
minutes and opens a window three times, for a few seconds each.

```powershell
.\Run.ps1 -ValidationOnly
.\Run.ps1 -SkipNative
.\Run.ps1 -SkipWindowedFrames -Repetitions 1
.\Run.ps1 -Scale 10 -ValidationOnly
.\Run.ps1 -Stages Measure -Suites cellsize,extent
```

Stages for `-Stages`: `Validation`, `Simulation`, `Measure`, `Variants`,
`ColdStart`.

Single commands, from this folder:

```powershell
.\bin\Release\net10.0\CollisionDetectionFromScratch.exe validate --scale=1 --only=boundary,contact
.\bin\Release\net10.0\CollisionDetectionFromScratch.exe measure --suite=queries --tracks=Circuit
.\bin\Release\net10.0\CollisionDetectionFromScratch.exe measure --suite=extent --affinity=efficiency
.\bin\Release\net10.0\CollisionDetectionFromScratch.exe coldstart --track=Circuit
.\bin\Release\net10.0\CollisionDetectionFromScratch.exe simulate --trials=20000
```

Validation groups for `--only`: `oracle`, `predicate`, `transform`, `input`,
`contact`, `boundary`, `structure`, `differential`, `metamorphic`,
`concurrency`. Measurement suites for `--suite`: `queries`, `paths`, `extent`,
`spacing`, `cellsize`, `breakdown`, `construction`, `single`.

`validate` exits with 0 when every check passes and with 1 otherwise.

## How answers are judged

Four references were written for this harness. None shares code with the
detector.

| Reference           | Arithmetic                        | Judges                                  |
| ------------------- | --------------------------------- | --------------------------------------- |
| Parametric oracle   | Exact integers, 128-bit or larger | Every yes or no answer                  |
| High-precision trig | Fixed point, 512 fractional bits  | Rotation direction and corner rounding  |
| Distance classifier | Binary64 distances                | Answers that are not within a tolerance |
| Integer lattice     | 64-bit integers                   | Touching and collinear contact          |

The parametric oracle solves the two segment equations for their parameters as
exact rational numbers. The detector's predicate compares orientation signs.
The oracle is first checked against hand-derived answers and against a second
exact formulation.

A yes or no answer can be right while the index is wrong, because another edge
may produce the same answer. The comparison therefore also requires every edge
that truly intersects to be among the candidates that the detector's own cell
lookup returns.

Private members of the detector are reached through compiled accessors in
`Source/Detector/DetectorInternals.cs`. They call the detector's own code.

## How time is measured

- One process per suite, pinned to one performance core, at high priority.
- Warmup continues until ten consecutive batches agree within four percent.
  The runtime replaces code while it runs, so a fixed warmup time can end early.
- Twenty-five timed batches of about 25 ms each. Tables report the median.
- Contact counts are checked in every batch, and allocation is measured apart.
- `single` times one call at a time, after rewriting a buffer of 1 to 64 MB.
  That displaces the index and the code from the caches, as the rest of a frame
  does in the application. The timer resolves 100 ns, so only means are useful.
- `coldstart` runs in a new process and times the first construction and the
  first queries, which include compiling the detector.
- The engine-side harness repeats the hot loops inside an exported release
  build and times one query per rendered frame, headless and in a window.

## Mutation check of this harness

`RunMutationTests.ps1` injects one defect at a time into a copy of the detector
under the repository's ignored `Build` folder, rebuilds the harness against it,
and records which validation suites fail. `RunNativeMutationTests.ps1` does the
same for the collision manager and the vehicle footprint, judged by the
engine-side checks in the Godot editor binary without a window.

```powershell
.\RunMutationTests.ps1 -Scale 0.1
.\RunMutationTests.ps1 -Only ReachWithoutRoundingMargin,CenterGridExpansionDropped
.\RunNativeMutationTests.ps1
```
