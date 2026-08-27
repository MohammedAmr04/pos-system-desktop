import { request, toQuery } from "@/lib/api"
import { Invoice, PagedInvoices } from "@/types/domain/domain.types"

export function listInvoices() {
  return request<Invoice[]>('/api/invoices')
}

export function listInvoicesPaged(
  page = 1,
  pageSize = 20,
  opts?: { from?: string; to?: string; q?: string; status?: string }
) {
  return request<PagedInvoices>(
    `/api/invoices/paged${toQuery({ page, pageSize, from: opts?.from, to: opts?.to, q: opts?.q, status: opts?.status !== 'all' ? opts?.status : undefined })}`
  )
}

export function getInvoice(id: string) {
  return request<Invoice>(`/api/invoices/${id}`)
}
