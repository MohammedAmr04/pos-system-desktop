import { request, toQuery } from "@/lib/api"
import { MasterUnit, PagedMasterData } from "@/types/domain/domain.types"

export function listUnits() {
  return request<MasterUnit[]>('/api/units')
}

export function listUnitsPaged(page = 1, pageSize = 20, q?: string) {
  return request<PagedMasterData<MasterUnit>>(`/api/units/paged${toQuery({ page, pageSize, q })}`)
}

export function getUnit(id: string) {
  return request<MasterUnit>(`/api/units/${id}`)
}
