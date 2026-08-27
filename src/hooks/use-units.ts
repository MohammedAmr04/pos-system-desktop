import { keepPreviousData, useQuery } from "@tanstack/react-query"
import { listUnitsPaged } from "@/api/units"
import { listUnits } from "@/api/units"

export interface UnitsFilter {
  q?: string
}

export const unitsKeys = {
  all: ["units"] as const,
  paged: (page: number, pageSize: number, filter: UnitsFilter) =>
    ["units", "paged", page, pageSize, filter] as const,
  list: () => ["units", "list"] as const,
}

export function useUnitsPage(page: number, pageSize: number, filter: UnitsFilter = {}) {
  return useQuery({
    queryKey: unitsKeys.paged(page, pageSize, filter),
    queryFn: () => listUnitsPaged(page, pageSize, filter.q?.trim() || undefined),
    placeholderData: keepPreviousData,
  })
}

export function useAllUnits(enabled = true) {
  return useQuery({
    queryKey: unitsKeys.list(),
    queryFn: listUnits,
    enabled,
  })
}
