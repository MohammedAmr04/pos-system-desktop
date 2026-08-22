import { api, LicenseStatus } from "@/lib/api"

export type { LicenseStatus }

export async function checkLicense(): Promise<LicenseStatus> {
  try {
    return await api.license.check()
  } catch {
    return { status: "ok" }
  }
}

// Unlock is now enforced server-side: the caller must be authenticated with
// license.manage and provide a valid 4-digit code (derived from the machine id
// with a backend-only secret). The hardcoded "2004" is gone.
export async function unlockLicense(code: string): Promise<{ success: boolean; message?: string }> {
  try {
    const status = await api.license.check()
    if (!status.machineId) return { success: false, message: "no machine id" }
    await api.license.unlock(status.machineId, code.trim())
    return { success: true }
  } catch (e) {
    return { success: false, message: (e as Error).message }
  }
}
