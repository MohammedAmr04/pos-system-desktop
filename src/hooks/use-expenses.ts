import { keepPreviousData, useQuery } from "@tanstack/react-query"
import { listExpenseCategories, listExpensesPaged } from "@/api/expenses"

export interface ExpensesFilter {
  categoryId?: string
  from?: string
  to?: string
}

export const expensesKeys = {
  all: ["expenses"] as const,
  paged: (page: number, pageSize: number, filter: ExpensesFilter) =>
    ["expenses", "paged", page, pageSize, filter] as const,
  categories: (includeInactive = false) => ["expenses", "categories", includeInactive] as const,
}

export function useExpensesPage(page: number, pageSize: number, filter: ExpensesFilter = {}) {
  return useQuery({
    queryKey: expensesKeys.paged(page, pageSize, filter),
    queryFn: () =>
      listExpensesPaged(
        page,
        pageSize,
        filter.categoryId || undefined,
        filter.from || undefined,
        filter.to || undefined
      ),
    placeholderData: keepPreviousData,
  })
}

export function useExpenseCategories(includeInactive = false) {
  return useQuery({
    queryKey: expensesKeys.categories(includeInactive),
    queryFn: () => listExpenseCategories(includeInactive),
  })
}
