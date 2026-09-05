import { acknowledgeAlert, cancelInventoryAdjustment, createInventoryAdjustment, deleteInventoryAdjustment, deleteInventoryAdjustmentLine, postInventoryAdjustment, saveInventoryAdjustmentLine, updateInventoryAdjustmentNotes } from "@/api/operations"
import { operationsKeys } from "@/hooks/use-operations"
import { queryClient } from "@/lib/query-client"
import { InventoryAdjustmentLineInput } from "@/types/domain/domain.types"

export async function acknowledgeAlertItem(id: string) {
  const result = await acknowledgeAlert(id)
  await queryClient.invalidateQueries({ queryKey: operationsKeys.all })
  return result
}

export async function createInventoryCount() {
  const result = await createInventoryAdjustment()
  await queryClient.invalidateQueries({ queryKey: operationsKeys.all })
  return result
}

export async function updateInventoryCountNotes(id: string, notes: string) {
  const result = await updateInventoryAdjustmentNotes(id, notes)
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

export async function deleteInventoryCount(id: string) {
  await deleteInventoryAdjustment(id)
  await queryClient.invalidateQueries({ queryKey: operationsKeys.all })
}
