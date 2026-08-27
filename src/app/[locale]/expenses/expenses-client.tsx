"use client"

import { useState } from "react"
import { useQueryClient } from "@tanstack/react-query"
import { useTranslations } from "next-intl"
import { Expense } from "@/types/domain/domain.types"
import { useAuth } from "@/components/common/auth-context"
import { PERMISSIONS } from "@/lib/constants"
import { AccessDenied } from "@/components/common/access-denied"
import { Button } from "@/components/ui/button"
import { useExpensesPage, useExpenseCategories, expensesKeys } from "@/hooks/use-expenses"
import { TableColumn, TableBuilder } from "@/components/common/table-builder"
import { DataPagination } from "@/components/common/data-pagination"
import { Plus, Settings2 } from "lucide-react"
import { ExpenseCreateDialog } from "./_components/expense-create-dialog"
import { ExpenseCategoriesDialog } from "./_components/expense-categories-dialog"

const PAGE_SIZE = 20

export function ExpensesClient() {
  const t = useTranslations("Expenses")
  const tc = useTranslations("Common")
  const { hasPermission } = useAuth()
  const canView = hasPermission(PERMISSIONS.EXPENSES_VIEW)
  const canCreate = hasPermission(PERMISSIONS.EXPENSES_CREATE)
  const canManageCategories = hasPermission(PERMISSIONS.EXPENSES_CATEGORIES)

  const [page, setPage] = useState(1)
  const [createOpen, setCreateOpen] = useState(false)
  const [catsOpen, setCatsOpen] = useState(false)
  const queryClient = useQueryClient()

  const { data: paged, isPending } = useExpensesPage(page, PAGE_SIZE)
  const expenses = paged?.items ?? []
  const total = paged?.total ?? 0
  const { data: categories = [] } = useExpenseCategories(true)

  if (!canView) return <AccessDenied />

  const money = (v: number | null | undefined) => (v ?? 0).toFixed(2)
  const fmtDate = (d: string | null | undefined) =>
    d ? new Date(d).toLocaleString(undefined, { dateStyle: "short", timeStyle: "short" }) : "—"

  const columns: TableColumn<Expense>[] = [
    {
      key: "date",
      header: t("date"),
      className: "whitespace-nowrap",
      cell: (exp) => fmtDate(exp.date),
    },
    {
      key: "category",
      header: t("category"),
      cell: (exp) => exp.categoryName ?? categories.find((c) => c.id === exp.categoryId)?.name ?? exp.categoryId,
    },
    {
      key: "description",
      header: t("description"),
      className: "max-w-xs",
      cell: (exp) => (
        <>
          <span className="line-clamp-1">{exp.description || "—"}</span>
          {exp.reference && (
            <span className="block text-xs text-muted-foreground">{exp.reference}</span>
          )}
        </>
      ),
    },
    {
      key: "method",
      header: t("method"),
      cell: (exp) => t(`method_${exp.paymentMethod}`),
    },
    {
      key: "amount",
      header: t("amount"),
      cell: (exp) => <span className="font-medium text-red-600"><span dir="ltr">-{money(exp.amount)}</span></span>,
    },
    {
      key: "recordedBy",
      header: t("recordedBy"),
      cell: (exp) => exp.createdBy ?? "—",
    },
  ]

  return (
    <div className="flex-1 space-y-4 pt-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-3xl font-bold tracking-tight">{t("title")}</h2>
        <div className="flex items-center gap-2">
          {canManageCategories && (
            <Button variant="outline" onClick={() => setCatsOpen(true)}>
              <Settings2 className="ml-2 h-4 w-4" />
              {t("manageCategories")}
            </Button>
          )}
          {canCreate && (
            <Button onClick={() => setCreateOpen(true)}>
              <Plus className="ml-2 h-4 w-4" />
              {t("newExpense")}
            </Button>
          )}
        </div>
      </div>

      <TableBuilder
        columns={columns}
        data={expenses}
        rowKey={(exp) => exp.id}
        loading={isPending}
        emptyMessage={tc("noResults")}
      />

      <DataPagination page={page} pageSize={PAGE_SIZE} total={total} onPageChange={setPage} />

      <ExpenseCreateDialog
        open={createOpen}
        onOpenChange={setCreateOpen}
        categories={categories}
        onSaved={() => queryClient.invalidateQueries({ queryKey: expensesKeys.all })}
      />

      <ExpenseCategoriesDialog open={catsOpen} onOpenChange={setCatsOpen} />
    </div>
  )
}
