import { queryClient } from "@/lib/query-client"
import { request } from "@/lib/api"
import { Category } from "@/types/domain/domain.types"
import { categoriesKeys } from "@/hooks/use-categories"

export async function createCategory(data: { name: string; description?: string | null }) {
  const result = await request<Category>('/api/categories', { method: 'POST', body: JSON.stringify(data) })
  await queryClient.invalidateQueries({ queryKey: categoriesKeys.all })
  return result
}

export async function updateCategory(id: string, data: { name?: string; description?: string | null; isActive?: boolean }) {
  const result = await request<Category>(`/api/categories/${id}`, { method: 'PUT', body: JSON.stringify(data) })
  await queryClient.invalidateQueries({ queryKey: categoriesKeys.all })
  return result
}

export async function deleteCategory(id: string) {
  const result = await request<{ success: boolean }>(`/api/categories/${id}`, { method: 'DELETE' })
  await queryClient.invalidateQueries({ queryKey: categoriesKeys.all })
  return result
}
