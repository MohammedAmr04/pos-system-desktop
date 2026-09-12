"use client"

import { useTranslations } from "next-intl"
import { TableRow, TableCell } from "@/components/ui/table"
import { ReportTable } from "@/components/common/report-table"
import type { InventoryValuationRow } from "@/types/domain/domain.types"

const money = (v: number | null | undefined) => (v ?? 0).toFixed(2)

export function InventoryTab({ data }: { data: InventoryValuationRow[] }) {
  const t = useTranslations("Reports")
  const tc = useTranslations("Common")
  const totalValue = data.reduce((s, r) => s + (r.value ?? 0), 0)

  return (
    <ReportTable
      headers={[t("product"), t("category"), t("stockQty"), t("unitCost"), t("value")]}
      empty={tc("noResults")}
    >
      {data.map((r) => (
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
        <TableCell dir="ltr">{money(totalValue)}</TableCell>
      </TableRow>
    </ReportTable>
  )
}
