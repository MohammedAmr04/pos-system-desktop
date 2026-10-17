import { keepPreviousData, useQuery } from "@tanstack/react-query"
import {
  getBranchAgentConfig,
  getBranchContext,
  getSetupStatus,
  listBranches,
  listBranchesPaged,
  listBranchTerminals,
} from "@/api/branches"

export interface BranchesFilter {
  q?: string
  activeOnly?: boolean
}

export const branchesKeys = {
  all: ["branches"] as const,
  paged: (page: number, pageSize: number, filter: BranchesFilter) =>
    ["branches", "paged", page, pageSize, filter] as const,
  list: (activeOnly: boolean) => ["branches", "list", activeOnly] as const,
  terminals: (branchId: string) => ["branches", "terminals", branchId] as const,
  context: () => ["branches", "context"] as const,
  agentConfig: () => ["branches", "agent-config"] as const,
  setupStatus: () => ["branches", "setup-status"] as const,
}

export function useBranchesPage(page: number, pageSize: number, filter: BranchesFilter = {}) {
  return useQuery({
    queryKey: branchesKeys.paged(page, pageSize, filter),
    queryFn: () => listBranchesPaged(page, pageSize, filter.q?.trim() || undefined, filter.activeOnly ?? false),
    placeholderData: keepPreviousData,
  })
}

export function useAllBranches(activeOnly = true, enabled = true) {
  return useQuery({
    queryKey: branchesKeys.list(activeOnly),
    queryFn: () => listBranches(activeOnly),
    enabled,
  })
}

export function useBranchTerminals(branchId: string, enabled = true) {
  return useQuery({
    queryKey: branchesKeys.terminals(branchId),
    queryFn: () => listBranchTerminals(branchId),
    enabled: enabled && branchId.trim().length > 0,
  })
}

export function useBranchContext(enabled = true) {
  return useQuery({ queryKey: branchesKeys.context(), queryFn: getBranchContext, enabled })
}

export function useBranchAgentConfig(enabled = true) {
  return useQuery({ queryKey: branchesKeys.agentConfig(), queryFn: getBranchAgentConfig, enabled })
}

export function useSetupStatus(enabled = true) {
  return useQuery({ queryKey: branchesKeys.setupStatus(), queryFn: getSetupStatus, enabled })
}
