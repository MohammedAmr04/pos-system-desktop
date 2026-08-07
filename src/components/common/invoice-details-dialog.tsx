"use client"

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
import { useTranslations } from "next-intl"
import { Invoice, InvoiceDetail } from "@/lib/api"

interface InvoiceDetailsDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  invoice: Invoice | null
}

export function InvoiceDetailsDialog({
  open,
  onOpenChange,
  invoice,
}: InvoiceDetailsDialogProps) {
  const t = useTranslations("Invoices")
  if (!invoice) return null

  const details: InvoiceDetail[] = invoice.InvoiceDetail ?? invoice.invoiceDetail ?? []

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[600px] max-h-[80vh] flex flex-col">
        <DialogHeader>
          <DialogTitle>{t("invoiceDetails")}</DialogTitle>
          <DialogDescription>
            {t("invoiceNumber")} #{invoice.invoiceNumber} - {new Date(invoice.createdAt).toLocaleString()}
          </DialogDescription>
        </DialogHeader>
        <div className="flex-1 overflow-y-auto mt-4 pr-2">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="text-center">{t("item")}</TableHead>
                <TableHead className="text-center">{t("qty")}</TableHead>
                <TableHead className="text-center">{t("price")}</TableHead>
                <TableHead className="text-center">{t("discount")}</TableHead>
                <TableHead className="text-center">{t("itemTotal")}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {details.map((detail: InvoiceDetail) => {
                const unitPrice = detail.unitPrice ?? detail.salePrice
                const isOverridden = detail.originalUnitPrice != null && detail.originalUnitPrice !== unitPrice
                const itemTotal = detail.finalTotal != null
                  ? detail.finalTotal
                  : ((unitPrice * detail.quantity) - (detail.discountAmount || 0))
                return (
                  <TableRow key={detail.id}>
                    <TableCell className="text-center">
                      <div>{detail.product?.name || t("unknownProduct")}</div>
                      {detail.unitName && (
                        <div className="text-xs text-muted-foreground">({detail.unitName})</div>
                      )}
                      {detail.priceEditNote && (
                        <div className="text-[11px] text-muted-foreground mt-0.5">
                          {t("note")}: {detail.priceEditNote}
                        </div>
                      )}
                    </TableCell>
                    <TableCell className="text-center">
                      {detail.quantity}{detail.unitName ? ` ${detail.unitName}` : ""}
                    </TableCell>
                    <TableCell className="text-center">
                      {isOverridden && (
                        <span className="line-through text-muted-foreground mr-1">
                          {detail.originalUnitPrice?.toFixed(2)}
                        </span>
                      )}
                      {unitPrice.toFixed(2)}
                    </TableCell>
                    <TableCell className="text-center">
                      {detail.discountAmount > 0 ? `-${detail.discountAmount.toFixed(2)}` : "0.00"}
                    </TableCell>
                    <TableCell className="text-center">{itemTotal.toFixed(2)}</TableCell>
                  </TableRow>
                )
              })}
            </TableBody>
          </Table>
          
          <div className="space-y-2 border-t pt-4 mt-4">
            <div className="flex justify-between text-muted-foreground">
              <span>{t("subtotal")}</span>
              <span>{(invoice.totalAmount + invoice.discount).toFixed(2)}</span>
            </div>
            <div className="flex justify-between text-muted-foreground">
              <span>{t("discount")}</span>
              <span>{invoice.discount?.toFixed(2)}</span>
            </div>
            <div className="flex justify-between font-bold text-lg pt-2 border-t">
              <span>{t("total")}</span>
              <span>{invoice.totalAmount?.toFixed(2)}</span>
            </div>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
