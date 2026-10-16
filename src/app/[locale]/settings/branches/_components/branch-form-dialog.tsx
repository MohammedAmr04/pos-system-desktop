"use client"

import { useState } from "react"
import { useTranslations } from "next-intl"
import { useApiError } from "@/lib/api-error"
import { toast } from "sonner"
import { Loader2 } from "lucide-react"

import { Branch } from "@/types/domain/domain.types"
import { createBranchAction, updateBranchAction } from "@/actions/branches.actions"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"

interface BranchFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  branch: Branch | null
  onSaved?: () => void
}

export function BranchFormDialog({ open, onOpenChange, branch, onSaved }: BranchFormDialogProps) {
  const t = useTranslations("BranchesAdmin")
  const resolveError = useApiError()
  const [code, setCode] = useState(branch?.code ?? "")
  const [name, setName] = useState(branch?.name ?? "")
  const [address, setAddress] = useState(branch?.address ?? "")
  const [isSaving, setIsSaving] = useState(false)

  const canSave = name.trim().length > 0 && code.trim().length > 0 && !isSaving

  const handleSave = async () => {
    if (!canSave) return
    setIsSaving(true)
    try {
      const payload = { code: code.trim(), name: name.trim(), address: address.trim() || null }
      if (branch) {
        await updateBranchAction(branch.id, payload)
        toast.success(t("updated"))
      } else {
        await createBranchAction(payload)
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
          <DialogTitle>{branch ? t("editBranch") : t("newBranch")}</DialogTitle>
          <DialogDescription>{branch ? t("editHint") : t("createHint")}</DialogDescription>
        </DialogHeader>
        <div className="py-4 space-y-4">
          <div className="space-y-2">
            <label className="text-sm font-medium">{t("name")} *</label>
            <Input value={name} onChange={(e) => setName(e.target.value)} placeholder={t("namePlaceholder")} autoFocus />
          </div>
          <div className="space-y-2">
            <label className="text-sm font-medium">{t("code")} *</label>
            <Input
              value={code}
              onChange={(e) => setCode(e.target.value)}
              placeholder={t("codePlaceholder")}
              dir="ltr"
            />
          </div>
          <div className="space-y-2">
            <label className="text-sm font-medium">{t("address")}</label>
            <Input value={address} onChange={(e) => setAddress(e.target.value)} placeholder={t("addressPlaceholder")} />
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            {t("cancel")}
          </Button>
          <Button onClick={handleSave} disabled={!canSave}>
            {isSaving ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : null}
            {t("save")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
