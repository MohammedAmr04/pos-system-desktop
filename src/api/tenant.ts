import { request } from "@/lib/api"
import { TenantFeature } from "@/types/domain/domain.types"

export function getTenantFeatures() {
  return request<{ features: TenantFeature[] }>('/api/tenant/features')
}
