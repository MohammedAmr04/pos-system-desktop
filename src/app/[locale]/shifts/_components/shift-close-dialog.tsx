"use client"

import { useState } from "react"
import { useQuery } from "@tanstack/react-query"
import { useTranslations } from "next-intl"
import { toast } from "sonner"
import { Loader2 } from "lucide-react"
import { Shift } from "@/types/domain/domain.types"
import { closeShift } from "@/actions/shifts.actions"
import { useShiftReport } from "@/hooks/use-shifts"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Card, CardContent } from "@/components/ui/card"

interface ShiftCloseDialogProps {
  shift: Shift | null
  onOpenChange: (open: boolean) => void
  onClosed: () => void
}

export function ShiftCloseDialog({ shift, onOpenChange, onClosed }: ShiftCloseDialogProps) {
  const t = useTranslations("Shifts")
  const [countedInput, setCountedInput] = useState("")
  const [closing, setClosing] = useState(false)

  const { data: closeReport } = useShiftReport(shift?.id ?? "", !!shift)

  const money = (v: number | null | undefined) => (v ?? 0).toFixed(2)

  const handleClose = async () => {
    if (!shift) return
    const counted = Number(countedInput)
    if (Number.isNaN(counted) || counted < 0) {
      toast.error(t("invalidCountedCash"))
      return
    }
    setClosing(true)
    try {
      await closeShift(shift.id, counted)
      toast.success(t("closed"))
      setCountedInput("")
      onClosed()
    } catch (e) {
      toast.error((e as Error).message || t("closeFailed"))
    } finally {
      setClosing(false)
    }
  }

  return (
    <Dialog open={!!shift} onOpenChange={(v) => !v && onOpenChange(false)}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>{t("closeDialogTitle", { number: shift?.number ?? 0 })}</DialogTitle>
          <DialogDescription>{t("closeDialogHint")}</DialogDescription>
        </DialogHeader>
        <Card>
          <CardContent className="space-y-1 pt-4 text-sm">
            <div className="flex justify-between"><span>{t("openingCash")}</span><span>{money(closeReport?.openingCash)}</span></div>
            <div className="flex justify-between"><span>{t("cashSales")}</span><span>+{money(closeReport?.cashSales)}</span></div>
            <div className="flex justify-between"><span>{t("saleRefunds")}</span><span>−{money(closeReport?.saleRefunds)}</span></div>
            <div className="flex justify-between"><span>{t("otherCashIn")}</span><span>+{money(closeReport?.otherCashIn)}</span></div>
            {(closeReport?.supplierPaymentsOut ?? 0) > 0 && (
              <div className="flex justify-between"><span>{t("supplierPaymentsOut")}</span><span>−{money(closeReport?.supplierPaymentsOut)}</span></div>
            )}
            {(closeReport?.supplierRefundsIn ?? 0) > 0 && (
              <div className="flex justify-between"><span>{t("supplierRefundsIn")}</span><span>+{money(closeReport?.supplierRefundsIn)}</span></div>
            )}
            <div className="flex justify-between border-t pt-1 font-bold">
              <span>{t("expectedCash")}</span><span>{money(closeReport?.expectedCash)}</span>
            </div>
          </CardContent>
        </Card>
        <label className="text-sm font-medium">{t("countedCash")}</label>
        <Input
          type="number"
          step="1"
          min="0"
          inputMode="decimal"
          value={countedInput}
          placeholder="0"
          onChange={(e) => setCountedInput(e.target.value)}
        />
        {countedInput !== "" && closeReport && !Number.isNaN(Number(countedInput)) && (
          <p className={`text-sm font-medium ${Number(countedInput) - closeReport.expectedCash === 0 ? "text-muted-foreground" : Number(countedInput) - closeReport.expectedCash > 0 ? "text-emerald-600" : "text-destructive"}`}>
            {t("differencePreview", { amount: (Number(countedInput) - closeReport.expectedCash).toFixed(2) })}
          </p>
        )}
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={closing}>
            {t("cancel")}
          </Button>
          <Button onClick={handleClose} disabled={closing || !closeReport}>
            {closing && <Loader2 className="ml-2 h-4 w-4 animate-spin" />}
            {t("confirmClose")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
