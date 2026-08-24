"use client"

import { useEffect, useState } from "react"
import { useTranslations } from "next-intl"
import { toast } from "sonner"
import { Loader2, Save } from "lucide-react"
import { api, type PrinterSettings as PrinterSettingsData } from "@/lib/api"
import { useAuth } from "@/components/common/auth-context"
import { PERMISSIONS } from "@/lib/constants"
import { AccessDenied } from "@/components/common/access-denied"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Switch } from "@/components/ui/switch"

const selectClass =
  "flex h-9 w-full min-w-0 rounded-lg border border-input bg-transparent px-2.5 py-1 text-base transition-colors outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 md:text-sm dark:bg-input/30"

export function PrintingSettingsClient() {
  const t = useTranslations("Printing")
  const { hasPermission } = useAuth()
  const canView = hasPermission(PERMISSIONS.SETTINGS_VIEW)
  const canUpdate = hasPermission(PERMISSIONS.SETTINGS_UPDATE)

  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [form, setForm] = useState<PrinterSettingsData | null>(null)

  useEffect(() => {
    if (!canView) return
    let cancelled = false
    api.printerSettings
      .get()
      .then((data) => {
        if (!cancelled) setForm(data)
      })
      .catch(() => {
        if (!cancelled) toast.error(t("loadFailed"))
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [canView])

  if (!canView) return <AccessDenied />
  if (loading || !form) {
    return (
      <div className="flex h-40 items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  const set = <K extends keyof PrinterSettingsData>(key: K, value: PrinterSettingsData[K]) =>
    setForm((f) => (f ? { ...f, [key]: value } : f))

  const handleSave = async () => {
    setSaving(true)
    try {
      const saved = await api.printerSettings.save(form)
      setForm(saved)
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
            <select
              className={selectClass}
              value={String(form.paperWidthMm)}
              onChange={(e) => set("paperWidthMm", Number(e.target.value) === 58 ? 58 : 80)}
              disabled={!canUpdate}
            >
              <option value="58">58 {t("mm")}</option>
              <option value="80">80 {t("mm")}</option>
            </select>
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
