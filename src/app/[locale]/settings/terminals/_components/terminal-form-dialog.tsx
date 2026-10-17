"use client"

import { useState } from "react"
import { useTranslations } from "next-intl"
import { useApiError } from "@/lib/api-error"
import { toast } from "sonner"
import { Loader2 } from "lucide-react"

import { Terminal } from "@/types/domain/domain.types"
import { useAllBranches } from "@/hooks/use-branches"
import { createTerminalAction, updateTerminalAction } from "@/actions/terminals.actions"
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

interface TerminalFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  terminal: Terminal | null
  defaultBranchId?: string
}

export function TerminalFormDialog({ open, onOpenChange, terminal, defaultBranchId }: TerminalFormDialogProps) {
  const t = useTranslations("TerminalsAdmin")
  const resolveError = useApiError()
  const branches = useAllBranches(false, open)

  const [branchId, setBranchId] = useState(terminal?.branchId ?? defaultBranchId ?? "")
  const [name, setName] = useState(terminal?.name ?? "")
  const [notes, setNotes] = useState(terminal?.notes ?? "")
  const [isSaving, setIsSaving] = useState(false)

  const canSave = name.trim().length > 0 && branchId.length > 0 && !isSaving

  const handleSave = async () => {
    if (!canSave) return
    setIsSaving(true)
    try {
      if (terminal) {
        await updateTerminalAction(terminal.id, {
          name: name.trim(),
          notes: notes.trim() || null,
          branchId,
        })
        toast.success(t("updated"))
      } else {
        await createTerminalAction({ branchId, name: name.trim(), notes: notes.trim() || null })
        toast.success(t("created"))
      }
      onOpenChange(false)
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
          <DialogTitle>{terminal ? t("editTerminal") : t("newTerminal")}</DialogTitle>
          <DialogDescription>{terminal ? t("editHint") : t("createHint")}</DialogDescription>
        </DialogHeader>
        <div className="py-4 space-y-4">
          <div className="space-y-2">
            <label className="text-sm font-medium">{t("branch")} *</label>
            <Select value={branchId} onValueChange={(value) => setBranchId(value ?? "")}>
              <SelectTrigger className="w-full">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {(branches.data ?? []).map((branch) => (
                  <SelectItem key={branch.id} value={branch.id}>
                    {branch.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="space-y-2">
            <label className="text-sm font-medium">{t("name")} *</label>
            <Input value={name} onChange={(e) => setName(e.target.value)} placeholder={t("namePlaceholder")} autoFocus />
          </div>
          <div className="space-y-2">
            <label className="text-sm font-medium">{t("notes")}</label>
            <Input value={notes} onChange={(e) => setNotes(e.target.value)} placeholder={t("notesPlaceholder")} />
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
