# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ModelingEvolution.Drawing is a .NET library that provides generic math types and utilities for 2D drawing and geometry. It targets .NET 9.0 and is designed with high-performance generic math capabilities using `INumber<T>` constraints.

## Build and Test Commands

```bash
# Build the solution
dotnet build Sources/ModelingEvolution.Drawing.sln

# Build in Release configuration
dotnet build Sources/ModelingEvolution.Drawing.sln --configuration Release

# Run all tests
dotnet test Sources/ModelingEvolution.Drawing.Tests/ModelingEvolution.Drawing.Tests.csproj

# Run tests with detailed output
dotnet test Sources/ModelingEvolution.Drawing.Tests/ModelingEvolution.Drawing.Tests.csproj --verbosity normal --logger:"console;verbosity=detailed"

# Run a specific test
dotnet test Sources/ModelingEvolution.Drawing.Tests/ModelingEvolution.Drawing.Tests.csproj --filter "FullyQualifiedName~TestName"

# Create NuGet package
dotnet pack Sources/ModelingEvolution.Drawing/ModelingEvolution.Drawing.csproj --configuration Release

# Restore dependencies
dotnet restore Sources/ModelingEvolution.Drawing.sln
```

## Architecture and Key Components

### Core Generic Types
All major geometric types are implemented as generic structs with `T` constrained to numeric types (`INumber<T>` and related interfaces):
- **Point<T>**: Cartesian 2D point representation
- **Vector<T>**: 2D vector with magnitude and direction operations
- **Rectangle<T>**: Axis-aligned rectangle with position and size
- **Size<T>**: Width and height dimensions
- **Matrix<T>**: 2D transformation matrix

### Coordinate Systems
- **PolarPoint<T>**: Polar coordinate representation with conversions to/from Cartesian
- **Degree<T>** and **Radian<T>**: Angular measurements with automatic conversions

### Curves and Shapes
- **BezierCurve<T>**: Cubic Bezier curve implementation
- **PolygonalCurve<T>**: Series of connected line segments
- **Polygon**: Complex polygon operations with Clipper2 integration
- **RadialCurveF**: Radial curve implementations

### Mathematical Equations
Located in `Sources/ModelingEvolution.Drawing/Equations/`:
- **LinearEquation**: Line equations and intersections
- **QuadraticEquation**: Quadratic curve calculations
- **CubicEquation**: Cubic polynomial solutions
- **CircleEquation**: Circle geometry and intersections

### Serialization Support
- **ProtoBuf-Net**: All core types have `[ProtoContract]` attributes for binary serialization
- **JSON Converters**: Custom converters for Point, Polygon, and Color types
- **Type Converters**: System.ComponentModel converters for UI frameworks

### Color System
- **Color**: RGBA color representation with web color parsing
- **HsvColor**: HSV color space with conversions to/from RGB
- **Colors**: Static color definitions and utilities

### Dependencies
- **Clipper2** (1.4.0): Advanced polygon clipping and offsetting operations
- **protobuf-net** (3.2.45): High-performance binary serialization

### Test Framework
Tests use xUnit with:
- **FluentAssertions**: Readable assertion syntax
- **NSubstitute**: Mocking framework
- Test files follow pattern `*Tests.cs` in `Sources/ModelingEvolution.Drawing.Tests/`

## Development Notes

- All numeric types use generic math interfaces from .NET 7+ for maximum performance and flexibility
- The library generates XML documentation (`.xml` files) for IntelliSense support
- Version numbers are managed in the `.csproj` file and should be updated for releases
- GitHub Actions workflows handle CI/CD (see `.github/workflows/`)