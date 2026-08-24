"use client"

import { useEffect, useState } from "react"
import { useTranslations } from "next-intl"
import { api, PartyStatement, PartyStatementEntry, Supplier, Client } from "@/lib/api"
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
import { Loader2 } from "lucide-react"

interface PartyStatementDialogProps {
  kind: "supplier" | "client"
  party: Supplier | Client | null
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function PartyStatementDialog({ kind, party, open, onOpenChange }: PartyStatementDialogProps) {
  const t = useTranslations(kind === "supplier" ? "Suppliers" : "Clients")
  const [cache, setCache] = useState<{ partyKey: string; data: PartyStatement | null } | null>(null)

  const partyKey = party ? `${kind}:${party.id}` : ""
  const loading = open && !!party && cache?.partyKey !== partyKey
  const statement = cache && cache.partyKey === partyKey ? cache.data : null

  useEffect(() => {
    if (!open || !party || cache?.partyKey === partyKey) return
    let cancelled = false
    const fetcher = kind === "supplier" ? api.suppliers.statement(party.id) : api.clients.statement(party.id)
    fetcher
      .then((res) => {
        if (!cancelled) setCache({ partyKey, data: res })
      })
      .catch(() => {
        if (!cancelled) setCache({ partyKey, data: null })
      })
    return () => {
      cancelled = true
    }
  }, [open, party, kind, cache, partyKey])

  const entries: PartyStatementEntry[] = statement?.entries ?? []

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[640px]">
        <DialogHeader>
          <DialogTitle>{t("statementTitle")}</DialogTitle>
          <DialogDescription>{party?.name}</DialogDescription>
        </DialogHeader>

        <div className="flex items-center justify-between rounded-lg border bg-muted/40 px-3 py-2">
          <span className="text-sm font-medium">{t("balance")}</span>
          <span className="text-lg font-bold">{(statement?.balance ?? 0).toFixed(2)}</span>
        </div>

        <div className="rounded-md border max-h-[320px] overflow-y-auto">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{t("date")}</TableHead>
                <TableHead>{t("entryDescription")}</TableHead>
                <TableHead>{t("debit")}</TableHead>
                <TableHead>{t("credit")}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {loading ? (
                <TableRow>
                  <TableCell colSpan={4} className="h-24 text-center">
                    <Loader2 className="mx-auto h-6 w-6 animate-spin" />
                  </TableCell>
                </TableRow>
              ) : entries.length ? (
                entries.map((entry, i) => (
                  <TableRow key={i}>
                    <TableCell>{new Date(entry.date).toLocaleDateString()}</TableCell>
                    <TableCell>{entry.description}</TableCell>
                    <TableCell>{entry.debit ? entry.debit.toFixed(2) : "—"}</TableCell>
                    <TableCell>{entry.credit ? entry.credit.toFixed(2) : "—"}</TableCell>
                  </TableRow>
                ))
              ) : (
                <TableRow>
                  <TableCell colSpan={4} className="h-24 text-center text-muted-foreground">
                    {t("noEntries")}
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </div>
      </DialogContent>
    </Dialog>
  )
}
