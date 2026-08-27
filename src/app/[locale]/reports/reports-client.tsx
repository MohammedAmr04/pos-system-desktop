"use client"

import { useEffect, useState } from "react"
import { useTranslations } from "next-intl"
import Link from "next/link"
import { toast } from "sonner"
import { Loader2, TriangleAlert } from "lucide-react"
import {
  useCashReport,
  useExpensesReport,
  useInventoryReport,
  useProfitReport,
  usePurchasesReport,
  useReturnsReport,
  useSalesReport,
} from "@/hooks/use-reports"
import {
  CashReport,
  ExpensesReport,
  InventoryValuationRow,
  ProfitReport,
  PurchasesReport,
  ReturnsReport,
  SalesReport,
} from "@/types/domain/domain.types"
import { useAuth } from "@/components/common/auth-context"
import { PERMISSIONS, FEATURES } from "@/lib/constants"
import { AccessDenied } from "@/components/common/access-denied"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"

type ReportKey = "sales" | "purchases" | "profit" | "inventory" | "returns" | "expenses" | "cash"

const REPORT_KEYS: ReportKey[] = ["sales", "purchases", "profit", "inventory", "returns", "expenses", "cash"]

const monthStart = () => {
  const d = new Date()
  return new Date(d.getFullYear(), d.getMonth(), 1).toISOString().slice(0, 10)
}
const today = () => new Date().toISOString().slice(0, 10)

export function ReportsClient() {
  const t = useTranslations("Reports")
  const tl = useTranslations("LowStock")
  const tc = useTranslations("Common")
  const { hasAccess, hasPermission } = useAuth()
  const canView = hasPermission(PERMISSIONS.REPORTS_VIEW)
  const canLowStock = hasAccess(PERMISSIONS.REPORTS_VIEW, FEATURES.LOW_STOCK_REPORT)

  const [tab, setTab] = useState<ReportKey>("sales")
  const [from, setFrom] = useState(monthStart())
  const [to, setTo] = useState(today())

  const { data: sales, isFetching: salesFetching, isError: salesError } = useSalesReport(from, to, canView && tab === "sales") as { data: SalesReport | undefined; isFetching: boolean; isError: boolean }
  const { data: purchases, isFetching: purchasesFetching, isError: purchasesError } = usePurchasesReport(from, to, canView && tab === "purchases") as { data: PurchasesReport | undefined; isFetching: boolean; isError: boolean }
  const { data: profit, isFetching: profitFetching, isError: profitError } = useProfitReport(from, to, canView && tab === "profit") as { data: ProfitReport | undefined; isFetching: boolean; isError: boolean }
  const { data: inventory, isFetching: inventoryFetching, isError: inventoryError } = useInventoryReport(canView && tab === "inventory") as { data: InventoryValuationRow[] | undefined; isFetching: boolean; isError: boolean }
  const { data: returns, isFetching: returnsFetching, isError: returnsError } = useReturnsReport(from, to, canView && tab === "returns") as { data: ReturnsReport | undefined; isFetching: boolean; isError: boolean }
  const { data: expenses, isFetching: expensesFetching, isError: expensesError } = useExpensesReport(from, to, canView && tab === "expenses") as { data: ExpensesReport | undefined; isFetching: boolean; isError: boolean }
  const { data: cash, isFetching: cashFetching, isError: cashError } = useCashReport(from, to, canView && tab === "cash") as { data: CashReport | undefined; isFetching: boolean; isError: boolean }

  const activeError = tab === "sales" ? salesError
    : tab === "purchases" ? purchasesError
    : tab === "profit" ? profitError
    : tab === "inventory" ? inventoryError
    : tab === "returns" ? returnsError
    : tab === "expenses" ? expensesError
    : cashError

  const loading = tab === "sales" ? salesFetching
    : tab === "purchases" ? purchasesFetching
    : tab === "profit" ? profitFetching
    : tab === "inventory" ? inventoryFetching
    : tab === "returns" ? returnsFetching
    : tab === "expenses" ? expensesFetching
    : cashFetching

  useEffect(() => {
    if (activeError) toast.error(t("loadFailed"))
  }, [activeError, t])

  if (!canView) return <AccessDenied />
  const fmtDay = (d: string) => (d || "").slice(0, 10)
  const money = (v: number | null | undefined) => (v ?? 0).toFixed(2)
  const methodLabel = (m: string) =>
    m === "credit" ? t("method_credit") : m === "card" ? t("method_card") : t("method_cash")

  const tabs: { key: ReportKey; label: string; visible: boolean }[] = [
    ...REPORT_KEYS.map((k) => ({ key: k, label: t(`tab_${k}`), visible: true })),
  ]

  return (
    <div className="flex-1 space-y-4 pt-6">
      <h2 className="text-3xl font-bold tracking-tight">{t("title")}</h2>

      <div className="flex flex-wrap items-center gap-2">
        <div className="flex flex-wrap rounded-lg border bg-background p-1">
          {tabs.map(({ key, label }) => (
            <Button
              key={key}
              variant={tab === key ? "default" : "ghost"}
              size="sm"
              className="h-8 px-3"
              onClick={() => setTab(key)}
            >
              {label}
            </Button>
          ))}
        </div>
        <div className="flex items-center gap-2">
          <Input type="date" value={from} onChange={(e) => setFrom(e.target.value)} className="w-40" />
          <span className="text-sm text-muted-foreground">—</span>
          <Input type="date" value={to} onChange={(e) => setTo(e.target.value)} className="w-40" />
        </div>
        {canLowStock && (
          <Link href="/low-stock/">
            <Button variant="outline" size="sm" className="h-9">
              <TriangleAlert className="ml-1 h-4 w-4" />
              {tl("title")}
            </Button>
          </Link>
        )}
      </div>

      {loading ? (
        <div className="flex h-40 items-center justify-center">
          <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
        </div>
      ) : (
        <>
          {tab === "sales" && sales && (
            <div className="space-y-4">
              <div className="grid gap-3 sm:grid-cols-4">
                <SummaryCard label={t("invoiceCount")} value={String(sales.invoiceCount)} />
                <SummaryCard label={t("grossSales")} value={money(sales.grossSales)} />
                <SummaryCard label={t("discounts")} value={money(sales.discounts)} />
                <SummaryCard label={t("netSales")} value={money(sales.netSales)} highlight />
              </div>
              <ReportTable headers={[t("day"), t("invoiceCount"), t("netSales"), t("discounts")]}>
                {sales.byDay.map((r: { day: string; invoiceCount: number; netSales: number; discounts: number }) => (
                  <TableRow key={r.day}>
                    <TableCell>{fmtDay(r.day)}</TableCell>
                    <TableCell dir="ltr">{r.invoiceCount}</TableCell>
                    <TableCell dir="ltr">{money(r.netSales)}</TableCell>
                    <TableCell dir="ltr">{money(r.discounts)}</TableCell>
                  </TableRow>
                ))}
              </ReportTable>
              <div className="grid gap-4 lg:grid-cols-2">
                <ReportTable headers={[t("paymentMethod"), t("invoiceCount"), t("total")]}>
                  {sales.byPaymentMethod.map((r: { paymentMethod: string; invoiceCount: number; total: number }) => (
                    <TableRow key={r.paymentMethod}>
                      <TableCell>{methodLabel(r.paymentMethod)}</TableCell>
                      <TableCell dir="ltr">{r.invoiceCount}</TableCell>
                      <TableCell dir="ltr">{money(r.total)}</TableCell>
                    </TableRow>
                  ))}
                </ReportTable>
                <ReportTable headers={[t("cashier"), t("invoiceCount"), t("total")]}>
                  {sales.byCashier.map((r: { userName: string; invoiceCount: number; total: number }) => (
                    <TableRow key={r.userName}>
                      <TableCell>{r.userName}</TableCell>
                      <TableCell dir="ltr">{r.invoiceCount}</TableCell>
                      <TableCell dir="ltr">{money(r.total)}</TableCell>
                    </TableRow>
                  ))}
                </ReportTable>
              </div>
              <ReportTable
                headers={[t("product"), t("quantity"), t("revenue"), t("cost")]}
                empty={tc("noResults")}
              >
                {sales.topProducts.map((r: { productId: string; productName: string; quantity: number; revenue: number; cost: number }) => (
                  <TableRow key={r.productId}>
                    <TableCell>{r.productName}</TableCell>
                    <TableCell dir="ltr">{r.quantity}</TableCell>
                    <TableCell dir="ltr">{money(r.revenue)}</TableCell>
                    <TableCell dir="ltr">{money(r.cost)}</TableCell>
                  </TableRow>
                ))}
              </ReportTable>
            </div>
          )}

          {tab === "purchases" && purchases && (
            <div className="space-y-4">
              <div className="grid gap-3 sm:grid-cols-2">
                <SummaryCard label={t("invoiceCount")} value={String(purchases.invoiceCount)} />
                <SummaryCard label={t("total")} value={money(purchases.total)} highlight />
              </div>
              <ReportTable headers={[t("day"), t("invoiceCount"), t("total")]}>
                {purchases.byDay.map((r: { day: string; invoiceCount: number; total: number }) => (
                  <TableRow key={r.day}>
                    <TableCell>{fmtDay(r.day)}</TableCell>
                    <TableCell dir="ltr">{r.invoiceCount}</TableCell>
                    <TableCell dir="ltr">{money(r.total)}</TableCell>
                  </TableRow>
                ))}
              </ReportTable>
              <ReportTable headers={[t("supplier"), t("invoiceCount"), t("total")]}>
                {purchases.bySupplier.map((r: { supplierName: string; invoiceCount: number; total: number }) => (
                  <TableRow key={r.supplierName}>
                    <TableCell>{r.supplierName}</TableCell>
                    <TableCell dir="ltr">{r.invoiceCount}</TableCell>
                    <TableCell dir="ltr">{money(r.total)}</TableCell>
                  </TableRow>
                ))}
              </ReportTable>
            </div>
          )}

          {tab === "profit" && profit && (
            <div className="space-y-4">
              <div className="grid gap-3 sm:grid-cols-4">
                <SummaryCard label={t("netRevenue")} value={money(profit.netRevenue)} />
                <SummaryCard label={t("netCogs")} value={money(profit.netCogs)} />
                <SummaryCard label={t("grossProfit")} value={money(profit.grossProfit)} highlight />
                <SummaryCard label={t("margin")} value={`${profit.marginPercent}%`} highlight />
              </div>
              <p className="text-xs text-muted-foreground">
                {t("profitHint", { refunds: money(profit.saleRefunds), restored: money(profit.restoredCosts) })}
              </p>
              <ReportTable headers={[t("day"), t("revenue"), t("cost"), t("profit")]}>
                {profit.byDay.map((r: { day: string; revenue: number; cost: number; profit: number }) => (
                  <TableRow key={r.day}>
                    <TableCell>{fmtDay(r.day)}</TableCell>
                    <TableCell dir="ltr">{money(r.revenue)}</TableCell>
                    <TableCell dir="ltr">{money(r.cost)}</TableCell>
                    <TableCell className="font-medium" dir="ltr">{money(r.profit)}</TableCell>
                  </TableRow>
                ))}
              </ReportTable>
            </div>
          )}

          {tab === "inventory" && inventory && (
            <ReportTable
              headers={[t("product"), t("category"), t("stockQty"), t("unitCost"), t("value")]}
              empty={tc("noResults")}
            >
              {inventory.map((r: InventoryValuationRow) => (
                <TableRow key={r.productId}>
                  <TableCell>{r.productName}</TableCell>
                  <TableCell>{r.categoryName || "—"}</TableCell>
                  <TableCell dir="ltr">{r.stockQuantity}</TableCell>
                  <TableCell dir="ltr">{money(r.unitCost)}</TableCell>
                  <TableCell className="font-medium" dir="ltr">{money(r.value)}</TableCell>
                </TableRow>
              ))}
              <TableRow className="font-semibold">
                <TableCell colSpan={4}>{t("totalValue")}</TableCell>
                <TableCell dir="ltr">{money(inventory.reduce((s, r) => s + (r.value ?? 0), 0))}</TableCell>
              </TableRow>
            </ReportTable>
          )}

          {tab === "returns" && returns && (
            <div className="space-y-4">
              <div className="grid gap-3 sm:grid-cols-4">
                <SummaryCard label={t("saleReturnCount")} value={String(returns.saleReturnCount)} />
                <SummaryCard label={t("saleRefundTotal")} value={money(returns.saleRefundTotal)} />
                <SummaryCard label={t("purchaseReturnCount")} value={String(returns.purchaseReturnCount)} />
                <SummaryCard label={t("purchaseRefundTotal")} value={money(returns.purchaseRefundTotal)} />
              </div>
              <ReportTable headers={[t("day"), t("count"), t("total")]}>
                {returns.saleReturnsByDay.map((r: { day: string; count: number; total: number }) => (
                  <TableRow key={r.day}>
                    <TableCell>{fmtDay(r.day)}</TableCell>
                    <TableCell dir="ltr">{r.count}</TableCell>
                    <TableCell dir="ltr">{money(r.total)}</TableCell>
                  </TableRow>
                ))}
              </ReportTable>
            </div>
          )}

          {tab === "expenses" && expenses && (
            <div className="space-y-4">
              <div className="grid gap-3 sm:grid-cols-2">
                <SummaryCard label={t("expenseCount")} value={String(expenses.count)} />
                <SummaryCard label={t("totalCash")} value={money(expenses.totalCash)} highlight />
              </div>
              <ReportTable headers={[t("category"), t("count"), t("total")]}>
                {expenses.byCategory.map((r: { categoryName: string; count: number; total: number }) => (
                  <TableRow key={r.categoryName}>
                    <TableCell>{r.categoryName}</TableCell>
                    <TableCell dir="ltr">{r.count}</TableCell>
                    <TableCell dir="ltr">{money(r.total)}</TableCell>
                  </TableRow>
                ))}
              </ReportTable>
            </div>
          )}

          {tab === "cash" && cash && (
            <div className="space-y-4">
              <div className="grid gap-3 sm:grid-cols-5">
                <SummaryCard label={t("shiftCount")} value={String(cash.shiftCount)} />
                <SummaryCard label={t("openingCash")} value={money(cash.totalOpening)} />
                <SummaryCard label={t("expectedCash")} value={money(cash.totalExpected)} />
                <SummaryCard label={t("countedCash")} value={money(cash.totalCounted)} />
                <SummaryCard
                  label={t("difference")}
                  value={money(cash.totalDifference)}
                  tone={cash.totalDifference === 0 ? "neutral" : cash.totalDifference > 0 ? "positive" : "negative"}
                />
              </div>
              <ReportTable
                headers={[
                  "#",
                  t("openedAt"),
                  t("closedAt"),
                  t("openingCash"),
                  t("expectedCash"),
                  t("countedCash"),
                  t("difference"),
                ]}
                empty={tc("noResults")}
              >
                {cash.shifts.map((r: { number: number; openedAt: string; closedAt: string; openingCash: number; expectedCash: number; countedCash: number; difference: number }) => (
                  <TableRow key={r.number}>
                    <TableCell>{r.number}</TableCell>
                    <TableCell>{fmtDay(r.openedAt)}</TableCell>
                    <TableCell>{fmtDay(r.closedAt)}</TableCell>
                    <TableCell dir="ltr">{money(r.openingCash)}</TableCell>
                    <TableCell dir="ltr">{money(r.expectedCash)}</TableCell>
                    <TableCell dir="ltr">{money(r.countedCash)}</TableCell>
                    <TableCell className={r.difference < 0 ? "text-red-600" : r.difference > 0 ? "text-emerald-600" : ""} dir="ltr">
                      {money(r.difference)}
                    </TableCell>
                  </TableRow>
                ))}
              </ReportTable>
            </div>
          )}
        </>
      )}
    </div>
  )
}

function SummaryCard({
  label,
  value,
  highlight,
  tone,
}: {
  label: string
  value: string
  highlight?: boolean
  tone?: "neutral" | "positive" | "negative"
}) {
  const toneClass =
    tone === "positive" ? "text-emerald-600" : tone === "negative" ? "text-red-600" : ""
  return (
    <div className={`rounded-lg border bg-card p-4 ${highlight ? "border-primary/40" : ""}`}>
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className={`mt-1 text-xl font-bold ${toneClass}`} dir="ltr">{value}</p>
    </div>
  )
}

function ReportTable({
  headers,
  children,
  empty,
}: {
  headers: string[]
  children: React.ReactNode
  empty?: string
}) {
  const tc = useTranslations("Common")
  let body = children
  const rows = Array.isArray(children) ? children : [children]
  const isEmpty = rows.every(
    (r) => r === null || r === undefined || r === false || (Array.isArray(r) && r.length === 0),
  )
  if (isEmpty) {
    body = (
      <TableRow>
        <TableCell colSpan={headers.length} className="h-20 text-center text-muted-foreground">
          {empty ?? tc("noResults")}
        </TableCell>
      </TableRow>
    )
  }
  return (
    <div className="rounded-md border bg-card">
      <Table>
        <TableHeader>
          <TableRow>
            {headers.map((h) => (
              <TableHead key={h}>{h}</TableHead>
            ))}
          </TableRow>
        </TableHeader>
        <TableBody>{body}</TableBody>
      </Table>
    </div>
  )
}
