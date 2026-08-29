"use client"

import { useTranslations } from "next-intl"
import { TableRow, TableCell } from "@/components/ui/table"
import { SummaryCard } from "@/components/common/summary-card"
import { ReportTable } from "@/components/common/report-table"
import type { PurchasesReport } from "@/types/domain/domain.types"

const fmtDay = (d: string) => (d || "").slice(0, 10)
const money = (v: number | null | undefined) => (v ?? 0).toFixed(2)

export function PurchasesTab({ data }: { data: PurchasesReport }) {
  const t = useTranslations("Reports")

  return (
    <div className="space-y-4">
      <div className="grid gap-3 sm:grid-cols-2">
        <SummaryCard label={t("invoiceCount")} value={String(data.invoiceCount)} />
        <SummaryCard label={t("total")} value={money(data.total)} highlight />
      </div>
      <ReportTable headers={[t("day"), t("invoiceCount"), t("total")]}>
        {data.byDay.map((r) => (
          <TableRow key={r.day}>
            <TableCell>{fmtDay(r.day)}</TableCell>
            <TableCell dir="ltr">{r.invoiceCount}</TableCell>
            <TableCell dir="ltr">{money(r.total)}</TableCell>
          </TableRow>
        ))}
      </ReportTable>
      <ReportTable headers={[t("supplier"), t("invoiceCount"), t("total")]}>
        {data.bySupplier.map((r) => (
          <TableRow key={r.supplierName}>
            <TableCell>{r.supplierName}</TableCell>
            <TableCell dir="ltr">{r.invoiceCount}</TableCell>
            <TableCell dir="ltr">{money(r.total)}</TableCell>
          </TableRow>
        ))}
      </ReportTable>
    </div>
  )
}
