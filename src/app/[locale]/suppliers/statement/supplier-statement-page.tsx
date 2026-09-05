"use client"

import { useState } from "react"
import { useSearchParams } from "next/navigation"
import { useTranslations } from "next-intl"
import { ArrowRight, Eye, Loader2, Wallet } from "lucide-react"

import { PurchaseInvoice, PartyStatementEntry } from "@/types/domain/domain.types"
import { useSupplier, useSupplierPurchases, useSupplierStatement } from "@/hooks/use-suppliers"
import { usePurchase } from "@/hooks/use-purchases"
import { useAuth } from "@/components/common/auth-context"
import { useRouter } from "@/i18n/navigation"
import { PERMISSIONS } from "@/lib/constants"
import { AccessDenied } from "@/components/common/access-denied"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import { TableColumn, TableBuilder } from "@/components/common/table-builder"
import { TooltipIconButton } from "@/components/common/tooltip-icon-button"
import { PurchaseDetailsDialog } from "@/components/common/purchase-details-dialog"
import { RecordPaymentDialog } from "@/components/common/record-payment-dialog"

export function SupplierStatementPage() {
  const t = useTranslations("Suppliers")
  const { hasPermission } = useAuth()
  const router = useRouter()
  const searchParams = useSearchParams()
  const supplierId = searchParams.get("id") ?? ""
  const isMissing = !supplierId

  const canView = hasPermission(PERMISSIONS.SUPPLIERS_VIEW)
  const canCreatePayments = hasPermission(PERMISSIONS.PAYMENTS_CREATE)

  const { data: supplier, isPending: supplierLoading, isError: supplierError } = useSupplier(supplierId, canView && !isMissing)
  const { data: statement, isPending: statementLoading, isError: statementError } = useSupplierStatement(supplierId, canView && !isMissing)
  const { data: purchasesData, isPending: purchasesLoading, isError: purchasesError } = useSupplierPurchases(supplierId, canView && !isMissing)

  const [selectedId, setSelectedId] = useState<string | null>(null)
  const { data: fullPurchase } = usePurchase(selectedId ?? "", !!selectedId)
  const [recordPaymentOpen, setRecordPaymentOpen] = useState(false)

  if (!canView) return <AccessDenied />

  const goBack = () => router.replace("/suppliers")

  if (isMissing || (supplierError && !supplierLoading)) {
    return (
      <div className="flex-1 space-y-4 pt-6">
        <p className="text-muted-foreground">{t("partyNotFound")}</p>
        <Button onClick={goBack}>
          <ArrowRight className="me-2 h-4 w-4" /> {t("backToSuppliers")}
        </Button>
      </div>
    )
  }

  const balance = statement?.balance ?? 0
  const entries: PartyStatementEntry[] = statement?.entries ?? []
  const purchases = purchasesData?.items ?? []
  const paidByInvoice = purchasesData?.paidByInvoice ?? {}

  // A fully returned purchase owes nothing: its linked refund payments would
  // otherwise leave a phantom remaining amount against the original total.
  const paidOf = (pur: PurchaseInvoice) =>
    pur.returnStatus === "full" ? pur.total : (paidByInvoice[pur.id] ?? 0)

  const purchaseColumns: TableColumn<PurchaseInvoice>[] = [
    {
      key: "number",
      header: t("invoiceNumber"),
      cell: (pur) => <span dir="ltr" className="font-medium">#{pur.invoiceNumber}</span>,
    },
    {
      key: "date",
      header: t("date"),
      cell: (pur) => new Date(pur.date).toLocaleDateString(),
    },
    {
      key: "supplierInvoiceNumber",
      header: t("supplierInvoiceNumber"),
      cell: (pur) => <span dir="ltr" className="text-muted-foreground">{pur.supplierInvoiceNumber ?? "—"}</span>,
    },
    {
      key: "total",
      header: t("total"),
      cell: (pur) => <span dir="ltr">{pur.total.toFixed(2)}</span>,
    },
    {
      key: "paid",
      header: t("paid"),
      cell: (pur) => <span dir="ltr" className="text-emerald-600">{paidOf(pur).toFixed(2)}</span>,
    },
    {
      key: "remaining",
      header: t("remaining"),
      cell: (pur) => {
        const remaining = pur.total - paidOf(pur)
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
      cell: (pur) => (
        <TooltipIconButton label={t("viewItems")} onClick={() => setSelectedId(pur.id)}>
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
          <TooltipIconButton label={t("backToSuppliers")} onClick={goBack}>
            <ArrowRight className="h-4 w-4" />
          </TooltipIconButton>
          <div>
            <h2 className="text-3xl font-bold tracking-tight">{t("statementTitle")}</h2>
            <p className="text-sm text-muted-foreground">
              {supplierLoading ? <Loader2 className="h-4 w-4 animate-spin" /> : (supplier?.name ?? "")}
            </p>
          </div>
        </div>
        {canCreatePayments && supplier && (
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
              <span dir="ltr">{supplier?.phone ?? "—"}</span>
            </div>
            <div className="flex justify-between gap-4">
              <span className="text-muted-foreground">{t("address")}</span>
              <span>{supplier?.address ?? "—"}</span>
            </div>
          </CardContent>
        </Card>
      </div>

      <div className="space-y-2">
        <h3 className="text-xl font-semibold">{t("purchasesTitle")}</h3>
        {purchasesError ? (
          <p className="text-sm text-destructive">{t("loadFailed")}</p>
        ) : (
          <TableBuilder
            columns={purchaseColumns}
            data={purchases}
            rowKey={(pur) => pur.id}
            loading={purchasesLoading}
            emptyMessage={t("noPurchases")}
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

      <PurchaseDetailsDialog
        purchase={fullPurchase ?? null}
        open={!!selectedId && !!fullPurchase}
        onOpenChange={(open) => {
          if (!open) setSelectedId(null)
        }}
      />

      {supplier && (
        <RecordPaymentDialog
          kind="supplier"
          party={supplier}
          open={recordPaymentOpen}
          onOpenChange={setRecordPaymentOpen}
        />
      )}
    </div>
  )
}
