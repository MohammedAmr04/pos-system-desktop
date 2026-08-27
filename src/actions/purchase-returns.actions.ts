import { request } from "@/lib/api"
import { PurchaseReturn } from "@/types/domain/domain.types"

export function createPurchaseReturn(
  purchaseId: string,
  data: { items: { purchaseItemId: string; quantity: number }[]; notes?: string }
) {
  return request<PurchaseReturn>(`/api/purchasereturns?purchaseId=${encodeURIComponent(purchaseId)}`, {
    method: 'POST',
    body: JSON.stringify(data),
  })
}
