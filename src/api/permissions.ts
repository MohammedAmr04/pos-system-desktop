import { request } from "@/lib/api"
import { PermissionInfo } from "@/types/domain/domain.types"

export function listPermissions() {
  return request<PermissionInfo[]>('/api/permissions')
}
