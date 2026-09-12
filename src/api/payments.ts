import { request, toQuery } from "@/lib/api"
import { Payment, PagedPayments } from "@/types/domain/domain.types"

export function listPaymentsPaged(
  page = 1,
  pageSize = 20,
  opts?: { clientId?: string; supplierId?: string; invoiceId?: string }
) {
  return request<PagedPayments>(`/api/payments${toQuery({ page, pageSize, ...opts })}`)
}

export function getPaymentSummary(ids: string[]) {
  return request<{ paid: Record<string, number> }>(`/api/payments/summary?ids=${ids.join(',')}`)
}
