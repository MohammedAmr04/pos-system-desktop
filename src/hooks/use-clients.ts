import { keepPreviousData, useQuery } from "@tanstack/react-query"
import { getClientStatement, listClients, listClientsPaged } from "@/api/clients"

export interface ClientsFilter {
  q?: string
}

export const clientsKeys = {
  all: ["clients"] as const,
  paged: (page: number, pageSize: number, filter: ClientsFilter) =>
    ["clients", "paged", page, pageSize, filter] as const,
  active: () => ["clients", "active"] as const,
  statement: (id: string) => ["clients", "statement", id] as const,
}

export function useClientsPage(page: number, pageSize: number, filter: ClientsFilter = {}) {
  return useQuery({
    queryKey: clientsKeys.paged(page, pageSize, filter),
    queryFn: () => listClientsPaged(page, pageSize, filter.q?.trim() || undefined),
    placeholderData: keepPreviousData,
  })
}

export function useActiveClients() {
  return useQuery({
    queryKey: clientsKeys.active(),
    queryFn: listClients,
  })
}

export function useClientStatement(id: string, enabled = false) {
  return useQuery({
    queryKey: clientsKeys.statement(id),
    queryFn: () => getClientStatement(id),
    enabled: enabled && !!id,
  })
}
