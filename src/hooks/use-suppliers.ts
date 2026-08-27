import { keepPreviousData, useQuery } from "@tanstack/react-query"
import { getSupplierStatement, listSuppliersPaged } from "@/api/suppliers"
import { listSuppliers } from "@/api/suppliers"

export interface SuppliersFilter {
  q?: string
}

export const suppliersKeys = {
  all: ["suppliers"] as const,
  paged: (page: number, pageSize: number, filter: SuppliersFilter) =>
    ["suppliers", "paged", page, pageSize, filter] as const,
  list: () => ["suppliers", "list"] as const,
  statement: (id: string) => ["suppliers", "statement", id] as const,
}

export function useSuppliersPage(page: number, pageSize: number, filter: SuppliersFilter = {}) {
  return useQuery({
    queryKey: suppliersKeys.paged(page, pageSize, filter),
    queryFn: () => listSuppliersPaged(page, pageSize, filter.q?.trim() || undefined),
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

export function useSupplierStatement(id: string, enabled = false) {
  return useQuery({
    queryKey: suppliersKeys.statement(id),
    queryFn: () => getSupplierStatement(id),
    enabled: enabled && !!id,
  })
}
