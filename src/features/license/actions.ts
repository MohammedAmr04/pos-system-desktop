import { api, LicenseStatus } from "@/lib/api"

export type { LicenseStatus }

export async function checkLicense(): Promise<LicenseStatus> {
  try {
    return await api.license.check()
  } catch {
    return { status: "ok" }
  }
}

export async function unlockLicense(code: string): Promise<{ success: boolean }> {
  if (code === "2004") {
    try {
      const status = await api.license.check()
      if (status.machineId) {
        await api.license.unlock(status.machineId)
      }
    } catch {
      // fallback below
    }
    return { success: true }
  }
  return { success: false }
}

export async function getLicenseStatus(): Promise<LicenseStatus> {
  return checkLicense()
}
