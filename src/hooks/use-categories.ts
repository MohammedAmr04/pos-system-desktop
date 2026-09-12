import { keepPreviousData, useQuery } from "@tanstack/react-query"
import { listCategoriesPaged } from "@/api/categories"
import { listCategories } from "@/api/categories"

export interface CategoriesFilter {
  q?: string
}

export const categoriesKeys = {
  all: ["categories"] as const,
  paged: (page: number, pageSize: number, filter: CategoriesFilter) =>
    ["categories", "paged", page, pageSize, filter] as const,
  list: () => ["categories", "list"] as const,
}

export function useCategoriesPage(page: number, pageSize: number, filter: CategoriesFilter = {}) {
  return useQuery({
    queryKey: categoriesKeys.paged(page, pageSize, filter),
    queryFn: () => listCategoriesPaged(page, pageSize, filter.q?.trim() || undefined),
    placeholderData: keepPreviousData,
  })
}

export function useAllCategories(enabled = true) {
  return useQuery({
    queryKey: categoriesKeys.list(),
    queryFn: listCategories,
    enabled,
  })
}
