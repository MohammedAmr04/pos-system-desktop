"use server"

import { prisma } from "@/lib/db"
import { execSync } from "child_process"

function getMachineId(): string {
  try {
    const output = execSync(
      'powershell -Command "(Get-CimInstance Win32_ComputerSystemProduct).UUID"',
      { encoding: "utf-8", timeout: 5000 }
    ).trim()
    if (output) return output
  } catch {
    // fallback below
  }
  return "unknown-" + Math.random().toString(36).substring(2, 15)
}

export type LicenseStatus =
  | { status: "ok" }
  | { status: "first_boot"; machineId: string }
  | { status: "tampered" }
  | { status: "locked"; daysSinceActivation: number }

export async function checkLicense(): Promise<LicenseStatus> {
  const TRIAL_DAYS = 30
  const now = new Date()

  let settings = await prisma.settings.findFirst()

  if (!settings) {
    const machineId = getMachineId()
    settings = await prisma.settings.create({
      data: {
        machineId,
        activatedAt: now,
        lastCheckedAt: now,
        unlocked: false,
      },
    })
    return { status: "first_boot", machineId: settings.machineId }
  }

  if (now < settings.lastCheckedAt) {
    return { status: "tampered" }
  }

  await prisma.settings.update({
    where: { id: settings.id },
    data: { lastCheckedAt: now },
  })

  if (settings.unlocked) {
    return { status: "ok" }
  }

  const diffMs = now.getTime() - settings.activatedAt.getTime()
  const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24))

  if (diffDays >= TRIAL_DAYS) {
    return { status: "locked", daysSinceActivation: diffDays }
  }

  return { status: "ok" }
}

export async function unlockLicense(code: string): Promise<{ success: boolean }> {
  if (code === "2004") {
    const settings = await prisma.settings.findFirst()
    if (settings) {
      await prisma.settings.update({
        where: { id: settings.id },
        data: { unlocked: true },
      })
    }
    return { success: true }
  }
  return { success: false }
}

export async function getLicenseStatus(): Promise<LicenseStatus> {
  return checkLicense()
}
