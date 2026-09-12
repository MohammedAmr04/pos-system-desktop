import { queryClient } from "@/lib/query-client"
import { request } from "@/lib/api"
import { Employee } from "@/types/domain/domain.types"
import { employeesKeys } from "@/hooks/use-employees"

export async function createEmployee(data: { name: string; phone?: string | null }) {
  const result = await request<Employee>('/api/employees', { method: 'POST', body: JSON.stringify(data) })
  await queryClient.invalidateQueries({ queryKey: employeesKeys.all })
  return result
}

export async function updateEmployee(id: string, data: { name?: string; phone?: string | null; isActive?: boolean }) {
  const result = await request<Employee>(`/api/employees/${id}`, { method: 'PUT', body: JSON.stringify(data) })
  await queryClient.invalidateQueries({ queryKey: employeesKeys.all })
  return result
}
