import { useQuery } from "@tanstack/react-query"
import { getTenantFeatures } from "@/api/tenant"
import { listPermissions } from "@/api/permissions"
import { getRolePermissions, listRoles } from "@/api/roles"
import { listUsers } from "@/api/users"

export const adminKeys = {
  all: () => ["admin"] as const,
  roles: () => ["admin", "roles"] as const,
  rolePermissions: (id: string) => ["admin", "roles", id, "permissions"] as const,
  permissions: () => ["admin", "permissions"] as const,
  users: () => ["admin", "users"] as const,
  tenantFeatures: () => ["admin", "tenant-features"] as const,
}

export function useRoles() {
  return useQuery({ queryKey: adminKeys.roles(), queryFn: listRoles })
}

export function useRolePermissions(id: string, enabled = true) {
  return useQuery({
    queryKey: adminKeys.rolePermissions(id),
    queryFn: () => getRolePermissions(id),
    enabled: enabled && !!id,
  })
}

export function usePermissions() {
  return useQuery({ queryKey: adminKeys.permissions(), queryFn: listPermissions })
}

export function useUsers() {
  return useQuery({ queryKey: adminKeys.users(), queryFn: listUsers })
}

export function useTenantFeatures() {
  return useQuery({ queryKey: adminKeys.tenantFeatures(), queryFn: getTenantFeatures })
}
