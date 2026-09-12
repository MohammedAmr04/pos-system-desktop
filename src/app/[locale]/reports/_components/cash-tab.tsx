"use client"

import { useTranslations } from "next-intl"
import { TableRow, TableCell } from "@/components/ui/table"
import { SummaryCard } from "@/components/common/summary-card"
import { ReportTable } from "@/components/common/report-table"
import type { CashReport } from "@/types/domain/domain.types"

const fmtDay = (d: string) => (d || "").slice(0, 10)
const money = (v: number | null | undefined) => (v ?? 0).toFixed(2)

export function CashTab({ data }: { data: CashReport }) {
  const t = useTranslations("Reports")
  const tc = useTranslations("Common")

  return (
    <div className="space-y-4">
      <div className="grid gap-3 sm:grid-cols-5">
        <SummaryCard label={t("shiftCount")} value={String(data.shiftCount)} />
        <SummaryCard label={t("openingCash")} value={money(data.totalOpening)} />
        <SummaryCard label={t("expectedCash")} value={money(data.totalExpected)} />
        <SummaryCard label={t("countedCash")} value={money(data.totalCounted)} />
        <SummaryCard
          label={t("difference")}
          value={money(data.totalDifference)}
          tone={data.totalDifference === 0 ? "neutral" : data.totalDifference > 0 ? "positive" : "negative"}
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
        {data.shifts.map((r) => (
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
  )
}
