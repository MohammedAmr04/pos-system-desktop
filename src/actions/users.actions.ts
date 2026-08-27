import { queryClient } from "@/lib/query-client"
import { request } from "@/lib/api"
import { UserSummary } from "@/types/domain/domain.types"
import { adminKeys } from "@/hooks/use-admin"

export async function createUser(data: { name: string; username: string; password: string; roleIds: string[] }) {
  const result = await request<UserSummary>('/api/users', { method: 'POST', body: JSON.stringify(data) })
  await queryClient.invalidateQueries({ queryKey: adminKeys.all() })
  return result
}

export async function updateUser(id: string, data: { name?: string; username?: string; password?: string; isActive?: boolean; roleIds?: string[] }) {
  const result = await request<UserSummary>(`/api/users/${id}`, { method: 'PUT', body: JSON.stringify(data) })
  await queryClient.invalidateQueries({ queryKey: adminKeys.all() })
  return result
}
