"use client"

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
import { PurchaseReturn } from "@/types/domain/domain.types"

interface PurchaseReturnDetailsDialogProps {
  ret: PurchaseReturn | null
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function PurchaseReturnDetailsDialog({ ret, open, onOpenChange }: PurchaseReturnDetailsDialogProps) {
  const t = useTranslations("Returns")
  const tp = useTranslations("Purchases")
  if (!ret) {
    return null
  }

  const details = ret.details ?? []

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[620px] max-h-[80vh] flex flex-col">
        <DialogHeader>
          <DialogTitle>
            {`${t("returnNumber")} #${ret.number}`}
          </DialogTitle>
          <DialogDescription>
            {`${t("purchaseNumber")} #${ret.purchaseInvoiceNumber ?? "—"} · ${new Date(ret.createdAt).toLocaleString()}`}
          </DialogDescription>
        </DialogHeader>

        <div className="grid grid-cols-3 gap-3 text-sm">
          <div className="rounded-lg border px-3 py-2">
            <span className="block text-muted-foreground">{t("method")}</span>
            <span className="font-medium">{ret.paymentMethod === "credit" ? t("credit") : t("cash")}</span>
          </div>
          <div className="rounded-lg border px-3 py-2">
            <span className="block text-muted-foreground">{t("refundAmount")}</span>
            <span className="font-medium">{ret.totalAmount.toFixed(2)}</span>
          </div>
          {ret.notes && (
            <div className="rounded-lg border px-3 py-2">
              <span className="block text-muted-foreground">{t("notes")}</span>
              <span className="font-medium">{ret.notes}</span>
            </div>
          )}
        </div>

        <div className="flex-1 overflow-y-auto mt-4 pr-2">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{tp("product")}</TableHead>
                <TableHead>{tp("unit")}</TableHead>
                <TableHead>{tp("quantity")}</TableHead>
                <TableHead>{tp("unitCost")}</TableHead>
                <TableHead>{tp("lineTotal")}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {details.map((d) => (
                <TableRow key={d.id}>
                  <TableCell>{d.product?.name ?? d.productId}</TableCell>
                  <TableCell>{d.unitName}</TableCell>
                  <TableCell dir="ltr">{d.quantity}</TableCell>
                  <TableCell dir="ltr">{d.unitCost.toFixed(2)}</TableCell>
                  <TableCell dir="ltr">{d.lineTotal.toFixed(2)}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>

        <div className="border-t pt-3 text-sm">
          <div className="flex justify-between font-bold text-base">
            <span>{t("refundAmount")}</span>
            <span dir="ltr">{ret.totalAmount.toFixed(2)}</span>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}