"use client"

import { useMemo } from "react"
import { useTranslations } from "next-intl"
import { Copy } from "lucide-react"
import { toast } from "sonner"

import { CreateBranchResult } from "@/types/domain/domain.types"
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

interface BranchSetupDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  result: CreateBranchResult | null
  centralBaseUrl: string
}

export function BranchSetupDialog({ open, onOpenChange, result, centralBaseUrl }: BranchSetupDialogProps) {
  const t = useTranslations("BranchesAdmin")

  const setupCode = useMemo(() => {
    if (!result) return ""
    try {
      return btoa(
        JSON.stringify({
          centralBaseUrl,
          branchId: result.branch.id,
          terminalId: result.terminal?.id ?? "",
          branchKey: result.syncKey,
        })
      )
    } catch {
      return ""
    }
  }, [centralBaseUrl, result])

  const copy = async (value: string) => {
    if (!value) return
    try {
      await navigator.clipboard.writeText(value)
      toast.success(t("copied"))
    } catch {
      toast.error(t("copyFailed"))
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[560px]">
        <DialogHeader>
          <DialogTitle>{t("setupTitle")}</DialogTitle>
          <DialogDescription>{t("setupDescription")}</DialogDescription>
        </DialogHeader>

        <div className="space-y-4 py-2">
          <CopyRow label={t("setupCentralUrl")} value={centralBaseUrl} onCopy={copy} />
          <CopyRow label={t("setupBranchId")} value={result?.branch.id ?? ""} onCopy={copy} />
          <CopyRow
            label={t("setupTerminalId")}
            value={result?.terminal?.id ?? ""}
            fallback={t("setupNoTerminal")}
            onCopy={copy}
          />
          <CopyRow label={t("setupBranchKey")} value={result?.syncKey ?? ""} onCopy={copy} />
          <CopyRow label={t("setupCode")} value={setupCode} onCopy={copy} hint={t("setupCodeHint")} />
        </div>

        <DialogFooter>
          <Button onClick={() => onOpenChange(false)}>{t("done")}</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

function CopyRow({
  label,
  value,
  fallback,
  hint,
  onCopy,
}: {
  label: string
  value: string
  fallback?: string
  hint?: string
  onCopy: (value: string) => void
}) {
  const t = useTranslations("BranchesAdmin")
  const display = value || fallback || "—"

  return (
    <div className="space-y-1">
      <label className="text-sm font-medium">{label}</label>
      <div className="flex gap-2">
        <Input value={display} readOnly dir={value ? "ltr" : undefined} className="font-mono text-xs" />
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={() => onCopy(value)}
          disabled={!value}
          className="shrink-0"
        >
          <Copy className="me-1 h-4 w-4" />
          {t("copy")}
        </Button>
      </div>
      {hint && value ? <p className="text-xs text-muted-foreground">{hint}</p> : null}
    </div>
  )
}
