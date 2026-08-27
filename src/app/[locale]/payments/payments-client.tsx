"use client"

import { useState } from "react"
import { useTranslations } from "next-intl"
import { Payment } from "@/types/domain/domain.types"
import { usePaymentsPage } from "@/hooks/use-payments"
import { TableColumn, TableBuilder } from "@/components/common/table-builder"
import { DataPagination } from "@/components/common/data-pagination"

const PAGE_SIZE = 20

export function PaymentsClient() {
  const t = useTranslations("Payments")
  const [page, setPage] = useState(1)

  const { data, isPending } = usePaymentsPage(page, PAGE_SIZE)
  const items = data?.items ?? []
  const total = data?.total ?? 0

  const partyName = (payment: Payment) =>
    payment.client?.name || payment.supplier?.name || t("unknownParty")

  const methodLabel = (method: string) => {
    if (method === "card") return t("methodCard")
    if (method === "bank_transfer") return t("methodBankTransfer")
    return t("methodCash")
  }

  const columns: TableColumn<Payment>[] = [
    {
      key: "date",
      header: <span className="block text-center">{t("date")}</span>,
      className: "text-center whitespace-nowrap",
      cell: (payment) => new Date(payment.date).toLocaleDateString(),
    },
    {
      key: "party",
      header: <span className="block text-center">{t("party")}</span>,
      className: "text-center",
      cell: (payment) => (
        <div>
          <div>{partyName(payment)}</div>
          <div className="text-xs text-muted-foreground">
            {payment.clientId ? t("client") : t("supplier")}
            {payment.invoiceNumber ? ` · ${payment.invoiceNumber}` : ""}
          </div>
        </div>
      ),
    },
    {
      key: "method",
      header: <span className="block text-center">{t("method")}</span>,
      className: "text-center",
      cell: (payment) => methodLabel(payment.paymentMethod),
    },
    {
      key: "reference",
      header: <span className="block text-center">{t("reference")}</span>,
      className: "text-center",
      cell: (payment) => payment.reference || "-",
    },
    {
      key: "amount",
      header: <span className="block text-center">{t("amount")}</span>,
      className: "text-center font-medium",
      cell: (payment) => <span dir="ltr">{payment.amount.toFixed(2)}</span>,
    },
  ]

  return (
    <>
      <p className="text-sm text-muted-foreground mb-4">{t("description")}</p>

      <TableBuilder
        columns={columns}
        data={items}
        rowKey={(payment) => payment.id}
        loading={isPending}
        emptyMessage={t("noPayments")}
      />

      <DataPagination
        page={page}
        pageSize={PAGE_SIZE}
        total={total}
        onPageChange={setPage}
        className="justify-center"
      />
    </>
  )
}
