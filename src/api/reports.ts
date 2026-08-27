import { request } from "@/lib/api"
import {
  Product,
  SalesReport,
  PurchasesReport,
  ProfitReport,
  InventoryValuationRow,
  ReturnsReport,
  ExpensesReport,
  CashReport,
} from "@/types/domain/domain.types"

export function getLowStockReport() {
  return request<Product[]>('/api/reports/low-stock')
}

export function getSalesReport(from: string, to: string) {
  return request<SalesReport>(`/api/reports/sales${rangeQuery(from, to)}`)
}

export function getPurchasesReport(from: string, to: string) {
  return request<PurchasesReport>(`/api/reports/purchases${rangeQuery(from, to)}`)
}

export function getProfitReport(from: string, to: string) {
  return request<ProfitReport>(`/api/reports/profit${rangeQuery(from, to)}`)
}

export function getInventoryReport() {
  return request<InventoryValuationRow[]>('/api/reports/inventory')
}

export function getReturnsReport(from: string, to: string) {
  return request<ReturnsReport>(`/api/reports/returns${rangeQuery(from, to)}`)
}

export function getExpensesReport(from: string, to: string) {
  return request<ExpensesReport>(`/api/reports/expenses${rangeQuery(from, to)}`)
}

export function getCashReport(from: string, to: string) {
  return request<CashReport>(`/api/reports/cash${rangeQuery(from, to)}`)
}

function rangeQuery(from: string, to: string) {
  return `?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`
}
