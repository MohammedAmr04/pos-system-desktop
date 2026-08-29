"use client"

import { useTranslations } from "next-intl"
import { TableRow, TableCell } from "@/components/ui/table"
import { SummaryCard } from "@/components/common/summary-card"
import { ReportTable } from "@/components/common/report-table"
import type { ReturnsReport } from "@/types/domain/domain.types"

const fmtDay = (d: string) => (d || "").slice(0, 10)
const money = (v: number | null | undefined) => (v ?? 0).toFixed(2)

export function ReturnsTab({ data }: { data: ReturnsReport }) {
  const t = useTranslations("Reports")

  return (
    <div className="space-y-4">
      <div className="grid gap-3 sm:grid-cols-4">
        <SummaryCard label={t("saleReturnCount")} value={String(data.saleReturnCount)} />
        <SummaryCard label={t("saleRefundTotal")} value={money(data.saleRefundTotal)} />
        <SummaryCard label={t("purchaseReturnCount")} value={String(data.purchaseReturnCount)} />
        <SummaryCard label={t("purchaseRefundTotal")} value={money(data.purchaseRefundTotal)} />
      </div>
      <ReportTable headers={[t("day"), t("count"), t("total")]}>
        {data.saleReturnsByDay.map((r) => (
          <TableRow key={r.day}>
            <TableCell>{fmtDay(r.day)}</TableCell>
            <TableCell dir="ltr">{r.count}</TableCell>
            <TableCell dir="ltr">{money(r.total)}</TableCell>
          </TableRow>
        ))}
      </ReportTable>
    </div>
  )
}
