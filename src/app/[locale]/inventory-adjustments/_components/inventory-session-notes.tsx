"use client"

import { useState } from "react"
import { useTranslations } from "next-intl"
import { toast } from "sonner"
import { updateInventoryCountNotes } from "@/actions/operations.actions"
import { Button } from "@/components/ui/button"

export function InventorySessionNotes({ id, defaultNotes, editable }: { id: string; defaultNotes: string | null; editable: boolean }) {
  const t = useTranslations("InventoryCounts")
  const [notes, setNotes] = useState(defaultNotes ?? "")
  const [saving, setSaving] = useState(false)

  const save = async () => {
    setSaving(true)
    try {
      await updateInventoryCountNotes(id, notes)
      toast.success(t("notesSaved"))
    } catch (error) {
      toast.error(error instanceof Error ? error.message : t("saveFailed"))
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="max-w-2xl space-y-2 rounded-xl border p-4">
      <label className="text-sm font-medium" htmlFor="inventory-notes">{t("notes")}</label>
      <textarea
        id="inventory-notes"
        value={notes}
        onChange={(event) => setNotes(event.target.value)}
        disabled={!editable || saving}
        placeholder={t("notesPlaceholder")}
        className="h-20 w-full min-w-0 resize-y rounded-lg border border-input bg-transparent px-2.5 py-1.5 text-base transition-colors outline-none placeholder:text-muted-foreground focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 disabled:pointer-events-none disabled:cursor-not-allowed disabled:bg-input/50 disabled:opacity-50 md:text-sm dark:bg-input/30"
      />
      {editable && <Button size="sm" disabled={saving} onClick={() => void save()}>{saving ? t("saving") : t("saveNotes")}</Button>}
    </div>
  )
}
