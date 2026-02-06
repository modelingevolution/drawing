# ModelingEvolution.Drawing

[![NuGet](https://img.shields.io/nuget/v/ModelingEvolution.Drawing.svg)](https://www.nuget.org/packages/ModelingEvolution.Drawing)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ModelingEvolution.Drawing.svg)](https://www.nuget.org/packages/ModelingEvolution.Drawing)
[![CI](https://github.com/modelingevolution/drawing/actions/workflows/ci.yml/badge.svg)](https://github.com/modelingevolution/drawing/actions/workflows/ci.yml)
[![License](https://img.shields.io/github/license/modelingevolution/drawing.svg)](https://github.com/modelingevolution/drawing/blob/main/LICENSE)

A high-performance .NET generic math library for 2D geometry, shapes, intersections, and drawing primitives. Built on `INumber<T>` constraints from .NET generic math, all types work with `float`, `double`, `decimal`, or any compatible numeric type -- with zero boxing and full type safety.

## Table of Contents

- [Why This Library](#why-this-library)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [API Reference](#api-reference)
  - [Core Types](#core-types)
  - [Shapes](#shapes)
  - [Lines and Segments](#lines-and-segments)
  - [Curves and Paths](#curves-and-paths)
  - [Angles and Coordinates](#angles-and-coordinates)
  - [Equations](#equations)
  - [Colors](#colors)
  - [Interfaces](#interfaces)
- [Key Features](#key-features)
  - [Generic Math](#generic-math)
  - [IShape Interface Hierarchy](#ishape-interface-hierarchy)
  - [Intersections](#intersections)
  - [Polygon Boolean Operations](#polygon-boolean-operations)
  - [Bezier Path Clipping](#bezier-path-clipping)
  - [Serialization](#serialization)
  - [SVG Export](#svg-export)
- [Code Examples](#code-examples)
  - [Points: Distance, Lerp, Rotate](#points-distance-lerp-rotate)
  - [Shapes: Area and Perimeter](#shapes-area-and-perimeter)
  - [Line-Circle Intersection](#line-circle-intersection)
  - [Segment-Rectangle Clipping](#segment-rectangle-clipping)
  - [Path-Rectangle Clipping](#path-rectangle-clipping)
  - [Triangle Properties](#triangle-properties)
  - [Polygon Operations](#polygon-operations)
  - [Matrix Transformations](#matrix-transformations)
- [Requirements](#requirements)
- [License](#license)

## Why This Library

- **Generic math** -- write geometry code once; use it with `float`, `double`, or any `IFloatingPointIeee754<T>` type.
- **No boxing** -- all core types are `struct` or `readonly record struct`, avoiding heap allocations.
- **Rich interface hierarchy** -- `IShape<T, TSelf>` unifies area, perimeter, centroid, bounding box, containment, rotation, and scaling across all shape types.
- **Comprehensive intersections** -- 21 intersection combinations (line, segment, circle, triangle, rectangle, polygon) with zero-alloc `FirstOf` variants.
- **Polygon booleans** -- union, intersection, difference, and clustering via Clipper2.
- **Exact Bezier clipping** -- `Path<T>.Intersect(Rectangle<T>)` computes analytically exact sub-curves, not polygon approximations.
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

// Points and vectors (using double precision)
var a = new Point<double>(1.0, 2.0);
var b = new Point<double>(4.0, 6.0);

double distance = a.DistanceTo(b);          // 5.0
Point<double> mid = a.Lerp(b, 0.5);         // {X=2.5, Y=4}

// Shapes with area and perimeter
var circle = new Circle<double>(Point<double>.Zero, 10.0);
double area = circle.Area();                 // ~314.159
double perimeter = circle.Perimeter();       // ~62.832
bool inside = circle.Contains(new Point<double>(3.0, 4.0)); // true

// Intersections
var line = Line<double>.From(new Point<double>(0, 0), new Point<double>(10, 10));
Segment<double>? chord = circle.Intersect(line); // the chord through the circle

// Polygon boolean operations
var poly1 = new Polygon<double>(
    new Point<double>(0, 0), new Point<double>(10, 0),
    new Point<double>(10, 10), new Point<double>(0, 10));
var poly2 = new Polygon<double>(
    new Point<double>(5, 5), new Point<double>(15, 5),
    new Point<double>(15, 15), new Point<double>(5, 15));

Polygon<double> merged = poly1 | poly2;     // union
Polygon<double> overlap = poly1 & poly2;    // intersection
```

## API Reference

### Core Types

| Type | Description |
|---|---|
| `Point<T>` | 2D Cartesian point. Supports arithmetic with vectors and sizes, distance, lerp, reflect, rotate, clamp, midpoint, matrix transform, tuple conversion, and parsing. |
| `Vector<T>` | 2D vector with magnitude, direction, dot/cross product, normalize, projection, reflect, rotate, perpendicular (CW/CCW), angle between, lerp, and matrix transform. |
| `Size<T>` | Width/height pair with arithmetic operators, component-wise multiply/divide, and parsing. |
| `Matrix<T>` | 3x3 affine transformation matrix. Supports translate, rotate, scale, skew, invert, append/prepend, and point/vector transforms. |
| `Rectangle<T>` | Axis-aligned rectangle with contains, intersect, inflate, offset, bounds, tiling, diagonal, distance-to-point, and LTRB construction. |

### Shapes

All shapes implement `IShape<T, TSelf>`, providing `Area()`, `Perimeter()`, `Centroid()`, `BoundingBox()`, `Contains(Point<T>)`, `Rotate(Degree<T>, Point<T>)`, and `Scale(T)`.

| Type | Description |
|---|---|
| `Circle<T>` | Center + radius. Intersects with lines, segments, circles, and rectangles. Tangent point detection. `PointAt(Radian<T>)` for parameterized access. |
| `Triangle<T>` | Three vertices. Incircle, circumcircle, orthocenter, angles, edges. Classification: `IsRight()`, `IsAcute()`, `IsObtuse()`, `IsEquilateral()`, `IsIsosceles()`, `IsScalene()`. Similarity and congruence tests. |
| `Polygon<T>` | Immutable polygon backed by `ReadOnlyMemory<Point<T>>`. Shoelace area, ray-casting containment, convex hull (Graham scan), `IsConvex()`, edges, simplify. Boolean operations: union, intersect, subtract, cluster. Operators: `\|` (union), `&` (intersect), `-` (subtract). |
| `Rectangle<T>` | Implements `IArea<T>`, `IPerimeter<T>`, `ICentroid<T>`, `IBoundingBox<T>`, `IScalable<T, Rectangle<T>>`. Rotation returns `Polygon<T>`. |

### Lines and Segments

| Type | Description |
|---|---|
| `Line<T>` | Infinite line (supports vertical lines). From two points, point + direction, or equation. Intersect with lines, segments, circles, triangles, rectangles, polygons. Distance-to-point, angle-between, parallel/perpendicular tests, project point, reflect point. |
| `Segment<T>` | Bounded line segment (start + end). Length, midpoint, direction, lerp, split, reverse. Intersect with segments, lines, circles, rectangles. Distance-to-point, project point, parallel test. Liang-Barsky rectangle clipping. |

### Curves and Paths

| Type | Description |
|---|---|
| `BezierCurve<T>` | Cubic Bezier curve (4 control points). `Evaluate(t)`, `Split(t)`, `SubCurve(t0, t1)` via De Casteljau. Extremum points, linear equation intersection, rectangle edge crossings. |
| `Path<T>` | Immutable sequence of `BezierCurve<T>` segments. Create from points (`FromPoints`) with smoothing, or from SVG path data (`Parse`). `Length()`, `PointAt(t)` with arc-length parameterization. Exact `Intersect(Rectangle<T>)` that splits Bezier curves at crossing points. Transform, rotate, reverse, append. `Close()` and `ToPolygon()` for rasterization. |
| `PolygonalCurve<T>` | Mutable sequence of connected points that generates smooth Bezier segments via `GetSmoothSegment(i)`. |

### Angles and Coordinates

| Type | Description |
|---|---|
| `Degree<T>` | Angle in degrees. Implicit conversion from numeric values and from `Radian<T>`. Arithmetic, normalize to (-180, 180], abs. |
| `Radian<T>` | Angle in radians. Implicit conversion from `Degree<T>`. Arithmetic, normalize to (-pi, pi], abs. Static `Sin`/`Cos` helpers. |
| `PolarPoint<T>` | Polar coordinates (radius, angle). Implicit conversions to/from `Point<T>`, explicit to `Vector2` and `Vector<T>`. |
| `CylindricalPoint<T>` | Cylindrical coordinates (radius, angle, height). Implicit from `PolarPoint<T>`, to `Vector3`. |

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

### Serialization

**Protobuf** -- all core types carry `[ProtoContract]` / `[ProtoMember]` attributes for use with protobuf-net:

```csharp
var point = new Point<float>(1f, 2f);
byte[] bytes = Serializer.Serialize(point);
```

**JSON** -- custom `System.Text.Json` converters for `Point<T>`, `Polygon<T>`, `Path<T>`, `Color`, and `HsvColor`:

```csharp
var polygon = new Polygon<double>( /* ... */ );
string json = JsonSerializer.Serialize(polygon);
```

**String parsing** -- `IParsable<T>` implementations on `Point<T>`, `Rectangle<T>`, `Size<T>`, `Path<T>`, `Color`, and `HsvColor`:

```csharp
var point = Point<double>.Parse("1.5, 2.5");
var rect  = Rectangle<double>.Parse("0 0 100 200");
var color = Color.Parse("#FF8800");
var path  = Path<double>.Parse("M 0 0 C 1 2, 3 4, 5 6", null);
```

### SVG Export

`Polygon<T>` and `Path<T>` support SVG export via the `[SvgExporter]` attribute and the `SvgExporter` infrastructure:

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

### Matrix Transformations

```csharp
// Build a transformation
var matrix = Matrix<double>.Identity;
matrix.Translate(10.0, 20.0);
matrix.Rotate(Degree<double>.Create(45.0));
matrix.Scale(2.0, 2.0);

// Transform points and vectors
Point<double> transformed = matrix.Transform(new Point<double>(1, 0));
Vector<double> rotatedVec = matrix.Transform(new Vector<double>(1, 0));

// Compose matrices
var combined = matrix1 * matrix2;

// Invert
matrix.Invert();
Point<double> original = matrix.Transform(transformed);

// Transform a path
var path = Path<double>.FromPoints(points);
Path<double> transformedPath = path.Transform(matrix);
```

## Requirements

- **.NET 9.0** or later (uses generic math interfaces from .NET 7+)
- **Dependencies**: [Clipper2](https://www.nuget.org/packages/Clipper2) 1.4.0, [protobuf-net](https://www.nuget.org/packages/protobuf-net) 3.2.45

## License

See [LICENSE](LICENSE) for details.
