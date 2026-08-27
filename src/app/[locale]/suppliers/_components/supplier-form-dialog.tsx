"use client"

import { Supplier } from "@/types/domain/domain.types"
import { createSupplier, updateSupplier } from "@/actions/suppliers.actions"
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

interface SupplierFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  supplier: Supplier | null
  onSaved?: () => void
}

interface SupplierFormState {
  name: string
  phone: string
  address: string
  notes: string
  isActive: boolean
}

export function SupplierFormDialog({ open, onOpenChange, supplier, onSaved }: SupplierFormDialogProps) {
  const t = useTranslations("Suppliers")
  const [form, setForm] = useState<SupplierFormState>(() => ({
    name: supplier?.name ?? "",
    phone: supplier?.phone ?? "",
    address: supplier?.address ?? "",
    notes: supplier?.notes ?? "",
    isActive: supplier?.isActive ?? true,
  }))
  const [isSaving, setIsSaving] = useState(false)

  const handleSave = async () => {
    const name = form.name.trim()
    if (!name) return
    setIsSaving(true)
    try {
      if (supplier) {
        await updateSupplier(supplier.id, {
          name,
          phone: form.phone.trim() || null,
          address: form.address.trim() || null,
          notes: form.notes.trim() || null,
          isActive: form.isActive,
        })
        toast.success(t("updated"))
      } else {
        await createSupplier({
          name,
          phone: form.phone.trim() || null,
          address: form.address.trim() || null,
          notes: form.notes.trim() || null,
        })
        toast.success(t("created"))
      }
      onOpenChange(false)
      onSaved?.()
    } catch (e) {
      toast.error((e as Error).message || t("saveFailed"))
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[520px]">
        <DialogHeader>
          <DialogTitle>{supplier ? t("editSupplier") : t("newSupplier")}</DialogTitle>
          <DialogDescription>{supplier ? t("editHint") : t("createHint")}</DialogDescription>
        </DialogHeader>
        <div className="py-4 space-y-4">
          <div className="space-y-2">
            <label className="text-sm font-medium">{t("name")} *</label>
            <Input
              value={form.name}
              onChange={(e) => setForm({ ...form, name: e.target.value })}
              placeholder={t("namePlaceholder")}
              autoFocus={!supplier}
            />
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <label className="text-sm font-medium">{t("phone")}</label>
              <Input
                value={form.phone}
                onChange={(e) => setForm({ ...form, phone: e.target.value })}
                placeholder={t("phonePlaceholder")}
                dir="ltr"
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t("address")}</label>
              <Input
                value={form.address}
                onChange={(e) => setForm({ ...form, address: e.target.value })}
                placeholder={t("addressPlaceholder")}
              />
            </div>
          </div>
          <div className="space-y-2">
            <label className="text-sm font-medium">{t("notes")}</label>
            <textarea
              value={form.notes}
              onChange={(e) => setForm({ ...form, notes: e.target.value })}
              className="h-20 w-full min-w-0 rounded-lg border border-input bg-transparent px-2.5 py-1.5 text-base transition-colors outline-none placeholder:text-muted-foreground focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 md:text-sm dark:bg-input/30 resize-y"
              placeholder={t("notesPlaceholder")}
            />
          </div>
          {supplier && (
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
