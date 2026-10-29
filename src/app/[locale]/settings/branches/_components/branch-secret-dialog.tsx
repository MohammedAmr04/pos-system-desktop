"use client"

import { useState } from "react"
import { useTranslations } from "next-intl"
import { useApiError } from "@/lib/api-error"
import { toast } from "sonner"
import { Copy, Loader2 } from "lucide-react"

import { Branch } from "@/types/domain/domain.types"
import { resetBranchSecretAction, setBranchSecretAction } from "@/actions/branches.actions"
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
  const [generatedKey, setGeneratedKey] = useState("")
  const [isSaving, setIsSaving] = useState(false)
  const [isGenerating, setIsGenerating] = useState(false)

  const canSave = key.trim().length >= MIN_KEY_LENGTH && !isSaving && !isGenerating

  const handleGenerate = async () => {
    if (!branch || isGenerating) return
    setIsGenerating(true)
    try {
      const result = await resetBranchSecretAction(branch.id)
      setGeneratedKey(result.key)
      setKey("")
      toast.success(t("generated"))
    } catch (e) {
      toast.error(resolveError(e) || t("saveFailed"))
    } finally {
      setIsGenerating(false)
    }
  }

  const handleSave = async () => {
    if (!branch || !canSave) return
    setIsSaving(true)
    try {
      await setBranchSecretAction(branch.id, key.trim())
      toast.success(t("secretSaved"))
      setKey("")
      setGeneratedKey("")
      onOpenChange(false)
    } catch (e) {
      toast.error(resolveError(e) || t("saveFailed"))
    } finally {
      setIsSaving(false)
    }
  }

  const copyGenerated = async () => {
    if (!generatedKey) return
    try {
      await navigator.clipboard.writeText(generatedKey)
      toast.success(t("copied"))
    } catch {
      toast.error(t("copyFailed"))
    }
  }

  return (
    <Dialog open={!!branch} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[520px]">
        <DialogHeader>
          <DialogTitle>{t("setSecret")}</DialogTitle>
          <DialogDescription>{t("secretHint", { branch: branch?.name ?? "" })}</DialogDescription>
        </DialogHeader>

        <div className="space-y-4 py-4">
          <div className="rounded-md border p-3 space-y-3">
            <div>
              <p className="text-sm font-medium">{t("generateSecret")}</p>
              <p className="text-xs text-muted-foreground">{t("generateSecretHint")}</p>
            </div>
            <Button variant="outline" onClick={handleGenerate} disabled={isGenerating || isSaving}>
              {isGenerating ? <Loader2 className="me-2 h-4 w-4 animate-spin" /> : null}
              {isGenerating ? t("generating") : t("generateSecretAction")}
            </Button>
            {generatedKey && (
              <div className="space-y-2">
                <label className="text-sm font-medium">{t("generatedKey")}</label>
                <div className="flex gap-2">
                  <Input value={generatedKey} readOnly dir="ltr" className="font-mono text-xs" />
                  <Button type="button" variant="outline" size="sm" onClick={copyGenerated} className="shrink-0">
                    <Copy className="me-1 h-4 w-4" />
                    {t("copy")}
                  </Button>
                </div>
                <p className="text-xs text-destructive">{t("generatedKeyHint")}</p>
              </div>
            )}
          </div>

          <div className="space-y-2">
            <label className="text-sm font-medium">{t("secretKey")}</label>
            <Input
              value={key}
              onChange={(e) => setKey(e.target.value)}
              placeholder={t("secretKeyPlaceholder")}
              dir="ltr"
            />
            <p className="text-xs text-muted-foreground">{t("secretMinLength", { count: MIN_KEY_LENGTH })}</p>
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
