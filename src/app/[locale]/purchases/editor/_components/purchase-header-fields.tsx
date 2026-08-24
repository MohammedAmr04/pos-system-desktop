"use client"

import { Supplier, api } from "@/lib/api"
import { useEffect, useState } from "react"
import { useTranslations } from "next-intl"
import { Input } from "@/components/ui/input"

const selectClass =
  "flex h-9 w-full min-w-0 rounded-lg border border-input bg-transparent px-2.5 py-1 text-base transition-colors outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 md:text-sm dark:bg-input/30"

interface PurchaseHeaderFieldsProps {
  disabled: boolean
  supplierId: string
  onSupplierChange: (value: string) => void
  supplierInvoiceNumber: string
  onSupplierInvoiceNumberChange: (value: string) => void
  date: string
  onDateChange: (value: string) => void
  paymentMethod: "cash" | "credit"
  onPaymentMethodChange: (value: "cash" | "credit") => void
  discount: string
  onDiscountChange: (value: string) => void
  tax: string
  onTaxChange: (value: string) => void
  notes: string
  onNotesChange: (value: string) => void
}

export function PurchaseHeaderFields(props: PurchaseHeaderFieldsProps) {
  const t = useTranslations("Purchases")
  const [suppliers, setSuppliers] = useState<Supplier[]>([])

  useEffect(() => {
    let cancelled = false
    api.suppliers
      .list()
      .then((list) => {
        if (!cancelled) setSuppliers(list)
      })
      .catch(() => {
        if (!cancelled) setSuppliers([])
      })
    return () => {
      cancelled = true
    }
  }, [])

  return (
    <div className="rounded-lg border bg-card p-4 space-y-4">
      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-2">
          <label className="text-sm font-medium">{t("supplier")}</label>
          <select
            className={selectClass}
            value={props.supplierId}
            onChange={(e) => props.onSupplierChange(e.target.value)}
            disabled={props.disabled}
            aria-label={t("supplier")}
          >
            <option value="none">{t("selectSupplier")}</option>
            {suppliers.map((s) => (
              <option key={s.id} value={s.id}>
                {s.name}
              </option>
            ))}
          </select>
        </div>
        <div className="space-y-2">
          <label className="text-sm font-medium">{t("supplierInvoiceNumber")}</label>
          <Input
            value={props.supplierInvoiceNumber}
            onChange={(e) => props.onSupplierInvoiceNumberChange(e.target.value)}
            placeholder={t("supplierInvoiceNumberPlaceholder")}
            disabled={props.disabled}
          />
        </div>
        <div className="space-y-2">
          <label className="text-sm font-medium">{t("date")}</label>
          <Input
            type="date"
            value={props.date}
            onChange={(e) => props.onDateChange(e.target.value)}
            disabled={props.disabled}
          />
        </div>
        <div className="space-y-2">
          <label className="text-sm font-medium">{t("paymentMethod")}</label>
          <select
            className={selectClass}
            value={props.paymentMethod}
            onChange={(e) => props.onPaymentMethodChange(e.target.value as "cash" | "credit")}
            disabled={props.disabled}
            aria-label={t("paymentMethod")}
          >
            <option value="cash">{t("cash")}</option>
            <option value="credit">{t("credit")}</option>
          </select>
        </div>
        <div className="space-y-2">
          <label className="text-sm font-medium">{t("discount")}</label>
          <Input
            type="number"
            step="1"
            min="0"
            dir="ltr"
            value={props.discount}
            onChange={(e) => props.onDiscountChange(e.target.value)}
            disabled={props.disabled}
          />
        </div>
        <div className="space-y-2">
          <label className="text-sm font-medium">{t("tax")}</label>
          <Input
            type="number"
            step="1"
            min="0"
            dir="ltr"
            value={props.tax}
            onChange={(e) => props.onTaxChange(e.target.value)}
            disabled={props.disabled}
          />
        </div>
      </div>
      <div className="space-y-2">
        <label className="text-sm font-medium">{t("notes")}</label>
        <textarea
          value={props.notes}
          onChange={(e) => props.onNotesChange(e.target.value)}
          disabled={props.disabled}
          className="h-16 w-full min-w-0 rounded-lg border border-input bg-transparent px-2.5 py-1.5 text-base transition-colors outline-none placeholder:text-muted-foreground focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 md:text-sm dark:bg-input/30 resize-y"
        />
      </div>
    </div>
  )
}
