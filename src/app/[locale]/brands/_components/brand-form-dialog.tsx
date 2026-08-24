"use client"

import { Brand, api } from "@/lib/api"
import { useState } from "react"
import { useTranslations } from "next-intl"
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

interface BrandFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  brand: Brand | null
  onSaved: () => void
}

interface BrandFormState {
  name: string
  isActive: boolean
}

export function BrandFormDialog({ open, onOpenChange, brand, onSaved }: BrandFormDialogProps) {
  const t = useTranslations("Brands")
  const [form, setForm] = useState<BrandFormState>(() => ({
    name: brand?.name ?? "",
    isActive: brand?.isActive ?? true,
  }))
  const [isSaving, setIsSaving] = useState(false)

  const handleSave = async () => {
    const name = form.name.trim()
    if (!name) return
    setIsSaving(true)
    try {
      if (brand) {
        await api.brands.update(brand.id, { name, isActive: form.isActive })
        toast.success(t("updated"))
      } else {
        await api.brands.create({ name })
        toast.success(t("created"))
      }
      onOpenChange(false)
      onSaved()
    } catch (e) {
      toast.error((e as Error).message || t("saveFailed"))
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[480px]">
        <DialogHeader>
          <DialogTitle>{brand ? t("editBrand") : t("newBrand")}</DialogTitle>
          <DialogDescription>{brand ? t("editHint") : t("createHint")}</DialogDescription>
        </DialogHeader>
        <div className="py-4 space-y-4">
          <div className="space-y-2">
            <label className="text-sm font-medium">{t("name")} *</label>
            <Input
              value={form.name}
              onChange={(e) => setForm({ ...form, name: e.target.value })}
              placeholder={t("namePlaceholder")}
              autoFocus={!brand}
            />
          </div>
          {brand && (
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
