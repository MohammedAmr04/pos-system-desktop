import { keepPreviousData, useQuery } from "@tanstack/react-query"
import { listBrandsPaged } from "@/api/brands"
import { listBrands } from "@/api/brands"

export interface BrandsFilter {
  q?: string
}

export const brandsKeys = {
  all: ["brands"] as const,
  paged: (page: number, pageSize: number, filter: BrandsFilter) =>
    ["brands", "paged", page, pageSize, filter] as const,
  list: () => ["brands", "list"] as const,
}

export function useBrandsPage(page: number, pageSize: number, filter: BrandsFilter = {}) {
  return useQuery({
    queryKey: brandsKeys.paged(page, pageSize, filter),
    queryFn: () => listBrandsPaged(page, pageSize, filter.q?.trim() || undefined),
    placeholderData: keepPreviousData,
  })
}

export function useAllBrands(enabled = true) {
  return useQuery({
    queryKey: brandsKeys.list(),
    queryFn: listBrands,
    enabled,
  })
}
