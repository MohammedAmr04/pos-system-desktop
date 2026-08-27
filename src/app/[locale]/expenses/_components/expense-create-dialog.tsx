"use client"

import { useState } from "react"
import { useTranslations } from "next-intl"
import { toast } from "sonner"
import { Loader2 } from "lucide-react"
import { ExpenseCategory } from "@/types/domain/domain.types"
import { createExpense } from "@/actions/expenses.actions"
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
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"

interface ExpenseCreateDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  categories: ExpenseCategory[]
  onSaved: () => void
}

export function ExpenseCreateDialog({ open, onOpenChange, categories, onSaved }: ExpenseCreateDialogProps) {
  const t = useTranslations("Expenses")
  const tc = useTranslations("Common")
  const [categoryInput, setCategoryInput] = useState("")
  const [amountInput, setAmountInput] = useState("")
  const [descriptionInput, setDescriptionInput] = useState("")
  const [referenceInput, setReferenceInput] = useState("")
  const [creating, setCreating] = useState(false)

  const activeCats = categories.filter((c) => c.isActive)

  const handleCreate = async () => {
    const amount = Number(amountInput)
    if (!categoryInput) {
      toast.error(t("invalidCategory"))
      return
    }
    if (Number.isNaN(amount) || amount <= 0) {
      toast.error(t("invalidAmount"))
      return
    }
    setCreating(true)
    try {
      await createExpense({
        categoryId: categoryInput,
        amount,
        paymentMethod: "cash",
        description: descriptionInput.trim() || undefined,
        reference: referenceInput.trim() || undefined,
      })
      toast.success(t("created"))
      onOpenChange(false)
      setCategoryInput("")
      setAmountInput("")
      setDescriptionInput("")
      setReferenceInput("")
      onSaved()
    } catch (e) {
      toast.error((e as Error).message || t("createFailed"))
    } finally {
      setCreating(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{t("newExpenseTitle")}</DialogTitle>
          <DialogDescription>{t("newExpenseHint")}</DialogDescription>
        </DialogHeader>
        <div className="space-y-4">
          <div className="space-y-2">
            <label className="text-sm font-medium">{t("category")}</label>
            <Select
              value={categoryInput}
              onValueChange={(v) => v != null && setCategoryInput(v)}
            >
              <SelectTrigger className="w-full">
                <SelectValue placeholder={t("selectCategory")} />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="">{t("selectCategory")}</SelectItem>
                {activeCats.map((c) => (
                  <SelectItem key={c.id} value={c.id}>
                    {c.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <label className="text-sm font-medium" htmlFor="expense-amount">
                {t("amount")}
              </label>
              <Input
                id="expense-amount"
                type="number"
                step="0.01"
                min="0.01"
                value={amountInput}
                onChange={(e) => setAmountInput(e.target.value)}
                autoFocus
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t("method")}</label>
              <div className="flex h-9 items-center rounded-lg border bg-muted/40 px-3 text-sm text-muted-foreground">
                {t("method_cash")}
              </div>
            </div>
          </div>
          <div className="space-y-2">
            <label className="text-sm font-medium" htmlFor="expense-desc">
              {t("description")}
            </label>
            <Input
              id="expense-desc"
              value={descriptionInput}
              onChange={(e) => setDescriptionInput(e.target.value)}
            />
          </div>
          <div className="space-y-2">
            <label className="text-sm font-medium" htmlFor="expense-ref">
              {t("reference")}
            </label>
            <Input
              id="expense-ref"
              value={referenceInput}
              onChange={(e) => setReferenceInput(e.target.value)}
            />
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={creating}>
            {tc("cancel")}
          </Button>
          <Button onClick={handleCreate} disabled={creating}>
            {creating && <Loader2 className="ml-2 h-4 w-4 animate-spin" />}
            {t("save")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
