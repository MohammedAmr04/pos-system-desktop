import { keepPreviousData, useQuery } from "@tanstack/react-query"
import { getTerminalHistory, listTerminalsPaged } from "@/api/terminals"

export interface TerminalsFilter {
  branchId?: string
  activeOnly?: boolean
}

export const terminalsKeys = {
  all: ["terminals"] as const,
  paged: (page: number, pageSize: number, filter: TerminalsFilter) =>
    ["terminals", "paged", page, pageSize, filter] as const,
  history: (id: string, limit: number) => ["terminals", "history", id, limit] as const,
}

export function useTerminalsPage(page: number, pageSize: number, filter: TerminalsFilter = {}) {
  return useQuery({
    queryKey: terminalsKeys.paged(page, pageSize, filter),
    queryFn: () => listTerminalsPaged(page, pageSize, filter.branchId, filter.activeOnly ?? false),
    placeholderData: keepPreviousData,
  })
}

export function useTerminalHistory(id: string, limit = 20, enabled = true) {
  return useQuery({
    queryKey: terminalsKeys.history(id, limit),
    queryFn: () => getTerminalHistory(id, limit),
    enabled: enabled && id.trim().length > 0,
  })
}
