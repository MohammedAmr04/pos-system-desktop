import { request, toQuery } from "@/lib/api"
import {
  Branch,
  BranchAgentConfig,
  BranchAssignmentResult,
  BranchCredentialsInput,
  BranchRuntimeContext,
  BranchRuntimeInput,
  BranchSwitchInput,
  CreateBranchInput,
  PagedMasterData,
  Terminal,
  UpdateBranchInput,
} from "@/types/domain/domain.types"

export function getBranchContext() {
  return request<BranchRuntimeContext>('/api/branch/context')
}

export function getBranchAgentConfig() {
  return request<BranchAgentConfig>('/api/branch/agent-config')
}

export function listBranches(activeOnly = true) {
  return request<Branch[]>(`/api/branch/list${toQuery({ activeOnly })}`)
}

export function listBranchesPaged(page = 1, pageSize = 20, q?: string, activeOnly = false) {
  return request<PagedMasterData<Branch>>(`/api/branch/paged${toQuery({ page, pageSize, q, activeOnly })}`)
}

export function getBranch(id: string) {
  return request<Branch>(`/api/branch/${id}`)
}

export function listBranchTerminals(id: string) {
  return request<Terminal[]>(`/api/branch/${id}/terminals`)
}

export function createBranch(data: CreateBranchInput) {
  return request<Branch>('/api/branch', { method: 'POST', body: JSON.stringify(data) })
}

export function updateBranch(id: string, data: UpdateBranchInput) {
  return request<Branch>(`/api/branch/${id}`, { method: 'PUT', body: JSON.stringify(data) })
}

export function activateBranch(id: string) {
  return request<{ success: boolean }>(`/api/branch/${id}/activate`, { method: 'POST' })
}

export function deactivateBranch(id: string) {
  return request<{ success: boolean }>(`/api/branch/${id}/deactivate`, { method: 'POST' })
}

export function deleteBranch(id: string) {
  return request<{ success: boolean }>(`/api/branch/${id}`, { method: 'DELETE' })
}

export function setBranchSecret(id: string, key: string) {
  return request<{ success: boolean }>(`/api/branch/${id}/secret`, { method: 'POST', body: JSON.stringify({ key }) })
}

export function saveBranchCredentials(data: BranchCredentialsInput) {
  return request<{ success: boolean }>('/api/branch/credentials', { method: 'POST', body: JSON.stringify(data) })
}

export function configureBranchRuntime(data: BranchRuntimeInput) {
  return request<BranchRuntimeContext>('/api/branch/runtime', { method: 'POST', body: JSON.stringify(data) })
}

export function requestBranchSwitch(data: BranchSwitchInput) {
  return request<BranchAssignmentResult>('/api/branch/switch', { method: 'POST', body: JSON.stringify(data) })
}

export function getBranchSwitch(requestId: string) {
  return request<BranchAssignmentResult>(`/api/branch/switch/${encodeURIComponent(requestId)}`)
}
