"use client"

import { PurchaseInvoice } from "@/lib/api"
import { useTranslations } from "next-intl"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"

interface PurchaseDetailsDialogProps {
  purchase: PurchaseInvoice | null
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function PurchaseDetailsDialog({ purchase, open, onOpenChange }: PurchaseDetailsDialogProps) {
  const t = useTranslations("Purchases")
  if (!purchase) return null

  const items = purchase.items ?? []

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[680px] max-h-[85vh] flex flex-col">
        <DialogHeader>
          <DialogTitle>
            {`${t("invoiceNumber")} #${purchase.invoiceNumber}`}
          </DialogTitle>
          <DialogDescription>
            {`${purchase.supplier?.name ?? "—"} · ${new Date(purchase.date).toLocaleString()}`}
          </DialogDescription>
        </DialogHeader>

        <div className="flex-1 overflow-y-auto mt-2 pr-2">
          <div className="grid grid-cols-3 gap-3 mb-4 text-sm">
            <div className="rounded-lg border px-3 py-2">
              <span className="block text-muted-foreground">{t("paymentMethod")}</span>
              <span className="font-medium">{purchase.paymentMethod === "credit" ? t("credit") : t("cash")}</span>
            </div>
            <div className="rounded-lg border px-3 py-2">
              <span className="block text-muted-foreground">{t("status")}</span>
              <span className="font-medium">
                {purchase.status === "posted" ? t("posted") : purchase.status === "cancelled" ? t("cancelled") : t("draft")}
              </span>
            </div>
            {purchase.supplierInvoiceNumber && (
              <div className="rounded-lg border px-3 py-2" dir="ltr">
                <span className="block text-muted-foreground">{t("supplierInvoiceNumber")}</span>
                <span className="font-medium">{purchase.supplierInvoiceNumber}</span>
              </div>
            )}
          </div>

          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{t("product")}</TableHead>
                <TableHead>{t("unit")}</TableHead>
                <TableHead>{t("quantity")}</TableHead>
                <TableHead>{t("unitCost")}</TableHead>
                <TableHead>{t("lineTotal")}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {items.map((item) => (
                <TableRow key={item.id}>
                  <TableCell>{item.product?.name ?? item.productId}</TableCell>
                  <TableCell>{item.unitName}</TableCell>
                  <TableCell dir="ltr">{item.quantity}</TableCell>
                  <TableCell dir="ltr">{item.unitCost.toFixed(2)}</TableCell>
                  <TableCell dir="ltr">{item.lineTotal.toFixed(2)}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>

        <div className="border-t pt-3 space-y-1 text-sm">
          <div className="flex justify-between">
            <span className="text-muted-foreground">{t("subtotal")}</span>
            <span dir="ltr">{purchase.subtotal.toFixed(2)}</span>
          </div>
          {purchase.discount > 0 && (
            <div className="flex justify-between">
              <span className="text-muted-foreground">{t("discount")}</span>
              <span dir="ltr">-{purchase.discount.toFixed(2)}</span>
            </div>
          )}
          {purchase.tax > 0 && (
            <div className="flex justify-between">
              <span className="text-muted-foreground">{t("tax")}</span>
              <span dir="ltr">+{purchase.tax.toFixed(2)}</span>
            </div>
          )}
          <div className="flex justify-between font-bold text-base">
            <span>{t("total")}</span>
            <span dir="ltr">{purchase.total.toFixed(2)}</span>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
