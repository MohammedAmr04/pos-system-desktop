"use client"

import { useEffect, useState } from "react"
import { useTranslations } from "next-intl"
import { toast } from "sonner"
import { Loader2, TriangleAlert } from "lucide-react"
import {
  useCashReport,
  useEmployeePerformance,
  useExpensesReport,
  useInventoryReport,
  useProfitReport,
  usePurchasesReport,
  useReturnsReport,
  useSalesReport,
} from "@/hooks/use-reports"
import {
  CashReport,
  EmployeePerformanceRow,
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

import { SalesTab } from "./_components/sales-tab"
import { PurchasesTab } from "./_components/purchases-tab"
import { ProfitTab } from "./_components/profit-tab"
import { InventoryTab } from "./_components/inventory-tab"
import { ReturnsTab } from "./_components/returns-tab"
import { ExpensesTab } from "./_components/expenses-tab"
import { CashTab } from "./_components/cash-tab"
import { EmployeesTab } from "./_components/employees-tab"
import { Link } from "@/i18n/navigation"

type ReportKey = "sales" | "purchases" | "profit" | "inventory" | "returns" | "expenses" | "cash" | "employees"

const REPORT_KEYS: ReportKey[] = ["sales", "purchases", "profit", "inventory", "returns", "expenses", "cash", "employees"]

const monthStart = () => {
  const d = new Date()
  return new Date(d.getFullYear(), d.getMonth(), 1).toISOString().slice(0, 10)
}
const today = () => new Date().toISOString().slice(0, 10)

export function ReportsClient() {
  const t = useTranslations("Reports")
  const tl = useTranslations("LowStock")
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
  const { data: employees, isFetching: employeesFetching, isError: employeesError } = useEmployeePerformance(from, to, canView && tab === "employees") as { data: EmployeePerformanceRow[] | undefined; isFetching: boolean; isError: boolean }

  const activeError = tab === "sales" ? salesError
    : tab === "purchases" ? purchasesError
    : tab === "profit" ? profitError
    : tab === "inventory" ? inventoryError
    : tab === "returns" ? returnsError
    : tab === "expenses" ? expensesError
    : tab === "cash" ? cashError
    : employeesError

  const loading = tab === "sales" ? salesFetching
    : tab === "purchases" ? purchasesFetching
    : tab === "profit" ? profitFetching
    : tab === "inventory" ? inventoryFetching
    : tab === "returns" ? returnsFetching
    : tab === "expenses" ? expensesFetching
    : tab === "cash" ? cashFetching
    : employeesFetching

  useEffect(() => {
    if (activeError) toast.error(t("loadFailed"))
  }, [activeError, t])

  if (!canView) return <AccessDenied />

  const tabs: { key: ReportKey; label: string; visible: boolean }[] =
    REPORT_KEYS.map((k) => ({ key: k, label: t(`tab_${k}`), visible: true }))

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
          <Link href="/low-stock">
            <Button variant="outline" size="sm" className="h-9">
              <TriangleAlert className="ms-1 h-4 w-4" />
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
          {tab === "sales" && sales && <SalesTab data={sales} />}
          {tab === "purchases" && purchases && <PurchasesTab data={purchases} />}
          {tab === "profit" && profit && <ProfitTab data={profit} />}
          {tab === "inventory" && inventory && <InventoryTab data={inventory} />}
          {tab === "returns" && returns && <ReturnsTab data={returns} />}
          {tab === "expenses" && expenses && <ExpensesTab data={expenses} />}
          {tab === "cash" && cash && <CashTab data={cash} />}
          {tab === "employees" && employees && <EmployeesTab data={employees} />}
        </>
      )}
    </div>
  )
}
