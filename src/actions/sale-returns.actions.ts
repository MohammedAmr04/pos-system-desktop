import { request } from "@/lib/api"
import { SaleReturn } from "@/types/domain/domain.types"

export function createSaleReturn(
  invoiceId: string,
  data: { items: { invoiceDetailId: string; quantity: number }[]; notes?: string }
) {
  return request<SaleReturn>(`/api/salereturns?invoiceId=${encodeURIComponent(invoiceId)}`, {
    method: 'POST',
    body: JSON.stringify(data),
  })
}
