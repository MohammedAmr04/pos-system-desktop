import { queryClient } from "@/lib/query-client"
import { request } from "@/lib/api"
import { Supplier } from "@/types/domain/domain.types"
import { suppliersKeys } from "@/hooks/use-suppliers"

export async function createSupplier(data: { name: string; phone?: string | null; address?: string | null; notes?: string | null }) {
  const result = await request<Supplier>('/api/suppliers', { method: 'POST', body: JSON.stringify(data) })
  await queryClient.invalidateQueries({ queryKey: suppliersKeys.all })
  return result
}

export async function updateSupplier(id: string, data: { name?: string; phone?: string | null; address?: string | null; notes?: string | null; isActive?: boolean }) {
  const result = await request<Supplier>(`/api/suppliers/${id}`, { method: 'PUT', body: JSON.stringify(data) })
  await queryClient.invalidateQueries({ queryKey: suppliersKeys.all })
  return result
}

export async function deleteSupplier(id: string) {
  const result = await request<{ success: boolean }>(`/api/suppliers/${id}`, { method: 'DELETE' })
  await queryClient.invalidateQueries({ queryKey: suppliersKeys.all })
  return result
}
