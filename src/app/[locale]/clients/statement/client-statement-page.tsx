"use client"

import { useState } from "react"
import { useSearchParams } from "next/navigation"
import { useTranslations } from "next-intl"
import { ArrowRight, Eye, Loader2, Wallet } from "lucide-react"

import { Invoice, PartyStatementEntry } from "@/types/domain/domain.types"
import { useClient, useClientInvoices, useClientStatement } from "@/hooks/use-clients"
import { useInvoice } from "@/hooks/use-invoices"
import { useAuth } from "@/components/common/auth-context"
import { useRouter } from "@/i18n/navigation"
import { PERMISSIONS } from "@/lib/constants"
import { AccessDenied } from "@/components/common/access-denied"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import { TableColumn, TableBuilder } from "@/components/common/table-builder"
import { TooltipIconButton } from "@/components/common/tooltip-icon-button"
import { InvoiceDetailsDialog } from "@/components/common/invoice-details-dialog"
import { RecordPaymentDialog } from "@/components/common/record-payment-dialog"

export function ClientStatementPage() {
  const t = useTranslations("Clients")
  const { hasPermission } = useAuth()
  const router = useRouter()
  const searchParams = useSearchParams()
  const clientId = searchParams.get("id") ?? ""
  const isMissing = !clientId

  const canView = hasPermission(PERMISSIONS.CLIENTS_VIEW)
  const canCreatePayments = hasPermission(PERMISSIONS.PAYMENTS_CREATE)

  const { data: client, isPending: clientLoading, isError: clientError } = useClient(clientId, canView && !isMissing)
  const { data: statement, isPending: statementLoading, isError: statementError } = useClientStatement(clientId, canView && !isMissing)
  const { data: invoicesData, isPending: invoicesLoading, isError: invoicesError } = useClientInvoices(clientId, canView && !isMissing)

  const [selectedId, setSelectedId] = useState<string | null>(null)
  const { data: fullInvoice } = useInvoice(selectedId ?? "", !!selectedId)
  const [recordPaymentOpen, setRecordPaymentOpen] = useState(false)

  if (!canView) return <AccessDenied />

  const goBack = () => router.replace("/clients")

  if (isMissing || (clientError && !clientLoading)) {
    return (
      <div className="flex-1 space-y-4 pt-6">
        <p className="text-muted-foreground">{t("partyNotFound")}</p>
        <Button onClick={goBack}>
          <ArrowRight className="me-2 h-4 w-4" /> {t("backToClients")}
        </Button>
      </div>
    )
  }

  const balance = statement?.balance ?? 0
  const entries: PartyStatementEntry[] = statement?.entries ?? []
  const invoices = invoicesData?.items ?? []
  const paidByInvoice = invoicesData?.paidByInvoice ?? {}

  // A fully returned invoice owes nothing: its linked refund payments would
  // otherwise leave a phantom remaining amount against the original total.
  const paidOf = (inv: Invoice) =>
    inv.returnStatus === "full" ? inv.totalAmount : (paidByInvoice[inv.id] ?? 0)

  const invoiceColumns: TableColumn<Invoice>[] = [
    {
      key: "number",
      header: t("invoiceNumber"),
      cell: (inv) => <span dir="ltr" className="font-medium">#{inv.invoiceNumber}</span>,
    },
    {
      key: "date",
      header: t("date"),
      cell: (inv) => new Date(inv.createdAt).toLocaleDateString(),
    },
    {
      key: "method",
      header: t("paymentMethod"),
      cell: (inv) => (inv.paymentMethod === "credit" ? t("credit") : t("cash")),
    },
    {
      key: "total",
      header: t("total"),
      cell: (inv) => <span dir="ltr">{inv.totalAmount.toFixed(2)}</span>,
    },
    {
      key: "paid",
      header: t("paid"),
      cell: (inv) => <span dir="ltr" className="text-emerald-600">{paidOf(inv).toFixed(2)}</span>,
    },
    {
      key: "remaining",
      header: t("remaining"),
      cell: (inv) => {
        const remaining = inv.totalAmount - paidOf(inv)
        return (
          <span dir="ltr" className={remaining > 0.005 ? "font-medium text-amber-600" : "text-muted-foreground"}>
            {remaining.toFixed(2)}
          </span>
        )
      },
    },
    {
      key: "actions",
      header: t("actions"),
      headClassName: "w-16",
      cell: (inv) => (
        <TooltipIconButton label={t("viewItems")} onClick={() => setSelectedId(inv.id)}>
          <Eye className="h-4 w-4" />
        </TooltipIconButton>
      ),
    },
  ]

  const entryColumns: TableColumn<PartyStatementEntry>[] = [
    {
      key: "date",
      header: t("date"),
      cell: (entry) => new Date(entry.date).toLocaleDateString(),
    },
    {
      key: "description",
      header: t("entryDescription"),
      cell: (entry) => entry.description,
    },
    {
      key: "debit",
      header: t("debit"),
      cell: (entry) => <span dir="ltr">{entry.debit ? entry.debit.toFixed(2) : "—"}</span>,
    },
    {
      key: "credit",
      header: t("credit"),
      cell: (entry) => <span dir="ltr">{entry.credit ? entry.credit.toFixed(2) : "—"}</span>,
    },
  ]

  return (
    <div className="flex-1 space-y-4 pt-6">
      <div className="flex items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <TooltipIconButton label={t("backToClients")} onClick={goBack}>
            <ArrowRight className="h-4 w-4" />
          </TooltipIconButton>
          <div>
            <h2 className="text-3xl font-bold tracking-tight">{t("statementTitle")}</h2>
            <p className="text-sm text-muted-foreground">
              {clientLoading ? <Loader2 className="h-4 w-4 animate-spin" /> : (client?.name ?? "")}
            </p>
          </div>
        </div>
        {canCreatePayments && client && (
          <Button onClick={() => setRecordPaymentOpen(true)}>
            <Wallet className="me-2 h-4 w-4" /> {t("recordPayment")}
          </Button>
        )}
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        <Card>
          <CardContent className="flex items-center justify-between px-4 py-3">
            <span className="text-sm font-medium">{t("balance")}</span>
            <span dir="ltr" className={`text-2xl font-bold ${balance > 0.005 ? "text-amber-600" : balance < -0.005 ? "text-emerald-600" : ""}`}>
              {Math.abs(balance).toFixed(2)}
            </span>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="space-y-1 px-4 py-3 text-sm">
            <div className="flex justify-between gap-4">
              <span className="text-muted-foreground">{t("phone")}</span>
              <span dir="ltr">{client?.phone ?? "—"}</span>
            </div>
            <div className="flex justify-between gap-4">
              <span className="text-muted-foreground">{t("address")}</span>
              <span>{client?.address ?? "—"}</span>
            </div>
          </CardContent>
        </Card>
      </div>

      <div className="space-y-2">
        <h3 className="text-xl font-semibold">{t("invoicesTitle")}</h3>
        {invoicesError ? (
          <p className="text-sm text-destructive">{t("loadFailed")}</p>
        ) : (
          <TableBuilder
            columns={invoiceColumns}
            data={invoices}
            rowKey={(inv) => inv.id}
            loading={invoicesLoading}
            emptyMessage={t("noInvoices")}
          />
        )}
      </div>

      <div className="space-y-2">
        <h3 className="text-xl font-semibold">{t("statement")}</h3>
        {statementError ? (
          <p className="text-sm text-destructive">{t("loadFailed")}</p>
        ) : (
          <TableBuilder
            columns={entryColumns}
            data={entries}
            rowKey={(_, index) => `entry-${index}`}
            loading={statementLoading}
            emptyMessage={t("noEntries")}
          />
        )}
      </div>

      <InvoiceDetailsDialog
        open={!!selectedId && !!fullInvoice}
        onOpenChange={(open) => {
          if (!open) setSelectedId(null)
        }}
        invoice={fullInvoice ?? null}
      />

      {client && (
        <RecordPaymentDialog
          kind="client"
          party={client}
          open={recordPaymentOpen}
          onOpenChange={setRecordPaymentOpen}
        />
      )}
    </div>
  )
}
