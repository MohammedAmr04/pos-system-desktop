import { queryClient } from "@/lib/query-client"
import { request } from "@/lib/api"
import { Expense, ExpenseCategory } from "@/types/domain/domain.types"
import { expensesKeys } from "@/hooks/use-expenses"

export async function createExpense(data: {
  categoryId: string
  amount: number
  paymentMethod?: string
  description?: string
  reference?: string
}) {
  const result = await request<Expense>('/api/expenses', { method: 'POST', body: JSON.stringify(data) })
  await queryClient.invalidateQueries({ queryKey: expensesKeys.all })
  return result
}

export async function createExpenseCategory(name: string) {
  const result = await request<ExpenseCategory>('/api/expenses/categories', { method: 'POST', body: JSON.stringify({ name }) })
  await queryClient.invalidateQueries({ queryKey: expensesKeys.all })
  return result
}

export async function updateExpenseCategory(id: string, data: { name: string; isActive: boolean }) {
  const result = await request<ExpenseCategory>(`/api/expenses/categories/${id}`, { method: 'PUT', body: JSON.stringify(data) })
  await queryClient.invalidateQueries({ queryKey: expensesKeys.all })
  return result
}
