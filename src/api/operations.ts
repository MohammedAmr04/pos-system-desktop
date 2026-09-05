import { request, toQuery } from "@/lib/api"
import { AlertItem, InventoryAdjustmentDetail, InventoryAdjustmentLine, InventoryAdjustmentLineInput, PagedAlerts, PagedAuditLogs, PagedInventoryAdjustments } from "@/types/domain/domain.types"

export function listAuditLogs(page = 1, pageSize = 20, action?: string, entityType?: string) { return request<PagedAuditLogs>(`/api/audit-logs${toQuery({ page, pageSize, action, entityType })}`) }
export function listAlerts(page = 1, pageSize = 20, status = "open") { return request<PagedAlerts>(`/api/alerts${toQuery({ page, pageSize, status })}`) }
export function acknowledgeAlert(id: string) { return request<AlertItem>(`/api/alerts/${id}/acknowledge`, { method: "POST" }) }
export function listInventoryAdjustments(page = 1, pageSize = 20) { return request<PagedInventoryAdjustments>(`/api/inventory-adjustments${toQuery({ page, pageSize })}`) }
export function createInventoryAdjustment(reason: string) { return request<{ id: string; number: number; status: string }>("/api/inventory-adjustments", { method: "POST", body: JSON.stringify({ reason }) }) }
export function getInventoryAdjustment(id: string) { return request<InventoryAdjustmentDetail>(`/api/inventory-adjustments/${id}`) }
export function saveInventoryAdjustmentLine(id: string, line: InventoryAdjustmentLineInput) { return request<InventoryAdjustmentLine>(`/api/inventory-adjustments/${id}/lines`, { method: "PUT", body: JSON.stringify(line) }) }
export function deleteInventoryAdjustmentLine(id: string, lineId: string) { return request<{ success: boolean }>(`/api/inventory-adjustments/${id}/lines/${lineId}`, { method: "DELETE" }) }
export function postInventoryAdjustment(id: string) { return request<InventoryAdjustmentDetail>(`/api/inventory-adjustments/${id}/post`, { method: "POST" }) }
export function cancelInventoryAdjustment(id: string) { return request<InventoryAdjustmentDetail>(`/api/inventory-adjustments/${id}/cancel`, { method: "POST" }) }
