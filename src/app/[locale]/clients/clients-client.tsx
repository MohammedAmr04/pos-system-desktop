"use client"

import { useEffect, useState } from "react"
import { useTranslations } from "next-intl"
import { toast } from "sonner"
import { CircleCheck, CircleX, Pencil, Plus, ScrollText, Wallet } from "lucide-react"

import { Client } from "@/types/domain/domain.types"
import { useClientsPage } from "@/hooks/use-clients"
import { updateClient } from "@/actions/clients.actions"
import { useDebouncedCallback } from "@/hooks/use-debounced-callback"
import { useAuth } from "@/components/common/auth-context"
import { PERMISSIONS } from "@/lib/constants"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { TableColumn, TableBuilder } from "@/components/common/table-builder"
import { DataPagination } from "@/components/common/data-pagination"
import { TooltipIconButton } from "@/components/common/tooltip-icon-button"
import { ClientFormDialog } from "./_components/client-form-dialog"
import { PartyStatementDialog } from "@/components/common/party-statement-dialog"
import { RecordPaymentDialog } from "@/components/common/record-payment-dialog"

const PAGE_SIZE = 20

export function ClientsClient() {
  const t = useTranslations("Clients")
  const tc = useTranslations("Common")
  const { hasPermission } = useAuth()
  const canCreate = hasPermission(PERMISSIONS.CLIENTS_CREATE)
  const canUpdate = hasPermission(PERMISSIONS.CLIENTS_UPDATE)
  const canCreatePayments = hasPermission(PERMISSIONS.PAYMENTS_CREATE)

  const [page, setPage] = useState(1)
  const [query, setQuery] = useState("")
  const [searchInput, setSearchInput] = useState("")
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [editingClient, setEditingClient] = useState<Client | null>(null)
  const [formSession, setFormSession] = useState(0)
  const [statementFor, setStatementFor] = useState<Client | null>(null)
  const [paymentFor, setPaymentFor] = useState<Client | null>(null)

  const debouncedQueryChange = useDebouncedCallback((value: string) => {
    setQuery(value)
    setPage(1)
  }, 300)

  useEffect(() => {
    const value = searchInput.trim()
    if (value === query) return
    debouncedQueryChange(value)
  }, [debouncedQueryChange, query, searchInput])

  const { data, isPending } = useClientsPage(page, PAGE_SIZE, { q: query })
  const items = data?.items ?? []
  const total = data?.total ?? 0

  const openCreate = () => {
    setEditingClient(null)
    setFormSession((s) => s + 1)
    setIsFormOpen(true)
  }

  const openEdit = (client: Client) => {
    setEditingClient(client)
    setFormSession((s) => s + 1)
    setIsFormOpen(true)
  }

  const handleToggleActive = async (client: Client) => {
    try {
      await updateClient(client.id, { isActive: !client.isActive })
      toast.success(client.isActive ? t("deactivated") : t("activated"))
    } catch (e) {
      toast.error((e as Error).message || t("saveFailed"))
    }
  }

  const columns: TableColumn<Client>[] = [
    {
      key: "name",
      header: t("name"),
      cell: (client) => <span className="font-medium">{client.name}</span>,
    },
    {
      key: "phone",
      header: t("phone"),
      cell: (client) => (
        <span dir="ltr" className="text-muted-foreground">{client.phone ?? "—"}</span>
      ),
    },
    {
      key: "status",
      header: t("status"),
      cell: (client) => (
        <span
          className={
            client.isActive
              ? "inline-flex items-center rounded-full bg-emerald-500/10 px-2 py-0.5 text-xs font-medium text-emerald-600"
              : "inline-flex items-center rounded-full bg-destructive/10 px-2 py-0.5 text-xs font-medium text-destructive"
          }
        >
          {client.isActive ? t("active") : t("inactive")}
        </span>
      ),
    },
    {
      key: "actions",
      header: t("actions"),
      headClassName: "w-40",
      cell: (client) => (
        <div className="flex items-center gap-1">
          {canCreatePayments && (
            <TooltipIconButton label={t("recordPayment")} onClick={() => setPaymentFor(client)}>
              <Wallet className="h-4 w-4" />
            </TooltipIconButton>
          )}
          <TooltipIconButton label={t("statement")} onClick={() => setStatementFor(client)}>
            <ScrollText className="h-4 w-4" />
          </TooltipIconButton>
          {canUpdate && (
            <>
              <TooltipIconButton
                label={client.isActive ? t("deactivate") : t("activate")}
                onClick={() => handleToggleActive(client)}
              >
                {client.isActive ? <CircleX className="h-4 w-4" /> : <CircleCheck className="h-4 w-4" />}
              </TooltipIconButton>
              <TooltipIconButton label={t("editClient")} onClick={() => openEdit(client)}>
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
            <Plus className="mr-2 h-4 w-4" /> {t("newClient")}
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
        rowKey={(client) => client.id}
        loading={isPending}
        emptyMessage={query ? t("noResults") : t("empty")}
      />

      <DataPagination page={page} pageSize={PAGE_SIZE} total={total} onPageChange={setPage} />

      <ClientFormDialog
        key={formSession}
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        client={editingClient}
      />

      <PartyStatementDialog
        kind="client"
        party={statementFor}
        open={!!statementFor}
        onOpenChange={(open) => {
          if (!open) setStatementFor(null)
        }}
      />

      <RecordPaymentDialog
        kind="client"
        party={paymentFor}
        open={!!paymentFor}
        onOpenChange={(open) => {
          if (!open) setPaymentFor(null)
        }}
      />
    </>
  )
}
