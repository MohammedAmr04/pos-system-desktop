import { keepPreviousData, useQuery } from "@tanstack/react-query"
import { getInvoice, listInvoicesPaged } from "@/api/invoices"

export interface InvoicesFilter {
  from?: string
  to?: string
  q?: string
  status?: string
}

export const invoicesKeys = {
  all: ["invoices"] as const,
  paged: (page: number, pageSize: number, filter: InvoicesFilter) =>
    ["invoices", "paged", page, pageSize, filter] as const,
  detail: (id: string) => ["invoices", "detail", id] as const,
}

export function useInvoicesPage(page: number, pageSize: number, filter: InvoicesFilter = {}) {
  return useQuery({
    queryKey: invoicesKeys.paged(page, pageSize, filter),
    queryFn: () =>
      listInvoicesPaged(page, pageSize, {
        from: filter.from || undefined,
        to: filter.to || undefined,
        q: filter.q?.trim() || undefined,
        status: filter.status,
      }),
    placeholderData: keepPreviousData,
  })
}

export function useInvoice(id: string, enabled = true) {
  return useQuery({
    queryKey: invoicesKeys.detail(id),
    queryFn: () => getInvoice(id),
    enabled: enabled && !!id,
  })
}
