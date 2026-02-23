# ModelingEvolution.Drawing

[![NuGet](https://img.shields.io/nuget/v/ModelingEvolution.Drawing.svg)](https://www.nuget.org/packages/ModelingEvolution.Drawing)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ModelingEvolution.Drawing.svg)](https://www.nuget.org/packages/ModelingEvolution.Drawing)
[![CI](https://github.com/modelingevolution/drawing/actions/workflows/ci.yml/badge.svg)](https://github.com/modelingevolution/drawing/actions/workflows/ci.yml)
[![License](https://img.shields.io/github/license/modelingevolution/drawing.svg)](https://github.com/modelingevolution/drawing/blob/main/LICENSE)

A high-performance .NET generic math library for 2D and 3D geometry, coordinate systems, shapes, intersections, trajectories, and drawing primitives. Built on `INumber<T>` constraints from .NET generic math, all types work with `float`, `double`, `decimal`, or any compatible numeric type -- with zero boxing and full type safety.

## Table of Contents

- [Why This Library](#why-this-library)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [API Reference](#api-reference)
  - [2D Core Types](#2d-core-types)
  - [3D Core Types](#3d-core-types)
  - [Shapes (2D)](#shapes-2d)
  - [Shapes (3D)](#shapes-3d)
  - [Lines and Segments](#lines-and-segments)
  - [Curves and Paths (2D)](#curves-and-paths-2d)
  - [Polylines (2D and 3D)](#polylines-2d-and-3d)
  - [Angles and Coordinate Systems](#angles-and-coordinate-systems)
  - [Rotations and Orientation](#rotations-and-orientation)
  - [Poses and Trajectories](#poses-and-trajectories)
  - [Kinematics](#kinematics)
  - [Equations](#equations)
  - [Colors](#colors)
  - [Data Structures and Algorithms](#data-structures-and-algorithms)
  - [Interfaces](#interfaces)
- [Key Features](#key-features)
  - [Generic Math](#generic-math)
  - [IShape Interface Hierarchy](#ishape-interface-hierarchy)
  - [Intersections](#intersections)
  - [Polygon Boolean Operations](#polygon-boolean-operations)
  - [Bezier Path Clipping](#bezier-path-clipping)
  - [Pooled Allocation Scopes](#pooled-allocation-scopes)
  - [Serialization](#serialization)
  - [SVG Export](#svg-export)
- [Code Examples](#code-examples)
  - [Points: Distance, Lerp, Rotate](#points-distance-lerp-rotate)
  - [3D Points, Vectors, and Rotations](#3d-points-vectors-and-rotations)
  - [Spherical Coordinates](#spherical-coordinates)
  - [Poses and Trajectories](#poses-and-trajectories-1)
  - [Shapes: Area and Perimeter](#shapes-area-and-perimeter)
  - [Line-Circle Intersection](#line-circle-intersection)
  - [Segment-Rectangle Clipping](#segment-rectangle-clipping)
  - [Path-Rectangle Clipping](#path-rectangle-clipping)
  - [Triangle Properties](#triangle-properties)
  - [Polygon Operations](#polygon-operations)
  - [Matrix Transformations (2D and 3D)](#matrix-transformations-2d-and-3d)
- [Requirements](#requirements)
- [License](#license)

## Why This Library

- **Generic math** -- write geometry code once; use it with `float`, `double`, or any `IFloatingPointIeee754<T>` type.
- **No boxing** -- all core types are `struct` or `readonly record struct`, avoiding heap allocations.
- **Full 2D and 3D** -- from `Point<T>` and `Vector<T>` to `Point3<T>`, `Vector3<T>`, `Rotation3<T>`, `Pose3<T>`, `Quaternion<T>`, and `Trajectory3<T>`.
- **Rich coordinate systems** -- Cartesian, polar, cylindrical, and spherical with implicit/explicit conversions between them.
- **Rich interface hierarchy** -- `IShape<T, TSelf>` unifies area, perimeter, centroid, bounding box, containment, rotation, and scaling across all shape types.
- **Comprehensive intersections** -- 21 intersection combinations (line, segment, circle, triangle, rectangle, polygon) with zero-alloc `FirstOf` variants.
- **Polygon booleans** -- union, intersection, difference, and clustering via Clipper2.
- **Exact Bezier clipping** -- `Path<T>.Intersect(Rectangle<T>)` computes analytically exact sub-curves, not polygon approximations.
- **Trajectory planning** -- `Trajectory3<T>` with time-parameterized waypoints, interpolation, resampling, and a real-time `TrajectoryController<T>`.
- **Robotics-ready** -- `Joints6<T>` for 6-DOF joint representations, `Pose3<T>.FromSurface()` for plane-based frame construction.
- **Pooled allocations** -- `AllocationScope` provides arena-style pooled memory for zero-GC geometry pipelines.
- **Serialization built in** -- Protobuf (protobuf-net) attributes on all core types, plus custom JSON converters and SVG exporters.

## Installation

```bash
dotnet add package ModelingEvolution.Drawing
```

Or via the NuGet Package Manager:

```
Install-Package ModelingEvolution.Drawing
```

## Quick Start

```csharp
using ModelingEvolution.Drawing;

// 2D points and vectors (using double precision)
var a = new Point<double>(1.0, 2.0);
var b = new Point<double>(4.0, 6.0);

double distance = a.DistanceTo(b);          // 5.0
Point<double> mid = a.Lerp(b, 0.5);         // {X=2.5, Y=4}

// 3D points and rotations
var p = new Point3<double>(1.0, 2.0, 3.0);
var rotation = new Rotation3<double>(45.0, 0.0, 0.0); // 45 deg roll
Point3<double> rotated = rotation.Rotate(p);

// Spherical coordinates
var orbit = SphericalPoint<double>.OnEquator(5.0, Degree<double>.Create(30));
Point3<double> cartesian = orbit; // implicit conversion

// Shapes with area and perimeter
var circle = new Circle<double>(Point<double>.Zero, 10.0);
double area = circle.Area();                 // ~314.159
bool inside = circle.Contains(new Point<double>(3.0, 4.0)); // true

// Polygon boolean operations
var poly1 = new Polygon<double>(
    new Point<double>(0, 0), new Point<double>(10, 0),
    new Point<double>(10, 10), new Point<double>(0, 10));
var poly2 = new Polygon<double>(
    new Point<double>(5, 5), new Point<double>(15, 5),
    new Point<double>(15, 15), new Point<double>(5, 15));

Polygon<double> merged = poly1 | poly2;     // union
Polygon<double> overlap = poly1 & poly2;    // intersection

// 3D trajectory with time-parameterized waypoints
var trajectory = new Trajectory3<double>(
    new Waypoint3<double>(new Pose3<double>(0, 0, 0, 0, 0, 0), 0.0),
    new Waypoint3<double>(new Pose3<double>(100, 0, 50, 0, 45, 0), 5.0));
Pose3<double> atHalf = trajectory.AtTime(2.5); // interpolated pose
```

## API Reference

### 2D Core Types

| Type | Description |
|---|---|
| `Point<T>` | 2D Cartesian point. Supports arithmetic with vectors and sizes, distance, lerp, reflect, rotate, clamp, midpoint, matrix transform, tuple conversion, and parsing. |
| `Vector<T>` | 2D vector with magnitude, direction, dot/cross product, normalize, projection, reflect, rotate, perpendicular (CW/CCW), angle between, lerp, and matrix transform. |
| `Size<T>` | Width/height pair with arithmetic operators, component-wise multiply/divide, and parsing. |
| `Matrix<T>` | 3x3 affine transformation matrix. Supports translate, rotate, scale, skew, invert, append/prepend, and point/vector transforms. |
| `Matrix2x2<T>` | 2x2 matrix for linear algebra: determinant, trace, transpose, inverse, eigendecomposition (closed-form for symmetric matrices), and point/vector transforms. |
| `Rectangle<T>` | Axis-aligned rectangle with contains, intersect, inflate, offset, bounds, tiling, diagonal, distance-to-point, and LTRB construction. |
| `Thickness<T>` | Four-sided thickness (top, right, bottom, left) for rectangular borders. |

### 3D Core Types

| Type | Description |
|---|---|
| `Point3<T>` | 3D Cartesian point. Distance, lerp, midpoint, random generation, arithmetic with `Vector3<T>`. Conversions to/from `System.Numerics.Vector3` and tuples. Protobuf and JSON serialization. |
| `Vector3<T>` | 3D vector with length, normalize, dot product, cross product, angle between, projection, lerp, random unit vector, and `RotationTo()` for computing the rotation between two directions. Static basis vectors `EX`, `EY`, `EZ`. |
| `Matrix3x3<T>` | 3x3 matrix for 3D transforms. Determinant, trace, transpose, inverse. Rotation factories: `RotationX`, `RotationY`, `RotationZ`, `RotationZYX`. Scale, column/row construction. Euler angle and quaternion extraction. |
| `Quaternion<T>` | Quaternion for 3D rotations. Normalize, conjugate, inverse, from axis-angle, rotate vector, slerp, dot product. Conversions to/from `System.Numerics.Quaternion`. |
| `Segment3<T>` | Bounded line segment in 3D. Length, midpoint, direction, lerp, split, reverse, distance-to-point, project point. |

### Shapes (2D)

All 2D shapes implement `IShape<T, TSelf>`, providing `Area()`, `Perimeter()`, `Centroid()`, `BoundingBox()`, `Contains(Point<T>)`, `Rotate(Degree<T>, Point<T>)`, and `Scale(T)`.

| Type | Description |
|---|---|
| `Circle<T>` | Center + radius. Intersects with lines, segments, circles, and rectangles. Tangent point detection. `PointAt(Radian<T>)` for parameterized access. |
| `Triangle<T>` | Three vertices. Incircle, circumcircle, orthocenter, angles, edges. Classification: `IsRight()`, `IsAcute()`, `IsObtuse()`, `IsEquilateral()`, `IsIsosceles()`, `IsScalene()`. Similarity and congruence tests. |
| `Polygon<T>` | Immutable polygon backed by `ReadOnlyMemory<Point<T>>`. Shoelace area, ray-casting containment, convex hull (Graham scan), `IsConvex()`, edges, simplify, skeleton (medial axis). Boolean operations: union, intersect, subtract, cluster. Operators: `\|` (union), `&` (intersect), `-` (subtract). |
| `Rectangle<T>` | Implements `IArea<T>`, `IPerimeter<T>`, `ICentroid<T>`, `IBoundingBox<T>`, `IScalable<T, Rectangle<T>>`. Rotation returns `Polygon<T>`. |

### Shapes (3D)

| Type | Description |
|---|---|
| `Triangle3<T>` | Three vertices in 3D space. Area (cross product), perimeter, centroid, unit normal vector. `ToPose()` creates a coordinate frame from the triangle plane. Translate by `Vector3<T>`. |

### Lines and Segments

| Type | Description |
|---|---|
| `Line<T>` | Infinite 2D line (supports vertical lines). From two points, point + direction, or equation. Intersect with lines, segments, circles, triangles, rectangles, polygons. Distance-to-point, angle-between, parallel/perpendicular tests, project point, reflect point. |
| `Segment<T>` | Bounded 2D line segment (start + end). Length, midpoint, direction, lerp, split, reverse. Intersect with segments, lines, circles, rectangles. Distance-to-point, project point, parallel test. Liang-Barsky rectangle clipping. |
| `Segment3<T>` | Bounded 3D line segment (start + end). Length, midpoint, direction, lerp, split, reverse, distance-to-point, project point. |

### Curves and Paths (2D)

| Type | Description |
|---|---|
| `BezierCurve<T>` | Cubic Bezier curve (4 control points). `Evaluate(t)`, `Split(t)`, `SubCurve(t0, t1)` via De Casteljau. Extremum points, linear equation intersection, rectangle edge crossings. |
| `Path<T>` | Immutable sequence of `BezierCurve<T>` segments. Create from points (`FromPoints`) with smoothing, or from SVG path data (`Parse`). `Length()`, `PointAt(t)` with arc-length parameterization. Exact `Intersect(Rectangle<T>)` that splits Bezier curves at crossing points. Transform, rotate, reverse, append. `Close()` and `ToPolygon()` for rasterization. |
| `ComplexCurve<T>` | Composite curve of mixed Bezier and polyline segments packed in a contiguous byte buffer for zero-copy decoding. SVG export and JSON serialization. |
| `PolygonalCurve<T>` | Mutable sequence of connected points that generates smooth Bezier segments via `GetSmoothSegment(i)`. |

### Polylines (2D and 3D)

| Type | Description |
|---|---|
| `Polyline<T>` | Immutable open polyline in 2D (non-closing). Length, edges, bounding box, simplification. JSON, Protobuf, and SVG serialization. |
| `Polyline3<T>` | Immutable open polyline in 3D. Length, edges, translate, transform. JSON and Protobuf serialization. |

### Angles and Coordinate Systems

| Type | Description |
|---|---|
| `Degree<T>` | Angle in degrees. Implicit conversion from numeric values and from `Radian<T>`. Arithmetic (+, -, *, /), normalize to (-180, 180], abs, comparison operators. |
| `Radian<T>` | Angle in radians. Implicit conversion from `Degree<T>`. Arithmetic (+, -, *, /), normalize to (-pi, pi], abs, comparison operators. Static `Sin`/`Cos` helpers. |
| `PolarPoint<T>` | Polar coordinates (radius, angle). Implicit conversions to/from `Point<T>`, explicit to `Vector2` and `Vector<T>`. |
| `CylindricalPoint<T>` | Cylindrical coordinates (radial distance, angle, height). Factory: `OnPlane(r, angle)`. Builder methods: `WithZ`, `WithRadialDistance`, `WithAngle`. `RotateAngle` for angular displacement. `+`/`-` operators with `Radian<T>` and `Point3<T>`. Conversions to/from `Point3<T>`, `Vector3<T>`, `SphericalPoint<T>`, and `System.Numerics.Vector3`. |
| `SphericalPoint<T>` | Spherical coordinates (radius, azimuth, inclination) with Degree-based storage (ISO convention: inclination from Z-axis). Factories: `OnEquator`, `FromElevation`, `NorthPole`, `SouthPole`. `Normalized`, `WithRadius`, `RotateAzimuth`, `RotateInclination`, `Rotate(az, inc)`. `AngularDistance` (Vincenty formula), `Slerp`, `Lerp`, Euclidean `Distance`. Radius scaling operators (`*`, `/`). `Point3<T>` offset operators (`+`, `-`). Conversions to/from `Point3<T>`, `Vector3<T>`, `CylindricalPoint<T>`, and `System.Numerics.Vector3`. |

### Rotations and Orientation

| Type | Description |
|---|---|
| `Rotation3<T>` | Euler angle rotation (Roll/Pitch/Yaw in degrees, ZYX convention). Create from degrees, radians, quaternion, or `Matrix3x3<T>`. `Rotate(Vector3<T>)`, `Rotate(Point3<T>)`. `Combine`, `Inverse`, `Negate`, `Slerp` (via quaternion). Convert to matrix, quaternion, or direction vector. Protobuf and JSON serialization. |
| `Quaternion<T>` | Unit quaternion for 3D rotations. From axis-angle, rotate vector, slerp, dot, normalize, conjugate, inverse. Hamilton product operator (`*`). Conversions to/from `System.Numerics.Quaternion`. Protobuf serialization. |
| `Matrix3x3<T>` | Also used as rotation representation. `RotationZYX(rx, ry, rz)` creates ZYX Euler rotation matrix. `ToEulerZYX()` extracts angles (with gimbal-lock handling). `ToQuaternion()` via Shepperd's method. |

### Poses and Trajectories

| Type | Description |
|---|---|
| `Pose3<T>` | 3D pose = position (`Point3<T>`) + orientation (`Rotation3<T>`). `TransformPoint`, `TransformVector`, `Inverse`, `Multiply` (compose). `Lerp` (position lerp + rotation slerp). `FromSurface(a, b, c)` creates a frame from three coplanar points (right-hand rule). Overload `FromSurface(a, b, c, h)` uses a hint point to determine Z direction. Operators: `*` (compose), `+`/`-` with `Vector3<T>` or `Rotation3<T>`. Tuple conversions, parsing. Protobuf and JSON serialization. |
| `Waypoint3<T>` | Timestamped `Pose3<T>` -- an element of a trajectory. |
| `PosePath3<T>` | Immutable sequence of `Pose3<T>` (no time). Length, reverse, transform, extract `Polyline3<T>`, convert to `Trajectory3<T>` with uniform timing. |
| `Trajectory3<T>` | Immutable time-parameterized path of `Waypoint3<T>`. `Duration`, `Length`, `AtTime(t)` (binary-search interpolation with position lerp + rotation slerp). `Resample(interval)`, `Reverse`, `Transform`. Convert to `PosePath3<T>` or `Polyline3<T>`. Create a real-time `TrajectoryController<T>` via `Control()`. |
| `TrajectoryController<T>` | Real-time trajectory playback using `Stopwatch` clock. Start, stop, pause, resume, seek. Fires `PoseChanged` events with current pose, time, and progress. |
| `Corner<T>` | Corner blending descriptor: `Sharp` (exact waypoint) or `Round(radius)` (arc blend before/after the waypoint). |

### Kinematics

| Type | Description |
|---|---|
| `Joints6<T>` | 6-DOF joint angles stored as six `Degree<T>` fields (no array allocation). Indexer `[0..5]`, arithmetic operators, `Lerp`, `MaxAbsDelta`, `IsWithin(tolerance)`, `IsBetween(min, max)`. `ToArray()`, `FromSpan()`, `FromArray()`, `CopyTo(Span)`. Protobuf and JSON serialization. |
| `Speed<T>` | Scalar speed in units/second. Arithmetic, `TimeFor(distance)`, `DistanceIn(time)`. Parsing. |
| `Velocity3<T>` | 3D velocity vector (direction + magnitude). Create from direction + speed. `Speed`, `Direction`, `DisplacementIn(time)`. Arithmetic operators. |

### Equations

| Type | Description |
|---|---|
| `LinearEquation<T>` | `y = Ax + B`. Create from points, angle, or direction. Compute, zero point, intersect, perpendicular, translate, mirror. |
| `QuadraticEquation<T>` | `ax^2 + bx + c = 0`. Zero points using the discriminant. |
| `CubicEquation<T>` | `ax^3 + bx^2 + cx + d = 0`. Root finding via Newton-Raphson. |
| `CircleEquation<T>` | `(x-cx)^2 + (y-cy)^2 = r^2`. Intersect with `LinearEquation<T>`. |

### Colors

| Type | Description |
|---|---|
| `Color` | ARGB color as a 32-bit uint. Parse from hex (`#RRGGBB`, `#AARRGGBB`), `rgba(r,g,b,a)`, or HSV strings. `GetHue()`, `GetSaturation()`, `GetLightness()`, `GetBrightness()`, `MakeTransparent(float)`. Implicit from string or tuple. JSON and Protobuf serialization. |
| `HsvColor` | HSV (hue, saturation, value) color space with optional alpha. Implicit conversions to/from `Color`. Parse from `hsv()`/`hsva()` strings, JSON arrays, or hex. |

### Data Structures and Algorithms

| Type | Description |
|---|---|
| `KdTree<T>` | 2D KD-tree for fast nearest-neighbor queries on `Point<T>`. Build from span, add individual points, find nearest neighbor. |
| `Skeleton<T>` | Medial axis approximation of a polygon (interior nodes + edges). Three algorithms: `StraightSkeleton`, `ChordalAxis`, and `Voronoi`. |
| `MovingAverage<T>` | Component-wise moving average over a sliding window of 2D points. O(1) add and query via circular buffer + running sums. |
| `MovingMedian<T>` | Component-wise moving median over a sliding window of 2D points. O(1) median read, O(n) add via maintained sorted arrays. |
| `AllocationScope` | Arena-style pooled allocation scope. Library allocations use `MemoryPool<T>.Shared` when active. Dispose returns all tracked memory at once. Supports `Persist()` to detach individual results. |
| `AlignmentResult<T>` | Result of a rigid 2D alignment (rotation + translation) between two point clouds, with rotation angle and residual error. |

### Interfaces

```csharp
// Composable geometry traits
public interface IArea<T>         { T Area(); }
public interface IPerimeter<T>    { T Perimeter(); }
public interface ICentroid<T>     { Point<T> Centroid(); }
public interface IBoundingBox<T>  { Rectangle<T> BoundingBox(); }
public interface IRotatable<T, TSelf> { TSelf Rotate(Degree<T> angle, Point<T> origin = default); }
public interface IScalable<T, TSelf>  { TSelf Scale(T factor); }

// Unified shape interface -- combines all of the above plus point containment
public interface IShape<T, TSelf> : IArea<T>, IPerimeter<T>, ICentroid<T>,
    IBoundingBox<T>, IRotatable<T, TSelf>, IScalable<T, TSelf>
{
    bool Contains(Point<T> point);
}
```

Implementing types: `Circle<T>`, `Triangle<T>`, `Polygon<T>`, and `Rectangle<T>` (partial -- no `IRotatable` since rotation produces a `Polygon<T>`).

## Key Features

### Generic Math

All geometry types are parameterized by `T` with constraints like `IFloatingPointIeee754<T>`, `IMinMaxValue<T>`, etc. This means the same code works with any numeric type:

```csharp
// Single precision
var pf = new Point<float>(1f, 2f);
var cf = new Circle<float>(pf, 5f);

// Double precision
var pd = new Point<double>(1.0, 2.0);
var cd = new Circle<double>(pd, 5.0);

// Both compute area with their respective precision
float areaF = cf.Area();
double areaD = cd.Area();
```

### IShape Interface Hierarchy

Write generic algorithms over any shape:

```csharp
void PrintShapeInfo<T, TShape>(TShape shape)
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>,
              IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
    where TShape : IShape<T, TShape>
{
    Console.WriteLine($"Area:      {shape.Area()}");
    Console.WriteLine($"Perimeter: {shape.Perimeter()}");
    Console.WriteLine($"Centroid:  {shape.Centroid()}");
    Console.WriteLine($"Bounds:    {shape.BoundingBox()}");
}
```

### Intersections

The `Intersections` static class provides 21 combination overloads covering every pair of: `Line`, `Segment`, `Circle`, `Triangle`, `Rectangle`, and `Polygon`. Each combination also has a zero-allocation `FirstOf` variant.

```csharp
// Line x Line
Point<double>? p = Intersections.Of(line1, line2);

// Line x Circle -> chord (Segment) or null
Segment<double>? chord = Intersections.Of(line, circle);

// Tangent detection
Point<double>? tangent = Intersections.TangentPoint(line, circle);

// Circle x Circle -> radical chord or tangent point
Segment<double>? radicalChord = Intersections.Of(circle1, circle2);

// Segment x Segment
Point<double>? hit = Intersections.Of(seg1, seg2);

// Zero-alloc first-hit variants
Segment<double>? first = Intersections.FirstOf(line, polygon);
```

Instance methods are also available on each shape for convenience:

```csharp
Segment<double>? chord = circle.Intersect(line);
Point<double>? hit = segment.Intersect(otherSegment);
Segment<double>? clipped = line.Intersect(rectangle);
```

### Polygon Boolean Operations

Powered by [Clipper2](https://github.com/AngusJohnson/Clipper2):

```csharp
var a = new Polygon<double>( /* ... */ );
var b = new Polygon<double>( /* ... */ );

// Operators
Polygon<double> union      = a | b;
Polygon<double> intersect  = a & b;
Polygon<double> difference = a - b;

// Methods returning multiple result polygons
List<Polygon<double>> unions = a.Union(b, removeHoles: true);
List<Polygon<double>> diffs  = a.Subtract(b);

// Batch operations
List<Polygon<double>> merged = Polygon<double>.Union(polygons);

// Clustering overlapping polygons
IEnumerable<Polygon<double>> clusters = Polygon<double>.Cluster(polygons);
```

### Bezier Path Clipping

`Path<T>.Intersect(Rectangle<T>)` performs analytically exact Bezier-rectangle clipping. It finds the precise parameter values where each cubic Bezier segment crosses the rectangle edges, then uses `SubCurve(t0, t1)` via De Casteljau subdivision to return exact sub-paths -- not polygon approximations.

```csharp
var path = Path<double>.FromPoints(points);
IEnumerable<Path<double>> clipped = path.Intersect(viewport);

foreach (var subPath in clipped)
{
    // Each sub-path contains only the Bezier segments inside the rectangle,
    // with curves split precisely at the rectangle boundaries.
}
```

### Pooled Allocation Scopes

When performing many geometry operations that allocate intermediate results (polygon booleans, skeleton computation, curve corrections), use `AllocationScope` to avoid GC pressure:

```csharp
using var scope = AllocationScope.Begin();

var polygon = somePolygon * new Size<float>(2, 2);  // pooled
var skeleton = polygon.Skeleton();                    // pooled
var lease = scope.Persist(ref skeleton);              // detach from scope

// scope.Dispose() returns everything except skeleton's memory
// lease.Dispose() returns skeleton's memory when you're done
```

### Serialization

**Protobuf** -- all core types carry `[ProtoContract]` / `[ProtoMember]` attributes for use with protobuf-net:

```csharp
var point = new Point<float>(1f, 2f);
byte[] bytes = Serializer.Serialize(point);
```

**JSON** -- custom `System.Text.Json` converters for `Point<T>`, `Point3<T>`, `Vector3<T>`, `Rotation3<T>`, `Pose3<T>`, `Joints6<T>`, `Polygon<T>`, `Polyline<T>`, `Polyline3<T>`, `PosePath3<T>`, `Trajectory3<T>`, `Path<T>`, `ComplexCurve<T>`, `BezierCurve<T>`, `Matrix2x2<T>`, `Matrix3x3<T>`, `Skeleton<T>`, `Color`, and `HsvColor`:

```csharp
var pose = new Pose3<double>(100, 200, 300, 45, 90, 0);
string json = JsonSerializer.Serialize(pose);  // [100,200,300,45,90,0]

var joints = new Joints6<float>(10, 20, 30, 40, 50, 60);
string j = JsonSerializer.Serialize(joints);   // [10,20,30,40,50,60]
```

**String parsing** -- `IParsable<T>` implementations on `Point<T>`, `Point3<T>`, `Vector3<T>`, `Rotation3<T>`, `Pose3<T>`, `Joints6<T>`, `Rectangle<T>`, `Size<T>`, `Path<T>`, `ComplexCurve<T>`, `Speed<T>`, `Color`, and `HsvColor`:

```csharp
var point = Point<double>.Parse("1.5, 2.5");
var point3 = Point3<double>.Parse("1.0 2.0 3.0");
var pose = Pose3<double>.Parse("100, 200, 300, 45, 90, 0");
var rect  = Rectangle<double>.Parse("0 0 100 200");
var color = Color.Parse("#FF8800");
var path  = Path<double>.Parse("M 0 0 C 1 2, 3 4, 5 6", null);
```

### SVG Export

`Polygon<T>`, `Polyline<T>`, `ComplexCurve<T>`, `Skeleton<T>`, and `Path<T>` support SVG export via the `[SvgExporter]` attribute and the `SvgExporter` infrastructure:

```csharp
// Path<T>.ToString() produces SVG path data
var path = Path<double>.FromPoints(points);
string svgData = path.ToString(); // "M 0 0 C 1.5 2.3, 3.1 4.2, 5 6 ..."
```

## Code Examples

### Points: Distance, Lerp, Rotate

```csharp
var a = new Point<double>(0, 0);
var b = new Point<double>(3, 4);

double dist = a.DistanceTo(b);               // 5.0
Point<double> quarter = a.Lerp(b, 0.25);     // {X=0.75, Y=1}
Point<double> reflected = a.Reflect(b);       // {X=6, Y=8}

// Rotate 90 degrees around the origin
Degree<double> angle = 90.0;
Point<double> rotated = b.Rotate(angle);      // {X=-4, Y=3}

// Rotate around a custom center
var center = new Point<double>(1, 1);
Point<double> rotated2 = b.Rotate(angle, center);

// Clamp to a rectangle
var bounds = new Rectangle<double>(0, 0, 10, 10);
Point<double> clamped = new Point<double>(15, -3).Clamp(bounds); // {X=10, Y=0}
```

### 3D Points, Vectors, and Rotations

```csharp
// 3D point operations
var p1 = new Point3<double>(1, 2, 3);
var p2 = new Point3<double>(4, 6, 3);
double dist3d = Point3<double>.Distance(p1, p2);  // 5.0
Point3<double> mid3d = Point3<double>.Lerp(p1, p2, 0.5);

// 3D vector operations
var v = new Vector3<double>(1, 0, 0);
var w = new Vector3<double>(0, 1, 0);
var cross = Vector3<double>.Cross(v, w);           // (0, 0, 1)
double dot = Vector3<double>.Dot(v, w);            // 0.0

// Compute rotation between two directions
Rotation3<double> rot = v.RotationTo(w);
Vector3<double> result = rot.Rotate(v);            // approximately (0, 1, 0)

// Euler angle rotation (ZYX convention)
var rotation = Rotation3<double>.FromDegrees(45, 0, 0);
Point3<double> rotated = rotation.Rotate(p1);

// Quaternion slerp
var r1 = Rotation3<double>.FromDegrees(0, 0, 0);
var r2 = Rotation3<double>.FromDegrees(0, 0, 90);
var halfway = Rotation3<double>.Slerp(r1, r2, 0.5);

// Matrix3x3 rotation
var m = Matrix3x3<double>.RotationZ(Degree<double>.Create(45));
var transformed = m.Transform(new Point3<double>(1, 0, 0));
```

### Spherical Coordinates

```csharp
// Create spherical points with factory methods
var northPole = SphericalPoint<double>.NorthPole(10.0);    // +Z, radius=10
var equator   = SphericalPoint<double>.OnEquator(5.0, Degree<double>.Create(45));
var elevated  = SphericalPoint<double>.FromElevation(5.0,
                    Degree<double>.Create(0), Degree<double>.Create(30));

// Convert to/from Cartesian
Point3<double> cartesian = equator;                        // implicit conversion
SphericalPoint<double> spherical = cartesian;              // implicit conversion

// Orbit around a 3D center
var center = new Point3<double>(10, 50, 0);
var points = new List<Point3<double>>();
for (int az = 0; az < 360; az += 30)
    points.Add(center + SphericalPoint<double>.OnEquator(5.0, Degree<double>.Create(az)));

// Rotation (O(1) angle operations)
var sp = SphericalPoint<double>.OnEquator(5.0, Degree<double>.Create(0));
var rotated = sp.RotateAzimuth(Degree<double>.Create(45)); // rotate around Z

// Angular distance (Vincenty formula -- numerically stable)
var a = SphericalPoint<double>.NorthPole(1.0);
var b = SphericalPoint<double>.SouthPole(1.0);
Degree<double> angDist = SphericalPoint<double>.AngularDistance(a, b); // 180 degrees

// Spherical linear interpolation
var mid = SphericalPoint<double>.Slerp(a, b, 0.5);

// Cylindrical coordinate conversions
CylindricalPoint<double> cyl = sp;                        // implicit conversion
SphericalPoint<double> back = cyl;                         // implicit conversion
```

### Poses and Trajectories

```csharp
// Pose = position + orientation
var pose = new Pose3<double>(100, 200, 300, 45, 0, 0); // x,y,z, rx,ry,rz (degrees)

// Compose poses
var world = new Pose3<double>(new Point3<double>(10, 0, 0), Rotation3<double>.Identity);
var local = new Pose3<double>(new Point3<double>(5, 0, 0), Rotation3<double>.FromDegrees(0, 0, 90));
var combined = world * local;

// Create frame from a surface (3 points define a plane)
var a = new Point3<double>(0, 0, 0);
var b = new Point3<double>(1, 0, 0);
var c = new Point3<double>(0, 1, 0);
var surfacePose = Pose3<double>.FromSurface(a, b, c); // Z-axis via right-hand rule

// Trajectory with timed waypoints
var trajectory = new Trajectory3<double>(
    new Waypoint3<double>(new Pose3<double>(0, 0, 0, 0, 0, 0), 0.0),
    new Waypoint3<double>(new Pose3<double>(50, 0, 25, 0, 30, 0), 2.5),
    new Waypoint3<double>(new Pose3<double>(100, 0, 0, 0, 0, 0), 5.0));

// Interpolate at any time (binary search + lerp/slerp)
Pose3<double> atT = trajectory.AtTime(1.25);

// Resample at uniform intervals
var resampled = trajectory.Resample(0.1); // every 0.1 seconds

// Real-time playback
var controller = trajectory.Control();
controller.PoseChanged += (_, e) => Console.WriteLine($"t={e.Time} pose={e.Pose}");
controller.Start();

// 6-DOF joint angles for robotics
var home = new Joints6<double>(0, -90, 0, 0, 90, 0);
var target = new Joints6<double>(45, -60, 30, 0, 90, -45);
var halfway = Joints6<double>.Lerp(home, target, 0.5);
bool inRange = target.IsBetween(minLimits, maxLimits);
```

### Shapes: Area and Perimeter

```csharp
// Circle
var circle = new Circle<double>(new Point<double>(5, 5), 3.0);
double cArea = circle.Area();        // ~28.274
double cPerim = circle.Perimeter();  // ~18.850

// Triangle
var triangle = new Triangle<double>(
    new Point<double>(0, 0),
    new Point<double>(4, 0),
    new Point<double>(0, 3));
double tArea = triangle.Area();      // 6.0
double tPerim = triangle.Perimeter(); // 12.0

// Polygon (shoelace formula)
var poly = new Polygon<double>(
    new Point<double>(0, 0), new Point<double>(4, 0),
    new Point<double>(4, 3), new Point<double>(0, 3));
double pArea = poly.Area();          // 12.0

// 3D triangle
var t3 = new Triangle3<double>(
    new Point3<double>(0, 0, 0),
    new Point3<double>(4, 0, 0),
    new Point3<double>(0, 3, 0));
double area3 = t3.Area;             // 6.0
Vector3<double> normal = t3.Normal; // (0, 0, 1)
```

### Line-Circle Intersection

```csharp
var circle = new Circle<double>(new Point<double>(0, 0), 5.0);
var line = Line<double>.From(new Point<double>(-10, 0), new Point<double>(10, 0));

// Secant -- returns the chord as a segment
Segment<double>? chord = circle.Intersect(line);
// chord.Value.Start ~ {X=-5, Y=0}, chord.Value.End ~ {X=5, Y=0}

// Tangent detection
var tangentLine = Line<double>.Horizontal(5.0);
Point<double>? tangent = circle.TangentPoint(tangentLine);
// tangent.Value ~ {X=0, Y=5}
```

### Segment-Rectangle Clipping

```csharp
var viewport = new Rectangle<double>(0, 0, 100, 100);
var segment = new Segment<double>(
    new Point<double>(-20, 50),
    new Point<double>(150, 50));

// Liang-Barsky clipping
Segment<double>? clipped = segment.Intersect(viewport);
// clipped.Value: Start={X=0, Y=50}, End={X=100, Y=50}
```

### Path-Rectangle Clipping

```csharp
var points = new[]
{
    new Point<double>(10, 50),
    new Point<double>(50, 10),
    new Point<double>(90, 50),
    new Point<double>(130, 90)
};
var path = Path<double>.FromPoints(points);
var viewport = new Rectangle<double>(20, 20, 60, 60); // 20,20 to 80,80

// Exact Bezier clipping -- returns sub-paths with curves split at boundaries
foreach (var subPath in path.Intersect(viewport))
{
    Console.WriteLine($"Sub-path with {subPath.Count} Bezier segments");
}
```

### Triangle Properties

```csharp
var t = new Triangle<double>(
    new Point<double>(0, 0),
    new Point<double>(4, 0),
    new Point<double>(2, 3));

// Derived circles
Circle<double> incircle = t.Incircle();       // largest circle inside
Circle<double> circumcircle = t.Circumcircle(); // passes through all vertices

// Center points
Point<double> centroid = t.Centroid();
Point<double> orthocenter = t.Orthocenter;

// Classification
bool right = t.IsRight();
bool equilateral = t.IsEquilateral();
bool isosceles = t.IsIsosceles();

// Similarity and congruence
var t2 = new Triangle<double>(
    new Point<double>(0, 0),
    new Point<double>(8, 0),
    new Point<double>(4, 6));
bool similar = t.IsSimilarTo(t2);     // true (same shape, scaled 2x)
bool congruent = t.IsCongruentTo(t2); // false (different size)

// Interior angles
var (atA, atB, atC) = t.Angles;
```

### Polygon Operations

```csharp
// Convex hull
var cloud = new Polygon<double>(
    new Point<double>(0, 0), new Point<double>(1, 3),
    new Point<double>(2, 1), new Point<double>(4, 4),
    new Point<double>(3, 0), new Point<double>(5, 2));
Polygon<double> hull = cloud.ConvexHull();

// Point containment (ray casting)
bool inside = hull.Contains(new Point<double>(2, 2));

// Boolean operations
var a = new Polygon<double>( /* ... */ );
var b = new Polygon<double>( /* ... */ );
List<Polygon<double>> union = a.Union(b, removeHoles: true);
List<Polygon<double>> diff = a.Subtract(b);
List<Polygon<double>> inter = a.Intersect(b);

// Simplify (remove collinear/close points)
Polygon<double> simplified = hull.Simplify(epsilon: 0.1);

// Convexity test
bool convex = hull.IsConvex();

// Clustering overlapping polygons
var clusters = Polygon<double>.Cluster(polygons);
```

### Matrix Transformations (2D and 3D)

```csharp
// 2D affine transformation
var matrix = Matrix<double>.Identity;
matrix.Translate(10.0, 20.0);
matrix.Rotate(Degree<double>.Create(45.0));
matrix.Scale(2.0, 2.0);

Point<double> transformed = matrix.Transform(new Point<double>(1, 0));
Vector<double> rotatedVec = matrix.Transform(new Vector<double>(1, 0));

var combined = matrix1 * matrix2;
matrix.Invert();

// 3D rotation matrix
var rx = Matrix3x3<double>.RotationX(Degree<double>.Create(45));
var ry = Matrix3x3<double>.RotationY(Degree<double>.Create(30));
var rz = Matrix3x3<double>.RotationZ(Degree<double>.Create(60));

// Compose rotations
var rotation = rz * ry * rx;
var p = new Point3<double>(1, 0, 0);
Point3<double> rotated3 = rotation * p; // or rotation.Transform(p)

// Extract Euler angles
var (eRx, eRy, eRz) = rotation.ToEulerZYX();

// Convert to quaternion
Quaternion<double> q = rotation.ToQuaternion();
```

## Requirements

- **.NET 10.0** or later (uses generic math interfaces from .NET 7+)
- **Dependencies**: [Clipper2](https://www.nuget.org/packages/Clipper2) 1.4.0, [protobuf-net](https://www.nuget.org/packages/protobuf-net) 3.2.45

## License

See [LICENSE](LICENSE) for details.
