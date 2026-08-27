import { request, toQuery } from "@/lib/api"
import { Client, PagedMasterData, PartyStatement } from "@/types/domain/domain.types"

export function listClients() {
  return request<Client[]>('/api/clients')
}

export function listClientsPaged(page = 1, pageSize = 20, q?: string) {
  return request<PagedMasterData<Client>>(`/api/clients/paged${toQuery({ page, pageSize, q })}`)
}

export function getClient(id: string) {
  return request<Client>(`/api/clients/${id}`)
}

export function getClientStatement(id: string) {
  return request<PartyStatement>(`/api/clients/${id}/statement`)
}
