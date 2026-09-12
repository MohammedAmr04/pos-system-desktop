import { queryClient } from "@/lib/query-client"
import { request } from "@/lib/api"
import { Client } from "@/types/domain/domain.types"
import { clientsKeys } from "@/hooks/use-clients"

export async function createClient(data: { name: string; phone?: string | null; address?: string | null; notes?: string | null }) {
  const result = await request<Client>('/api/clients', { method: 'POST', body: JSON.stringify(data) })
  await queryClient.invalidateQueries({ queryKey: clientsKeys.all })
  return result
}

export async function updateClient(id: string, data: { name?: string; phone?: string | null; address?: string | null; notes?: string | null; isActive?: boolean }) {
  const result = await request<Client>(`/api/clients/${id}`, { method: 'PUT', body: JSON.stringify(data) })
  await queryClient.invalidateQueries({ queryKey: clientsKeys.all })
  return result
}

export async function deleteClient(id: string) {
  const result = await request<{ success: boolean }>(`/api/clients/${id}`, { method: 'DELETE' })
  await queryClient.invalidateQueries({ queryKey: clientsKeys.all })
  return result
}
