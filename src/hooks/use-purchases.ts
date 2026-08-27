import { keepPreviousData, useQuery } from "@tanstack/react-query"
import { getPurchase, listPurchasesPaged } from "@/api/purchases"

export interface PurchasesFilter {
  status?: string
  q?: string
}

export const purchasesKeys = {
  all: ["purchases"] as const,
  paged: (page: number, pageSize: number, filter: PurchasesFilter) =>
    ["purchases", "paged", page, pageSize, filter] as const,
  detail: (id: string) => ["purchases", "detail", id] as const,
}

export function usePurchasesPage(page: number, pageSize: number, filter: PurchasesFilter = {}) {
  return useQuery({
    queryKey: purchasesKeys.paged(page, pageSize, filter),
    queryFn: () => listPurchasesPaged(page, pageSize, filter),
    placeholderData: keepPreviousData,
  })
}

export function usePurchase(id: string, enabled = true) {
  return useQuery({
    queryKey: purchasesKeys.detail(id),
    queryFn: () => getPurchase(id),
    enabled: enabled && !!id,
  })
}
