import { keepPreviousData, useQuery } from "@tanstack/react-query"
import { getEmployeePerformance, listEmployees, listEmployeesPaged } from "@/api/employees"

export interface EmployeesFilter {
  q?: string
}

export const employeesKeys = {
  all: ["employees"] as const,
  paged: (page: number, pageSize: number, filter: EmployeesFilter) =>
    ["employees", "paged", page, pageSize, filter] as const,
  active: () => ["employees", "active"] as const,
  performance: (from?: string, to?: string) =>
    ["employees", "performance", from, to] as const,
}

export function useEmployeesPage(page: number, pageSize: number, filter: EmployeesFilter = {}) {
  return useQuery({
    queryKey: employeesKeys.paged(page, pageSize, filter),
    queryFn: () => listEmployeesPaged(page, pageSize, filter.q?.trim() || undefined),
    placeholderData: keepPreviousData,
  })
}

export function useActiveEmployees() {
  return useQuery({
    queryKey: employeesKeys.active(),
    queryFn: listEmployees,
  })
}

export function useEmployeePerformance(from?: string, to?: string) {
  return useQuery({
    queryKey: employeesKeys.performance(from, to),
    queryFn: () => getEmployeePerformance(from!, to!),
    enabled: !!from && !!to,
  })
}
