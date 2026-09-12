import { keepPreviousData, useQuery } from "@tanstack/react-query"
import { getSupplier, getSupplierStatement, listSupplierPurchases, listSuppliersPaged } from "@/api/suppliers"
import { listSuppliers } from "@/api/suppliers"
import { BalanceFilter } from "@/types/domain/domain.types"

export interface SuppliersFilter {
  q?: string
  balance?: BalanceFilter
}

export const suppliersKeys = {
  all: ["suppliers"] as const,
  paged: (page: number, pageSize: number, filter: SuppliersFilter) =>
    ["suppliers", "paged", page, pageSize, filter] as const,
  list: () => ["suppliers", "list"] as const,
  detail: (id: string) => ["suppliers", "detail", id] as const,
  statement: (id: string) => ["suppliers", "statement", id] as const,
  purchases: (id: string) => ["suppliers", "purchases", id] as const,
}

export function useSuppliersPage(page: number, pageSize: number, filter: SuppliersFilter = {}) {
  return useQuery({
    queryKey: suppliersKeys.paged(page, pageSize, filter),
    queryFn: () => listSuppliersPaged(
      page,
      pageSize,
      filter.q?.trim() || undefined,
      filter.balance && filter.balance !== "all" ? filter.balance : undefined,
    ),
    placeholderData: keepPreviousData,
  })
}

export function useAllSuppliers(enabled = true) {
  return useQuery({
    queryKey: suppliersKeys.list(),
    queryFn: listSuppliers,
    enabled,
  })
}

export function useSupplier(id: string, enabled = true) {
  return useQuery({
    queryKey: suppliersKeys.detail(id),
    queryFn: () => getSupplier(id),
    enabled: enabled && !!id,
  })
}

export function useSupplierStatement(id: string, enabled = false) {
  return useQuery({
    queryKey: suppliersKeys.statement(id),
    queryFn: () => getSupplierStatement(id),
    enabled: enabled && !!id,
  })
}

export function useSupplierPurchases(id: string, enabled = true) {
  return useQuery({
    queryKey: suppliersKeys.purchases(id),
    queryFn: () => listSupplierPurchases(id),
    enabled: enabled && !!id,
  })
}
