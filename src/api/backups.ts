import { request } from "@/lib/api"
import { BackupSummary } from "@/types/domain/domain.types"

export function listBackups() { return request<BackupSummary[]>("/api/backups") }
export function createBackup() { return request<BackupSummary>("/api/backups", { method: "POST" }) }
export function restoreBackup(fileName: string) { return request<BackupSummary>("/api/backups/restore", { method: "POST", body: JSON.stringify({ fileName }) }) }
