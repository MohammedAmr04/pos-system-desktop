using System.Collections.Generic;
using System.Linq;
using PosCs.Domain.Exceptions;

namespace PosCs.Domain.Rules
{
    /// <summary>
    /// Pure FIFO allocation rule (plan Phase 4). Operates entirely in BASE units — the caller
    /// converts the sold quantity to base units before invoking. One sales line may consume
    /// multiple layers. Layers with remaining &lt;= 0 are skipped (they may exist after a
    /// purchase reversal when goods were already sold).
    /// </summary>
    public static class FifoAllocator
    {
        public sealed class Layer
        {
            public string Id { get; }
            public double Remaining { get; }

            public Layer(string id, double remaining)
            {
                Id = id;
                Remaining = remaining;
            }
        }

        public sealed class Allocation
        {
            public string LayerId { get; }
            public double Quantity { get; }

            public Allocation(string layerId, double quantity)
            {
                LayerId = layerId;
                Quantity = quantity;
            }
        }

        /// <summary>Allocates the required base-unit quantity across layers in order.
        /// Throws InsufficientStockException when total available is not enough.</summary>
        public static List<Allocation> Allocate(IEnumerable<Layer> layersInFifoOrder, double requiredQuantity)
        {
            var allocations = new List<Allocation>();
            var remaining = requiredQuantity;

            foreach (var layer in layersInFifoOrder)
            {
                if (remaining <= 0) break;
                if (layer.Remaining <= 0) continue;

                var take = layer.Remaining >= remaining ? remaining : layer.Remaining;
                allocations.Add(new Allocation(layer.Id, take));
                remaining -= take;
            }

            if (remaining > 0)
                throw new InsufficientStockException(
                    $"Insufficient cost layers: short by {remaining}");

            return allocations;
        }
    }
}
