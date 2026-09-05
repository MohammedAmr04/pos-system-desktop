import { request, toQuery } from "@/lib/api"
import { BalanceFilter, Client, PagedMasterData, PartyInvoices, PartyStatement } from "@/types/domain/domain.types"

export function listClients() {
  return request<Client[]>('/api/clients')
}

export function listClientsPaged(page = 1, pageSize = 20, q?: string, balance?: BalanceFilter) {
  return request<PagedMasterData<Client>>(`/api/clients/paged${toQuery({ page, pageSize, q, balance: balance && balance !== 'all' ? balance : undefined })}`)
}

export function getClient(id: string) {
  return request<Client>(`/api/clients/${id}`)
}

export function getClientStatement(id: string) {
  return request<PartyStatement>(`/api/clients/${id}/statement`)
}

export function listClientInvoices(id: string) {
  return request<PartyInvoices>(`/api/clients/${id}/invoices`)
}
