import { request } from "@/lib/api"
import { LicenseStatus } from "@/types/domain/domain.types"

export function checkLicenseStatus() {
  return request<LicenseStatus>('/api/license')
}
