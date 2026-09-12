using System.Collections.Generic;
using System.Linq;
using PosCs.Domain.Exceptions;
using PosCs.Domain.Rules;
using Xunit;

namespace PosCs.Tests.Domain
{
    public class FifoAllocatorTests
    {
        private static FifoAllocator.Layer L(string id, double remaining) => new FifoAllocator.Layer(id, remaining);

        [Fact]
        public void Consumes_SingleLayer_Exactly()
        {
            var layers = new[] { L("a", 10) };

            var result = FifoAllocator.Allocate(layers, 7);

            var alloc = Assert.Single(result);
            Assert.Equal("a", alloc.LayerId);
            Assert.Equal(7, alloc.Quantity);
        }

        [Fact]
        public void Spills_Into_NextLayers_InOrder()
        {
            var layers = new[] { L("a", 4), L("b", 6), L("c", 100) };

            var result = FifoAllocator.Allocate(layers, 9);

            Assert.Equal(2, result.Count);
            Assert.Equal(("a", 4), (result[0].LayerId, result[0].Quantity));
            Assert.Equal(("b", 5), (result[1].LayerId, result[1].Quantity));
            Assert.False(result.Any(x => x.LayerId == "c"));
        }

        [Fact]
        public void Skips_Empty_And_Negative_Layers()
        {
            var layers = new[] { L("empty", 0), L("negative", -2), L("good", 5) };

            var result = FifoAllocator.Allocate(layers, 3);

            var alloc = Assert.Single(result);
            Assert.Equal("good", alloc.LayerId);
            Assert.Equal(3, alloc.Quantity);
        }

        [Fact]
        public void Throws_When_Total_Remaining_Is_Not_Enough()
        {
            var layers = new[] { L("a", 2), L("b", 3) };

            var ex = Assert.Throws<InsufficientStockException>(() => FifoAllocator.Allocate(layers, 6));
            Assert.Contains("short by 1", ex.Message);
        }

        [Fact]
        public void Throws_On_Zero_Layers()
        {
            Assert.Throws<InsufficientStockException>(() => FifoAllocator.Allocate(new List<FifoAllocator.Layer>(), 1));
        }

        [Fact]
        public void Zero_Requirement_Returns_Nothing()
        {
            var layers = new[] { L("a", 5) };
            Assert.Empty(FifoAllocator.Allocate(layers, 0));
        }

        [Fact]
        public void Fractional_Base_Quantities_Are_Supported()
        {
            var layers = new[] { L("a", 1.5), L("b", 1.5) };

            var result = FifoAllocator.Allocate(layers, 2.25);

            Assert.Equal(2, result.Count);
            Assert.Equal(1.5, result[0].Quantity);
            Assert.Equal(0.75, result[1].Quantity);
        }
    }
}
