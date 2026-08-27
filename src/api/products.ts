import { request, toQuery } from "@/lib/api"
import { Product, PagedProducts } from "@/types/domain/domain.types"

export function listProducts() {
  return request<Product[]>('/api/products')
}

export function listProductsForPOS() {
  return request<Product[]>('/api/products/pos')
}

export function searchProducts(q: string, limit = 20, signal?: AbortSignal) {
  return request<Product[]>(`/api/products/search${toQuery({ q, limit })}`, { signal })
}

export function listProductsPaged(page = 1, pageSize = 20, q?: string) {
  return request<PagedProducts>(`/api/products/paged${toQuery({ page, pageSize, q })}`)
}

export function countProducts() {
  return request<number>('/api/products/count')
}

export function getProduct(id: string) {
  return request<Product>(`/api/products/${id}`)
}
