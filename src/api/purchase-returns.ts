import { request, toQuery } from "@/lib/api"
import { PurchaseReturn, PagedPurchaseReturns } from "@/types/domain/domain.types"

export function listPurchaseReturnsPaged(page = 1, pageSize = 20, purchaseId?: string) {
  return request<PagedPurchaseReturns>(`/api/purchasereturns${toQuery({ page, pageSize, purchaseId })}`)
}
