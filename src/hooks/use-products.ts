import { keepPreviousData, useQuery } from "@tanstack/react-query"
import { countProducts, getProduct, listProducts, listProductsPaged, searchProducts } from "@/api/products"

export interface ProductsFilter {
  q?: string
}

export const productsKeys = {
  all: ["products"] as const,
  paged: (page: number, pageSize: number, filter: ProductsFilter) =>
    ["products", "paged", page, pageSize, filter] as const,
  search: (q: string, limit: number) => ["products", "search", q, limit] as const,
  detail: (id: string) => ["products", "detail", id] as const,
  count: () => ["products", "count"] as const,
}

export function useProductsPage(page: number, pageSize: number, filter: ProductsFilter = {}) {
  return useQuery({
    queryKey: productsKeys.paged(page, pageSize, filter),
    queryFn: () => listProductsPaged(page, pageSize, filter.q?.trim() || undefined),
    placeholderData: keepPreviousData,
  })
}

export function useProductSearch(q: string, limit = 20, enabled = true) {
  return useQuery({
    queryKey: productsKeys.search(q, limit),
    queryFn: ({ signal }) => searchProducts(q, limit, signal),
    enabled: enabled && q.trim().length > 0,
    placeholderData: keepPreviousData,
  })
}

export function useProduct(id: string, enabled = true) {
  return useQuery({
    queryKey: productsKeys.detail(id),
    queryFn: () => getProduct(id),
    enabled: enabled && !!id,
  })
}

export function useProductCount() {
  return useQuery({
    queryKey: productsKeys.count(),
    queryFn: countProducts,
  })
}

export function useAllProducts(enabled = true) {
  return useQuery({
    queryKey: [...productsKeys.all, "list"] as const,
    queryFn: listProducts,
    enabled,
  })
}
