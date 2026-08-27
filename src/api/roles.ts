import { request } from "@/lib/api"
import { RoleSummary } from "@/types/domain/domain.types"

export function listRoles() {
  return request<RoleSummary[]>('/api/roles')
}

export function getRole(id: string) {
  return request<RoleSummary>(`/api/roles/${id}`)
}

export function getRolePermissions(id: string) {
  return request<{ roleId: string; permissionIds: string[] }>(`/api/roles/${id}/permissions`)
}
