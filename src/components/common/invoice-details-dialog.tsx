"use client"

import { useState } from "react"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
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
import { Button } from "@/components/ui/button"
import { Wallet } from "lucide-react"
import { Invoice, InvoiceDetail } from "@/types/domain/domain.types"
import { RecordPaymentDialog } from "@/components/common/record-payment-dialog"

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
  const [recordPaymentOpen, setRecordPaymentOpen] = useState(false)
  if (!invoice) return null

  const canRecordPayment = (invoice.status ?? 'posted') === 'posted' && invoice.paymentMethod === 'credit' && !!invoice.client

  const details: InvoiceDetail[] = invoice.InvoiceDetail ?? invoice.invoiceDetail ?? []
  const hasCostData = details.some((d) => d.totalCost != null)
  const totalCost = details.reduce((sum, d) => sum + (d.totalCost ?? 0), 0)
  const totalRevenue = details.reduce((sum, d) => sum + (d.finalTotal ?? ((d.unitPrice ?? d.salePrice) * d.quantity - (d.discountAmount || 0))), 0)
  const totalProfit = totalRevenue - totalCost

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
                {hasCostData && (
                  <>
                    <TableHead className="text-center">{t("cost")}</TableHead>
                    <TableHead className="text-center">{t("profit")}</TableHead>
                  </>
                )}
              </TableRow>            </TableHeader>
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
                    {hasCostData && (
                      <>
                        <TableCell className="text-center text-muted-foreground">
                          {(detail.totalCost ?? 0).toFixed(2)}
                        </TableCell>
                        <TableCell className={`text-center font-medium ${(itemTotal - (detail.totalCost ?? 0)) >= 0 ? "text-emerald-600" : "text-destructive"}`}>
                          {(itemTotal - (detail.totalCost ?? 0)).toFixed(2)}
                        </TableCell>
                      </>
                    )}
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
            {hasCostData && (
              <>
                <div className="flex justify-between text-muted-foreground border-t pt-2 mt-2">
                  <span>{t("totalCost")}</span>
                  <span>{totalCost.toFixed(2)}</span>
                </div>
                <div className={`flex justify-between font-bold ${totalProfit >= 0 ? "text-emerald-600" : "text-destructive"}`}>
                  <span>{t("grossProfit")}</span>
                  <span>{totalProfit.toFixed(2)}</span>
                </div>
              </>
            )}
          </div>
        </div>
        {canRecordPayment && (
          <DialogFooter className="mt-4">
            <Button onClick={() => setRecordPaymentOpen(true)}>
              <Wallet className="mr-2 h-4 w-4" /> {t("recordPayment")}
            </Button>
          </DialogFooter>
        )}
      </DialogContent>
      {canRecordPayment && (
        <RecordPaymentDialog
          kind="client"
          party={invoice.client ?? null}
          invoiceId={invoice.id}
          invoiceLabel={`${t("invoiceNumber")} #${invoice.invoiceNumber}`}
          open={recordPaymentOpen}
          onOpenChange={setRecordPaymentOpen}
        />
      )}
    </Dialog>
  )
}
