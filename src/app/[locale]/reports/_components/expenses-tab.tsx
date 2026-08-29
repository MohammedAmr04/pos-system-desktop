"use client"

import { useTranslations } from "next-intl"
import { TableRow, TableCell } from "@/components/ui/table"
import { SummaryCard } from "@/components/common/summary-card"
import { ReportTable } from "@/components/common/report-table"
import type { ExpensesReport } from "@/types/domain/domain.types"

const money = (v: number | null | undefined) => (v ?? 0).toFixed(2)

export function ExpensesTab({ data }: { data: ExpensesReport }) {
  const t = useTranslations("Reports")

  return (
    <div className="space-y-4">
      <div className="grid gap-3 sm:grid-cols-2">
        <SummaryCard label={t("expenseCount")} value={String(data.count)} />
        <SummaryCard label={t("totalCash")} value={money(data.totalCash)} highlight />
      </div>
      <ReportTable headers={[t("category"), t("count"), t("total")]}>
        {data.byCategory.map((r) => (
          <TableRow key={r.categoryName}>
            <TableCell>{r.categoryName}</TableCell>
            <TableCell dir="ltr">{r.count}</TableCell>
            <TableCell dir="ltr">{money(r.total)}</TableCell>
          </TableRow>
        ))}
      </ReportTable>
    </div>
  )
}
