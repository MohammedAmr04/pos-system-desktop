"use client"

import { useTranslations } from "next-intl"
import { TableRow, TableCell } from "@/components/ui/table"
import { SummaryCard } from "@/components/common/summary-card"
import { ReportTable } from "@/components/common/report-table"
import type { SalesReport } from "@/types/domain/domain.types"

const fmtDay = (d: string) => (d || "").slice(0, 10)
const money = (v: number | null | undefined) => (v ?? 0).toFixed(2)

export function SalesTab({ data }: { data: SalesReport }) {
  const t = useTranslations("Reports")
  const tc = useTranslations("Common")
  const methodLabel = (m: string) =>
    m === "credit" ? t("method_credit") : m === "card" ? t("method_card") : t("method_cash")

  return (
    <div className="space-y-4">
      <div className="grid gap-3 sm:grid-cols-4">
        <SummaryCard label={t("invoiceCount")} value={String(data.invoiceCount)} />
        <SummaryCard label={t("grossSales")} value={money(data.grossSales)} />
        <SummaryCard label={t("discounts")} value={money(data.discounts)} />
        <SummaryCard label={t("netSales")} value={money(data.netSales)} highlight />
      </div>
      <ReportTable headers={[t("day"), t("invoiceCount"), t("netSales"), t("discounts")]}>
        {data.byDay.map((r) => (
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
          {data.byPaymentMethod.map((r) => (
            <TableRow key={r.paymentMethod}>
              <TableCell>{methodLabel(r.paymentMethod)}</TableCell>
              <TableCell dir="ltr">{r.invoiceCount}</TableCell>
              <TableCell dir="ltr">{money(r.total)}</TableCell>
            </TableRow>
          ))}
        </ReportTable>
        <ReportTable headers={[t("cashier"), t("invoiceCount"), t("total")]}>
          {data.byCashier.map((r) => (
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
        {data.topProducts.map((r) => (
          <TableRow key={r.productId}>
            <TableCell>{r.productName}</TableCell>
            <TableCell dir="ltr">{r.quantity}</TableCell>
            <TableCell dir="ltr">{money(r.revenue)}</TableCell>
            <TableCell dir="ltr">{money(r.cost)}</TableCell>
          </TableRow>
        ))}
      </ReportTable>
    </div>
  )
}
