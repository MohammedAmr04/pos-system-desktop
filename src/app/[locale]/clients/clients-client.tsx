"use client"

import { useEffect, useState } from "react"
import { useTranslations } from "next-intl"
import { useApiError } from "@/lib/api-error"
import { toast } from "sonner"
import { CircleCheck, CircleX, Pencil, Plus, ScrollText, Wallet } from "lucide-react"

import { BalanceFilter, Client } from "@/types/domain/domain.types"
import { useClientsPage } from "@/hooks/use-clients"
import { updateClient } from "@/actions/clients.actions"
import { useDebouncedCallback } from "@/hooks/use-debounced-callback"
import { useAuth } from "@/components/common/auth-context"
import { useRouter } from "@/i18n/navigation"
import { PERMISSIONS } from "@/lib/constants"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { TableColumn, TableBuilder } from "@/components/common/table-builder"
import { DataPagination } from "@/components/common/data-pagination"
import { TooltipIconButton } from "@/components/common/tooltip-icon-button"
import { ClientFormDialog } from "./_components/client-form-dialog"
import { RecordPaymentDialog } from "@/components/common/record-payment-dialog"

const PAGE_SIZE = 20

export function ClientsClient() {
  const t = useTranslations("Clients")
  const tc = useTranslations("Common")
  const resolveError = useApiError()
  const router = useRouter()
  const { hasPermission } = useAuth()
  const canCreate = hasPermission(PERMISSIONS.CLIENTS_CREATE)
  const canUpdate = hasPermission(PERMISSIONS.CLIENTS_UPDATE)
  const canCreatePayments = hasPermission(PERMISSIONS.PAYMENTS_CREATE)

  const [page, setPage] = useState(1)
  const [query, setQuery] = useState("")
  const [searchInput, setSearchInput] = useState("")
  const [balance, setBalance] = useState<BalanceFilter>("all")
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [editingClient, setEditingClient] = useState<Client | null>(null)
  const [formSession, setFormSession] = useState(0)
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

  const { data, isPending } = useClientsPage(page, PAGE_SIZE, { q: query, balance })
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
      toast.error(resolveError(e) || t("saveFailed"))
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
      key: "balance",
      header: t("balanceColumn"),
      cell: (client) => {
        const value = client.balance ?? 0
        if (Math.abs(value) < 0.005) {
          return <span className="text-muted-foreground">—</span>
        }
        const owesUs = value > 0
        return (
          <span
            dir="ltr"
            className={owesUs ? "font-medium text-amber-600" : "font-medium text-emerald-600"}
          >
            {Math.abs(value).toFixed(2)}{" "}
            <span className="text-xs font-normal">{owesUs ? t("owesUs") : t("advanceShort")}</span>
          </span>
        )
      },
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
          <TooltipIconButton label={t("statement")} onClick={() => router.push(`/clients/statement?id=${client.id}`)}>
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

      <div className="flex items-center gap-3 mb-4">
        <Input
          placeholder={tc("search")}
          value={searchInput}
          onChange={(e) => setSearchInput(e.target.value)}
          className="max-w-sm"
        />
        <Select
          value={balance}
          onValueChange={(v) => {
            if (v == null) return
            setBalance(v as BalanceFilter)
            setPage(1)
          }}
        >
          <SelectTrigger aria-label={t("balanceColumn")} className="w-[160px]">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">{t("balanceAll")}</SelectItem>
            <SelectItem value="positive">{t("balanceOwesUs")}</SelectItem>
            <SelectItem value="negative">{t("balanceAdvance")}</SelectItem>
            <SelectItem value="zero">{t("balanceSettled")}</SelectItem>
          </SelectContent>
        </Select>
      </div>

      <TableBuilder
        columns={columns}
        data={items}
        rowKey={(client) => client.id}
        loading={isPending}
        emptyMessage={query || balance !== "all" ? t("noResults") : t("empty")}
      />

      <DataPagination page={page} pageSize={PAGE_SIZE} total={total} onPageChange={setPage} />

      <ClientFormDialog
        key={formSession}
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        client={editingClient}
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
