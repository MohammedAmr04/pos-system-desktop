"use client"

import { useTranslations } from "next-intl"
import { Loader2 } from "lucide-react"

import { Terminal } from "@/types/domain/domain.types"
import { useAllBranches } from "@/hooks/use-branches"
import { useTerminalHistory } from "@/hooks/use-terminals"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"

interface TerminalHistoryDialogProps {
  terminal: Terminal | null
  onOpenChange: (open: boolean) => void
}

function formatDate(value: string) {
  return new Date(value).toLocaleString(undefined, { dateStyle: "short", timeStyle: "short" })
}

export function TerminalHistoryDialog({ terminal, onOpenChange }: TerminalHistoryDialogProps) {
  const t = useTranslations("TerminalsAdmin")
  const branches = useAllBranches(false, !!terminal)
  const history = useTerminalHistory(terminal?.id ?? "", 50, !!terminal)

  const branchName = (id: string | null) =>
    (id && branches.data?.find((branch) => branch.id === id)?.name) || id || "—"

  return (
    <Dialog open={!!terminal} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[560px]">
        <DialogHeader>
          <DialogTitle>{t("historyTitle")}</DialogTitle>
          <DialogDescription>{t("historyHint", { terminal: terminal?.name ?? "" })}</DialogDescription>
        </DialogHeader>

        <div className="max-h-[60vh] overflow-y-auto py-2">
          {history.isPending ? (
            <div className="flex h-24 items-center justify-center">
              <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
            </div>
          ) : (history.data ?? []).length === 0 ? (
            <p className="py-6 text-center text-sm text-muted-foreground">{t("historyEmpty")}</p>
          ) : (
            <ul className="space-y-2">
              {(history.data ?? []).map((entry) => (
                <li key={entry.id} className="rounded-md border px-3 py-2 text-sm">
                  <div className="flex items-center justify-between gap-2">
                    <span className="font-medium">
                      {branchName(entry.fromBranchId)} → {branchName(entry.toBranchId)}
                    </span>
                    <span
                      className={
                        entry.outcome === "accepted"
                          ? "text-xs font-medium text-emerald-600"
                          : "text-xs font-medium text-destructive"
                      }
                    >
                      {entry.outcome}
                    </span>
                  </div>
                  <div className="mt-1 flex items-center justify-between gap-2 text-xs text-muted-foreground">
                    <span>{entry.changedBy || "—"}</span>
                    <span dir="ltr">{formatDate(entry.createdAt)}</span>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </div>
      </DialogContent>
    </Dialog>
  )
}
