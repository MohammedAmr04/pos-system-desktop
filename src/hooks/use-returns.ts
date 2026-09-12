import { keepPreviousData, useQuery } from "@tanstack/react-query"
import { listSaleReturnsPaged } from "@/api/sale-returns"
import { listPurchaseReturnsPaged } from "@/api/purchase-returns"

export const saleReturnsKeys = {
  all: ["sale-returns"] as const,
  paged: (page: number, pageSize: number, invoiceId?: string) =>
    ["sale-returns", "paged", page, pageSize, invoiceId ?? null] as const,
}

export const purchaseReturnsKeys = {
  all: ["purchase-returns"] as const,
  paged: (page: number, pageSize: number, purchaseId?: string) =>
    ["purchase-returns", "paged", page, pageSize, purchaseId ?? null] as const,
}

export function useSaleReturnsPage(page: number, pageSize: number, invoiceId?: string, options?: { enabled?: boolean }) {
  const enabled = options?.enabled
  return useQuery({
    queryKey: saleReturnsKeys.paged(page, pageSize, invoiceId),
    queryFn: () => listSaleReturnsPaged(page, pageSize, invoiceId || undefined),
    placeholderData: keepPreviousData,
    enabled: enabled === undefined ? true : enabled && !!invoiceId,
  })
}

export function usePurchaseReturnsPage(page: number, pageSize: number, purchaseId?: string, options?: { enabled?: boolean }) {
  const enabled = options?.enabled
  return useQuery({
    queryKey: purchaseReturnsKeys.paged(page, pageSize, purchaseId),
    queryFn: () => listPurchaseReturnsPaged(page, pageSize, purchaseId || undefined),
    placeholderData: keepPreviousData,
    enabled: enabled === undefined ? true : enabled && !!purchaseId,
  })
}
