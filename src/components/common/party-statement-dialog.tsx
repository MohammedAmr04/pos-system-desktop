"use client"

import { useTranslations } from "next-intl"
import { PartyStatement, PartyStatementEntry, Supplier, Client } from "@/types/domain/domain.types"
import { getClientStatement } from "@/api/clients"
import { getSupplierStatement } from "@/api/suppliers"
import { useQuery } from "@tanstack/react-query"
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
  const enabled = open && !!party

  const { data: statement, isPending } = useQuery({
    queryKey: [kind, "statement", party?.id ?? ""],
    queryFn: () =>
      kind === "supplier"
        ? getSupplierStatement(party!.id)
        : getClientStatement(party!.id),
    enabled,
    staleTime: 0,
    gcTime: 0,
  })

  const loading = enabled && isPending
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
