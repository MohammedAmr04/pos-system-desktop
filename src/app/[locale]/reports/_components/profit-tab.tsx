"use client"

import { useTranslations } from "next-intl"
import { TableRow, TableCell } from "@/components/ui/table"
import { SummaryCard } from "@/components/common/summary-card"
import { ReportTable } from "@/components/common/report-table"
import type { ProfitReport } from "@/types/domain/domain.types"

const fmtDay = (d: string) => (d || "").slice(0, 10)
const money = (v: number | null | undefined) => (v ?? 0).toFixed(2)

export function ProfitTab({ data }: { data: ProfitReport }) {
  const t = useTranslations("Reports")

  return (
    <div className="space-y-4">
      <div className="grid gap-3 sm:grid-cols-4">
        <SummaryCard label={t("netRevenue")} value={money(data.netRevenue)} />
        <SummaryCard label={t("netCogs")} value={money(data.netCogs)} />
        <SummaryCard label={t("grossProfit")} value={money(data.grossProfit)} highlight />
        <SummaryCard label={t("margin")} value={`${data.marginPercent}%`} highlight />
      </div>
      <p className="text-xs text-muted-foreground">
        {t("profitHint", { refunds: money(data.saleRefunds), restored: money(data.restoredCosts) })}
      </p>
      <ReportTable headers={[t("day"), t("revenue"), t("cost"), t("profit")]}>
        {data.byDay.map((r) => (
          <TableRow key={r.day}>
            <TableCell>{fmtDay(r.day)}</TableCell>
            <TableCell dir="ltr">{money(r.revenue)}</TableCell>
            <TableCell dir="ltr">{money(r.cost)}</TableCell>
            <TableCell className="font-medium" dir="ltr">{money(r.profit)}</TableCell>
          </TableRow>
        ))}
      </ReportTable>
    </div>
  )
}
