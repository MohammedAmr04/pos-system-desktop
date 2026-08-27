"use client"

import { Supplier } from "@/types/domain/domain.types"
import { useAllSuppliers } from "@/hooks/use-suppliers"
import { useTranslations } from "next-intl"
import { Input } from "@/components/ui/input"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"

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
  const { data: suppliers = [] } = useAllSuppliers()

  return (
    <div className="rounded-lg border bg-card p-4 space-y-4">
      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-2">
          <label className="text-sm font-medium">{t("supplier")}</label>
          <Select
            value={props.supplierId}
            onValueChange={(v) => v != null && props.onSupplierChange(v)}
            disabled={props.disabled}
          >
            <SelectTrigger aria-label={t("supplier")} className="w-full">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="none">{t("selectSupplier")}</SelectItem>
              {suppliers.map((s) => (
                <SelectItem key={s.id} value={s.id}>
                  {s.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
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
          <Select
            value={props.paymentMethod}
            onValueChange={(v) => v != null && props.onPaymentMethodChange(v as "cash" | "credit")}
            disabled={props.disabled}
          >
            <SelectTrigger aria-label={t("paymentMethod")} className="w-full">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="cash">{t("cash")}</SelectItem>
              <SelectItem value="credit">{t("credit")}</SelectItem>
            </SelectContent>
          </Select>
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
