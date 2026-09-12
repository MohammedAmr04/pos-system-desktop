import { request, toQuery } from "@/lib/api"
import { Employee, EmployeePerformanceRow, PagedMasterData } from "@/types/domain/domain.types"

export function listEmployees() {
  return request<Employee[]>('/api/employees')
}

export function listEmployeesPaged(page = 1, pageSize = 20, q?: string) {
  return request<PagedMasterData<Employee>>(`/api/employees/paged${toQuery({ page, pageSize, q })}`)
}

export function getEmployee(id: string) {
  return request<Employee>(`/api/employees/${id}`)
}

export function getEmployeePerformance(from: string, to: string) {
  return request<EmployeePerformanceRow[]>(`/api/reports/employee-performance${toQuery({ from, to })}`)
}
