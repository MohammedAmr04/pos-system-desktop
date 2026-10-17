import { request } from "@/lib/api"
import { PrintQueueStatus, SyncStatus } from "@/types/domain/domain.types"

export function getSyncStatus() {
  return request<SyncStatus>('/api/sync/status')
}

export function getPrintQueueStatus() {
  return request<PrintQueueStatus>('/api/printing/queue/status')
}
