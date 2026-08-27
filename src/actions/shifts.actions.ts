import { queryClient } from "@/lib/query-client"
import { request } from "@/lib/api"
import { Shift } from "@/types/domain/domain.types"
import { shiftsKeys } from "@/hooks/use-shifts"

export async function openShift(data: { openingCash: number; notes?: string }) {
  const result = await request<Shift>('/api/shifts', { method: 'POST', body: JSON.stringify(data) })
  await queryClient.invalidateQueries({ queryKey: shiftsKeys.all })
  return result
}

export async function closeShift(id: string, countedCash: number) {
  const result = await request<Shift>(`/api/shifts/${id}/close`, { method: 'POST', body: JSON.stringify({ countedCash }) })
  await queryClient.invalidateQueries({ queryKey: shiftsKeys.all })
  return result
}
