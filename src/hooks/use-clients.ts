import { keepPreviousData, useQuery } from "@tanstack/react-query"
import { getClient, getClientStatement, listClientInvoices, listClients, listClientsPaged } from "@/api/clients"
import { BalanceFilter } from "@/types/domain/domain.types"

export interface ClientsFilter {
  q?: string
  balance?: BalanceFilter
}

export const clientsKeys = {
  all: ["clients"] as const,
  paged: (page: number, pageSize: number, filter: ClientsFilter) =>
    ["clients", "paged", page, pageSize, filter] as const,
  active: () => ["clients", "active"] as const,
  detail: (id: string) => ["clients", "detail", id] as const,
  statement: (id: string) => ["clients", "statement", id] as const,
  invoices: (id: string) => ["clients", "invoices", id] as const,
}

export function useClientsPage(page: number, pageSize: number, filter: ClientsFilter = {}) {
  return useQuery({
    queryKey: clientsKeys.paged(page, pageSize, filter),
    queryFn: () => listClientsPaged(
      page,
      pageSize,
      filter.q?.trim() || undefined,
      filter.balance && filter.balance !== "all" ? filter.balance : undefined,
    ),
    placeholderData: keepPreviousData,
  })
}

export function useActiveClients() {
  return useQuery({
    queryKey: clientsKeys.active(),
    queryFn: listClients,
  })
}

export function useClient(id: string, enabled = true) {
  return useQuery({
    queryKey: clientsKeys.detail(id),
    queryFn: () => getClient(id),
    enabled: enabled && !!id,
  })
}

export function useClientStatement(id: string, enabled = false) {
  return useQuery({
    queryKey: clientsKeys.statement(id),
    queryFn: () => getClientStatement(id),
    enabled: enabled && !!id,
  })
}

export function useClientInvoices(id: string, enabled = true) {
  return useQuery({
    queryKey: clientsKeys.invoices(id),
    queryFn: () => listClientInvoices(id),
    enabled: enabled && !!id,
  })
}
