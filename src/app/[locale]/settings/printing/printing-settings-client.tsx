"use client"

import { useState } from "react"
import { useQueryClient } from "@tanstack/react-query"
import { useTranslations } from "next-intl"
import { toast } from "sonner"
import { Loader2, Save } from "lucide-react"
import { PrinterSettings } from "@/types/domain/domain.types"
import { savePrinterSettings } from "@/actions/settings.actions"
import { printerSettingsKeys, usePrinterSettings } from "@/hooks/use-printer-settings"
import { useAuth } from "@/components/common/auth-context"
import { PERMISSIONS } from "@/lib/constants"
import { AccessDenied } from "@/components/common/access-denied"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Switch } from "@/components/ui/switch"

export function PrintingSettingsClient() {
  const t = useTranslations("Printing")
  const { hasPermission } = useAuth()
  const canView = hasPermission(PERMISSIONS.SETTINGS_VIEW)
  const canUpdate = hasPermission(PERMISSIONS.SETTINGS_UPDATE)

  const { data, isPending: loading, isError } = usePrinterSettings()

  if (!canView) return <AccessDenied />
  if (loading) {
    return (
      <div className="flex h-40 items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    )
  }
  if (!data || isError) {
    return (
      <div className="flex h-40 items-center justify-center">
        <p className="text-sm text-muted-foreground">{t("loadFailed")}</p>
      </div>
    )
  }

  return <PrintingSettingsForm initial={data} canUpdate={canUpdate} />
}

function PrintingSettingsForm({
  initial,
  canUpdate,
}: {
  initial: PrinterSettings
  canUpdate: boolean
}) {
  const t = useTranslations("Printing")
  const queryClient = useQueryClient()

  const [saving, setSaving] = useState(false)
  const [form, setForm] = useState<PrinterSettings>(initial)

  const set = <K extends keyof PrinterSettings>(key: K, value: PrinterSettings[K]) =>
    setForm((f) => (f ? { ...f, [key]: value } : f))

  const handleSave = async () => {
    setSaving(true)
    try {
      const saved = await savePrinterSettings(form)
      setForm(saved)
      await queryClient.invalidateQueries({ queryKey: printerSettingsKeys.current() })
      toast.success(t("saved"))
    } catch (e) {
      toast.error((e as Error).message || t("saveFailed"))
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="flex-1 space-y-4 pt-6 max-w-3xl">
      <h2 className="text-3xl font-bold tracking-tight">{t("title")}</h2>

      {/* Printers */}
      <section className="rounded-md border bg-card p-4 space-y-4">
        <h3 className="font-semibold">{t("printers")}</h3>
        <div className="grid gap-4 sm:grid-cols-2">
          <div className="space-y-2">
            <label className="text-sm font-medium">{t("receiptPrinter")}</label>
            <Input
              value={form.receiptPrinterName}
              onChange={(e) => set("receiptPrinterName", e.target.value)}
              disabled={!canUpdate}
            />
          </div>
          <div className="space-y-2">
            <label className="text-sm font-medium">{t("labelPrinter")}</label>
            <Input
              value={form.labelPrinterName}
              onChange={(e) => set("labelPrinterName", e.target.value)}
              disabled={!canUpdate}
            />
          </div>
        </div>
        <div className="grid gap-4 sm:grid-cols-2">
          <div className="space-y-2">
            <label className="text-sm font-medium">{t("paperWidth")}</label>
            <Select
              value={String(form.paperWidthMm)}
              onValueChange={(v) => {
                if (v == null) return
                set("paperWidthMm", Number(v) === 58 ? 58 : 80)
              }}
              disabled={!canUpdate}
            >
              <SelectTrigger className="w-full">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="58">58 {t("mm")}</SelectItem>
                <SelectItem value="80">80 {t("mm")}</SelectItem>
              </SelectContent>
            </Select>
          </div>
          <div className="space-y-2">
            <label className="text-sm font-medium">{t("copies")}</label>
            <Input
              type="number"
              min={1}
              max={5}
              value={form.copies}
              onChange={(e) => set("copies", Number(e.target.value) || 1)}
              disabled={!canUpdate}
            />
          </div>
        </div>
        <div className="grid gap-3 sm:grid-cols-3">
          <ToggleRow
            label={t("autoCut")}
            checked={form.autoCut}
            disabled={!canUpdate}
            onCheckedChange={(v) => set("autoCut", v)}
          />
          <ToggleRow
            label={t("cashDrawer")}
            checked={form.openCashDrawer}
            disabled={!canUpdate}
            onCheckedChange={(v) => set("openCashDrawer", v)}
          />
          <ToggleRow
            label={t("showLogo")}
            checked={form.showLogo}
            disabled={!canUpdate}
            onCheckedChange={(v) => set("showLogo", v)}
          />
        </div>
      </section>

      {/* Store info */}
      <section className="rounded-md border bg-card p-4 space-y-4">
        <h3 className="font-semibold">{t("storeInfo")}</h3>
        <div className="space-y-2">
          <label className="text-sm font-medium">{t("storeName")}</label>
          <Input
            value={form.storeName ?? ""}
            onChange={(e) => set("storeName", e.target.value)}
            disabled={!canUpdate}
          />
        </div>
        <div className="grid gap-4 sm:grid-cols-2">
          <div className="space-y-2">
            <label className="text-sm font-medium">{t("storePhone")}</label>
            <Input
              value={form.storePhone ?? ""}
              onChange={(e) => set("storePhone", e.target.value)}
              disabled={!canUpdate}
            />
          </div>
          <div className="space-y-2">
            <label className="text-sm font-medium">{t("storeAddress")}</label>
            <Input
              value={form.storeAddress ?? ""}
              onChange={(e) => set("storeAddress", e.target.value)}
              disabled={!canUpdate}
            />
          </div>
        </div>
      </section>

      {/* Receipt texts */}
      <section className="rounded-md border bg-card p-4 space-y-4">
        <h3 className="font-semibold">{t("receiptTexts")}</h3>
        <div className="space-y-2">
          <label className="text-sm font-medium">{t("receiptHeader")}</label>
          <Input
            value={form.receiptHeader ?? ""}
            onChange={(e) => set("receiptHeader", e.target.value)}
            placeholder={t("receiptHeaderPlaceholder")}
            disabled={!canUpdate}
          />
        </div>
        <div className="space-y-2">
          <label className="text-sm font-medium">{t("receiptFooter")}</label>
          <Input
            value={form.receiptFooter ?? ""}
            onChange={(e) => set("receiptFooter", e.target.value)}
            disabled={!canUpdate}
          />
        </div>
      </section>

      {canUpdate && (
        <Button onClick={handleSave} disabled={saving}>
          {saving ? <Loader2 className="ml-2 h-4 w-4 animate-spin" /> : <Save className="ml-2 h-4 w-4" />}
          {t("save")}
        </Button>
      )}
    </div>
  )
}

function ToggleRow({
  label,
  checked,
  disabled,
  onCheckedChange,
}: {
  label: string
  checked: boolean
  disabled?: boolean
  onCheckedChange: (v: boolean) => void
}) {
  return (
    <div className="flex items-center justify-between rounded-md border px-3 py-2.5">
      <span className="text-sm font-medium">{label}</span>
      <Switch checked={checked} disabled={disabled} onCheckedChange={onCheckedChange} />
    </div>
  )
}
