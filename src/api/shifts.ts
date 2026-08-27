import { request, toQuery } from "@/lib/api"
import { Shift, PagedShifts, ShiftReport } from "@/types/domain/domain.types"

export function listShiftsPaged(page = 1, pageSize = 20, status?: string) {
  return request<PagedShifts>(
    `/api/shifts${toQuery({ page, pageSize, status: status !== 'all' ? status : undefined })}`
  )
}

export function getActiveShift() {
  return request<Shift | null>('/api/shifts/active')
}

export function getShiftReport(id: string) {
  return request<ShiftReport>(`/api/shifts/${id}/report`)
}
