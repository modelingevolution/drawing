using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;

namespace ModelingEvolution.Drawing.Tests
{
    public class RectangleTests
    {
        [Fact]
        public void ComputeTiles_ReturnsEmptyArray_WhenTileSizeIsZero()
        {
            // Arrange
            var rectangle = new Rectangle<float>(0, 0, 100, 100);
            var tileSize = new Size<float> { Width = 0, Height = 0 };
            // Act
            var result = rectangle.ComputeTiles(tileSize);
            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }
        [Fact]
        public void ComputeTiles_ReturnsEmptyArray_WhenRectangleSizeIsZero()
        {
            // Arrange
            var rectangle = new Rectangle<float>(0, 0, 0, 0);
            var tileSize = new Size<float> { Width = 10, Height = 10 };
            // Act
            var result = rectangle.ComputeTiles(tileSize);
            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }
        [Fact]
        public void ComputeTiles_ReturnsCorrectNumberOfTiles_WithNoOverlap()
        {
            // Arrange
            var rectangle = new Rectangle<float>(0, 0, 100, 100);
            var tileSize = new Size<float> { Width = 20, Height = 20 };
            // Act
            var result = rectangle.ComputeTiles(tileSize);
            // Assert
            Assert.NotNull(result);
            Assert.Equal(25, result.Length); // 5x5 tiles
        }
        [Fact]
        public void ComputeTiles_ReturnsCorrectNumberOfTiles_WithOverlap()
        {
            // Arrange
            var rectangle = new Rectangle<float>(0, 0, 100, 100);
            var tileSize = new Size<float> { Width = 20, Height = 20 };
            float overlapPercentage = 0.5f;
            // Act
            var result = rectangle.ComputeTiles(tileSize, overlapPercentage);
            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > 25); // More tiles due to overlap
        }
        [Fact]
        public void ComputeTiles_AdjustsLastTilePosition_ToStayWithinBounds()
        {
            // Arrange
            var rectangle = new Rectangle<float>(0, 0, 100, 100);
            var tileSize = new Size<float> { Width = 30, Height = 30 };
            // Act
            var result = rectangle.ComputeTiles(tileSize);
            // Assert
            Assert.NotNull(result);
            foreach (var tile in result)
            {
                Assert.True(tile.Right <= rectangle.Right);
                Assert.True(tile.Bottom <= rectangle.Bottom);
            }
        }
        [Fact]
        public void ComputeTiles_HorizontalTiling_ChecksIntersections()
        {
            // Arrange
            var rectangle = new Rectangle<float>(0, 0, 100, 50); // Rectangle of width 100 and height 50
            var tileSize = new Size<float> { Width = 60, Height = 50 }; // Tile size
            float overlapPercentage = 0.5f; // 50% overlap
            // Act
            var tiles = rectangle.ComputeTiles(tileSize, overlapPercentage);
            // Assert
            Assert.NotNull(tiles);
            Assert.Equal(3, tiles.Length); // Expect 3 tiles due to overlap
                                           // Validate intersections

            var intersection = tiles[0] & tiles[1];
            Assert.False(intersection.IsEmpty); // Ensure there is an overlap
            Assert.Equal(30, intersection.Width); // 50% of tile width (60 * 0.5)
            Assert.Equal(50, intersection.Height); // Full height of the tiles

            intersection = tiles[1] & tiles[2];
            Assert.False(intersection.IsEmpty); // Ensure there is an overlap
            Assert.Equal(50, intersection.Width); 
            Assert.Equal(50, intersection.Height); // Full height of the tiles

        }
        [Fact]
        public void ComputeTiles_VerticalTiling_ChecksIntersections()
        {
            // Arrange
            var rectangle = new Rectangle<float>(0, 0, 50, 100); // Rectangle of width 50 and height 100
            var tileSize = new Size<float> { Width = 50, Height = 60 }; // Tile size
            float overlapPercentage = 0.5f; // 50% overlap
            // Act
            var tiles = rectangle.ComputeTiles(tileSize, overlapPercentage);
            // Assert
            Assert.NotNull(tiles);
            Assert.Equal(3, tiles.Length); // Expect 3 tiles due to overlap
                                           // Validate intersections

            var intersection = tiles[0] & tiles[1];
            Assert.False(intersection.IsEmpty); // Ensure there is an overlap
            Assert.Equal(50, intersection.Width); // Full width of the tiles
            Assert.Equal(30, intersection.Height); // 50% of tile height (60 * 0.5)
            
            intersection = tiles[1] & tiles[2];
            Assert.False(intersection.IsEmpty); // Ensure there is an overlap
            Assert.Equal(50, intersection.Width); // Full width of the tiles
            Assert.Equal(50, intersection.Height); // 50% of tile height (60 * 0.5)

        }

        [Fact]
        public void Rotate_90Degrees_ReturnsPolygonWithCorrectVertices()
        {
            var rect = new Rectangle<double>(0, 0, 10, 5);
            var rotated = rect.Rotate(Degree<double>.Create(90));

            rotated.Count.Should().Be(4);
            // TL (0,0) → (0,0), TR (10,0) → (0,10), BR (10,5) → (-5,10), BL (0,5) → (-5,0)
            rotated[0].X.Should().BeApproximately(0, 1e-9);
            rotated[0].Y.Should().BeApproximately(0, 1e-9);
            rotated[1].X.Should().BeApproximately(0, 1e-9);
            rotated[1].Y.Should().BeApproximately(10, 1e-9);
            rotated[2].X.Should().BeApproximately(-5, 1e-9);
            rotated[2].Y.Should().BeApproximately(10, 1e-9);
            rotated[3].X.Should().BeApproximately(-5, 1e-9);
            rotated[3].Y.Should().BeApproximately(0, 1e-9);
        }

        [Fact]
        public void Rotate_PreservesArea()
        {
            var rect = new Rectangle<double>(0, 0, 10, 5);
            var rotated = rect.Rotate(Degree<double>.Create(37));
            rotated.Area().Should().BeApproximately(50, 1e-8);
        }

        [Fact]
        public void Rotate_AroundCenter()
        {
            var rect = new Rectangle<double>(0, 0, 10, 10);
            var center = rect.Centroid();
            var rotated = rect.Rotate(Degree<double>.Create(45), center);
            // Area should be preserved
            rotated.Area().Should().BeApproximately(100, 1e-8);
        }
    }
}
