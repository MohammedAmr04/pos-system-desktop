"use client"

import { useEffect, useState } from "react"
import { useTranslations } from "next-intl"
import { useApiError } from "@/lib/api-error"
import { toast } from "sonner"
import { CircleCheck, CircleX, Pencil, Plus, ScrollText, Wallet } from "lucide-react"

import { Supplier } from "@/types/domain/domain.types"
import { useSuppliersPage } from "@/hooks/use-suppliers"
import { updateSupplier } from "@/actions/suppliers.actions"
import { useDebouncedCallback } from "@/hooks/use-debounced-callback"
import { useAuth } from "@/components/common/auth-context"
import { PERMISSIONS } from "@/lib/constants"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { TableColumn, TableBuilder } from "@/components/common/table-builder"
import { DataPagination } from "@/components/common/data-pagination"
import { TooltipIconButton } from "@/components/common/tooltip-icon-button"
import { SupplierFormDialog } from "./_components/supplier-form-dialog"
import { PartyStatementDialog } from "@/components/common/party-statement-dialog"
import { RecordPaymentDialog } from "@/components/common/record-payment-dialog"

const PAGE_SIZE = 20

export function SuppliersClient() {
  const t = useTranslations("Suppliers")
  const tc = useTranslations("Common")
  const resolveError = useApiError()
  const { hasPermission } = useAuth()
  const canCreate = hasPermission(PERMISSIONS.SUPPLIERS_CREATE)
  const canUpdate = hasPermission(PERMISSIONS.SUPPLIERS_UPDATE)
  const canCreatePayments = hasPermission(PERMISSIONS.PAYMENTS_CREATE)

  const [page, setPage] = useState(1)
  const [query, setQuery] = useState("")
  const [searchInput, setSearchInput] = useState("")
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [editingSupplier, setEditingSupplier] = useState<Supplier | null>(null)
  const [formSession, setFormSession] = useState(0)
  const [statementFor, setStatementFor] = useState<Supplier | null>(null)
  const [paymentFor, setPaymentFor] = useState<Supplier | null>(null)

  const debouncedQueryChange = useDebouncedCallback((value: string) => {
    setQuery(value)
    setPage(1)
  }, 300)

  useEffect(() => {
    const value = searchInput.trim()
    if (value === query) return
    debouncedQueryChange(value)
  }, [debouncedQueryChange, query, searchInput])

  const { data, isPending } = useSuppliersPage(page, PAGE_SIZE, { q: query })
  const items = data?.items ?? []
  const total = data?.total ?? 0

  const openCreate = () => {
    setEditingSupplier(null)
    setFormSession((s) => s + 1)
    setIsFormOpen(true)
  }

  const openEdit = (supplier: Supplier) => {
    setEditingSupplier(supplier)
    setFormSession((s) => s + 1)
    setIsFormOpen(true)
  }

  const handleToggleActive = async (supplier: Supplier) => {
    try {
      await updateSupplier(supplier.id, { isActive: !supplier.isActive })
      toast.success(supplier.isActive ? t("deactivated") : t("activated"))
    } catch (e) {
      toast.error(resolveError(e) || t("saveFailed"))
    }
  }

  const columns: TableColumn<Supplier>[] = [
    {
      key: "name",
      header: t("name"),
      cell: (supplier) => <span className="font-medium">{supplier.name}</span>,
    },
    {
      key: "phone",
      header: t("phone"),
      cell: (supplier) => (
        <span className="text-muted-foreground">{supplier.phone ?? "—"}</span>
      ),
    },
    {
      key: "status",
      header: t("status"),
      cell: (supplier) => (
        <span
          className={
            supplier.isActive
              ? "inline-flex items-center rounded-full bg-emerald-500/10 px-2 py-0.5 text-xs font-medium text-emerald-600"
              : "inline-flex items-center rounded-full bg-destructive/10 px-2 py-0.5 text-xs font-medium text-destructive"
          }
        >
          {supplier.isActive ? t("active") : t("inactive")}
        </span>
      ),
    },
    {
      key: "actions",
      header: t("actions"),
      headClassName: "w-40",
      cell: (supplier) => (
        <div className="flex items-center gap-1">
          {canCreatePayments && (
            <TooltipIconButton label={t("recordPayment")} onClick={() => setPaymentFor(supplier)}>
              <Wallet className="h-4 w-4" />
            </TooltipIconButton>
          )}
          <TooltipIconButton label={t("statement")} onClick={() => setStatementFor(supplier)}>
            <ScrollText className="h-4 w-4" />
          </TooltipIconButton>
          {canUpdate && (
            <>
              <TooltipIconButton
                label={supplier.isActive ? t("deactivate") : t("activate")}
                onClick={() => handleToggleActive(supplier)}
              >
                {supplier.isActive ? <CircleX className="h-4 w-4" /> : <CircleCheck className="h-4 w-4" />}
              </TooltipIconButton>
              <TooltipIconButton label={t("editSupplier")} onClick={() => openEdit(supplier)}>
                <Pencil className="h-4 w-4" />
              </TooltipIconButton>
            </>
          )}
        </div>
      ),
    },
  ]

  return (
    <>
      <div className="flex items-center justify-between gap-4 mb-4">
        <p className="text-sm text-muted-foreground">{t("description")}</p>
        {canCreate && (
          <Button onClick={openCreate}>
            <Plus className="me-2 h-4 w-4" /> {t("newSupplier")}
          </Button>
        )}
      </div>

      <Input
        placeholder={tc("search")}
        value={searchInput}
        onChange={(e) => setSearchInput(e.target.value)}
        className="max-w-sm mb-4"
      />

      <TableBuilder
        columns={columns}
        data={items}
        rowKey={(supplier) => supplier.id}
        loading={isPending}
        emptyMessage={query ? t("noResults") : t("empty")}
      />

      <DataPagination page={page} pageSize={PAGE_SIZE} total={total} onPageChange={setPage} />

      <SupplierFormDialog
        key={formSession}
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        supplier={editingSupplier}
      />

      <PartyStatementDialog
        kind="supplier"
        party={statementFor}
        open={!!statementFor}
        onOpenChange={(open) => {
          if (!open) setStatementFor(null)
        }}
      />

      <RecordPaymentDialog
        kind="supplier"
        party={paymentFor}
        open={!!paymentFor}
        onOpenChange={(open) => {
          if (!open) setPaymentFor(null)
        }}
      />
    </>
  )
}
