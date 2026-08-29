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
import { SaleReturn } from "@/types/domain/domain.types"

interface ReturnDetailsDialogProps {
  ret: SaleReturn | null
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function ReturnDetailsDialog({ ret, open, onOpenChange }: ReturnDetailsDialogProps) {
  const t = useTranslations("Returns")
  const ti = useTranslations("Invoices")
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
            {`${ti("invoiceNumber")} #${ret.invoiceNumber ?? "—"} · ${new Date(ret.createdAt).toLocaleString()}`}
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
                <TableHead className="text-center">{ti("item")}</TableHead>
                <TableHead className="text-center">{ti("qty")}</TableHead>
                <TableHead className="text-center">{ti("price")}</TableHead>
                <TableHead className="text-center">{ti("itemTotal")}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {details.map((d) => (
                <TableRow key={d.id}>
                  <TableCell className="text-center">
                    <div>{d.product?.name ?? ti("unknownProduct")}</div>
                    {d.unitName && (
                      <div className="text-xs text-muted-foreground">({d.unitName})</div>
                    )}
                  </TableCell>
                  <TableCell className="text-center">
                    {d.quantity}{d.unitName ? ` ${d.unitName}` : ""}
                  </TableCell>
                  <TableCell className="text-center">{d.unitPrice.toFixed(2)}</TableCell>
                  <TableCell className="text-center">{d.lineTotal.toFixed(2)}</TableCell>
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