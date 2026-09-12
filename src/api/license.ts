import { request } from "@/lib/api"
import { LicenseStatus } from "@/types/domain/domain.types"

export interface LicenseConfigurationRequest {
  licenseType: "trial" | "monthly" | "annual" | "permanent"
  trialDays: number
  licenseStartedAt: string
}

export function checkLicenseStatus() {
  return request<LicenseStatus>('/api/license')
}

export function saveLicenseConfiguration(data: LicenseConfigurationRequest) {
  return request<LicenseStatus>("/api/license/configuration", {
    method: "PUT",
    body: JSON.stringify(data),
  })
}
