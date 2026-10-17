"use client"

import { useState } from "react"
import { useTranslations } from "next-intl"
import { useApiError } from "@/lib/api-error"
import { toast } from "sonner"
import { Loader2 } from "lucide-react"

import { Branch } from "@/types/domain/domain.types"
import { setBranchSecretAction } from "@/actions/branches.actions"
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

const MIN_KEY_LENGTH = 16

interface BranchSecretDialogProps {
  branch: Branch | null
  onOpenChange: (open: boolean) => void
}

export function BranchSecretDialog({ branch, onOpenChange }: BranchSecretDialogProps) {
  const t = useTranslations("BranchesAdmin")
  const resolveError = useApiError()
  const [key, setKey] = useState("")
  const [isSaving, setIsSaving] = useState(false)

  const canSave = key.trim().length >= MIN_KEY_LENGTH && !isSaving

  const handleSave = async () => {
    if (!branch || !canSave) return
    setIsSaving(true)
    try {
      await setBranchSecretAction(branch.id, key.trim())
      toast.success(t("secretSaved"))
      setKey("")
      onOpenChange(false)
    } catch (e) {
      toast.error(resolveError(e) || t("saveFailed"))
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <Dialog open={!!branch} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[480px]">
        <DialogHeader>
          <DialogTitle>{t("setSecret")}</DialogTitle>
          <DialogDescription>{t("secretHint", { branch: branch?.name ?? "" })}</DialogDescription>
        </DialogHeader>
        <div className="py-4 space-y-2">
          <label className="text-sm font-medium">{t("secretKey")}</label>
          <Input
            value={key}
            onChange={(e) => setKey(e.target.value)}
            placeholder={t("secretKeyPlaceholder")}
            dir="ltr"
            autoFocus
          />
          <p className="text-xs text-muted-foreground">{t("secretMinLength", { count: MIN_KEY_LENGTH })}</p>
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
