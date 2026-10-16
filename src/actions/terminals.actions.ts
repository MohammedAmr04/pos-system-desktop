import { queryClient } from "@/lib/query-client"
import { activateTerminal, createTerminal, deactivateTerminal, updateTerminal } from "@/api/terminals"
import { terminalsKeys } from "@/hooks/use-terminals"
import { branchesKeys } from "@/hooks/use-branches"
import { CreateTerminalInput, UpdateTerminalInput } from "@/types/domain/domain.types"

async function invalidateTerminals() {
  await queryClient.invalidateQueries({ queryKey: terminalsKeys.all })
  await queryClient.invalidateQueries({ queryKey: branchesKeys.all })
}

export async function createTerminalAction(data: CreateTerminalInput) {
  const result = await createTerminal(data)
  await invalidateTerminals()
  return result
}

export async function updateTerminalAction(id: string, data: UpdateTerminalInput) {
  const result = await updateTerminal(id, data)
  await invalidateTerminals()
  return result
}

export async function setTerminalActiveAction(id: string, isActive: boolean) {
  const result = isActive ? await activateTerminal(id) : await deactivateTerminal(id)
  await invalidateTerminals()
  return result
}
