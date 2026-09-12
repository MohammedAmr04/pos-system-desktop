import { LicenseStatus } from "@/types/domain/domain.types"
import { checkLicenseStatus } from "@/api/license"
import { request } from "@/lib/api"

export type { LicenseStatus }

export async function checkLicense(): Promise<LicenseStatus> {
  try {
    return await checkLicenseStatus()
  } catch (e) {
    console.error("License check failed", e)
    return { status: "locked" }
  }
}

// Unlock is now enforced server-side: the caller must be authenticated with
// license.manage and provide a valid 4-digit code (derived from the machine id
// with a backend-only secret). The hardcoded "2004" is gone.
export async function unlockLicense(code: string): Promise<{ success: boolean; message?: string }> {
  try {
    const status = await checkLicenseStatus()
    if (!status.machineId) {
      return { success: false, message: "no machine id" }
    }
    await request<{ success: boolean; machineId: string }>('/api/license/unlock', {
      method: 'POST',
      body: JSON.stringify({ machineId: status.machineId, code: code.trim() }),
    })
    return { success: true }
  } catch (e) {
    return { success: false, message: (e as Error).message }
  }
}
