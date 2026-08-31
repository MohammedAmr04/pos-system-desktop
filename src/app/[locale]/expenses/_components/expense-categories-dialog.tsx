"use client"

import { useState } from "react"
import { useTranslations } from "next-intl"
import { useApiError } from "@/lib/api-error"
import { toast } from "sonner"
import { Plus } from "lucide-react"
import { ExpenseCategory } from "@/types/domain/domain.types"
import { createExpenseCategory, updateExpenseCategory } from "@/actions/expenses.actions"
import { useExpenseCategories } from "@/hooks/use-expenses"
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
import { Switch } from "@/components/ui/switch"

interface ExpenseCategoriesDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function ExpenseCategoriesDialog({ open, onOpenChange }: ExpenseCategoriesDialogProps) {
  const t = useTranslations("Expenses")
  const tc = useTranslations("Common")
  const resolveError = useApiError()
  const { data: categories = [] } = useExpenseCategories(true)
  const [newCatName, setNewCatName] = useState("")
  const [renamingId, setRenamingId] = useState<string | null>(null)
  const [renameValue, setRenameValue] = useState("")

  const handleAddCategory = async () => {
    const name = newCatName.trim()
    if (!name) return
    try {
      await createExpenseCategory(name)
      setNewCatName("")
    } catch (e) {
      toast.error(resolveError(e) || t("categorySaveFailed"))
    }
  }

  const handleRename = async (cat: ExpenseCategory) => {
    const name = renameValue.trim()
    setRenamingId(null)
    if (!name || name === cat.name) return
    try {
      await updateExpenseCategory(cat.id, { name, isActive: cat.isActive })
    } catch (e) {
      toast.error(resolveError(e) || t("categorySaveFailed"))
    }
  }

  const handleToggle = async (cat: ExpenseCategory, isActive: boolean) => {
    try {
      await updateExpenseCategory(cat.id, { name: cat.name, isActive })
    } catch (e) {
      toast.error(resolveError(e) || t("categorySaveFailed"))
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{t("categoriesTitle")}</DialogTitle>
          <DialogDescription>{t("categoriesHint")}</DialogDescription>
        </DialogHeader>
        <div className="max-h-72 space-y-2 overflow-y-auto">
          {categories.map((c) => (
            <div key={c.id} className="flex items-center gap-2 rounded-md border px-3 py-2">
              {renamingId === c.id ? (
                <>
                  <Input
                    value={renameValue}
                    onChange={(e) => setRenameValue(e.target.value)}
                    onKeyDown={(e) => e.key === "Enter" && handleRename(c)}
                    autoFocus
                  />
                  <Button size="sm" variant="outline" onClick={() => handleRename(c)}>
                    ✓
                  </Button>
                </>
              ) : (
                <>
                  <button
                    type="button"
                    className={`flex-1 truncate text-right text-sm ${c.isActive ? "" : "text-muted-foreground line-through"}`}
                    onClick={() => {
                      setRenamingId(c.id)
                      setRenameValue(c.name)
                    }}
                  >
                    {c.name}
                  </button>
                  <Switch checked={c.isActive} onCheckedChange={(v) => handleToggle(c, v)} />
                </>
              )}
            </div>
          ))}
        </div>
        <div className="flex gap-2">
          <Input
            value={newCatName}
            onChange={(e) => setNewCatName(e.target.value)}
            onKeyDown={(e) => e.key === "Enter" && handleAddCategory()}
            placeholder={t("newCategoryPlaceholder")}
          />
          <Button variant="outline" onClick={handleAddCategory}>
            <Plus className="ml-1 h-4 w-4" />
            {t("add")}
          </Button>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            {tc("cancel")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
