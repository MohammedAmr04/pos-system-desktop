"use client"

import { Product, PurchaseInvoice, api } from "@/lib/api"
import { useEffect, useState } from "react"
import { useTranslations } from "next-intl"
import { useSearchParams } from "next/navigation"
import { useRouter } from "@/i18n/navigation"
import { useAuth } from "@/components/common/auth-context"
import { PERMISSIONS } from "@/lib/constants"
import { Button } from "@/components/ui/button"
import { Loader2, Save, Upload } from "lucide-react"
import { toast } from "sonner"
import { PurchaseHeaderFields } from "./_components/purchase-header-fields"
import { LinesEditor } from "./_components/lines-editor"

export interface EditorLine {
  key: string
  productId: string
  productUnitId: string
  quantity: string
  unitCost: string
  newRetailPrice: string
  newWholesalePrice: string
}

const makeKey = () => Math.random().toString(36).slice(2)

const emptyLine = (): EditorLine => ({
  key: makeKey(),
  productId: "",
  productUnitId: "",
  quantity: "1",
  unitCost: "",
  newRetailPrice: "",
  newWholesalePrice: "",
})

const toNum = (value: string): number | null => {
  const trimmed = value.trim()
  if (!trimmed) return null
  const parsed = Number(trimmed)
  return Number.isFinite(parsed) ? parsed : null
}

export function EditorClient() {
  const t = useTranslations("Purchases")
  const params = useSearchParams()
  const router = useRouter()
  const { hasPermission } = useAuth()
  const editId = params.get("id")

  const [products, setProducts] = useState<Product[]>([])
  const [invoiceNumber, setInvoiceNumber] = useState<number | null>(null)
  const [status, setStatus] = useState<"draft" | "posted" | "cancelled">("draft")
  const [supplierId, setSupplierId] = useState("none")
  const [supplierInvoiceNumber, setSupplierInvoiceNumber] = useState("")
  const [date, setDate] = useState(() => new Date().toISOString().slice(0, 10))
  const [paymentMethod, setPaymentMethod] = useState<"cash" | "credit">("cash")
  const [discount, setDiscount] = useState("0")
  const [tax, setTax] = useState("0")
  const [notes, setNotes] = useState("")
  const [lines, setLines] = useState<EditorLine[]>([emptyLine()])
  const [loading, setLoading] = useState(!!params.get("id"))
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    let cancelled = false
    api.products.list().then((list) => {
      if (!cancelled) setProducts(list)
    }).catch(() => {
      if (!cancelled) setProducts([])
    })

    const editId = params.get("id")
    if (!editId) {
      return () => {
        cancelled = true
      }
    }

    api.purchases.get(editId).then((inv: PurchaseInvoice) => {
      if (cancelled) return
      setInvoiceNumber(inv.invoiceNumber)
      setStatus(inv.status)
      setSupplierId(inv.supplierId ?? "none")
      setSupplierInvoiceNumber(inv.supplierInvoiceNumber ?? "")
      setDate(inv.date.slice(0, 10))
      setPaymentMethod(inv.paymentMethod === "credit" ? "credit" : "cash")
      setDiscount(String(inv.discount))
      setTax(String(inv.tax))
      setNotes(inv.notes ?? "")
      setLines(
        (inv.items ?? []).map((item) => ({
          key: makeKey(),
          productId: item.productId,
          productUnitId: item.productUnitId,
          quantity: String(item.quantity),
          unitCost: String(item.unitCost),
          newRetailPrice: item.newRetailPrice != null ? String(item.newRetailPrice) : "",
          newWholesalePrice: item.newWholesalePrice != null ? String(item.newWholesalePrice) : "",
        }))
      )
      setLoading(false)
    }).catch(() => {
      if (cancelled) return
      toast.error(t("saveFailed"))
      router.push("/purchases")
    })

    return () => {
      cancelled = true
    }
  }, [params, router, t])

  const readOnly = status === "cancelled"
  const canSaveNew = !editId && hasPermission(PERMISSIONS.PURCHASES_CREATE)
  const canEditExisting = !!editId && hasPermission(PERMISSIONS.PURCHASES_UPDATE)
  const canModify = !readOnly && (canSaveNew || canEditExisting)

  const subtotal = lines.reduce((sum, line) => sum + (toNum(line.quantity) ?? 0) * (toNum(line.unitCost) ?? 0), 0)
  const total = subtotal - (toNum(discount) ?? 0) + (toNum(tax) ?? 0)

  const buildRequest = (targetStatus: "draft" | "posted") => ({
    supplierId: supplierId === "none" ? null : supplierId,
    supplierInvoiceNumber: supplierInvoiceNumber.trim() || null,
    date,
    paymentMethod,
    status: targetStatus,
    discount: toNum(discount) ?? 0,
    tax: toNum(tax) ?? 0,
    notes: notes.trim() || null,
    lines: lines
      .filter((line) => line.productId && line.productUnitId)
      .map((line) => ({
        productId: line.productId,
        productUnitId: line.productUnitId,
        quantity: toNum(line.quantity) ?? 0,
        unitCost: toNum(line.unitCost) ?? 0,
        newRetailPrice: toNum(line.newRetailPrice),
        newWholesalePrice: toNum(line.newWholesalePrice),
      })),
  })

  const handleSave = async (targetStatus: "draft" | "posted") => {
    if (!buildRequest(targetStatus).lines.length) {
      toast.error(t("addLine"))
      return
    }
    if (paymentMethod === "credit" && supplierId === "none") {
      toast.error(t("creditRequiresSupplier"))
      return
    }
    setSaving(true)
    try {
      if (editId) {
        await api.purchases.update(editId, buildRequest(targetStatus))
      } else {
        await api.purchases.create(buildRequest(targetStatus))
      }
      toast.success(targetStatus === "posted" ? t("posted") : t("updated"))
      router.push("/purchases")    } catch (e) {
      toast.error((e as Error).message || t("saveFailed"))
    } finally {
      setSaving(false)
    }
  }

  const addLine = () => setLines((prev) => [...prev, emptyLine()])
  const removeLine = (key: string) =>
    setLines((prev) => (prev.length > 1 ? prev.filter((l) => l.key !== key) : prev.map((l) => (l.key === key ? emptyLine() : l))))
  const updateLine = (key: string, patch: Partial<EditorLine>) =>
    setLines((prev) => prev.map((l) => (l.key === key ? { ...l, ...patch } : l)))

  if (loading) {
    return <Loader2 className="mx-auto mt-10 h-6 w-6 animate-spin" />
  }

  return (
    <div className="flex-1 space-y-4 pt-6">
      <div className="flex items-center justify-between">
        <h2 className="text-3xl font-bold tracking-tight">
          {editId ? `${t("editPurchase")}${invoiceNumber ? ` #${invoiceNumber}` : ""}` : t("newPurchase")}
        </h2>
        {status !== "draft" && (
          <span className={`rounded-full px-3 py-1 text-xs font-medium ${status === "posted" ? "bg-emerald-500/10 text-emerald-600" : "bg-destructive/10 text-destructive"}`}>
            {status === "posted" ? t("posted") : t("cancelled")}
          </span>
        )}
      </div>

      {readOnly && (
        <p className="rounded-lg border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm text-destructive">
          {t("cancelledReadOnly")}
        </p>
      )}
      {!readOnly && editId && status === "posted" && (
        <p className="rounded-lg border border-amber-500/30 bg-amber-500/5 px-4 py-3 text-sm text-amber-600">
          {t("postedEditHint")}
        </p>
      )}

      <PurchaseHeaderFields
        disabled={!canModify}
        supplierId={supplierId}
        onSupplierChange={setSupplierId}
        supplierInvoiceNumber={supplierInvoiceNumber}
        onSupplierInvoiceNumberChange={setSupplierInvoiceNumber}
        date={date}
        onDateChange={setDate}
        paymentMethod={paymentMethod}
        onPaymentMethodChange={setPaymentMethod}
        discount={discount}
        onDiscountChange={setDiscount}
        tax={tax}
        onTaxChange={setTax}
        notes={notes}
        onNotesChange={setNotes}
      />

      <LinesEditor
        products={products}
        lines={lines}
        disabled={!canModify}
        onAdd={addLine}
        onRemove={removeLine}
        onUpdate={updateLine}
      />

      <div className="flex items-center justify-between rounded-lg border bg-card px-4 py-3">
        <div className="space-y-1 text-sm">
          <div className="flex gap-8">
            <span className="text-muted-foreground">{`${t("subtotal")}: ${subtotal.toFixed(2)}`}</span>
            <span className="text-muted-foreground">{`${t("total")}: `}<span dir="ltr" className="font-bold">{total.toFixed(2)}</span></span>
          </div>
        </div>
        {canModify && (
          <div className="flex items-center gap-2">
            <Button variant="outline" disabled={saving} onClick={() => handleSave("draft")}>
              {saving ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <Save className="mr-2 h-4 w-4" />}
              {editId && status === "posted" ? t("saveChanges") : t("saveDraft")}
            </Button>
            {(!editId || status === "draft") && (
              <Button disabled={saving} onClick={() => handleSave("posted")}>
                {saving ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <Upload className="mr-2 h-4 w-4" />}
                {editId ? t("post") : t("saveAndPost")}
              </Button>
            )}
          </div>
        )}
      </div>
    </div>
  )
}
