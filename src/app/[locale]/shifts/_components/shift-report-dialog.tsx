"use client"

import { useTranslations } from "next-intl"
import { Loader2 } from "lucide-react"
import { Shift } from "@/types/domain/domain.types"
import { useShiftReport } from "@/hooks/use-shifts"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"

interface ShiftReportDialogProps {
  shift: Shift | null
  onOpenChange: (open: boolean) => void
}

export function ShiftReportDialog({ shift, onOpenChange }: ShiftReportDialogProps) {
  const t = useTranslations("Shifts")
  const { data: report, isPending } = useShiftReport(shift?.id ?? "", !!shift)

  const money = (v: number | null | undefined) => (v ?? 0).toFixed(2)

  return (
    <Dialog open={!!shift} onOpenChange={(v) => !v && onOpenChange(false)}>
      <DialogContent className="max-w-xl">
        <DialogHeader>
          <DialogTitle>{t("reportTitle", { number: shift?.number ?? 0 })}</DialogTitle>
        </DialogHeader>
        {isPending || !report ? (
          <div className="py-8 text-center"><Loader2 className="mx-auto h-6 w-6 animate-spin" /></div>
        ) : (
          <>
            <div className="grid grid-cols-2 gap-x-6 gap-y-1 rounded-md border p-3 text-sm">
              <div className="flex justify-between"><span>{t("openingCash")}</span><span>{money(report.openingCash)}</span></div>
              <div className="flex justify-between"><span>{t("cashSales")}</span><span>+{money(report.cashSales)}</span></div>
              <div className="flex justify-between"><span>{t("saleRefunds")}</span><span>−{money(report.saleRefunds)}</span></div>
              <div className="flex justify-between"><span>{t("otherCashIn")}</span><span>+{money(report.otherCashIn)}</span></div>
              <div className="flex justify-between"><span>{t("supplierPaymentsOut")}</span><span>−{money(report.supplierPaymentsOut)}</span></div>
              <div className="flex justify-between"><span>{t("supplierRefundsIn")}</span><span>+{money(report.supplierRefundsIn)}</span></div>
              <div className="col-span-2 flex justify-between border-t pt-1 font-bold">
                <span>{t("expectedCash")}</span><span>{money(report.expectedCash)}</span>
              </div>
              {report.shift.countedCash != null && (
                <>
                  <div className="flex justify-between"><span>{t("countedCash")}</span><span>{money(report.shift.countedCash)}</span></div>
                  <div className={`flex justify-between font-bold ${(report.shift.difference ?? 0) >= 0 ? "text-emerald-600" : "text-destructive"}`}>
                    <span>{t("difference")}</span><span>{money(report.shift.difference)}</span>
                  </div>
                </>
              )}
            </div>
            <div className="max-h-64 overflow-y-auto rounded-md border">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>{t("date")}</TableHead>
                    <TableHead>{t("movement")}</TableHead>
                    <TableHead className="text-left">{t("amount")}</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {report.entries.map((e, i) => (
                    <TableRow key={i}>
                      <TableCell dir="ltr" className="whitespace-nowrap text-muted-foreground">
                        {new Date(e.date).toLocaleTimeString()}
                      </TableCell>
                      <TableCell>{e.description}</TableCell>
                      <TableCell className={`text-left font-medium ${e.amount < 0 ? "text-destructive" : ""}`}>
                        {e.amount.toFixed(2)}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          </>
        )}
      </DialogContent>
    </Dialog>
  )
}
