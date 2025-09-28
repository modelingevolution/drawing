using FluentAssertions;
using ModelingEvolution.Drawing;
using Xunit;

namespace ModelingEvolution.Drawing.Tests;

public class TupleOperatorTests
{
    [Fact]
    public void Point_TupleConversion_Works()
    {
        // Arrange
        var tuple = (3.5f, 4.2f);

        // Act - Implicit conversion from tuple to Point
        Point<float> point = tuple;

        // Assert
        point.X.Should().Be(3.5f);
        point.Y.Should().Be(4.2f);

        // Act - Implicit conversion from Point to tuple
        (float x, float y) result = point;

        // Assert
        result.x.Should().Be(3.5f);
        result.y.Should().Be(4.2f);
    }

    [Fact]
    public void Vector_TupleConversion_Works()
    {
        // Arrange
        var tuple = (10.0, 20.0);

        // Act - Implicit conversion from tuple to Vector
        Vector<double> vector = tuple;

        // Assert
        vector.X.Should().Be(10.0);
        vector.Y.Should().Be(20.0);

        // Act - Implicit conversion from Vector to tuple
        (double x, double y) result = vector;

        // Assert
        result.x.Should().Be(10.0);
        result.y.Should().Be(20.0);
    }

    [Fact]
    public void Rectangle_TupleConversion_Works()
    {
        // Arrange
        var tuple = (1.0f, 2.0f, 100.0f, 200.0f);

        // Act - Implicit conversion from tuple to Rectangle
        Rectangle<float> rectangle = tuple;

        // Assert
        rectangle.X.Should().Be(1.0f);
        rectangle.Y.Should().Be(2.0f);
        rectangle.Width.Should().Be(100.0f);
        rectangle.Height.Should().Be(200.0f);

        // Act - Implicit conversion from Rectangle to tuple
        (float x, float y, float width, float height) result = rectangle;

        // Assert
        result.x.Should().Be(1.0f);
        result.y.Should().Be(2.0f);
        result.width.Should().Be(100.0f);
        result.height.Should().Be(200.0f);
    }

    [Fact]
    public void Size_TupleConversion_Works()
    {
        // Arrange
        var tuple = (50.5, 75.5);

        // Act - Implicit conversion from tuple to Size
        Size<double> size = tuple;

        // Assert
        size.Width.Should().Be(50.5);
        size.Height.Should().Be(75.5);

        // Act - Implicit conversion from Size to tuple
        (double width, double height) result = size;

        // Assert
        result.width.Should().Be(50.5);
        result.height.Should().Be(75.5);
    }

    [Fact]
    public void Thickness_TupleConversion_Works()
    {
        // Arrange
        var tuple = (5.0f, 10.0f, 15.0f, 20.0f);

        // Act - Implicit conversion from tuple to Thickness
        Thickness<float> thickness = tuple;

        // Assert
        thickness.Left.Should().Be(5.0f);
        thickness.Top.Should().Be(10.0f);
        thickness.Right.Should().Be(15.0f);
        thickness.Bottom.Should().Be(20.0f);

        // Act - Implicit conversion from Thickness to tuple
        (float left, float top, float right, float bottom) result = thickness;

        // Assert
        result.left.Should().Be(5.0f);
        result.top.Should().Be(10.0f);
        result.right.Should().Be(15.0f);
        result.bottom.Should().Be(20.0f);
    }

    [Fact]
    public void Color_RGBATupleConversion_Works()
    {
        // Arrange
        var rgbaTuple = ((byte)255, (byte)128, (byte)64, (byte)200);

        // Act - Implicit conversion from RGBA tuple to Color
        Color color = rgbaTuple;

        // Assert
        color.R.Should().Be(255);
        color.G.Should().Be(128);
        color.B.Should().Be(64);
        color.A.Should().Be(200);

        // Act - Implicit conversion from Color to RGBA tuple
        (byte r, byte g, byte b, byte a) result = color;

        // Assert
        result.r.Should().Be(255);
        result.g.Should().Be(128);
        result.b.Should().Be(64);
        result.a.Should().Be(200);
    }

    [Fact]
    public void Color_RGBTupleConversion_Works()
    {
        // Arrange
        var rgbTuple = ((byte)100, (byte)150, (byte)200);

        // Act - Implicit conversion from RGB tuple to Color
        Color color = rgbTuple;

        // Assert
        color.R.Should().Be(100);
        color.G.Should().Be(150);
        color.B.Should().Be(200);
        color.A.Should().Be(255); // Default alpha for RGB tuple

        // Act - Conversion to RGBA tuple to verify
        (byte r, byte g, byte b, byte a) result = color;

        // Assert
        result.r.Should().Be(100);
        result.g.Should().Be(150);
        result.b.Should().Be(200);
        result.a.Should().Be(255);
    }

    [Fact]
    public void HsvColor_TupleConversion_Works()
    {
        // Arrange
        var tuple = (120.0f, 0.5f, 0.8f);

        // Act - Implicit conversion from tuple to HsvColor
        HsvColor hsvColor = tuple;

        // Assert
        hsvColor.H.Should().Be(120.0f);
        hsvColor.S.Should().Be(0.5f);
        hsvColor.V.Should().Be(0.8f);
        hsvColor.A.Should().Be(1.0f); // Default alpha

        // Act - Implicit conversion from HsvColor to tuple
        (float h, float s, float v) result = hsvColor;

        // Assert
        result.h.Should().Be(120.0f);
        result.s.Should().Be(0.5f);
        result.v.Should().Be(0.8f);
    }

    [Fact]
    public void Point_TupleRoundTrip_PreservesValues()
    {
        // Arrange
        Point<double> original = new(42.42, 84.84);

        // Act - Convert to tuple and back
        (double x, double y) tuple = original;
        Point<double> restored = tuple;

        // Assert
        restored.Should().Be(original);
    }

    [Fact]
    public void Rectangle_TupleRoundTrip_PreservesValues()
    {
        // Arrange
        Rectangle<float> original = new(10f, 20f, 30f, 40f);

        // Act - Convert to tuple and back
        (float x, float y, float w, float h) tuple = original;
        Rectangle<float> restored = tuple;

        // Assert
        restored.Should().Be(original);
    }

    [Fact]
    public void Color_TupleRoundTrip_PreservesValues()
    {
        // Arrange
        Color original = Color.FromArgb(100, 150, 200, 250);

        // Act - Convert to tuple and back
        (byte r, byte g, byte b, byte a) tuple = original;
        Color restored = tuple;

        // Assert
        restored.Should().Be(original);
    }
}