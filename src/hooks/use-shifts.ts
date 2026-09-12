import { keepPreviousData, useQuery } from "@tanstack/react-query"
import { listShiftsPaged } from "@/api/shifts"
import { getActiveShift, getShiftReport, getShiftInvoices } from "@/api/shifts"

export interface ShiftsFilter {
  status?: string
}

export const shiftsKeys = {
  all: ["shifts"] as const,
  paged: (page: number, pageSize: number, filter: ShiftsFilter) =>
    ["shifts", "paged", page, pageSize, filter] as const,
  active: () => ["shifts", "active"] as const,
  report: (id: string) => ["shifts", "report", id] as const,
  invoices: (id: string, page: number, pageSize: number) =>
    ["shifts", "invoices", id, page, pageSize] as const,
}

export function useShiftsPage(page: number, pageSize: number, filter: ShiftsFilter = {}) {
  return useQuery({
    queryKey: shiftsKeys.paged(page, pageSize, filter),
    queryFn: () => listShiftsPaged(page, pageSize, filter.status),
    placeholderData: keepPreviousData,
  })
}

export function useActiveShift() {
  return useQuery({
    queryKey: shiftsKeys.active(),
    queryFn: getActiveShift,
  })
}

export function useShiftReport(id: string, enabled = false) {
  return useQuery({
    queryKey: shiftsKeys.report(id),
    queryFn: () => getShiftReport(id),
    enabled: enabled && !!id,
  })
}

export function useShiftInvoices(id: string, page: number, pageSize: number, enabled = true) {
  return useQuery({
    queryKey: shiftsKeys.invoices(id, page, pageSize),
    queryFn: () => getShiftInvoices(id, page, pageSize),
    enabled: enabled && !!id,
    placeholderData: keepPreviousData,
  })
}
