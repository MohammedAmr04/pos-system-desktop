"use client"

import { Payment } from "@/lib/api"
import { useTranslations } from "next-intl"
import { Button } from "@/components/ui/button"
import { Loader2 } from "lucide-react"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"

interface PaymentsClientProps {
  items: Payment[]
  total: number
  page: number
  pageSize: number
  loading: boolean
  onPageChange: (page: number) => void
}

export function PaymentsClient({
  items,
  total,
  page,
  pageSize,
  loading,
  onPageChange,
}: PaymentsClientProps) {
  const t = useTranslations("Payments")

  const pageCount = Math.max(1, Math.ceil(total / pageSize))

  const partyName = (payment: Payment) =>
    payment.client?.name || payment.supplier?.name || t("unknownParty")

  const methodLabel = (method: string) => {
    if (method === "card") return t("methodCard")
    if (method === "bank_transfer") return t("methodBankTransfer")
    return t("methodCash")
  }

  return (
    <>
      <p className="text-sm text-muted-foreground mb-4">{t("description")}</p>

      <div className="rounded-md border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead className="text-center">{t("date")}</TableHead>
              <TableHead className="text-center">{t("party")}</TableHead>
              <TableHead className="text-center">{t("method")}</TableHead>
              <TableHead className="text-center">{t("reference")}</TableHead>
              <TableHead className="text-center">{t("amount")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              <TableRow>
                <TableCell colSpan={5} className="h-24 text-center">
                  <Loader2 className="mx-auto h-6 w-6 animate-spin text-muted-foreground" />
                </TableCell>
              </TableRow>
            ) : items.length === 0 ? (
              <TableRow>
                <TableCell colSpan={5} className="h-24 text-center text-muted-foreground">
                  {t("noPayments")}
                </TableCell>
              </TableRow>
            ) : (
              items.map((payment) => (
                <TableRow key={payment.id}>
                  <TableCell className="text-center whitespace-nowrap">
                    {new Date(payment.date).toLocaleDateString()}
                  </TableCell>
                  <TableCell className="text-center">
                    <div>{partyName(payment)}</div>
                    <div className="text-xs text-muted-foreground">
                      {payment.clientId ? t("client") : t("supplier")}
                      {payment.invoiceNumber ? ` · ${payment.invoiceNumber}` : ""}
                    </div>
                  </TableCell>
                  <TableCell className="text-center">{methodLabel(payment.paymentMethod)}</TableCell>
                  <TableCell className="text-center">{payment.reference || "-"}</TableCell>
                  <TableCell className="text-center font-medium">{payment.amount.toFixed(2)}</TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      <div className="flex items-center justify-end space-x-2 py-4 rtl:space-x-reverse">
        <Button variant="outline" size="sm" onClick={() => onPageChange(page - 1)} disabled={page <= 1}>
          {t("previous")}
        </Button>
        <span className="text-sm text-muted-foreground">
          {t("pageOf", { page, total: pageCount })}
        </span>
        <Button
          variant="outline"
          size="sm"
          onClick={() => onPageChange(page + 1)}
          disabled={page >= pageCount}
        >
          {t("next")}
        </Button>
      </div>
    </>
  )
}
