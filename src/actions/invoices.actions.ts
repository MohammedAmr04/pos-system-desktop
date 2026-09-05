import { request } from "@/lib/api"
import { Invoice, InvoiceCreatePayload, PriceMode } from "@/types/domain/domain.types"
import { CartItem } from "@/store/pos.store"
import { printInvoiceDocument } from "@/actions/printing.actions"

interface InvoiceOptions {
  clientId?: string | null
  employeeId?: string | null
  paymentMethod?: 'cash' | 'credit' | 'card' | 'bank_transfer'
  status?: 'draft' | 'posted'
}

function buildPayload(
  cartItems: CartItem[],
  discount: number,
  discountType?: string,
  discountValue?: number,
  priceMode: PriceMode = 'retail',
  opts?: InvoiceOptions
): InvoiceCreatePayload {
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
    employeeId: opts?.employeeId ?? null,
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
  const invoice = await request<Invoice>('/api/invoices', { method: 'POST', body: JSON.stringify(payload) })

  if (printInvoice) {
    try {
      await printInvoiceDocument(invoice)
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
    return request<Invoice>(`/api/invoices/${opts.draftId}`, { method: 'PUT', body: JSON.stringify(payload) })
  }
  return request<Invoice>('/api/invoices', { method: 'POST', body: JSON.stringify(payload) })
}
