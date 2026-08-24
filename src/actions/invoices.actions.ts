import { api, Invoice, PriceMode } from "@/lib/api"
import { CartItem } from "@/store/pos.store"

interface InvoiceOptions {
  clientId?: string | null
  paymentMethod?: 'cash' | 'credit'
  status?: 'draft' | 'posted'
}

function buildPayload(
  cartItems: CartItem[],
  discount: number,
  discountType?: string,
  discountValue?: number,
  priceMode: PriceMode = 'retail',
  opts?: InvoiceOptions
) {
  return {
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
    clientId: opts?.clientId ?? null,
    paymentMethod: opts?.paymentMethod ?? 'cash',
    status: opts?.status ?? 'posted',
  }
}

export async function createInvoice(
  cartItems: CartItem[],
  discount: number,
  printInvoice: boolean = true,
  discountType?: string,
  discountValue?: number,
  priceMode: PriceMode = 'retail',
  opts?: InvoiceOptions
): Promise<Invoice> {
  const payload = buildPayload(cartItems, discount, discountType, discountValue, priceMode, opts)
  const invoice = await api.invoices.create(payload)

  if (printInvoice) {
    try {
      await api.printing.print(invoice)
    } catch {
      console.error("Silent printing failed, printer API might be offline")
    }
  }

  return invoice
}

export async function saveDraftInvoice(
  cartItems: CartItem[],
  discount: number,
  discountType: string | undefined,
  discountValue: number | undefined,
  priceMode: PriceMode,
  opts?: InvoiceOptions & { draftId?: string | null }
): Promise<Invoice> {
  const payload = buildPayload(cartItems, discount, discountType, discountValue, priceMode, opts)
  if (opts?.draftId) {
    return api.invoices.updateDraft(opts.draftId, payload)
  }
  return api.invoices.create(payload)
}
