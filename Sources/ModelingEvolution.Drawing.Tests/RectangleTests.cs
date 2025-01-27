using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
