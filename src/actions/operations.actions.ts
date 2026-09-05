import { acknowledgeAlert, cancelInventoryAdjustment, createInventoryAdjustment, deleteInventoryAdjustmentLine, postInventoryAdjustment, saveInventoryAdjustmentLine } from "@/api/operations"
import { operationsKeys } from "@/hooks/use-operations"
import { queryClient } from "@/lib/query-client"
import { InventoryAdjustmentLineInput } from "@/types/domain/domain.types"

export async function acknowledgeAlertItem(id: string) {
  const result = await acknowledgeAlert(id)
  await queryClient.invalidateQueries({ queryKey: operationsKeys.all })
  return result
}

export async function createInventoryCount(reason: string) {
  const result = await createInventoryAdjustment(reason)
  await queryClient.invalidateQueries({ queryKey: operationsKeys.all })
  return result
}

export async function saveInventoryCountLine(id: string, line: InventoryAdjustmentLineInput) {
  const result = await saveInventoryAdjustmentLine(id, line)
  await queryClient.invalidateQueries({ queryKey: operationsKeys.all })
  return result
}

export async function removeInventoryCountLine(id: string, lineId: string) {
  await deleteInventoryAdjustmentLine(id, lineId)
  await queryClient.invalidateQueries({ queryKey: operationsKeys.all })
}

export async function postInventoryCount(id: string) {
  const result = await postInventoryAdjustment(id)
  await queryClient.invalidateQueries({ queryKey: operationsKeys.all })
  return result
}

export async function cancelInventoryCount(id: string) {
  const result = await cancelInventoryAdjustment(id)
  await queryClient.invalidateQueries({ queryKey: operationsKeys.all })
  return result
}
