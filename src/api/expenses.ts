import { request, toQuery } from "@/lib/api"
import { ExpenseCategory, PagedExpenses } from "@/types/domain/domain.types"

export function listExpensesPaged(page = 1, pageSize = 20, categoryId?: string, from?: string, to?: string) {
  return request<PagedExpenses>(`/api/expenses${toQuery({ page, pageSize, categoryId, from, to })}`)
}

export function listExpenseCategories(includeInactive = false) {
  return request<ExpenseCategory[]>(`/api/expenses/categories${toQuery({ includeInactive })}`)
}
