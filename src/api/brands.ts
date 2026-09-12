import { request, toQuery } from "@/lib/api"
import { Brand, PagedMasterData } from "@/types/domain/domain.types"

export function listBrands() {
  return request<Brand[]>('/api/brands')
}

export function listBrandsPaged(page = 1, pageSize = 20, q?: string) {
  return request<PagedMasterData<Brand>>(`/api/brands/paged${toQuery({ page, pageSize, q })}`)
}

export function getBrand(id: string) {
  return request<Brand>(`/api/brands/${id}`)
}
