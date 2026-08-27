import { queryClient } from "@/lib/query-client"
import { request } from "@/lib/api"
import { RoleSummary } from "@/types/domain/domain.types"
import { adminKeys } from "@/hooks/use-admin"

export async function createRole(data: { name: string; description?: string | null }) {
  const result = await request<RoleSummary>('/api/roles', { method: 'POST', body: JSON.stringify(data) })
  await queryClient.invalidateQueries({ queryKey: adminKeys.all() })
  return result
}

export async function updateRole(id: string, data: { name: string; description?: string | null }) {
  const result = await request<RoleSummary>(`/api/roles/${id}`, { method: 'PUT', body: JSON.stringify(data) })
  await queryClient.invalidateQueries({ queryKey: adminKeys.all() })
  return result
}

export async function deleteRole(id: string) {
  const result = await request<{ success: boolean }>(`/api/roles/${id}`, { method: 'DELETE' })
  await queryClient.invalidateQueries({ queryKey: adminKeys.all() })
  return result
}

export async function setRolePermissions(id: string, permissionIds: string[]) {
  const result = await request<{ success: boolean; permissionIds: string[] }>(`/api/roles/${id}/permissions`, {
    method: 'PUT',
    body: JSON.stringify({ permissionIds }),
  })
  await queryClient.invalidateQueries({ queryKey: adminKeys.all() })
  return result
}
