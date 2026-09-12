import { queryClient } from "@/lib/query-client"
import { request } from "@/lib/api"
import { MasterUnit } from "@/types/domain/domain.types"
import { unitsKeys } from "@/hooks/use-units"

export async function createUnit(data: { name: string }) {
  const result = await request<MasterUnit>('/api/units', { method: 'POST', body: JSON.stringify(data) })
  await queryClient.invalidateQueries({ queryKey: unitsKeys.all })
  return result
}

export async function updateUnit(id: string, data: { name?: string; isActive?: boolean }) {
  const result = await request<MasterUnit>(`/api/units/${id}`, { method: 'PUT', body: JSON.stringify(data) })
  await queryClient.invalidateQueries({ queryKey: unitsKeys.all })
  return result
}

export async function deleteUnit(id: string) {
  const result = await request<{ success: boolean }>(`/api/units/${id}`, { method: 'DELETE' })
  await queryClient.invalidateQueries({ queryKey: unitsKeys.all })
  return result
}
