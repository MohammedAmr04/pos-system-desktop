import { queryClient } from "@/lib/query-client"
import {
  activateBranch,
  configureBranchRuntime,
  createBranch,
  deactivateBranch,
  deleteBranch,
  requestBranchSwitch,
  saveBranchCredentials,
  setBranchSecret,
  updateBranch,
} from "@/api/branches"
import { branchesKeys } from "@/hooks/use-branches"
import {
  BranchCredentialsInput,
  BranchRuntimeInput,
  BranchSwitchInput,
  CreateBranchInput,
  UpdateBranchInput,
} from "@/types/domain/domain.types"

async function invalidateBranches() {
  await queryClient.invalidateQueries({ queryKey: branchesKeys.all })
}

export async function createBranchAction(data: CreateBranchInput) {
  const result = await createBranch(data)
  await invalidateBranches()
  return result
}

export async function updateBranchAction(id: string, data: UpdateBranchInput) {
  const result = await updateBranch(id, data)
  await invalidateBranches()
  return result
}

export async function setBranchActiveAction(id: string, isActive: boolean) {
  const result = isActive ? await activateBranch(id) : await deactivateBranch(id)
  await invalidateBranches()
  return result
}

export async function deleteBranchAction(id: string) {
  const result = await deleteBranch(id)
  await invalidateBranches()
  return result
}

export async function setBranchSecretAction(id: string, key: string) {
  const result = await setBranchSecret(id, key)
  await invalidateBranches()
  return result
}

export async function saveBranchCredentialsAction(data: BranchCredentialsInput) {
  const result = await saveBranchCredentials(data)
  await invalidateBranches()
  return result
}

export async function configureBranchRuntimeAction(data: BranchRuntimeInput) {
  const result = await configureBranchRuntime(data)
  await invalidateBranches()
  return result
}

export async function requestBranchSwitchAction(data: BranchSwitchInput) {
  const result = await requestBranchSwitch(data)
  await invalidateBranches()
  return result
}
