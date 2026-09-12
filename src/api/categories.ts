import { request, toQuery } from "@/lib/api"
import { Category, PagedMasterData } from "@/types/domain/domain.types"

export function listCategories() {
  return request<Category[]>('/api/categories')
}

export function listCategoriesPaged(page = 1, pageSize = 20, q?: string) {
  return request<PagedMasterData<Category>>(`/api/categories/paged${toQuery({ page, pageSize, q })}`)
}

export function getCategory(id: string) {
  return request<Category>(`/api/categories/${id}`)
}
