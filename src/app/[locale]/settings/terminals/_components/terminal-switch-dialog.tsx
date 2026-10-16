"use client"

import { useState } from "react"
import { useTranslations } from "next-intl"
import { useApiError } from "@/lib/api-error"
import { toast } from "sonner"
import { Loader2, TriangleAlert } from "lucide-react"

import { Terminal } from "@/types/domain/domain.types"
import { useAllBranches } from "@/hooks/use-branches"
import { useSyncStatus } from "@/hooks/use-sync"
import { requestBranchSwitchAction } from "@/actions/branches.actions"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"

interface TerminalSwitchDialogProps {
  terminal: Terminal | null
  onOpenChange: (open: boolean) => void
}

function newRequestId() {
  if (typeof crypto !== "undefined" && "randomUUID" in crypto) return crypto.randomUUID()
  return `switch-${Date.now()}-${Math.random().toString(16).slice(2)}`
}

export function TerminalSwitchDialog({ terminal, onOpenChange }: TerminalSwitchDialogProps) {
  const t = useTranslations("TerminalsAdmin")
  const resolveError = useApiError()
  const branches = useAllBranches(false, !!terminal)
  const sync = useSyncStatus()

  const [targetBranchId, setTargetBranchId] = useState("")
  const [targetKey, setTargetKey] = useState("")
  const [isSubmitting, setIsSubmitting] = useState(false)

  const unresolved = (sync.data?.pending ?? 0) + (sync.data?.failed ?? 0) + (sync.data?.dead ?? 0)
  const blocked = unresolved > 0
  const canSubmit = !!terminal && targetBranchId.length > 0 && targetBranchId !== terminal.branchId && !blocked && !isSubmitting

  const handleSwitch = async () => {
    if (!terminal || !canSubmit) return
    setIsSubmitting(true)
    try {
      const result = await requestBranchSwitchAction({
        requestId: newRequestId(),
        terminalId: terminal.id,
        targetBranchId,
        targetBranchKey: targetKey.trim() || null,
      })
      toast.success(t("switchAccepted", { branch: result.branchName ?? result.branchId }))
      setTargetBranchId("")
      setTargetKey("")
      onOpenChange(false)
    } catch (e) {
      toast.error(resolveError(e) || t("switchFailed"))
    } finally {
      setIsSubmitting(false)
    }
  }

  const currentBranchName = branches.data?.find((branch) => branch.id === terminal?.branchId)?.name ?? terminal?.branchId

  return (
    <Dialog open={!!terminal} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[520px]">
        <DialogHeader>
          <DialogTitle>{t("switchBranch")}</DialogTitle>
          <DialogDescription>{t("switchHint", { terminal: terminal?.name ?? "" })}</DialogDescription>
        </DialogHeader>

        <div className="py-4 space-y-4">
          <div className="rounded-md border bg-muted/30 px-3 py-2 text-sm">
            <span className="text-muted-foreground">{t("currentBranch")}: </span>
            <span className="font-medium">{currentBranchName}</span>
          </div>

          {blocked && (
            <div className="flex items-start gap-2 rounded-md border border-destructive/30 bg-destructive/5 px-3 py-2 text-sm text-destructive">
              <TriangleAlert className="mt-0.5 h-4 w-4 shrink-0" />
              <span>{t("switchBlocked", { count: unresolved })}</span>
            </div>
          )}

          <div className="space-y-2">
            <label className="text-sm font-medium">{t("targetBranch")} *</label>
            <Select value={targetBranchId} onValueChange={(value) => setTargetBranchId(value ?? "")}>
              <SelectTrigger className="w-full">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {(branches.data ?? [])
                  .filter((branch) => branch.id !== terminal?.branchId)
                  .map((branch) => (
                    <SelectItem key={branch.id} value={branch.id}>
                      {branch.name}
                    </SelectItem>
                  ))}
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-2">
            <label className="text-sm font-medium">{t("targetKey")}</label>
            <Input
              value={targetKey}
              onChange={(e) => setTargetKey(e.target.value)}
              placeholder={t("targetKeyPlaceholder")}
              dir="ltr"
            />
            <p className="text-xs text-muted-foreground">{t("targetKeyHint")}</p>
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            {t("cancel")}
          </Button>
          <Button onClick={handleSwitch} disabled={!canSubmit}>
            {isSubmitting ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : null}
            {t("confirmSwitch")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
