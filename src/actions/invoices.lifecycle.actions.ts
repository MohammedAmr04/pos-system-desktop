import { queryClient } from "@/lib/query-client"
import { request } from "@/lib/api"
import { Invoice } from "@/types/domain/domain.types"
import { invoicesKeys } from "@/hooks/use-invoices"

export async function postInvoice(id: string) {
  const result = await request<Invoice>(`/api/invoices/${id}/post`, { method: 'POST' })
  await queryClient.invalidateQueries({ queryKey: invoicesKeys.all })
  return result
}

export async function cancelInvoice(id: string) {
  const result = await request<Invoice>(`/api/invoices/${id}/cancel`, { method: 'POST' })
  await queryClient.invalidateQueries({ queryKey: invoicesKeys.all })
  return result
}
