import { useQuery } from "@tanstack/react-query"
import { getPrintQueueStatus, getSyncStatus } from "@/api/sync"

export const syncKeys = {
  all: ["sync"] as const,
  status: () => [...syncKeys.all, "status"] as const,
  printQueue: () => [...syncKeys.all, "print-queue"] as const,
}

export function useSyncStatus() {
  return useQuery({ queryKey: syncKeys.status(), queryFn: getSyncStatus })
}

export function usePrintQueueStatus() {
  return useQuery({ queryKey: syncKeys.printQueue(), queryFn: getPrintQueueStatus })
}
