"use client"

import { Category } from "@/types/domain/domain.types"
import { createCategory, updateCategory } from "@/actions/categories.actions"
import { useState } from "react"
import { useTranslations } from "next-intl"
import { useApiError } from "@/lib/api-error"
import { toast } from "sonner"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Switch } from "@/components/ui/switch"
import { Loader2 } from "lucide-react"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"

interface CategoryFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  category: Category | null
  onSaved?: () => void
}

interface CategoryFormState {
  name: string
  description: string
  isActive: boolean
}

export function CategoryFormDialog({ open, onOpenChange, category, onSaved }: CategoryFormDialogProps) {
  const t = useTranslations("Categories")
  const resolveError = useApiError()
  const [form, setForm] = useState<CategoryFormState>(() => ({
    name: category?.name ?? "",
    description: category?.description ?? "",
    isActive: category?.isActive ?? true,
  }))
  const [isSaving, setIsSaving] = useState(false)

  const handleSave = async () => {
    const name = form.name.trim()
    if (!name) return
    setIsSaving(true)
    try {
      if (category) {
        await updateCategory(category.id, {
          name,
          description: form.description.trim() || null,
          isActive: form.isActive,
        })
        toast.success(t("updated"))
      } else {
        await createCategory({ name, description: form.description.trim() || null })
        toast.success(t("created"))
      }
      onOpenChange(false)
      onSaved?.()
    } catch (e) {
      toast.error(resolveError(e) || t("saveFailed"))
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[480px]">
        <DialogHeader>
          <DialogTitle>{category ? t("editCategory") : t("newCategory")}</DialogTitle>
          <DialogDescription>{category ? t("editHint") : t("createHint")}</DialogDescription>
        </DialogHeader>
        <div className="py-4 space-y-4">
          <div className="space-y-2">
            <label className="text-sm font-medium">{t("name")} *</label>
            <Input
              value={form.name}
              onChange={(e) => setForm({ ...form, name: e.target.value })}
              placeholder={t("namePlaceholder")}
              autoFocus={!category}
            />
          </div>
          <div className="space-y-2">
            <label className="text-sm font-medium">{t("desc")}</label>
            <Input
              value={form.description}
              onChange={(e) => setForm({ ...form, description: e.target.value })}
              placeholder={t("descPlaceholder")}
            />
          </div>
          {category && (
            <div className="flex items-center justify-between rounded-lg border px-3 py-2">
              <span className="text-sm font-medium">{t("active")}</span>
              <Switch
                checked={form.isActive}
                onCheckedChange={(checked) => setForm({ ...form, isActive: checked })}
              />
            </div>
          )}
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            {t("cancel")}
          </Button>
          <Button onClick={handleSave} disabled={!form.name.trim() || isSaving}>
            {isSaving ? <Loader2 className="h-4 w-4 animate-spin" /> : null}
            {t("save")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
