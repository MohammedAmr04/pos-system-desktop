import { request } from "@/lib/api"
import { TenantFeature } from "@/types/domain/domain.types"

export function setTenantFeatures(features: TenantFeature[]) {
  return request<{ success: boolean }>('/api/tenant/features', {
    method: 'PUT',
    body: JSON.stringify({ features }),
  })
}
