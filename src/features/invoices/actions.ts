import { api, Invoice } from "@/lib/api"
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
  discountValue?: number
): Promise<Invoice> {
  const invoice = await api.invoices.create({
    items: cartItems.map(item => ({
      productId: item.productId,
      name: item.name,
      buyPrice: item.buyPrice,
      salePrice: item.salePrice,
      quantity: item.quantity,
      maxStock: item.maxStock,
      allowDiscount: item.allowDiscount,
    })),
    discount,
    discountType,
    discountValue,
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
