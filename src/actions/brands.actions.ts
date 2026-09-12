import { queryClient } from "@/lib/query-client"
import { request } from "@/lib/api"
import { Brand } from "@/types/domain/domain.types"
import { brandsKeys } from "@/hooks/use-brands"

export async function createBrand(data: { name: string }) {
  const result = await request<Brand>('/api/brands', { method: 'POST', body: JSON.stringify(data) })
  await queryClient.invalidateQueries({ queryKey: brandsKeys.all })
  return result
}

export async function updateBrand(id: string, data: { name?: string; isActive?: boolean }) {
  const result = await request<Brand>(`/api/brands/${id}`, { method: 'PUT', body: JSON.stringify(data) })
  await queryClient.invalidateQueries({ queryKey: brandsKeys.all })
  return result
}

export async function deleteBrand(id: string) {
  const result = await request<{ success: boolean }>(`/api/brands/${id}`, { method: 'DELETE' })
  await queryClient.invalidateQueries({ queryKey: brandsKeys.all })
  return result
}
