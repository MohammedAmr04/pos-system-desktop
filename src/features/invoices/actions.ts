import { api, Invoice, PriceMode } from "@/lib/api"
import { CartItem } from "@/features/pos/store/usePOSStore"

export const getInvoices = async (): Promise<Invoice[]> => {
  return api.invoices.list()
}

export async function getFilteredInvoices(from?: string, to?: string): Promise<Invoice[]> {
  return api.invoices.filter(from, to)
}

export async function createInvoice(
  cartItems: CartItem[],
  discount: number,
  printInvoice: boolean = true,
  discountType?: string,
  discountValue?: number,
  priceMode: PriceMode = 'retail'
): Promise<Invoice> {
  const invoice = await api.invoices.create({
    items: cartItems.map(item => ({
      productId: item.productId,
      productUnitId: item.productUnitId,
      unitName: item.unitName,
      name: item.name,
      buyPrice: item.buyPrice,
      salePrice: item.unitPrice,
      originalUnitPrice: item.originalUnitPrice,
      unitPrice: item.unitPrice,
      quantity: item.quantity,
      maxStock: item.maxStock,
      allowDiscount: item.allowDiscount,
      discountType: item.discountType ?? null,
      discountValue: item.discountValue ?? 0,
      quantityFactor: item.quantityFactor,
      priceEditNote: item.priceEditNote ?? null,
    })),
    discount,
    discountType,
    discountValue,
    priceMode,
  })

  if (printInvoice) {
    try {
      await api.printing.print(invoice)
    } catch (e) {
      console.error("Silent printing failed, printer API might be offline")
    }
  }

  return invoice
}
