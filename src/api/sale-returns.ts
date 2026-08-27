import { request, toQuery } from "@/lib/api"
import { SaleReturn, PagedSaleReturns } from "@/types/domain/domain.types"

export function listSaleReturnsPaged(page = 1, pageSize = 20, invoiceId?: string) {
  return request<PagedSaleReturns>(`/api/salereturns${toQuery({ page, pageSize, invoiceId })}`)
}
