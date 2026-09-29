# Collision detection review from scratch

September 29, 2026. Repository revision `3a42483`; the collision code last
changed in `b5726dc` and `cf412b5`. Windows 11 Pro build 26200, Intel Core
Ultra 9 275HX with 8 performance and 16 efficiency cores, NVIDIA GeForce RTX
5090 Laptop GPU, .NET SDK 10.0.401 with runtime 10.0.12, Godot
`4.7.2.stable.mono.official.ed1daf0bf`.

This is one of two separate reviews of the same code. This one takes eight
source files from the application and nothing else. The
[other review](../CollisionReviewUsingExistingInfrastructure20260929/Report.md)
uses the repository's existing harness, data, and measurements. Neither
review's conclusions depend on the other.

No production source was changed. The harness that produced every number here
is in
[Verification/CollisionDetectionFromScratch](../../Verification/CollisionDetectionFromScratch).

## Findings

| Number | Finding                                                                                      | Kind            |
| ------ | -------------------------------------------------------------------------------------------- | --------------- |
| 1      | No wrong answer in 316 million checks; the checks catch every injected defect that matters   | Result          |
| 2      | A query costs 6 to 160 ns on the reference circuit, and about 2 µs at most with cold caches  | Result          |
| 3      | Past 65,536 cells the detector changes index and costs 2 to 16 times as much                 | Design limit    |
| 4      | One pose per frame misses small obstacles long before it misses walls                        | Accepted design |
| 5      | The rectangle is where the engine places the mesh; the front enters a barrier by 0.165 first | Result          |
| 6      | One index serves every vehicle; it is built for the largest and slows the smaller ones       | Design limit    |
| 7      | First use costs 4 to 17 ms to build the index and 0.4 to 2 ms for the first query            | Result          |
| 8      | Invalid input is refused; two messages name the wrong cause                                  | Minor           |
| 9      | Forty percent of the detector serves the fallback index                                      | Maintenance     |
| 10     | Smaller observations                                                                         | Maintenance     |

Times are in nanoseconds (ns), microseconds (µs), and milliseconds (ms). Two
sets of timings appear, and every table says which one it shows:

| Name                | Placement                                     | Priority | Machine                                       |
| ------------------- | --------------------------------------------- | -------- | --------------------------------------------- |
| Timing run          | One performance core, logical processor 23    | High     | Not recorded                                  |
| Efficiency-core run | Efficiency cores, logical processors 19 to 21 | Normal   | In use; a game occupied the performance cores |

Each value is the median of three processes unless stated.

### 1. No wrong answer was found

Every answer of the detector was compared with a reference that shares no code
with it. None differed.

| Run                                       | Threads |      Checks | Failures |
| ----------------------------------------- | ------: | ----------: | -------: |
| Standard                                  |      22 |  34,668,279 |        0 |
| Extended, ten times the random poses      |      22 | 281,528,924 |        0 |
| Engine-side, in an exported release build |       1 |      64,311 |        0 |
| Total                                     |         | 316,261,514 |        0 |
| Standard, repeated with the final harness |       3 |  22,456,279 |        0 |
| Engine-side, repeated                     |       1 |      64,311 |        0 |

The repeated run has fewer checks because the concurrency checks scale with
the number of threads.

| Group        | Suites | Checks, standard | Checks, extended | Compared with                                                |
| ------------ | -----: | ---------------: | ---------------: | ------------------------------------------------------------ |
| Oracle       |      3 |          793,374 |        7,093,380 | Hand-derived answers and a second exact formulation          |
| Predicate    |      4 |        2,101,149 |       21,011,363 | Signs and segment answers from exact integers                |
| Transform    |      4 |          800,032 |        8,000,032 | Corners from 512-bit trigonometry                            |
| Input        |      4 |              118 |              118 | Expected refusals; exact answers for extreme valid input     |
| Contact      |      2 |        1,532,388 |        1,532,388 | 64-bit integer arithmetic on lattice poses                   |
| Boundary     |      3 |        5,307,778 |        6,087,403 | The exact oracle, on poses built to need the full reach      |
| Structure    |      4 |          328,240 |          328,240 | Index contents computed directly                             |
| Differential |     14 |        5,629,200 |       56,292,000 | The exact oracle, on 14 tracks with 8 footprints             |
| Metamorphic  |      6 |        4,032,000 |       40,320,000 | The exact oracle, after changes that must not change answers |
| Concurrency  |      2 |       14,144,000 |      140,864,000 | The same queries on one thread                               |

What the groups establish, with counts from the standard run:

- **The segment test is exact.** The checks include 286,400 triples that are
  exactly collinear, triples one representable step from collinear, values
  across the whole binary32 range, and exponent spans on both sides of the
  limit of the 128-bit route.
- **Corners are rounded correctly.** See the table below.
- **Touching is contact.** On 153,228 lattice poses, 50,034 are contacts and
  4,806 of those only touch a barrier without crossing it. All agree with
  integer arithmetic.
- **The index holds every edge that a pose can reach.** In 746,928 poses a
  corner points along a grid axis at a barrier exactly one reach away, from the
  first representable position of a cell. In 724,451 of them the corner lands
  exactly on the barrier's row. Every list of candidates contained the edge.
  A direct computation for all 197,335 cells of nine tracks found no edge
  missing from any cell.
- **Every index family and every lookup route agrees with the oracle.** The
  14 tracks cover the expanded grid, the dense and the sparse center grid, the
  long-edge tree, and the tree alone. The 8 footprints cover the origin lookup,
  the center lookup, and the full scan.
- **Answers do not depend on the index.** Other cell sizes, other index
  footprints, and another order of the same edges give the same answers.
- **Simultaneous queries give the answers of one thread.** The detector makes
  no promise about threads. This is what the implementation does.

| Yaw                       | Coordinates | Not the nearest binary32 value | Largest error, in spacings of binary32 |
| ------------------------- | ----------: | -----------------------------: | -------------------------------------: |
| Within one turn           |     480,000 |                              0 |                              0.4999986 |
| Up to 1,000 radians       |     160,000 |                              0 |                              0.4999999 |
| Any finite binary32 value |     160,000 |                            333 |                           0.5000000013 |

The detector is exact for the rectangle whose corners are rounded to binary32.
For any yaw that a vehicle can have, each rounded coordinate is the binary32
value nearest to the true one. For enormous yaw values the platform's sine and
cosine are slightly less accurate, and 0.2 percent of coordinates are the
second nearest value.

#### Injected defects

One defect at a time was injected into a copy of the collision code. The
harness was rebuilt against the copy and run with one tenth of the standard
number of random poses. A harness that still passes has missed the defect.

| Area                    | Injected | Detected | Not detected          |
| ----------------------- | -------: | -------: | --------------------- |
| Expanded grid           |       15 |       15 | none                  |
| Edge construction       |        2 |        2 | none                  |
| Rectangle transform     |        4 |        4 | none                  |
| Narrow phase            |        7 |        6 | TouchingIsNotCrossing |
| Orientation arithmetic  |        6 |        5 | SubnormalCheckRemoved |
| Fallback center grid    |        5 |        5 | none                  |
| Fallback long-edge tree |        4 |        4 | none                  |
| Full scan               |        1 |        1 | none                  |
| Input handling          |        3 |        3 | none                  |
| Manager pose            |        2 |        2 | none                  |
| Manager bounds          |        3 |        3 | none                  |
| Manager cache           |        3 |        3 | none                  |
| Vehicle footprint       |        5 |        5 | none                  |
| Scaled bounds           |        3 |        3 | none                  |
| Total                   |       63 |       61 |                       |

The last five areas were judged by the engine-side checks. Every defect is
listed in [Analysis/Summary.md](Analysis/Summary.md).

Neither of the two undetected defects changes an answer:

- `TouchingIsNotCrossing` makes the last line of the segment test require
  strictly opposite signs. That line is reached only after every zero sign has
  been handled. A zero sign that remains belongs to a point on the other
  segment's line and outside the segment. Then the two ends of the other
  segment are on one side, so both versions answer no.
- `SubnormalCheckRemoved` removes the six checks that send subnormal
  coordinates to exact arithmetic. Binary64 holds every binary32 value, and no
  product of two differences of binary32 values can underflow or overflow in
  binary64. The filter's error bound therefore holds for these inputs as well.

Three detected defects say something about the detector itself:

| Injected defect            | Failed checks | What it shows                                                 |
| -------------------------- | ------------: | ------------------------------------------------------------- |
| ReachShortByOneMillionth   |         2,701 | The reach has no slack beyond its rounding margin             |
| ReachWithoutRoundingMargin |            54 | The rounding margin is needed; without it contacts are missed |
| SparseCellKeysCollide      |             1 | Only a track built for the purpose reaches this code          |

Without the rounding margin the affected poses answer clear where the exact
answer is contact. Random poses did not find them. The poses that did were
built with a corner at full reach on a cell boundary.

### 2. What a query costs

Hot loops over 4,096 poses, timing run:

| Track                | Index                                 | Edges | LapCenter | LapBesideBarrier | UniformClear | UniformMixed | NearMiss | Contact | Outside | BusiestClearCell |
| -------------------- | ------------------------------------- | ----: | --------: | ---------------: | -----------: | -----------: | -------: | ------: | ------: | ---------------: |
| Circuit              | expanded grid                         |   691 |    6.4 ns |           145 ns |      16.0 ns |      29.3 ns |   160 ns |  119 ns |  8.8 ns |           111 ns |
| CircuitFine          | expanded grid                         |  4607 |    6.5 ns |           556 ns |      22.7 ns |      52.2 ns |   360 ns |  214 ns |  9.0 ns |           505 ns |
| CircuitCompact       | expanded grid                         |   805 |   17.8 ns |           384 ns |      63.9 ns |       105 ns |   282 ns |  175 ns |  8.6 ns |           329 ns |
| Scattered            | expanded grid                         |  2880 |       n/a |              n/a |       140 ns |       145 ns |   214 ns |  143 ns |  9.8 ns |           177 ns |
| RectangularSegmented | expanded grid                         |   640 |       n/a |              n/a |      11.0 ns |      27.0 ns |  66.0 ns |  116 ns |  9.4 ns |          47.5 ns |
| Rectangular          | expanded grid                         |     8 |       n/a |              n/a |      10.3 ns |      20.5 ns |  51.1 ns | 73.1 ns |  9.3 ns |          39.6 ns |
| CircuitFar           | expanded grid                         |   691 |    6.4 ns |           145 ns |      16.1 ns |      29.4 ns |   160 ns |  121 ns |  9.9 ns |           110 ns |
| CircuitWide          | dense center grid with long-edge tree |  1257 |   98.0 ns |           222 ns |       100 ns |       113 ns |   298 ns |  219 ns | 46.3 ns |              n/a |
| CircuitVast          | long-edge tree                        |  6032 |    109 ns |           214 ns |      81.8 ns |      84.1 ns |   312 ns |  228 ns | 47.5 ns |              n/a |
| ScatteredRemote      | sparse center grid                    |   432 |       n/a |              n/a |       318 ns |       320 ns |   520 ns |  289 ns | 47.0 ns |              n/a |
| Crowded              | dense center grid                     | 55296 |       n/a |              n/a |      1.12 µs |       545 ns |   376 ns |  521 ns | 47.7 ns |              n/a |

| Workload         | Poses                                                    |
| ---------------- | -------------------------------------------------------- |
| LapCenter        | A lap along the centerline, 0.25 units per frame         |
| LapBesideBarrier | A lap with the vehicle's side 0.1 from the outer barrier |
| UniformClear     | Random poses over the track's bounds that are clear      |
| UniformMixed     | Random poses over the track's bounds, clear or not       |
| NearMiss         | Clear poses within 0.5 of a barrier                      |
| Contact          | Colliding poses within 1.0 of a barrier                  |
| Outside          | Poses beyond the track's bounds                          |
| BusiestClearCell | The clear pose whose cell lists the most edges, repeated |

No query allocates memory. Process medians differ by at most 17 percent, and
batches within one measurement by at most 24 percent.

A vehicle that drives along the middle of the track pays 6 ns per query. The
expanded grid finds an empty cell and answers before it computes any sine. A
vehicle that scrapes along a barrier pays 145 ns on the reference circuit.

**Cost follows the number of edges within reach.** Fixtures with a known
number of candidate edges give these costs:

| Candidate edges | All rejected by their bounding box | All need orientation tests |
| --------------: | ---------------------------------: | -------------------------: |
|               3 |                            27.3 ns |                     141 ns |
|              12 |                            39.8 ns |                     478 ns |
|              48 |                            86.6 ns |                    1.78 µs |
|             192 |                             311 ns |                    7.03 µs |

An edge that its bounding box rejects costs 1.5 ns. An edge that needs
orientation tests against all four sides costs 36 ns. Short barrier edges
therefore cost more than long ones: CircuitFine has the shape of Circuit with
edges of 0.3 instead of 2.0, and its near misses cost 360 ns instead of 160.

| Orientation route                     |  Median | Bytes allocated |
| ------------------------------------- | ------: | --------------: |
| The binary64 filter decides           |  5.4 ns |               0 |
| Exactly collinear, 128-bit integers   | 16.3 ns |               0 |
| Exponent span of 30, 128-bit integers | 16.1 ns |               0 |
| Exponent span of 60, large integers   |  195 ns |             456 |

The large-integer route needs coordinates whose magnitudes differ by a factor
above 2 to the power 37 and three points that are almost collinear. No timed
query took it, because none allocated.

**Cold caches raise the cost to about 2 µs.** The hot loops keep the index and
the code in the processor's caches. In the application the rest of the frame
runs between two queries. Single calls were therefore timed after a buffer had
been rewritten. Timing run, mean per call:

| Track       | Workload  | Nothing rewritten |    1 MB |    4 MB |  16 MB |   64 MB |
| ----------- | --------- | ----------------: | ------: | ------: | -----: | ------: |
| Circuit     | LapCenter |           12.1 ns | 17.7 ns | 93.7 ns | 222 ns |  648 ns |
| Circuit     | NearMiss  |            161 ns |  227 ns |  383 ns | 637 ns | 1.35 µs |
| Circuit     | Contact   |            123 ns |  170 ns |  307 ns | 590 ns | 1.39 µs |
| CircuitWide | LapCenter |            108 ns |  142 ns |  351 ns | 490 ns | 1.28 µs |
| CircuitWide | NearMiss  |            305 ns |  384 ns |  599 ns | 823 ns | 1.97 µs |
| CircuitWide | Contact   |            228 ns |  285 ns |  503 ns | 732 ns | 1.88 µs |

Rewriting 64 MB displaces every cache level. It is a deliberately harsh upper
bound. At 1,000 frames per second, 2 µs is 0.2 percent of a frame.

**The exported engine build runs the detector at the same speed.**
Efficiency-core run, hot loops:

| Workload                  | Console program | Detector in the engine | Manager in the engine |
| ------------------------- | --------------: | ---------------------: | --------------------: |
| LapCenter                 |          9.2 ns |                 9.2 ns |               35.0 ns |
| NearMiss                  |          184 ns |                 186 ns |                197 ns |
| Contact                   |          137 ns |                 138 ns |                150 ns |
| Stationary, the same pose |             n/a |                 9.1 ns |                1.4 ns |

The detector costs the same within 1 percent in both programs. A repeated pose
costs 1.4 ns, because the manager keeps its last answer. The manager's other
costs are higher than the detector's because its index is built for a larger
vehicle, not because of the manager. Finding 6 shows that.

One query per frame was also timed inside the running engine, without a
window. It cost 1.9 µs on average, 4.1 µs at the 99th percentile, and 76 µs at
most over 18,000 frames. Godot sleeps 6.9 ms in every frame when it cannot
draw, so each of these queries follows a sleep and finds cold caches. The
numbers agree with the harsh end of the table above. A frame with a window was
not timed; see [Limits](#limits).

**Efficiency cores and runtime settings.** Timing run, against its default:

| Setting or core                         | Queries on Circuit | Queries on CircuitWide |
| --------------------------------------- | -----------------: | ---------------------: |
| Dynamic profile-guided optimization off |     1.15x to 1.29x |         1.06x to 1.16x |
| Tiered compilation off                  |     1.14x to 1.29x |         1.06x to 1.15x |
| Efficiency core                         |     1.11x to 1.46x |         1.19x to 1.22x |
| Not pinned                              |     1.01x to 1.38x |         1.19x to 1.22x |

A process that is not pinned often ran at the speed of an efficiency core.

### 3. Past 65,536 cells the detector changes index

The expanded grid is built only when it needs at most 65,536 cells and
1,048,576 edge references. Cell size is half the shorter side of the index
footprint, 1.5 for a vehicle 3 wide. The grid covers the track's bounds
widened by the vehicle's reach on every side. The cell limit is therefore
reached at 147,456 square units, which is a square track of about 377 by 377.
Beyond a limit the detector builds a center grid and a tree of long edges.

The same circuit shape at growing size, timing run:

| Mean radius | Edges | Index                                  | Grid cells | LapCenter | UniformClear | NearMiss | Contact |
| ----------: | ----: | -------------------------------------- | ---------: | --------: | -----------: | -------: | ------: |
|          40 |   251 | expanded grid                          |      5,293 |    6.5 ns |      36.0 ns |   162 ns |  120 ns |
|         110 |   691 | expanded grid                          |     29,172 |    6.4 ns |      16.0 ns |   160 ns |  119 ns |
|         165 | 1,037 | expanded grid                          |     61,472 |    6.4 ns |      13.4 ns |   158 ns |  119 ns |
|         170 | 1,068 | expanded grid                          |     65,240 |    6.5 ns |      13.3 ns |   157 ns |  119 ns |
|         180 | 1,131 | dense center grid with long-edge tree  |      1,591 |    105 ns |       101 ns |   308 ns |  219 ns |
|         300 | 1,885 | dense center grid with long-edge tree  |      3,685 |    109 ns |      94.3 ns |   314 ns |  223 ns |
|       1,200 | 7,540 | sparse center grid with long-edge tree |     47,970 |    115 ns |      94.5 ns |   380 ns |  260 ns |

Cost does not grow with the track while the expanded grid is in use. Six
percent more radius, from 170 to 180, changes the index. The lap along the
centerline then costs 16 times as much, clear poses 7.6 times, near misses 2.0
times, and contacts 1.8 times. With cold caches the difference is smaller: 2.0
times for the lap and 1.5 times for near misses at 64 MB.

The fallback is slowest when barrier edges are shorter than a cell, because
they all go to the sparse center grid. A circuit of radius 200 with edges of
1.0 costs 290 ns per query along the centerline and 847 ns for a near miss.

**A larger cell keeps the expanded grid.** The cell size was varied on the
four tracks that receive the fallback. Efficiency-core run; the first row of
each pair is what the detector builds today:

| Track           | Cell size | Index                                 | Grid cells | References |   Clear | NearMiss | Contact |
| --------------- | --------: | ------------------------------------- | ---------: | ---------: | ------: | -------: | ------: |
| CircuitWide     |       1.5 | dense center grid with long-edge tree |         75 |         16 |  126 ns |   380 ns |  276 ns |
| CircuitWide     |         3 | expanded grid                         |     21,285 |     16,903 | 11.6 ns |   189 ns |  141 ns |
| CircuitVast     |       1.5 | long-edge tree                        |          0 |          0 |  143 ns |   426 ns |  313 ns |
| CircuitVast     |        12 | expanded grid                         |     44,940 |     17,258 | 52.7 ns |   197 ns |  152 ns |
| ScatteredRemote |       1.5 | sparse center grid                    | 46,963,600 |        432 |  369 ns |   613 ns |  347 ns |
| ScatteredRemote |        48 | expanded grid                         |     46,225 |        594 | 17.4 ns |   183 ns |  125 ns |
| Crowded         |       1.5 | dense center grid                     |     10,609 |     55,296 | 1.51 µs |   523 ns |  656 ns |
| Crowded         |         3 | expanded grid                         |      2,916 |    616,178 | 1.59 µs |   528 ns |  701 ns |

Clear is the lap along the centerline on circuits and random clear poses on
the two obstacle fields. The second row of each pair has the smallest cell,
among sizes doubled from 1.5, that gives the expanded grid.

Three of the four tracks exceed the cell limit. With a larger cell they
receive the expanded grid, and their queries cost 2 to 21 times less.
Crowded exceeds the limit of references instead. It fits with a cell of 3 and
then costs 1 to 7 percent more than its fallback. On a performance core,
CircuitWide costs 8.3, 166, and 125 ns with a cell of 3 against 106, 302, and
223 ns today.

Two things limit the choice of cell:

- A cell larger than necessary costs time in the middle of the track, because
  its cells stop being empty. On the reference circuit the lap costs 11.5 ns
  with a cell of 1.5, 15.4 ns with 3, and 46.6 ns with 6.
- A cell that is still too small gives the sparse center grid, which is slower
  than the tree it replaces. CircuitVast costs 260, 747, and 419 ns with a
  cell of 3.

**Recommendation.** When the grid does not fit, double the cell size until it
does, instead of changing the index.

### 4. One pose per frame

The detector answers for one pose. `CollisionManager` states that a fast
vehicle can cross a barrier between two frames and that this is accepted. This
finding measures how fast that is, and what happens afterwards. A rectangle of
3 by 5.835 was driven along straight lines at a constant distance per frame,
20,000 times for each row.

**Walls.** A wall is missed only if the vehicle moves farther in one frame
than its own extent along the direction to the wall:

| Incidence | Largest tested step without a miss | Computed limit | Missed at 2 lengths per frame | Missed at 5 lengths per frame |
| --------: | ---------------------------------: | -------------: | ----------------------------: | ----------------------------: |
|         0 |                       1.00 lengths |   1.00 lengths |                        49.78% |                        80.69% |
|        30 |                       1.25 lengths |   1.30 lengths |                        35.18% |                        74.13% |
|        60 |                       1.50 lengths |   1.89 lengths |                         5.63% |                        62.37% |
|        80 |                       3.00 lengths |   3.92 lengths |                         0.00% |                        21.88% |

Incidence is the angle in degrees between the direction of travel and the
wall's normal. The limit is the vehicle's length plus its width times the
tangent of the incidence. The measured share of misses is within 0.8
percentage points of the share computed from that limit in all 44 rows.

One length per frame is 350 units per second at 60 frames per second, and
5,835 units per second at 1,000.

When contact is reported, the vehicle's leading point is already inside the
barrier. The depth is half of the advance toward the wall in one frame on
average, and the whole advance at most.

**Obstacles smaller than the vehicle.** The detector tests the rectangle's
perimeter. An obstacle that is entirely inside the rectangle is not reported.
A small obstacle is therefore reported only while the front side or the rear
side passes over it:

| Obstacle side | Largest tested step without a miss | First tested step with misses | Missed at that step | Missed at a step of 6.0 |
| ------------: | ---------------------------------: | ----------------------------: | ------------------: | ----------------------: |
|           0.5 |                                0.5 |                           1.0 |              33.60% |                  89.02% |
|           1.0 |                                1.0 |                           1.5 |              22.87% |                  80.26% |
|           2.0 |                                2.0 |                           3.0 |              27.68% |                  64.05% |

An obstacle is missed as soon as the step per frame exceeds the obstacle's
depth. For an obstacle of 0.5 that is 30 units per second at 60 frames per
second. A wall needs 350.

**After a miss nothing is reported.** A vehicle that has passed through a
wall is inside the barrier's outline. Of 1,111,255 later frames in which the
vehicle was entirely inside the barrier, 1,111,254 were reported clear.

**Recommendation.** Compare these limits with the fastest vehicle and the
longest frame that the application allows. If a vehicle can move farther in
one frame than its own length, or than the depth of the smallest obstacle,
test the path between two frames as well as the pose. The rectangle swept from
the previous pose to the current one is such a test.

### 5. The rectangle and the engine agree

The engine-side checks build vehicles from box meshes in an exported release
build, place them, and ask the engine where it puts the corners of each mesh.

| Check                                                 | Result                                                       |
| ----------------------------------------------------- | ------------------------------------------------------------ |
| Rectangle against the engine's corners, 8,000 poses   | Largest difference 0.0000153, one spacing of binary32        |
| Pose given to the manager against the detector's pose | Identical answers in 56,000 queries, 23,724 of them contacts |
| Front face 0.10 and 0.16 inside a wall                | Clear                                                        |
| Front face 0.17 inside a wall                         | Contact                                                      |
| Rear face and either side, 0.01 short of a wall       | Clear                                                        |
| Rear face and either side, 0.01 inside a wall         | Contact                                                      |
| Height above the ground                               | Ignored                                                      |
| Vehicle entirely inside a barrier's outline           | Clear                                                        |

The engine transforms in binary32 and the detector in binary64 with one final
rounding. That explains the difference of one spacing.

The manager moves the front limit back by 0.165. The front of the mesh is
therefore up to 0.165 inside a barrier before contact is reported. The amount
is a length, not a share of the vehicle:

| Vehicle of the fixture           | Mesh, width by length | Bounds the manager holds   | Front moved by |
| -------------------------------- | --------------------: | -------------------------- | -------------: |
| Box                              |            3.0 by 6.0 | (-1.5, -2.835, 1.5, 3)     |          0.165 |
| Small box                        |            2.0 by 4.5 | (-1, -2.085, 1, 2.25)      |          0.165 |
| Box with a hidden wing, 3.4 wide |            3.4 by 6.4 | (-1.7, -3.085, 1.7, 3.15)  |          0.165 |
| Box scaled by 1.5                |            4.5 by 9.0 | (-2.25, -4.335, 2.25, 4.5) |          0.165 |

Three behaviors are worth knowing. Whether they matter depends on the
application's scenes, which this review did not read.

- A hidden mesh counts toward the footprint. The wing above is invisible and
  widens the rectangle from 3.0 to 3.4.
- The manager measures its vehicles once. A vehicle that is scaled afterwards
  keeps its old rectangle.
- A mesh below a node that has no transform of its own is measured as if it
  moved with the vehicle. The engine does not move it: with the vehicle at
  X = 100 the engine draws that mesh at X = 0.

### 6. One index for every vehicle

`CollisionManager` builds one index, for the union of the bounds of all its
vehicles. The reach of the index and the size of its cells follow the largest
vehicle. A smaller vehicle then looks up cells that list edges it cannot
touch.

The reference circuit was indexed for three vehicles and queried with the
reference vehicle each time. Efficiency-core run, console program:

| Track indexed for            | Cell size | LapCenter | Edges listed | NearMiss | Edges listed | Contact | Edges listed |
| ---------------------------- | --------: | --------: | -----------: | -------: | -----------: | ------: | -----------: |
| The vehicle itself           |       1.5 |   10.2 ns |         0.00 |   194 ns |         4.31 |  144 ns |         5.08 |
| A vehicle 1.5 times as large |      2.25 |   36.0 ns |         1.14 |   208 ns |         7.06 |  153 ns |         7.52 |
| A vehicle twice as large     |         3 |   59.8 ns |         7.43 |   212 ns |         9.58 |  156 ns |         9.89 |

Edges listed is the mean number of edges in the cell of a pose. The band of
the circuit is 18 wide. Along its centerline the vehicle's own index has only
empty cells. The index for a vehicle 1.5 times as large lists one edge there
on average, and the query costs 3.5 times as much. Near a barrier the cost
rises by 6 to 9 percent.

The engine-side fixture has four vehicles, and the largest is 1.5 times the
reference vehicle. Its manager costs 35.0, 197, and 150 ns for the three
workloads, which is the second row within 6 percent. The index explains the
manager's cost. The manager itself adds nothing that these measurements can
resolve.

This matters only when vehicles differ in size, and the cost stays below
60 ns along the centerline.

**Recommendation.** If vehicles of clearly different sizes share a track,
build one index for each size. The reference circuit's index takes 0.07 ms to
build and retains 366 KB.

### 7. First use

The first construction and the first query in a process include the
compilation of the detector's code. Each process was new:

| Track       | Index                                 | Run             | Processes | First construction |             Range | First query |             Range |
| ----------- | ------------------------------------- | --------------- | --------: | -----------------: | ----------------: | ----------: | ----------------: |
| Circuit     | expanded grid                         | Timing          |        15 |            4.25 ms |   4.05 to 4.33 ms |      362 µs |     351 to 373 µs |
| CircuitFine | expanded grid                         | Timing          |         4 |            6.52 ms |   6.20 to 6.60 ms |      377 µs |     352 to 385 µs |
| Circuit     | expanded grid                         | Efficiency-core |        15 |            5.35 ms |   5.08 to 5.94 ms |      471 µs |     454 to 587 µs |
| CircuitFine | expanded grid                         | Efficiency-core |        15 |            5.86 ms |   5.66 to 6.21 ms |      469 µs |     456 to 505 µs |
| CircuitWide | dense center grid with long-edge tree | Efficiency-core |        15 |            5.05 ms |   4.77 to 6.49 ms |    1,303 µs | 1,240 to 1,695 µs |
| Crowded     | dense center grid                     | Efficiency-core |        15 |           16.61 ms | 15.91 to 17.93 ms |    1,911 µs | 1,876 to 2,319 µs |

The application pays the first construction when it loads a track and the
first query in the first frame. Both happen once. After the code is compiled
the reference circuit takes 0.07 ms to build and 6 to 160 ns to query. The
first query of a fallback index costs 2.8 to 4.1 times as much as the first
query of the expanded grid.

Inside the engine the first manager took 40 ms to construct on an efficiency
core and the second 1.0 ms. These times include work of the fixture: it builds
four vehicles and writes the collider as text, which the manager then reads.

Later calls in these processes are not reported. The process is pinned to one
core, so the runtime's background compiler shares that core with the query and
delays it. In the application the compiler runs on another core.

### 8. Invalid input

Every invalid input that was tried is refused with an `ArgumentException` or a
type derived from it: 38 cases of construction and 10 of queries. They cover
missing, empty, and null outlines; fewer than three vertices; repeated
vertices, including positive and negative zero; coordinates, bounds, poses, and
cell sizes that are not finite; bounds without extent; and rectangles whose
corners overflow binary32. Both index families refuse the same queries.

Extreme valid input is answered exactly in 59 cases: coordinates from the
smallest subnormal values to the largest finite one, yaw up to the largest
finite value, positions beyond 1e12, and index footprints from 0.002 to 2,000
wide.

Two messages name a cause that is not the cause:

| Input                                    | Message                                        | Cause                                             |
| ---------------------------------------- | ---------------------------------------------- | ------------------------------------------------- |
| Collider text whose keys are `x` and `y` | `Outline 0 contains a zero-length segment.`    | The keys are not `X` and `Y`; every vertex is 0,0 |
| A manager without any vehicle            | `The value must be finite. (Parameter 'minX')` | The list of vehicles is empty                     |

The first row used `System.Text.Json` with its default options. The
application's own reader was replaced by a stand-in in this review and may
treat keys differently.

**Recommendation.** Refuse an empty list of vehicles by name. Refuse an
outline whose vertices are all equal with a message that suggests checking the
keys.

**Resolution.** `CoordinateXY` now requires both `X` and `Y` in JSON, so
misspelled coordinate keys fail during deserialization instead of appearing as
zero-length segments. `CollisionManager` names an empty vehicle list before it
calculates bounds. `TrackSwitcher` applies its existing vehicle-list and start
index checks when switching tracks as well as at initial construction.

### 9. Forty percent of the detector serves the fallback

| Part of the detector                      | Lines |
| ----------------------------------------- | ----: |
| Center grid                               |   329 |
| Long-edge list and tree                   |   213 |
| Queries of the center grid                |    44 |
| Choice of the fallback in the constructor |    40 |
| Fallback in total                         |   626 |
| Both detector files                       | 1,544 |

The fallback is 40.5 percent of the detector. It runs only for a track that
exceeds a limit of the expanded grid. Finding 3 shows that a larger cell
serves the four fallback tracks that were timed better or about equally well.

**Recommendation.** If the cell size is chosen as finding 3 recommends, remove
the fallback. One case then needs a decision: the expanded grid is not built
for coordinates beyond 1e12. The full scan answers any query and can serve
that case.

### 10. Smaller observations

- **Six checks per orientation are redundant.** Finding 1 shows that removing
  the subnormal checks changes no answer. Every orientation test runs them.
- **The long-edge tree allocates more than it uses.** It allocates one node
  less than twice the number of long edges, and a leaf holds up to eight
  edges. CircuitVast allocates 12,063 nodes and uses 2,047. CircuitWide
  allocates 2,481 and uses 511.
- **Two classes define the same helpers.** `Guard` and `CollisionMath` both
  define `IsFinite` and `ThrowIfNotFinite`. `CollisionMath.BitIncrement` and
  `BitDecrement` repeat `Math.BitIncrement` and `Math.BitDecrement`.
- **The segment test recomputes bounding boxes.** Each of the four segment
  tests for a candidate edge computes the box of the edge and the box of the
  rectangle's side again.
- **The index costs little memory.** The reference circuit retains 366 KB and
  takes 0.07 ms to build once the code is compiled. The largest, Crowded with
  55,296 edges, retains 2.1 MB and takes 2.6 ms.
- **Distance from the origin costs precision, not time.** CircuitFar is the
  reference circuit moved 94,000 units from the origin. Its queries cost the
  same. Its cells list 27,913 edges where 27,724 are within reach, because the
  rounding margin grows with the coordinates. Binary32 spaces its values
  0.0078 apart there, against 0.0000076 at 100 units from the origin. A pose
  that touches a barrier by less than the spacing can be answered either way.
- **A footprint larger than the indexed one is scanned in full.** The query
  is still correct. On the reference circuit it costs 799 ns instead of 28 ns.
  The manager never does this, because it indexes the union of its vehicles.

## Method

### What was taken and what was made

| Taken from the application, unmodified   | Made for this review                                  |
| ---------------------------------------- | ----------------------------------------------------- |
| `ColliderJson.cs`                        | Every track: computed from parameters and a seed      |
| `CoordinateXY.cs`                        | Every vehicle: boxes built in code                    |
| `Guard.cs`                               | Every pose set                                        |
| `Outline.cs`                             | Four references that judge answers                    |
| `TrackCollisionDetector.cs`              | The timing method                                     |
| `TrackCollisionDetector.ExpandedGrid.cs` | A Godot project, exported as a release build          |
| `VehicleCollisionFootprint.cs`           | Stand-ins for `Car`, `CarSwitcher`, and `JsonUtility` |
| `CollisionManager.cs`                    | A motion model for the sampling experiments           |

No track file, settings file, existing verification code, saved measurement,
or earlier report is read by the harness or used as evidence here. The same
session produced both reviews, so the author had seen the existing material
before writing this one.

The vehicle footprint is a choice made for this review: three units wide and
six long, with the front limit moved by 0.165 as `CollisionManager` does. It
gives a rectangle from (-1.5, -2.835) to (1.5, 3).

### Tracks

| Track                | Shape                                                    |  Edges |
| -------------------- | -------------------------------------------------------- | -----: |
| Circuit              | Closed band 18 wide, mean radius 110, edges of 2.0       |    691 |
| CircuitFine          | The same, edges of 0.3                                   |  4,607 |
| CircuitCompact       | Band 12 wide, mean radius 32, edges of 0.5               |    805 |
| CircuitWide          | Band 24 wide, mean radius 200, edges of 2.0              |  1,257 |
| CircuitVast          | Band 28 wide, mean radius 1,200, edges of 2.5            |  6,032 |
| CircuitFar           | Circuit moved to (50,000, -80,000)                       |    691 |
| CircuitMinute        | Circuit and vehicle scaled by 1/1024                     |    691 |
| CircuitGiant         | Circuit and vehicle scaled by 1024                       |    691 |
| Rectangular          | Rectangles of 220 by 140 and 180 by 100, one edge a side |      8 |
| RectangularSegmented | The same in pieces of 2.0                                |    640 |
| Scattered            | 576 obstacles of 5 edges, 9 apart                        |  2,880 |
| ScatteredRemote      | 144 obstacles of 3 edges, 900 apart                      |    432 |
| ScatteredAligned     | Four squares whose cell numbers differ only in high bits |     16 |
| Crowded              | 9,216 obstacles of 6 edges, 1.6 apart                    | 55,296 |

### References

Four references judge the detector. None shares code with it.

| Reference           | Arithmetic                        | Judges                                  |
| ------------------- | --------------------------------- | --------------------------------------- |
| Parametric oracle   | Exact integers, 128-bit or larger | Every yes or no answer                  |
| High-precision trig | Fixed point, 512 fractional bits  | Rotation direction and corner rounding  |
| Distance classifier | Binary64 distances                | Answers that are not within a tolerance |
| Integer lattice     | 64-bit integers                   | Touching and collinear contact          |

The detector decides intersection from the signs of four orientations. The
parametric oracle instead solves the two segment equations for their
parameters as exact rational numbers, and treats parallel, collinear, and
zero-length segments explicitly. Before judging the detector it is checked
against 29 hand-derived cases under 32 symmetries, 6 scales, and 4 shifts, and
against a second exact formulation on 700,000 random cases.

The detector rounds the rectangle's corners to binary32 and is exact for the
rounded rectangle. The parametric oracle therefore judges the rounded corners,
which it reads from the detector. Two other references cover what that leaves
out. The high-precision reference computes the unrounded corners from the pose
convention, with sine and cosine from their Taylor series, and measures how far
the rounded corners are from them. The distance classifier builds its own
rectangle and decides only poses that are clear or colliding by more than a
tolerance.

A matching answer does not show that the index is right, because a second
edge can produce the same answer. Every comparison therefore also requires
each edge that truly intersects to be among the candidates that the detector's
own cell lookup returns.

### Adversarial poses

A cell of the expanded grid lists the edges within reach of the cell. The list
is only just sufficient when three things coincide: a corner points straight
along a grid axis, it reaches a barrier at its full distance, and the pose is
on the first representable position of a cell. Random poses almost never do
this. Two suites construct it:

- Narrow triangular barriers are placed so that their tips are exactly one
  reach from a cell boundary, and then moved by up to thirteen representable
  steps either way. The vehicle is placed on the boundary and up to six steps
  to either side, with each corner in turn pointing at the tip.
- Every cell corner of two tracks is visited with each corner pointing along
  each axis, at the corner and one step to either side.

### Timing

- One process per suite, pinned to one logical processor.
- Warmup continues until ten consecutive batches agree within four percent.
- Twenty-five batches of about 25 ms each. Tables give the median over three
  processes of the per-process median.
- Contact counts are checked in every batch. Allocation is measured apart.
- Hot loops repeat 4,096 poses. They measure the cost with the index and the
  code in the processor's caches.
- Single calls are timed one at a time after a buffer of 1 to 64 MB has been
  rewritten. The timer resolves 100 ns, so only means are useful.
- First use is timed in new processes.
- The engine-side harness repeats the hot loops inside an exported release
  build and times one query per frame.

Two sets of timings were taken.

**The timing run** pinned each process to logical processor 23, a performance
core, at high priority. It did not record what else the machine was doing. One
later process for each suite, placed the same way, was 1.7 percent slower in
the median over 98 query measurements. The largest differences were 7.4
percent slower and 2.5 percent faster.

**The efficiency-core run** was taken while a game used the performance
cores. Pinned processes at high priority had disturbed the game, so every
later process ran at normal priority on efficiency cores that were idle,
logical processors 19 to 21, one process at a time. The launcher recorded how
busy every logical processor was before and after each part.

| Part of the efficiency-core run        | Time           | Warmup not settled | Widest ratio between processes |
| -------------------------------------- | -------------- | -----------------: | -----------------------------: |
| Checks, queries, engine, and first use | 15:18 to 15:32 |            1 of 98 |                           1.14 |
| Cell size, six tracks                  | 15:33 to 15:51 |          53 of 144 |                           1.10 |
| Index footprint                        | 15:52 to 15:54 |             8 of 9 |                           1.23 |

The timing run had measured 20 of the queries on the same efficiency core at
high priority. The efficiency-core run repeats them 1.6 percent slower in the
median, between 1.7 percent faster and 19.4 percent slower. An efficiency
core is 1.22 times slower than a performance core in the median over 98
queries, between 0.87 and 1.65 times.

During the last two parts other software had begun to use the efficiency
cores, and many warmups reached their time limit before ten batches agreed.
The medians of three processes still agree within 10 and 23 percent. Every
conclusion drawn from these two parts rests on a ratio of 2 or more, with two
exceptions. One is that Crowded costs about the same with either index. The
other is that an index for a larger vehicle costs 6 to 9 percent more near a
barrier. For that one, no process of the vehicle's own index was as slow as
any process of the index for twice the vehicle.

The runtime optimizes code with a profile of its first calls. A process that
queries a fallback index first runs the expanded grid's empty-cell answer in
8.3 ns instead of 6.4 ns. The cell-size sweep is such a process. Its rows
compare with each other, not with the table of queries.

### Injected defects

One defect at a time is injected into a copy of the collision sources. The
harness is rebuilt against the copy and must fail. Each defect is a text
replacement that must match exactly once.

## Limits

- Every track is synthetic. The conclusions hold for the geometry that was
  generated: closed circuits, rectangles, and scattered obstacles, from 1/1024
  to 1024 times the reference scale and up to 94,000 units from the origin.
- The footprint is an assumption. Costs scale with the number of edges within
  reach, so a different footprint changes the numbers, not the behavior.
- Timings come from one machine. The hot-loop numbers are lower bounds for the
  application; the single-call numbers with 64 MB rewritten are a deliberately
  harsh upper bound.
- A frame with a window was not timed. It would have shared the graphics
  processor with the game in the foreground and opened a window over it. The
  per-frame numbers come from the engine without a window, which sleeps 6.9 ms
  in every frame (`OS::add_frame_delay` in the engine's `core/os/os.cpp`).
- The engine-side timings, the index footprints, the cell-size sweep over six
  tracks, and first use on the fallback tracks exist for efficiency cores
  only. Times on an efficiency core are about 1.2 times those on a performance
  core. For CircuitWide the effect of the cell size was measured on both kinds
  of core and is similar.
- First use was timed in a process pinned to one core. Only the first
  construction and the first query are reported from it.
- The injected defects were chosen by reading the code. A harness that detects
  all of them can still miss others.
- The sampling experiments use constant velocity along straight lines. They
  describe the detector's sampling, not the application's motion code.
- The application's JSON reader, its vehicles, and its scenes are not part of
  this review. Stand-ins replace them.

## Reproduction

```powershell
cd Veehiicuul_Godot_CSharp\Veehiicuul\Verification\CollisionDetectionFromScratch
.\Build.cmd
.\Run.cmd
.\Run.cmd -ValidationOnly -Scale 10
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\RunMutationTests.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\RunNativeMutationTests.ps1

cd ..\..\Documentation\CollisionReviewFromScratch20260929\Analysis
python SummarizeResults.py
```

`Run.cmd` without parameters places its processes as the timing run did, on a
performance core at high priority, and uses all but two logical processors for
the checks. The efficiency-core run used the commands below. Name logical
processors that are idle on the machine at hand.

```powershell
.\Run.cmd -Affinity 19,20,21 -Priority normal -SkipWindowedFrames -Stages Validation,Simulation,Measure,NativeMeasure,ColdStart -Suites queries
.\Run.cmd -Affinity 19,20,21 -Priority normal -Stages Measure -Suites cellsize
.\Run.cmd -Affinity 19,20,21 -Priority normal -Stages Measure -Suites indexfootprint
```

The harness is described in its
[readme](../../Verification/CollisionDetectionFromScratch/Readme.md).

Saved evidence:

| File or folder                                                   | Content                                                    |
| ---------------------------------------------------------------- | ---------------------------------------------------------- |
| [Analysis/Summary.md](Analysis/Summary.md)                       | Every table, generated                                     |
| `Measurements/Validation.json` and `.txt`                        | Standard run of the checks                                 |
| `Measurements/ValidationExtended.json` and `.txt`                | Extended run of the checks                                 |
| `Measurements/NativeChecks.json` and `.log`                      | Engine-side checks                                         |
| `Measurements/MutationResults.json`                              | Defects injected into the detector, and outcomes           |
| `Measurements/NativeMutationResults.json`                        | Defects injected into the manager and the footprint        |
| `Measurements/Simulation.json` and `.txt`                        | Sampling experiments                                       |
| `Measurements/Measure_*.json`                                    | Timing run, eight suites, three processes each             |
| `Measurements/Variant_*.json`                                    | Timing run under other runtime settings and on other cores |
| `Measurements/ColdStart_*.jsonl`                                 | Timing run, first use                                      |
| [Measurements/SecondRun](Measurements/SecondRun)                 | One later process for each suite of the timing run         |
| [Measurements/EfficiencyCoreRun](Measurements/EfficiencyCoreRun) | Efficiency-core run, its placement, and processor use      |
