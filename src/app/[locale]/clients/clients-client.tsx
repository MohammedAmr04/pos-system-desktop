"use client"

import { Client, api } from "@/lib/api"
import { useEffect, useState } from "react"
import { useDebouncedCallback } from "@/hooks/use-debounced-callback"
import { useAuth } from "@/components/common/auth-context"
import { PERMISSIONS } from "@/lib/constants"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Loader2, Plus, Pencil, CircleCheck, CircleX, ScrollText, Wallet } from "lucide-react"
import { useTranslations } from "next-intl"
import { toast } from "sonner"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { ClientFormDialog } from "./_components/client-form-dialog"
import { PartyStatementDialog } from "@/components/common/party-statement-dialog"
import { RecordPaymentDialog } from "@/components/common/record-payment-dialog"

interface ClientsClientProps {
  items: Client[]
  total: number
  page: number
  pageSize: number
  loading: boolean
  query: string
  onQueryChange: (query: string) => void
  onPageChange: (page: number) => void
  onRefresh?: () => void
}

export function ClientsClient({
  items,
  total,
  page,
  pageSize,
  loading,
  query,
  onQueryChange,
  onPageChange,
  onRefresh,
}: ClientsClientProps) {
  const t = useTranslations("Clients")
  const tc = useTranslations("Common")
  const { hasPermission } = useAuth()
  const canCreate = hasPermission(PERMISSIONS.CLIENTS_CREATE)
  const canUpdate = hasPermission(PERMISSIONS.CLIENTS_UPDATE)
  const canCreatePayments = hasPermission(PERMISSIONS.PAYMENTS_CREATE)
  const [searchInput, setSearchInput] = useState(query)
  const debouncedQueryChange = useDebouncedCallback(onQueryChange, 300)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [editingClient, setEditingClient] = useState<Client | null>(null)
  const [formSession, setFormSession] = useState(0)
  const [statementFor, setStatementFor] = useState<Client | null>(null)
  const [paymentFor, setPaymentFor] = useState<Client | null>(null)

  useEffect(() => {
    const value = searchInput.trim()
    if (value === query) return
    debouncedQueryChange(value)
  }, [debouncedQueryChange, query, searchInput])

  const pageCount = Math.max(1, Math.ceil(total / pageSize))

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
      await api.clients.update(client.id, { isActive: !client.isActive })
      toast.success(client.isActive ? t("deactivated") : t("activated"))
      if (onRefresh) onRefresh()
    } catch (e) {
      toast.error((e as Error).message || t("saveFailed"))
    }
  }

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

      <div className="rounded-md border bg-card">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{t("name")}</TableHead>
              <TableHead>{t("phone")}</TableHead>
              <TableHead>{t("status")}</TableHead>
              <TableHead>{t("actions")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              <TableRow>
                <TableCell colSpan={4} className="h-24 text-center">
                  <Loader2 className="mx-auto h-6 w-6 animate-spin" />
                </TableCell>
              </TableRow>
            ) : items.length ? (
              items.map((client) => (
                <TableRow key={client.id}>
                  <TableCell className="font-medium">{client.name}</TableCell>
                  <TableCell dir="ltr" className="text-muted-foreground">{client.phone ?? "—"}</TableCell>
                  <TableCell>
                    <span
                      className={
                        client.isActive
                          ? "inline-flex items-center rounded-full bg-emerald-500/10 px-2 py-0.5 text-xs font-medium text-emerald-600"
                          : "inline-flex items-center rounded-full bg-destructive/10 px-2 py-0.5 text-xs font-medium text-destructive"
                      }
                    >
                      {client.isActive ? t("active") : t("inactive")}
                    </span>
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center gap-1">
                      {canCreatePayments && (
                        <Button
                          variant="ghost"
                          size="icon"
                          aria-label={t("recordPayment")}
                          title={t("recordPayment")}
                          onClick={() => setPaymentFor(client)}
                        >
                          <Wallet className="h-4 w-4" />
                        </Button>
                      )}
                      <Button
                        variant="ghost"
                        size="icon"
                        aria-label={t("statement")}
                        title={t("statement")}
                        onClick={() => setStatementFor(client)}
                      >
                        <ScrollText className="h-4 w-4" />
                      </Button>
                      {canUpdate && (
                        <>
                          <Button
                            variant="ghost"
                            size="icon"
                            aria-label={client.isActive ? t("deactivated") : t("activated")}
                            onClick={() => handleToggleActive(client)}
                          >
                            {client.isActive ? <CircleX className="h-4 w-4" /> : <CircleCheck className="h-4 w-4" />}
                          </Button>
                          <Button variant="ghost" size="icon" aria-label={t("editClient")} onClick={() => openEdit(client)}>
                            <Pencil className="h-4 w-4" />
                          </Button>
                        </>
                      )}
                    </div>
                  </TableCell>
                </TableRow>
              ))
            ) : (
              <TableRow>
                <TableCell colSpan={4} className="h-24 text-center">
                  {query ? t("noResults") : t("empty")}
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>

      <div className="flex items-center justify-between py-4">
        <span className="text-sm text-muted-foreground">{`${total}`}</span>
        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" onClick={() => onPageChange(page - 1)} disabled={page <= 1}>
            {tc("previous")}
          </Button>
          <span className="text-sm text-muted-foreground whitespace-nowrap">{`${page} / ${pageCount}`}</span>
          <Button variant="outline" size="sm" onClick={() => onPageChange(page + 1)} disabled={page >= pageCount}>
            {tc("next")}
          </Button>
        </div>
      </div>

      <ClientFormDialog
        key={formSession}
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        client={editingClient}
        onSaved={() => onRefresh?.()}
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
        onSaved={() => onRefresh?.()}
      />
    </>
  )
}
