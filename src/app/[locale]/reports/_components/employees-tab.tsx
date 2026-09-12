"use client"

import { useTranslations } from "next-intl"
import { Trophy } from "lucide-react"
import { TableRow, TableCell } from "@/components/ui/table"
import { ReportTable } from "@/components/common/report-table"
import type { EmployeePerformanceRow } from "@/types/domain/domain.types"

const money = (v: number | null | undefined) => (v ?? 0).toFixed(2)

export function EmployeesTab({ data }: { data: EmployeePerformanceRow[] }) {
  const t = useTranslations("Reports")
  const tc = useTranslations("Common")

  return (
    <div className="space-y-4">
      <ReportTable
        headers={[t("employee"), t("invoiceCount"), t("total"), t("averageTicket")]}
        empty={tc("noResults")}
      >
        {data.map((r, i) => (
          <TableRow key={r.employeeId ?? "unassigned"}>
            <TableCell>
              <span className="inline-flex items-center gap-2">
                {i === 0 && r.employeeId && <Trophy className="h-4 w-4 text-amber-500" />}
                <span className={r.employeeId ? "font-medium" : "text-muted-foreground"}>
                  {r.employeeName ?? t("unassigned")}
                </span>
              </span>
            </TableCell>
            <TableCell dir="ltr">{r.invoiceCount}</TableCell>
            <TableCell dir="ltr">{money(r.total)}</TableCell>
            <TableCell dir="ltr">{money(r.averageTicket)}</TableCell>
          </TableRow>
        ))}
      </ReportTable>
    </div>
  )
}
