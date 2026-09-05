import { request, toQuery } from "@/lib/api"
import { BalanceFilter, Supplier, PagedMasterData, PartyPurchases, PartyStatement } from "@/types/domain/domain.types"

export function listSuppliers() {
  return request<Supplier[]>('/api/suppliers')
}

export function listSuppliersPaged(page = 1, pageSize = 20, q?: string, balance?: BalanceFilter) {
  return request<PagedMasterData<Supplier>>(`/api/suppliers/paged${toQuery({ page, pageSize, q, balance: balance && balance !== 'all' ? balance : undefined })}`)
}

export function getSupplier(id: string) {
  return request<Supplier>(`/api/suppliers/${id}`)
}

export function getSupplierStatement(id: string) {
  return request<PartyStatement>(`/api/suppliers/${id}/statement`)
}

export function listSupplierPurchases(id: string) {
  return request<PartyPurchases>(`/api/suppliers/${id}/purchases`)
}
