"use client"

import { useState } from "react"
import { useTranslations } from "next-intl"
import { toast } from "sonner"
import { Loader2 } from "lucide-react"
import { openShift } from "@/actions/shifts.actions"
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

interface ShiftOpenDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onOpened: () => void
}

export function ShiftOpenDialog({ open, onOpenChange, onOpened }: ShiftOpenDialogProps) {
  const t = useTranslations("Shifts")
  const [openingCashInput, setOpeningCashInput] = useState("")
  const [notesInput, setNotesInput] = useState("")
  const [opening, setOpening] = useState(false)

  const handleOpen = async () => {
    const value = Number(openingCashInput || "0")
    if (Number.isNaN(value) || value < 0) {
      toast.error(t("invalidOpeningCash"))
      return
    }
    setOpening(true)
    try {
      await openShift({ openingCash: value, notes: notesInput.trim() || undefined })
      toast.success(t("opened"))
      onOpenChange(false)
      setOpeningCashInput("")
      setNotesInput("")
      onOpened()
    } catch (e) {
      toast.error((e as Error).message || t("openFailed"))
    } finally {
      setOpening(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>{t("openDialogTitle")}</DialogTitle>
          <DialogDescription>{t("openDialogHint")}</DialogDescription>
        </DialogHeader>
        <label className="text-sm font-medium">{t("openingCash")}</label>
        <Input
          type="number"
          step="1"
          min="0"
          inputMode="decimal"
          value={openingCashInput}
          placeholder="0"
          onChange={(e) => setOpeningCashInput(e.target.value)}
        />
        <label className="text-sm font-medium">{t("notes")}</label>
        <Input value={notesInput} onChange={(e) => setNotesInput(e.target.value)} />
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={opening}>
            {t("cancel")}
          </Button>
          <Button onClick={handleOpen} disabled={opening}>
            {opening && <Loader2 className="ml-2 h-4 w-4 animate-spin" />}
            {t("confirmOpen")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
