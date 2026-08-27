import { request, toQuery } from "@/lib/api"
import { PurchaseInvoice, PagedPurchases } from "@/types/domain/domain.types"

export function listPurchasesPaged(page = 1, pageSize = 20, opts?: { status?: string; q?: string }) {
  return request<PagedPurchases>(
    `/api/purchases${toQuery({ page, pageSize, status: opts?.status !== 'all' ? opts?.status : undefined, q: opts?.q })}`
  )
}

export function getPurchase(id: string) {
  return request<PurchaseInvoice>(`/api/purchases/${id}`)
}
