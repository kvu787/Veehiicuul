# Collision detection review using the existing data and infrastructure

September 29, 2026. Repository revision `3a42483`; the collision code last
changed in `b5726dc` and `cf412b5`. Windows 11 Pro build 26200, Intel Core
Ultra 9 275HX, NVIDIA GeForce RTX 5090 Laptop GPU, .NET SDK 10.0.401 with
runtime 10.0.12, Godot `4.7.2.stable.mono.official.ed1daf0bf`.

This is one of two separate reviews of the same code. This one uses what the
repository already has: its verification harness, its collider files, its
track settings, and its saved measurements. The
[other review](../CollisionReviewFromScratch20260929/Report.md) uses nothing
except the collision source. Neither review's conclusions depend on the other.

No production source and no existing harness source was changed. This folder
adds five scripts. Where the existing harness had to judge data or
code it was not written for, it ran from an isolated copy with the input
swapped; the copies live in ignored folders.

## Findings

| Number | Finding                                                                     | Kind             |
| ------ | --------------------------------------------------------------------------- | ---------------- |
| 1      | Every existing check passes, on all nine existing collider files            | Result           |
| 2      | The existing Track005 exceeds the fast index's limit and is 9 to 16x slower | Design limit     |
| 3      | The existing checks miss 10 of 23 injected defects                          | Verification gap |
| 4      | The timing harness has six weaknesses that change what its numbers mean     | Measurement gap  |
| 5      | Saved kernel timings reproduce within 20 percent, native within 7 percent   | Result           |
| 6      | With the existing car settings, a barrier can be skipped below 124 FPS      | Accepted design  |
| 7      | Smaller observations on code, data, and documents                           | Maintenance      |

### 1. Every existing check passes

| What ran                                   | Processes | Outcome                                           |
| ------------------------------------------ | --------: | ------------------------------------------------- |
| Harness build, forced full rebuild         |         1 | 0 warnings, 0 errors                              |
| Original suite (`Run.cmd`, first stage)    |         3 | Pass                                              |
| Ribeye suite (`--performance --ribeye`)    |         9 | Pass                                              |
| Extended suite (`--performance`)           |         1 | Pass                                              |
| Ribeye suite with each other collider file |         8 | Pass                                              |
| Native Godot analysis (`RunNative.ps1`)    |         3 | Pass, no errors or warnings in the engine log     |
| Planar coordinate suite                    |         1 | Pass                                              |
| Release build and export (`Build.cmd`)     |         1 | 0 warnings, 0 errors                              |
| Exported application, 240 frames           |         1 | Exit code 0; DirectX 12, Forward+, VSync disabled |

Per Ribeye process the harness made 8,096 indexed-against-linear comparisons,
2,358,720 grid-boundary comparisons, 10,056 comparisons with its independent
exact oracle, 32,019 orientation checks, and 11 invalid-input checks. The
extended suite adds Track001 and synthetic fixtures for 11,877 oracle
comparisons in total. Each native process compared 24,576 moving poses across
the six imported cars, the same number of cached repeats, and 800
track-boundary poses against the historical manager from `afcad73`.

Every timed Ribeye query allocated zero bytes, including exact vertex contacts.
The earlier engine log error about the root certificate store did not occur.

The harness was also pointed at each of the eight other collider files, by
replacing the Ribeye data in a copy of its build output. All eight pass their
comparisons. The files themselves satisfy every input rule the
detector enforces:

| Collider file                               | Outlines | Vertices per outline | Edges |        Extent | Shortest edge | Median edge | Longest edge |
| ------------------------------------------- | -------: | -------------------- | ----: | ------------: | ------------: | ----------: | -----------: |
| Tracks/Ribeye/Ribeye_ColliderData.json      |        3 | 512, 224, 64         |   800 | 302.4 x 261.5 |         0.690 |       1.972 |        5.186 |
| Tracks/TrackData/Basic_ColliderData.json    |        2 | 4, 4                 |     8 |   84.0 x 84.0 |        44.880 |      64.428 |       83.976 |
| Tracks/TrackData/ColliderData.example.json  |        3 | 4, 4, 4              |    12 |   20.0 x 20.0 |         2.000 |       2.000 |       20.000 |
| Tracks/TrackData/Track001_ColliderData.json |        3 | 1024, 448, 512       |  1984 | 154.7 x 133.7 |         0.110 |       0.276 |        1.327 |
| Tracks/TrackData/Track002_ColliderData.json |        2 | 225, 152             |   377 |   36.3 x 37.8 |         0.314 |       0.390 |        0.748 |
| Tracks/TrackData/Track003_ColliderData.json |        3 | 1024, 448, 512       |  1984 | 154.7 x 133.7 |         0.110 |       0.276 |        1.327 |
| Tracks/TrackData/Track004_ColliderData.json |        2 | 300, 304             |   604 |  120.0 x 40.0 |         0.263 |       0.644 |        1.589 |
| Tracks/TrackData/Track005_ColliderData.json |        2 | 448, 448             |   896 | 399.6 x 437.6 |         0.504 |       2.053 |        9.313 |
| Testyo/Track009_MiniComb4_ColliderData.json |        2 | 360, 288             |   648 |  145.1 x 76.7 |         0.208 |       0.784 |        3.621 |

No file has an outline with fewer than three vertices, a zero-length edge, a
repeated vertex, or two edges that touch or cross. Every JSON number is exactly
representable in binary32. Every outline winds counterclockwise.

### 2. Track005 exceeds the fast index's limit

The expanded grid is the index that produced the Ribeye speedup. It is built
only when the grid has at most 65,536 cells. Cell size is half the shorter side
of the vehicle footprint, which is 1.5 for the existing cars. The limit
therefore covers about 147,000 square units, including a margin of one vehicle
corner distance on every side. Ribeye uses 56.5 percent of it.

| Collider file                               |    Grid | Cells | Share of cell limit | References | References per cell | Edges longer than a cell | Predicted index      |
| ------------------------------------------- | ------: | ----: | ------------------: | ---------: | ------------------: | -----------------------: | -------------------- |
| Tracks/Ribeye/Ribeye_ColliderData.json      | 207x179 | 37053 |               56.5% |      32134 |                0.87 |                      474 | Expanded grid        |
| Tracks/TrackData/Basic_ColliderData.json    |   61x61 |  3721 |                5.7% |       2112 |                0.57 |                        8 | Expanded grid        |
| Tracks/TrackData/ColliderData.example.json  |   18x18 |   324 |                0.5% |        682 |                2.10 |                       12 | Expanded grid        |
| Tracks/TrackData/Track001_ColliderData.json |  108x94 | 10152 |               15.5% |      63241 |                6.23 |                        0 | Expanded grid        |
| Tracks/TrackData/Track002_ColliderData.json |   29x30 |   870 |                1.3% |      11772 |               13.53 |                        0 | Expanded grid        |
| Tracks/TrackData/Track003_ColliderData.json |  108x94 | 10152 |               15.5% |      63241 |                6.23 |                        0 | Expanded grid        |
| Tracks/TrackData/Track004_ColliderData.json |   85x32 |  2720 |                4.2% |      20198 |                7.43 |                       20 | Expanded grid        |
| Tracks/TrackData/Track005_ColliderData.json | 271x297 | 80487 |              122.8% |      40726 |                0.51 |                      614 | Center grid and tree |
| Testyo/Track009_MiniComb4_ColliderData.json |  102x56 |  5712 |                8.7% |      22772 |                3.99 |                      114 | Expanded grid        |

The prediction repeats the index arithmetic for the production footprint. It
reproduces the grid size and occupied-cell count that the harness reported for
every file, and the 32,134 references that the engine log reports for Ribeye.

The harness confirms Track005's index: a sparse center grid of 167 by 241
logical cells with 237 occupied, and a long-edge tree that holds 614 of the 896
edges in 255 nodes. Its queries cost this much, in nanoseconds, one process per
file:

| Collider file                               | Mixed | Clear | Contact | Vertex contact |
| ------------------------------------------- | ----: | ----: | ------: | -------------: |
| Tracks/Ribeye/Ribeye_ColliderData.json      |  28.9 |  15.6 |    78.0 |           60.7 |
| Tracks/TrackData/Basic_ColliderData.json    |  36.7 |  16.0 |    54.6 |           64.8 |
| Tracks/TrackData/ColliderData.example.json  |  76.8 |  45.7 |    61.9 |           60.5 |
| Tracks/TrackData/Track001_ColliderData.json |  70.5 |  27.5 |   206.5 |           79.7 |
| Tracks/TrackData/Track002_ColliderData.json | 183.1 | 119.3 |   166.0 |           66.1 |
| Tracks/TrackData/Track003_ColliderData.json |  70.1 |  28.1 |   201.3 |           78.8 |
| Tracks/TrackData/Track004_ColliderData.json | 119.8 |  58.2 |   134.9 |           65.2 |
| Tracks/TrackData/Track005_ColliderData.json | 269.2 | 253.8 |   280.4 |          146.5 |
| Testyo/Track009_MiniComb4_ColliderData.json |  86.3 |  41.7 |   138.4 |           67.1 |

Values are medians of the final ten of fifteen batches; finding 4 explains why.

Track005's clear queries cost 253.8 ns against 15.6 ns on Ribeye, a factor of
16. Its mixed queries cost 9.3 times as much. Two causes combine:

- The expanded grid rejects a pose in an empty cell before any trigonometry.
  The fallback computes the rectangle, its bounds, and a cell range first.
- The cell size of 1.5 was chosen for the expanded grid. The fallback grid
  holds only edges no longer than a cell, and Track005's median edge is 2.05.
  So 614 edges go to the tree and the grid holds 282.

The absolute cost stays small: 0.27 microseconds per query. Track005 is not
playable today, because `Main.TrackNames` lists only Ribeye. It is in the
repository and in the exported package.

Among tracks that do receive the expanded grid, cost follows edge density.
Track002 has 13.5 references per cell and costs 119 ns for a clear query;
Ribeye has 0.87 and costs 16 ns.

**Recommendation.** Before a track larger than about 375 units on a side is
added, decide how the index should respond. Growing the cell size until the
grid fits keeps the expanded grid; the saved cell-size sweep shows that Ribeye
at four times the cell size still costs only 39 ns for mixed queries.

### 3. The existing checks miss 10 of 23 injected defects

One defect at a time was injected into a copy of the detector. The unchanged
harness was rebuilt against it and its three suites were run. A suite that
still passes has missed the defect.

| Injected defect                | Code path               | Original suite | Ribeye suite | Extended suite | Effect on answers                                                      |
| ------------------------------ | ----------------------- | -------------- | ------------ | -------------- | ---------------------------------------------------------------------- |
| RadiusIgnoresShorterExtent     | Expanded grid           | Detected       | Detected     | Detected       | Missed contacts on every track                                         |
| RadiusShortByTenth             | Expanded grid           | Missed         | Detected     | Detected       | Missed contacts on every track                                         |
| RadiusShortByHundredth         | Expanded grid           | Missed         | Missed       | Missed         | Missed contacts on every track, rarely                                 |
| RadiusShortByThousandth        | Expanded grid           | Missed         | Missed       | Missed         | Missed contacts on every track, rarely                                 |
| RadiusWithoutRoundingMargin    | Expanded grid           | Missed         | Missed       | Missed         | Missed contacts within one rounding step of a cell boundary            |
| MaximumColumnOmitsRadius       | Expanded grid           | Detected       | Detected     | Detected       | Missed contacts on every track                                         |
| LastCandidateSkipped           | Expanded grid           | Detected       | Detected     | Detected       | Missed contacts on every track                                         |
| SupportsIgnoresBoundsLimit     | Expanded grid           | Missed         | Detected     | Detected       | Missed contacts for footprints larger than the indexed one             |
| CenteredLookupUsesOrigin       | Expanded grid           | Missed         | Detected     | Detected       | Missed contacts for footprints with a shifted origin                   |
| ClosingEdgeTargetsSecondVertex | Edge construction       | Detected       | Detected     | Detected       | Wrong closing edge on every outline                                    |
| RotationDirectionReversed      | Rectangle transform     | Missed         | Detected     | Detected       | Wrong rectangle for every nonzero yaw                                  |
| FourthSideNotTested            | Narrow phase            | Missed         | Detected     | Detected       | Missed contacts on one rectangle side                                  |
| EndpointContactExcluded        | Narrow phase            | Missed         | Missed       | Missed         | Missed contacts only for rectangles rounded to zero width              |
| CrossingUsesEitherStraddle     | Narrow phase            | Missed         | Detected     | Detected       | False contacts on every track                                          |
| TouchingBoundsDoNotOverlap     | Narrow phase            | Detected       | Detected     | Detected       | Missed contacts that touch along an axis                               |
| ErrorBoundFarTooSmall          | Orientation filter      | Missed         | Missed       | Missed         | Wrong signs only for nearly collinear points with wide exponent ranges |
| ExactFallbackReturnsZero       | Orientation filter      | Missed         | Detected     | Detected       | Wrong signs for nearly collinear points                                |
| BoundedIntegerShiftTooWide     | Exact arithmetic        | Missed         | Missed       | Missed         | Wrong signs for exponent spans of 38 to 60                             |
| SubnormalCheckRemoved          | Orientation filter      | Missed         | Missed       | Missed         | None; behavior preserving                                              |
| CenterGridExpansionDropped     | Center grid fallback    | Missed         | Missed       | Missed         | Missed contacts on tracks that use the fallback grid                   |
| TreeVisitsLeftChildOnly        | Long-edge tree fallback | Detected       | Missed       | Detected       | Missed contacts on tracks that use the long-edge tree                  |
| TreeLeafSkipsLastEdge          | Long-edge tree fallback | Missed         | Missed       | Missed         | Missed contacts on tracks that use the long-edge tree                  |
| SparseCellsSkipLastRow         | Center grid fallback    | Missed         | Missed       | Missed         | Missed contacts on tracks that use the sparse fallback grid            |

The last column is a judgement from reading each change, not a measurement.
The ten misses fall into four groups.

**The fallback index is barely checked.** Three of four fallback defects pass
every suite. The default `Run.cmd` runs the fallback only on twelve-edge
fixtures. The extended suite adds tiled and long-edge fixtures, but compares
answers only for the few poses it times. This is the index Track005 receives.

Given Track005 data in place of Ribeye data, the same Ribeye suite detects all
four fallback defects:

| Injected defect            | Code path               | Ribeye suite | First failure               |
| -------------------------- | ----------------------- | ------------ | --------------------------- |
| Unmodified                 | None                    | Passes       | none                        |
| CenterGridExpansionDropped | Center grid fallback    | Detected     | Ribeye broad-phase mismatch |
| TreeVisitsLeftChildOnly    | Long-edge tree fallback | Detected     | Ribeye broad-phase mismatch |
| TreeLeafSkipsLastEdge      | Long-edge tree fallback | Detected     | Ribeye broad-phase mismatch |
| SparseCellsSkipLastRow     | Center grid fallback    | Detected     | Ribeye broad-phase mismatch |

The checks are adequate. The default data never reaches the fallback index.

**A reach that is slightly too short passes.** A reach 0.1 too short is caught
by the grid-boundary sweep. A reach 0.01 too short is not. The sweep uses seven
fixed yaw angles, and none points a vehicle corner straight along a grid axis,
which is the only direction in which the full reach is needed. The rounding
margin is untested for the same reason.

**The exact-arithmetic tests do not reach the arithmetic.** The test for
"both sides of the Int128 fallback limit" places its three points on the line
Y = X. Every such determinant is zero whatever the integers contain, because X
equals Y in each point. The cases moved off the line by one step are decided by
the binary64 filter and never reach the integers. Raising the limit from 37 to
60, which overflows 64-bit operands, passes. So does a filter bound of 1e-30.

**Two defects are close to harmless.** Excluding a collinear point at the low
X end of a segment changes an answer only when all four orientation signs are
zero. A rectangle with area always has another side and edge pair that shows
the contact, so this needs a rectangle rounded to zero width. Removing the
subnormal check changes nothing: binary64 holds every binary32 value exactly,
and no product of two differences can underflow.

**Recommendation.** Add fallback comparisons on a real fallback track, poses
with a corner at full reach on cell boundaries, and wide-exponent orientation
cases that are off the diagonal and moved by one step in the smallest point.

### 4. The timing harness has six weaknesses

**The run transcript omits the program output.** `Run.ps1` records a
transcript, and the readme says each run saves it. Today's `Run.log` is 986
bytes: a header, a results path, a footer. Windows PowerShell transcripts do
not include the output of console programs started this way. The pass lines
and printed timings are lost; only the JSON samples survive. The console files
in [Measurements](Measurements) were captured by redirection.

**Warmup can end before the runtime finishes optimizing.** The first benchmark
of a process warms up for 150 ms. With the smaller collider files that is not
enough:

| Collider file | First batches, ns per query | Later batches, ns per query | Printed batch p95 |
| ------------- | --------------------------- | --------------------------- | ----------------: |
| Track002      | 919, 916, 918, 946, 915     | 172 to 222                  |            946 ns |
| Track004      | 649, 626, 626, 603, 200     | 116 to 136                  |            649 ns |
| Basic         | 129, 124, 123, 73           | 36 to 41                    |            129 ns |

Batch size was calibrated during the slow phase, so later batches last about
3 ms instead of the intended 15 ms. Ribeye is unaffected only because its
2.36 million boundary comparisons run first.

**The linear reference is not stable within a process.** The same 4,096 Ribeye
poses take 1.15 microseconds per linear query in the first benchmark and 0.87
to 0.90 in the cell-size sweep that follows. With tiered compilation disabled
both are 0.87. The printed ratio of 40 for mixed queries is about 31 against
the settled value.

**Construction time measures code that is still being optimized.** Twenty
constructions are too few for the runtime to finish. The default median is
0.489 ms and the median with tiering disabled is 0.097 ms. Neither is what a
track load pays, which is one first call including compilation.

**Native contact counts cannot confirm agreement.** The native timing loops
print 93,379 to 93,390 contacts for the optimized manager and 93,372 to 93,381
for the historical one. The warmup runs for a fixed time, so each timed loop
starts at a different pose. Agreement is established elsewhere, by the direct
comparison of 24,576 poses.

**The standalone footprint is not the production footprint.** The harness
hard-codes (-1.5, -2.992522, 1.5, 3). The imported cars measure
(-1.5000004, -2.9925215, 1.5000001, 3.0000002), so production cells are
1.500000238 wide. The boundary sweep therefore probes grid lines that
production never has. The native checks use the real footprint, with random
poses only.

### 5. Saved measurements reproduce

Kernel medians, six processes today against three saved processes:

| Ribeye query                   | Saved median |          Saved range | Today median |          Today range | Today vs saved | No tiering | No tiering / default |
| ------------------------------ | -----------: | -------------------: | -----------: | -------------------: | -------------: | ---------: | -------------------: |
| Ribeye/Mixed                   |      33.7 ns |   32.6 ns to 35.4 ns |      28.4 ns |   27.7 ns to 29.9 ns |         -15.7% |    38.4 ns |                1.35x |
| Ribeye/ClearInsideBounds       |      18.4 ns |   16.9 ns to 21.9 ns |      15.8 ns |   15.6 ns to 16.6 ns |         -14.0% |    19.4 ns |                1.23x |
| Ribeye/Contact                 |      97.5 ns |  81.4 ns to 106.5 ns |      78.2 ns |   77.7 ns to 79.5 ns |         -19.8% |   102.2 ns |                1.31x |
| Ribeye/ExactVertexContact      |      65.7 ns |   65.5 ns to 78.5 ns |      61.1 ns |   60.8 ns to 62.4 ns |          -6.9% |    83.8 ns |                1.37x |
| Ribeye/Spawn                   |      11.0 ns |    9.6 ns to 12.1 ns |       9.1 ns |     8.6 ns to 9.5 ns |         -17.1% |    10.6 ns |                1.17x |
| Ribeye/OutsideBounds           |       9.4 ns |     9.0 ns to 9.7 ns |       9.2 ns |     8.5 ns to 9.5 ns |          -1.8% |    10.3 ns |                1.11x |
| Ribeye/HugeContainingRectangle |     15.82 µs | 14.96 µs to 16.06 µs |     14.66 µs | 14.39 µs to 14.74 µs |          -7.3% |   14.67 µs |                1.00x |

Today's medians are 2 to 20 percent lower, and today's ranges are narrower.
Process medians within today's session span at most 12 percent. The saved
conclusions hold. Absolute values carry about 20 percent of uncertainty between
sessions.

Disabling tiered compilation makes queries 11 to 37 percent slower. It removes
the profile-guided optimization that the default runtime applies.

Native operations, three processes today against three saved:

| Native operation              | Saved median |          Saved range | Today median |          Today range | Today vs saved | Bytes per call |
| ----------------------------- | -----------: | -------------------: | -----------: | -------------------: | -------------: | -------------: |
| OptimizedMovingQuery          |      29.6 ns |   29.4 ns to 29.8 ns |      29.2 ns |   29.0 ns to 31.4 ns |          -1.4% |            0.0 |
| OptimizedApplyAndQuery        |     112.6 ns | 110.2 ns to 113.2 ns |     107.2 ns | 105.7 ns to 108.2 ns |          -4.8% |            0.0 |
| BaselineApplyAndQuery         |     749.8 ns | 747.4 ns to 841.7 ns |     717.5 ns | 698.8 ns to 735.1 ns |          -4.3% |            0.0 |
| ApplyOnly                     |      76.0 ns |   74.8 ns to 77.3 ns |      73.5 ns |   72.0 ns to 77.9 ns |          -3.3% |            0.0 |
| OptimizedContactApplyAndQuery |     180.8 ns | 177.2 ns to 197.9 ns |     175.2 ns | 169.4 ns to 178.1 ns |          -3.1% |            0.0 |
| BaselineContactApplyAndQuery  |     763.0 ns | 761.4 ns to 807.9 ns |     744.7 ns | 728.7 ns to 745.7 ns |          -2.4% |            0.0 |
| OptimizedStationarySpawn      |       1.5 ns |     1.4 ns to 1.6 ns |       1.6 ns |     1.3 ns to 1.6 ns |          +6.7% |            0.0 |
| BaselineStationarySpawn       |     509.6 ns | 490.8 ns to 549.4 ns |     498.9 ns | 494.5 ns to 507.2 ns |          -2.1% |            0.0 |
| OptimizedStationaryContact    |       1.3 ns |     1.3 ns to 1.3 ns |       1.3 ns |     1.3 ns to 1.3 ns |          +0.0% |            0.0 |
| BaselineStationaryContact     |     520.4 ns | 502.7 ns to 556.9 ns |     486.6 ns | 476.1 ns to 495.0 ns |          -6.5% |            0.0 |

The moving write-and-query operation is 6.69 times faster than the historical
manager today, against 6.66 when saved. The boundary operation is 4.25 times
faster, against 4.22.

The cell-size sweep keeps its order: 1.5 is fastest for the expanded grid.

| Cell size |    Grid | Occupied cells | Saved mixed median | Today mixed median |        Today range |
| --------: | ------: | -------------: | -----------------: | -----------------: | -----------------: |
|       1.5 | 207x179 |           8274 |            34.1 ns |            29.4 ns | 27.8 ns to 29.8 ns |
|         3 |  104x90 |           2413 |            37.1 ns |            33.1 ns | 32.0 ns to 34.5 ns |
|       4.5 |   69x60 |           1228 |            41.3 ns |            36.3 ns | 35.5 ns to 38.4 ns |
|         6 |   52x45 |            772 |            46.8 ns |            39.3 ns | 38.4 ns to 43.6 ns |
|         9 |   35x30 |            406 |            53.0 ns |            46.5 ns | 45.0 ns to 48.0 ns |
|        12 |   26x23 |            254 |            61.6 ns |            50.7 ns | 50.4 ns to 54.7 ns |

Across the three generations of the detector, Ribeye mixed queries went from
318.9 ns with the center grid to 33.7 ns when the expanded grid was saved and
28.4 ns today.

### 6. Sampling limits with the existing car settings

The detector tests one pose per frame, and `CollisionManager` states that a
fast vehicle can cross a barrier between two poses. That is accepted behavior.
This section only measures how close the existing data is to it.

A vehicle that moves farther than its own length in one frame can clear a
barrier edge without any sampled pose touching it. The collision rectangle is
5.9925 long. No existing settings file sets `VelocityLimiter`, in any of the
seven tracks, so speed is bounded only by how long a car can accelerate.

On Ribeye the longest straight run a clear car can make before touching a
barrier is 275.5 units; from the spawn pose it is 214.0. Median over sampled
clear poses is 13.5.

| Car             | Forward acceleration | Top speed, longest run | Longest safe frame | Frames per second |
| --------------- | -------------------: | ---------------------: | -----------------: | ----------------: |
| SlopeCarGreen   |                   10 |                     74 |            80.7 ms |                12 |
| SlopeCarBlue    |                   35 |                    139 |            43.2 ms |                23 |
| SlopeCarRed     |                   70 |                    196 |            30.5 ms |                33 |
| SlopeCarYellow  |                  140 |                    278 |            21.6 ms |                46 |
| SlopeCarCyan    |                  280 |                    393 |            15.3 ms |                66 |
| SlopeCarMagenta |                 1000 |                    742 |             8.1 ms |               124 |

Top speed assumes full forward acceleration from rest over the longest run.
The longest safe frame is the vehicle length divided by that speed.

The magenta car reaches one vehicle length per frame after 65 units of
straight at 60 FPS and after 259 units at 120 FPS. The default car, yellow,
needs a frame longer than 21.6 ms. With VSync off and no frame limit the
application normally runs far above these rates, so the exposure is a single
long frame at high speed.

A car that has crossed is not reported afterwards. The detector tests the
rectangle's perimeter against the outlines, so a car entirely outside the
track, or entirely inside a barrier outline, is clear.

### 7. Smaller observations

Code:

- One cell size serves both index families. Finding 2 shows the cost.
- The long-edge tree allocates `2 * EdgeCount - 1` nodes and uses far fewer,
  because a leaf holds up to eight edges. Track005 allocates 1,227 and uses 255.
- `Guard` and `CollisionMath` both define `IsFinite` and `ThrowIfNotFinite`.
  `CollisionMath.BitIncrement` and `BitDecrement` repeat `Math.BitIncrement`
  and `Math.BitDecrement`, which the expanded grid calls directly.
- `OccupiedGridCellCount` and `OccupiedCellCount` are the same value, as are
  `OutlierEdgeCount` and `OversizedEdgeCount`. `BroadQueryCellThreshold` stays
  zero when the expanded grid is used.
- `IsCollidingLinear` shares the rectangle transform and the segment predicate
  with the indexed query. Agreement between the two says nothing about either
  shared part. Only the harness's independent oracle does.
- The front shortening of 0.165 is an absolute length. It does not follow a
  vehicle's scale.

Data and packaging:

- The exported package stores seven collider and six settings files under
  `Tracks/TrackData` that the application cannot load. `CollisionManager`
  reads `res://Tracks/{name}/{name}_ColliderData.json`, and only Ribeye has
  that layout. They add about 0.75 MB to a 2.3 MB package.
- Seven collider files carry `FormatVersion` and `CoordinateSystem` keys.
  Ribeye and Track009 do not. The detector ignores both keys.

Documents:

- The first report's recommendation of six-unit cells applied to the center
  grid. The second report says so; the first still reads as current.
- The verification readme says the orientation checks cover both sides of the
  128-bit limit. Finding 3 shows that they do not exercise it.

## Method

**Swapping data.** The harness reads `Ribeye_ColliderData.json` from its build
output. For each other collider file, the build output was copied to a
temporary folder and that file was replaced. The harness was not rebuilt. Its
labels still say Ribeye; the file names in
[Measurements/PerTrack](Measurements/PerTrack) say which track each run judged.
The Ribeye spawn pose in those runs is meaningless and is not reported.

**Injecting defects.** `RunMutationTests.ps1` copies the detector, the harness,
and both data files into the repository's ignored `Build` folder. Each defect
is a text replacement that must match exactly once. The harness is rebuilt and
its suites run with a time limit. Nothing outside the copy is touched.

**Predicting the index.** `AnalyzeTrackData.py` repeats the expanded grid's
sizing arithmetic, including its outward rounding, in binary64.

**Sampling limits.** `AnalyzeTunneling.py` marches the production footprint
along its heading in steps of 0.25 from 115,189 clear poses. It uses binary64
geometry and is an estimate.

## Limits

- Timings come from one machine, without core pinning or priority changes, as
  the existing harness runs. Other activity on the machine affects them.
- Per-track timings are one process each.
- The mutation check covers 23 defects chosen by reading the code. A suite
  that detects all of them could still miss others.
- The existing native harness runs optimized C# inside a debug engine session,
  not the exported release build. Rendering and the frame loop are excluded.
- No recorded gameplay was available. Pose sets are uniform or constructed.

## Reproduction

```powershell
cd Veehiicuul_Godot_CSharp\Veehiicuul\Verification\CollisionDetection
.\Build.cmd
.\Run.cmd
.\Run.cmd -DisableTieredCompilation -Repetitions 3
.\bin\Release\net10.0\CollisionDetectionVerification.exe --performance --output=MyLogOutput\AllFixtures.json
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\RunNative.ps1

cd ..\..\Documentation\CollisionReviewUsingExistingInfrastructure20260929\Analysis
python SummarizeMeasurements.py
python AnalyzeTrackData.py
python AnalyzeTunneling.py
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\RunMutationTests.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\RunMutationTests.ps1 -SkipExtendedSuite -SubstituteRibeyeData Tracks\TrackData\Track005_ColliderData.json -Only Unmodified,CenterGridExpansionDropped,TreeVisitsLeftChildOnly,TreeLeafSkipsLastEdge,SparseCellsSkipLastRow
python SummarizeMutations.py
```

Saved evidence:

| File or folder                                   | Content                                              |
| ------------------------------------------------ | ---------------------------------------------------- |
| [Analysis/Summary.md](Analysis/Summary.md)       | Every timing table, generated                        |
| [Analysis/TrackData.md](Analysis/TrackData.md)   | Collider files, predicted index, per-track cost      |
| [Analysis/Tunneling.md](Analysis/Tunneling.md)   | Sampling limits on Ribeye                            |
| [Analysis/Mutations.md](Analysis/Mutations.md)   | Injected defects and outcomes                        |
| `Measurements/KernelDefault1.json` to `6.json`   | Six default Ribeye processes                         |
| `Measurements/KernelNoTiering1.json` to `3.json` | Three processes with tiering disabled                |
| `Measurements/AllFixtures.json`                  | Extended suite                                       |
| `Measurements/Native1.txt` to `3.txt`            | Three native Godot processes                         |
| [Measurements/PerTrack](Measurements/PerTrack)   | The Ribeye suite on each other collider file         |
| `Measurements/MutationResults.json`              | Raw mutation outcomes and first failures             |
| `Measurements/MutationResultsTrack005.json`      | The same, with Track005 data in place of Ribeye data |
| `Measurements/ReleaseStartup.txt`                | Log of the exported application                      |
