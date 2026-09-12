import { useQuery, UseQueryResult } from "@tanstack/react-query"
import {
  CashReport,
  EmployeePerformanceRow,
  ExpensesReport,
  InventoryValuationRow,
  Product,
  ProfitReport,
  PurchasesReport,
  ReturnsReport,
  SalesReport,
} from "@/types/domain/domain.types"
import {
  getCashReport,
  getEmployeePerformance,
  getExpensesReport,
  getInventoryReport,
  getLowStockReport,
  getProfitReport,
  getPurchasesReport,
  getReturnsReport,
  getSalesReport,
} from "@/api/reports"

export const reportsKeys = {
  all: ["reports"] as const,
  lowStock: () => [...reportsKeys.all, "low-stock"] as const,
  sales: (from: string, to: string) => [...reportsKeys.all, "sales", from, to] as const,
  purchases: (from: string, to: string) => [...reportsKeys.all, "purchases", from, to] as const,
  profit: (from: string, to: string) => [...reportsKeys.all, "profit", from, to] as const,
  inventory: () => [...reportsKeys.all, "inventory"] as const,
  returns: (from: string, to: string) => [...reportsKeys.all, "returns", from, to] as const,
  expenses: (from: string, to: string) => [...reportsKeys.all, "expenses", from, to] as const,
  cash: (from: string, to: string) => [...reportsKeys.all, "cash", from, to] as const,
  employees: (from: string, to: string) => [...reportsKeys.all, "employees", from, to] as const,
}

export function useLowStockReport(enabled = true) {
  return useQuery<Product[]>({
    queryKey: reportsKeys.lowStock(),
    queryFn: getLowStockReport,
    enabled,
  })
}

export function useSalesReport(from: string, to: string, enabled = true) {
  return useQuery<SalesReport>({
    queryKey: reportsKeys.sales(from, to),
    queryFn: () => getSalesReport(from, to),
    enabled,
  })
}

export function usePurchasesReport(from: string, to: string, enabled = true) {
  return useQuery<PurchasesReport>({
    queryKey: reportsKeys.purchases(from, to),
    queryFn: () => getPurchasesReport(from, to),
    enabled,
  })
}

export function useProfitReport(from: string, to: string, enabled = true) {
  return useQuery<ProfitReport>({
    queryKey: reportsKeys.profit(from, to),
    queryFn: () => getProfitReport(from, to),
    enabled,
  })
}

export function useInventoryReport(enabled = true) {
  return useQuery<InventoryValuationRow[]>({
    queryKey: reportsKeys.inventory(),
    queryFn: getInventoryReport,
    enabled,
  })
}

export function useReturnsReport(from: string, to: string, enabled = true) {
  return useQuery<ReturnsReport>({
    queryKey: reportsKeys.returns(from, to),
    queryFn: () => getReturnsReport(from, to),
    enabled,
  })
}

export function useExpensesReport(from: string, to: string, enabled = true) {
  return useQuery<ExpensesReport>({
    queryKey: reportsKeys.expenses(from, to),
    queryFn: () => getExpensesReport(from, to),
    enabled,
  })
}

export function useCashReport(from: string, to: string, enabled = true) {
  return useQuery<CashReport>({
    queryKey: reportsKeys.cash(from, to),
    queryFn: () => getCashReport(from, to),
    enabled,
  })
}

export function useEmployeePerformance(from: string, to: string, enabled = true) {
  return useQuery<EmployeePerformanceRow[]>({
    queryKey: reportsKeys.employees(from, to),
    queryFn: () => getEmployeePerformance(from, to),
    enabled,
  })
}
