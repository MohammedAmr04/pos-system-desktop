using System.Collections.Generic;
using PosCs.Domain.Rules;
using Xunit;

namespace PosCs.Tests.Domain
{
    public class PaymentAllocationTests
    {
        private static AllocationInput Doc(string id, double total, double linked)
        {
            return new AllocationInput { Id = id, Total = total, LinkedPaid = linked };
        }

        [Fact]
        public void Allocate_EmptyPool_KeepsLinked()
        {
            var result = PaymentAllocation.Allocate(
                new List<AllocationInput> { Doc("a", 100, 40), Doc("b", 200, 0) }, 0);

            Assert.Equal(40, result["a"]);
            Assert.Equal(0, result["b"]);
        }

        [Fact]
        public void Allocate_NegativePool_TreatedAsZero()
        {
            var result = PaymentAllocation.Allocate(
                new List<AllocationInput> { Doc("a", 100, 0) }, -50);

            Assert.Equal(0, result["a"]);
        }

        [Fact]
        public void Allocate_FillsOldestFirst()
        {
            var result = PaymentAllocation.Allocate(
                new List<AllocationInput> { Doc("a", 1000, 0), Doc("b", 1000, 0) }, 1500);

            Assert.Equal(1000, result["a"]);
            Assert.Equal(500, result["b"]);
        }

        [Fact]
        public void Allocate_CombinesLinkedAndGeneral()
        {
            var result = PaymentAllocation.Allocate(
                new List<AllocationInput> { Doc("a", 1000, 400), Doc("b", 1000, 0) }, 800);

            Assert.Equal(1000, result["a"]);
            Assert.Equal(200, result["b"]);
        }

        [Fact]
        public void Allocate_SkipsFullyPaidDocuments()
        {
            var result = PaymentAllocation.Allocate(
                new List<AllocationInput> { Doc("a", 100, 100), Doc("b", 100, 0) }, 60);

            Assert.Equal(100, result["a"]);
            Assert.Equal(60, result["b"]);
        }

        [Fact]
        public void Allocate_OverLinked_KeepsLinkedValue()
        {
            var result = PaymentAllocation.Allocate(
                new List<AllocationInput> { Doc("a", 100, 150) }, 500);

            Assert.Equal(150, result["a"]);
        }

        [Fact]
        public void Allocate_PoolSurplus_LeavesRemainderUnallocated()
        {
            var result = PaymentAllocation.Allocate(
                new List<AllocationInput> { Doc("a", 100, 0) }, 10000);

            Assert.Equal(100, result["a"]);
        }
    }
}
