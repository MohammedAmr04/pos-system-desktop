import { request, toQuery } from "@/lib/api"
import { Supplier, PagedMasterData, PartyStatement } from "@/types/domain/domain.types"

export function listSuppliers() {
  return request<Supplier[]>('/api/suppliers')
}

export function listSuppliersPaged(page = 1, pageSize = 20, q?: string) {
  return request<PagedMasterData<Supplier>>(`/api/suppliers/paged${toQuery({ page, pageSize, q })}`)
}

export function getSupplier(id: string) {
  return request<Supplier>(`/api/suppliers/${id}`)
}

export function getSupplierStatement(id: string) {
  return request<PartyStatement>(`/api/suppliers/${id}/statement`)
}
