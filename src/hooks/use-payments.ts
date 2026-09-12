import { keepPreviousData, useQuery } from "@tanstack/react-query"
import { getPaymentSummary, listPaymentsPaged } from "@/api/payments"

export interface PaymentsFilter {
  clientId?: string
  supplierId?: string
  invoiceId?: string
}

export const paymentsKeys = {
  all: ["payments"] as const,
  paged: (page: number, pageSize: number, filter: PaymentsFilter) =>
    ["payments", "paged", page, pageSize, filter] as const,
  summary: (ids: string[]) => ["payments", "summary", ids] as const,
}

export function usePaymentsPage(page: number, pageSize: number, filter: PaymentsFilter = {}) {
  return useQuery({
    queryKey: paymentsKeys.paged(page, pageSize, filter),
    queryFn: () =>
      listPaymentsPaged(page, pageSize, {
        clientId: filter.clientId || undefined,
        supplierId: filter.supplierId || undefined,
        invoiceId: filter.invoiceId || undefined,
      }),
    placeholderData: keepPreviousData,
  })
}

export function usePaymentSummary(ids: string[], enabled = true) {
  return useQuery({
    queryKey: paymentsKeys.summary(ids),
    queryFn: () => getPaymentSummary(ids),
    enabled: enabled && ids.length > 0,
  })
}
