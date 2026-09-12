import { keepPreviousData, useQuery } from "@tanstack/react-query"
import { getInventoryAdjustment, listAlerts, listAuditLogs, listInventoryAdjustments } from "@/api/operations"
import { listBackups } from "@/api/backups"
export const operationsKeys = { all: ["operations"] as const, audit: (page: number) => ["operations", "audit", page] as const, alerts: (page: number, status: string) => ["operations", "alerts", page, status] as const, adjustments: (page: number) => ["operations", "adjustments", page] as const, adjustment: (id: string) => ["operations", "adjustment", id] as const, backups: () => ["operations", "backups"] as const }
export function useAuditLogs(page: number, action?: string, entityType?: string) { return useQuery({ queryKey: [...operationsKeys.audit(page), action, entityType], queryFn: () => listAuditLogs(page, 20, action, entityType), placeholderData: keepPreviousData }) }
export function useAlerts(page: number, status = "open") { return useQuery({ queryKey: operationsKeys.alerts(page, status), queryFn: () => listAlerts(page, 20, status), placeholderData: keepPreviousData }) }
export function useInventoryAdjustments(page: number) { return useQuery({ queryKey: operationsKeys.adjustments(page), queryFn: () => listInventoryAdjustments(page, 20), placeholderData: keepPreviousData }) }
export function useInventoryAdjustment(id: string | null) { return useQuery({ queryKey: operationsKeys.adjustment(id ?? ""), queryFn: () => getInventoryAdjustment(id!), enabled: !!id }) }
export function useBackups() { return useQuery({ queryKey: operationsKeys.backups(), queryFn: listBackups }) }
