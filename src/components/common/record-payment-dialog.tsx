"use client"

import { useState } from "react"
import { useTranslations } from "next-intl"
import { Client, Supplier } from "@/types/domain/domain.types"
import { createPayment } from "@/actions/payments.actions"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Loader2, Wallet } from "lucide-react"

interface RecordPaymentDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  /** Exactly one target party per payment (spec Â§19). */
  kind: "client" | "supplier"
  party: Client | Supplier | null
  /** Optional invoice/purchase link. */
  invoiceId?: string
  invoiceLabel?: string
  defaultAmount?: number
  onSaved?: () => void
}

export function RecordPaymentDialog({
  open,
  onOpenChange,
  kind,
  party,
  invoiceId,
  invoiceLabel,
  defaultAmount,
  onSaved,
}: RecordPaymentDialogProps) {
  const t = useTranslations("Payments")
  const [amount, setAmount] = useState("")
  const [method, setMethod] = useState<"cash" | "card" | "bank_transfer">("cash")
  const [reference, setReference] = useState("")
  const [notes, setNotes] = useState("")
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [initializedFor, setInitializedFor] = useState<string | null>(null)

  const partyKey = party ? `${kind}:${party.id}:${invoiceId ?? ""}` : ""

  // Canonical React "reset state when props change" pattern.
  if (open && party && initializedFor !== partyKey) {
    setInitializedFor(partyKey)
    setAmount(defaultAmount != null ? String(defaultAmount) : "")
    setReference("")
    setNotes("")
    setMethod("cash")
    setError(null)
  }

  async function handleSubmit() {
    if (!party) return
    const value = Number(amount)
    setError(null)
    if (!Number.isFinite(value) || value <= 0) {
      setError(t("amountMustBePositive"))
      return
    }
    setSaving(true)
    try {
      await createPayment({
        amount: value,
        paymentMethod: method,
        clientId: kind === "client" ? party.id : null,
        supplierId: kind === "supplier" ? party.id : null,
        invoiceId: invoiceId ?? null,
        reference: reference.trim() || null,
        notes: notes.trim() || null,
      })
      onSaved?.()
      onOpenChange(false)
      setAmount("")
      setReference("")
      setNotes("")
      setMethod("cash")
      setInitializedFor(null)
    } catch (err) {
      setError(err instanceof Error ? err.message : t("saveFailed"))
    } finally {
      setSaving(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[440px]">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Wallet className="h-5 w-5 text-primary" />
            {kind === "client" ? t("recordClientPayment") : t("recordSupplierPayment")}
          </DialogTitle>
          <DialogDescription>{party?.name}</DialogDescription>
          {invoiceLabel && (
            <p className="text-xs text-muted-foreground">{invoiceLabel}</p>
          )}
        </DialogHeader>

        <div className="space-y-4 py-2">
          <div className="space-y-2">
            <label htmlFor="payment-amount" className="text-sm font-medium">{t("amount")}</label>
            <Input
              id="payment-amount"
              type="number"
              step="1"
              min="1"
              inputMode="numeric"
              value={amount}
              onChange={(e) => setAmount(e.target.value)}
              placeholder="0"
              autoFocus
            />
          </div>

          <div className="space-y-2">
            <label htmlFor="payment-method" className="text-sm font-medium">{t("method")}</label>
            <Select
              value={method}
              onValueChange={(v) => v && setMethod(v as typeof method)}
            >
              <SelectTrigger id="payment-method" className="w-full">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="cash">{t("methodCash")}</SelectItem>
                <SelectItem value="card">{t("methodCard")}</SelectItem>
                <SelectItem value="bank_transfer">{t("methodBankTransfer")}</SelectItem>
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-2">
            <label htmlFor="payment-reference" className="text-sm font-medium">{t("reference")}</label>
            <Input
              id="payment-reference"
              type="text"
              value={reference}
              onChange={(e) => setReference(e.target.value)}
              placeholder={t("referencePlaceholder")}
            />
          </div>

          <div className="space-y-2">
            <label htmlFor="payment-notes" className="text-sm font-medium">{t("notes")}</label>
            <Input
              id="payment-notes"
              type="text"
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
            />
          </div>

          {error && (
            <p className="text-sm text-destructive" role="alert">
              {error}
            </p>
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={saving}>
            {t("cancel")}
          </Button>
          <Button onClick={handleSubmit} disabled={saving}>
            {saving && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {t("save")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

