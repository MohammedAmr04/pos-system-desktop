import { request, toQuery } from "@/lib/api"
import {
  BranchAssignmentHistory,
  CreateTerminalInput,
  PagedTerminals,
  Terminal,
  UpdateTerminalInput,
} from "@/types/domain/domain.types"

export function listTerminalsPaged(page = 1, pageSize = 20, branchId?: string, activeOnly = false) {
  return request<PagedTerminals>(`/api/terminals${toQuery({ page, pageSize, branchId, activeOnly })}`)
}

export function getTerminal(id: string) {
  return request<Terminal>(`/api/terminals/${id}`)
}

export function getTerminalHistory(id: string, limit = 20) {
  return request<BranchAssignmentHistory[]>(`/api/terminals/${id}/history${toQuery({ limit })}`)
}

export function createTerminal(data: CreateTerminalInput) {
  return request<Terminal>('/api/terminals', { method: 'POST', body: JSON.stringify(data) })
}

export function updateTerminal(id: string, data: UpdateTerminalInput) {
  return request<Terminal>(`/api/terminals/${id}`, { method: 'PUT', body: JSON.stringify(data) })
}

export function activateTerminal(id: string) {
  return request<{ success: boolean }>(`/api/terminals/${id}/activate`, { method: 'POST' })
}

export function deactivateTerminal(id: string) {
  return request<{ success: boolean }>(`/api/terminals/${id}/deactivate`, { method: 'POST' })
}
