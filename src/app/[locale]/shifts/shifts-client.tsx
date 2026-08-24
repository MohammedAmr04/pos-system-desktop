"use client"

import { useEffect, useState } from "react"
import { api, Shift, ShiftReport } from "@/lib/api"
import { useAuth } from "@/components/common/auth-context"
import { PERMISSIONS } from "@/lib/constants"
import { AccessDenied } from "@/components/common/access-denied"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { Card, CardContent } from "@/components/ui/card"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { useTranslations } from "next-intl"
import { toast } from "sonner"
import { Eye, Loader2, PlayCircle, StopCircle } from "lucide-react"

const PAGE_SIZE = 20

export function ShiftsClient() {
  const t = useTranslations("Shifts")
  const tc = useTranslations("Common")
  const { hasPermission } = useAuth()
  const canView = hasPermission(PERMISSIONS.SHIFTS_VIEW)
  const canOpen = hasPermission(PERMISSIONS.SHIFTS_OPEN)
  const canClose = hasPermission(PERMISSIONS.SHIFTS_CLOSE)

  const [shifts, setShifts] = useState<Shift[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [loading, setLoading] = useState(true)
  const [activeShift, setActiveShift] = useState<Shift | null>(null)

  const [openDialog, setOpenDialog] = useState(false)
  const [openingCashInput, setOpeningCashInput] = useState("")
  const [notesInput, setNotesInput] = useState("")
  const [opening, setOpening] = useState(false)

  const [closeTarget, setCloseTarget] = useState<Shift | null>(null)
  const [closeReport, setCloseReport] = useState<ShiftReport | null>(null)
  const [countedInput, setCountedInput] = useState("")
  const [closing, setClosing] = useState(false)

  const [reportFor, setReportFor] = useState<Shift | null>(null)
  const [report, setReport] = useState<ShiftReport | null>(null)
  const [reportLoading, setReportLoading] = useState(false)

  const [reloadKey, setReloadKey] = useState(0)

  useEffect(() => {
    if (!canView) return
    let cancelled = false
    Promise.all([
      api.shifts.listPaged(page, PAGE_SIZE),
      api.shifts.getActive().catch(() => null),
    ])
      .then(([paged, active]) => {
        if (cancelled) return
        setShifts(paged.items)
        setTotal(paged.total)
        setActiveShift(active)
      })
      .catch(() => {
        if (!cancelled) toast.error(t("reportFailed"))
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [page, reloadKey, canView])

  const refresh = () => {
    setLoading(true)
    setReloadKey((k) => k + 1)
  }

  const pageCount = Math.max(1, Math.ceil(total / PAGE_SIZE))

  const handleOpen = async () => {
    const value = Number(openingCashInput || "0")
    if (Number.isNaN(value) || value < 0) {
      toast.error(t("invalidOpeningCash"))
      return
    }
    setOpening(true)
    try {
      await api.shifts.open({ openingCash: value, notes: notesInput.trim() || undefined })
      toast.success(t("opened"))
      setOpenDialog(false)
      setOpeningCashInput("")
      setNotesInput("")
      refresh()
    } catch (e) {
      toast.error((e as Error).message || t("openFailed"))
    } finally {
      setOpening(false)
    }
  }

  const startClose = async (shift: Shift) => {
    setCloseTarget(shift)
    setCountedInput("")
    setCloseReport(null)
    try {
      setCloseReport(await api.shifts.report(shift.id))
    } catch {
      toast.error(t("reportFailed"))
    }
  }

  const handleClose = async () => {
    if (!closeTarget) return
    const counted = Number(countedInput)
    if (Number.isNaN(counted) || counted < 0) {
      toast.error(t("invalidCountedCash"))
      return
    }
    setClosing(true)
    try {
      await api.shifts.close(closeTarget.id, counted)
      toast.success(t("closed"))
      setCloseTarget(null)
      refresh()
    } catch (e) {
      toast.error((e as Error).message || t("closeFailed"))
    } finally {
      setClosing(false)
    }
  }

  const viewReport = async (shift: Shift) => {
    setReportFor(shift)
    setReportLoading(true)
    try {
      setReport(await api.shifts.report(shift.id))
    } catch {
      toast.error(t("reportFailed"))
    } finally {
      setReportLoading(false)
    }
  }

  if (!canView) return <AccessDenied />

  const statusBadge = (s: string) =>
    s === "open" ? (
      <span className="inline-flex items-center rounded-full bg-emerald-500/10 px-2 py-0.5 text-xs font-medium text-emerald-600">
        {t("open")}
      </span>
    ) : (
      <span className="inline-flex items-center rounded-full bg-slate-500/10 px-2 py-0.5 text-xs font-medium text-slate-600">
        {t("closed")}
      </span>
    )

  const money = (v: number | null | undefined) => (v ?? 0).toFixed(2)

  return (
    <div className="flex-1 space-y-4 pt-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-3xl font-bold tracking-tight">{t("title")}</h2>
        {canOpen && !activeShift && (
          <Button onClick={() => setOpenDialog(true)}>
            <PlayCircle className="ml-2 h-4 w-4" />
            {t("openShift")}
          </Button>
        )}
        {activeShift && (
          <div className="flex items-center gap-2 rounded-lg border border-emerald-500/30 bg-emerald-500/10 px-3 py-1.5 text-sm font-medium text-emerald-700">
            <span className="h-2 w-2 animate-pulse rounded-full bg-emerald-500" />
            {t("activeSince", { number: activeShift.number })}
            {canClose && (
              <Button variant="outline" size="sm" onClick={() => startClose(activeShift)}>
                <StopCircle className="ml-1 h-4 w-4" />
                {t("closeShift")}
              </Button>
            )}
          </div>
        )}
      </div>

      <div className="rounded-md border bg-card">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>#</TableHead>
              <TableHead>{t("openedBy")}</TableHead>
              <TableHead>{t("openedAt")}</TableHead>
              <TableHead>{t("closedAt")}</TableHead>
              <TableHead>{t("openingCash")}</TableHead>
              <TableHead>{t("expectedCash")}</TableHead>
              <TableHead>{t("countedCash")}</TableHead>
              <TableHead>{t("difference")}</TableHead>
              <TableHead>{t("status")}</TableHead>
              <TableHead>{t("actions")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              <TableRow>
                <TableCell colSpan={10} className="h-24 text-center">
                  <Loader2 className="mx-auto h-6 w-6 animate-spin" />
                </TableCell>
              </TableRow>
            ) : shifts.length ? (
              shifts.map((s) => (
                <TableRow key={s.id}>
                  <TableCell className="font-medium">#{s.number}</TableCell>
                  <TableCell>{s.openedBy}</TableCell>
                  <TableCell dir="ltr" className="text-muted-foreground">
                    {new Date(s.openedAt).toLocaleString()}
                  </TableCell>
                  <TableCell dir="ltr" className="text-muted-foreground">
                    {s.closedAt ? new Date(s.closedAt).toLocaleString() : "—"}
                  </TableCell>
                  <TableCell>{money(s.openingCash)}</TableCell>
                  <TableCell>{money(s.expectedCash)}</TableCell>
                  <TableCell>{s.countedCash == null ? "—" : money(s.countedCash)}</TableCell>
                  <TableCell>
                    {s.difference == null ? (
                      "—"
                    ) : (
                      <span className={s.difference === 0 ? "" : s.difference > 0 ? "text-emerald-600" : "text-destructive"}>
                        {money(s.difference)}
                      </span>
                    )}
                  </TableCell>
                  <TableCell>{statusBadge(s.status)}</TableCell>
                  <TableCell>
                    <Button variant="ghost" size="icon" aria-label={t("details")} title={t("details")} onClick={() => viewReport(s)}>
                      <Eye className="h-4 w-4" />
                    </Button>
                    {s.status === "open" && canClose && (
                      <Button variant="ghost" size="icon" aria-label={t("closeShift")} title={t("closeShift")} onClick={() => startClose(s)}>
                        <StopCircle className="h-4 w-4" />
                      </Button>
                    )}
                  </TableCell>
                </TableRow>
              ))
            ) : (
              <TableRow>
                <TableCell colSpan={10} className="h-24 text-center">
                  {tc("noResults")}
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>

      <div className="flex items-center justify-between py-4">
        <span className="text-sm text-muted-foreground">{`${total}`}</span>
        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" onClick={() => setPage(page - 1)} disabled={page <= 1}>
            {tc("previous")}
          </Button>
          <Button variant="outline" size="sm" onClick={() => setPage(page + 1)} disabled={page >= pageCount}>
            {tc("next")}
          </Button>
        </div>
      </div>

      <Dialog open={openDialog} onOpenChange={setOpenDialog}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>{t("openDialogTitle")}</DialogTitle>
            <DialogDescription>{t("openDialogHint")}</DialogDescription>
          </DialogHeader>
          <label className="text-sm font-medium">{t("openingCash")}</label>
          <Input
            type="number"
            step="1"
            min="0"
            inputMode="decimal"
            value={openingCashInput}
            placeholder="0"
            onChange={(e) => setOpeningCashInput(e.target.value)}
          />
          <label className="text-sm font-medium">{t("notes")}</label>
          <Input value={notesInput} onChange={(e) => setNotesInput(e.target.value)} />
          <DialogFooter>
            <Button variant="outline" onClick={() => setOpenDialog(false)} disabled={opening}>
              {t("cancel")}
            </Button>
            <Button onClick={handleOpen} disabled={opening}>
              {opening && <Loader2 className="ml-2 h-4 w-4 animate-spin" />}
              {t("confirmOpen")}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={!!closeTarget} onOpenChange={(v) => !v && setCloseTarget(null)}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>{t("closeDialogTitle", { number: closeTarget?.number ?? 0 })}</DialogTitle>
            <DialogDescription>{t("closeDialogHint")}</DialogDescription>
          </DialogHeader>
          <Card>
            <CardContent className="space-y-1 pt-4 text-sm">
              <div className="flex justify-between"><span>{t("openingCash")}</span><span>{money(closeReport?.openingCash)}</span></div>
              <div className="flex justify-between"><span>{t("cashSales")}</span><span>+{money(closeReport?.cashSales)}</span></div>
              <div className="flex justify-between"><span>{t("saleRefunds")}</span><span>−{money(closeReport?.saleRefunds)}</span></div>
              <div className="flex justify-between"><span>{t("otherCashIn")}</span><span>+{money(closeReport?.otherCashIn)}</span></div>
              {(closeReport?.supplierPaymentsOut ?? 0) > 0 && (
                <div className="flex justify-between"><span>{t("supplierPaymentsOut")}</span><span>−{money(closeReport?.supplierPaymentsOut)}</span></div>
              )}
              {(closeReport?.supplierRefundsIn ?? 0) > 0 && (
                <div className="flex justify-between"><span>{t("supplierRefundsIn")}</span><span>+{money(closeReport?.supplierRefundsIn)}</span></div>
              )}
              <div className="flex justify-between border-t pt-1 font-bold">
                <span>{t("expectedCash")}</span><span>{money(closeReport?.expectedCash)}</span>
              </div>
            </CardContent>
          </Card>
          <label className="text-sm font-medium">{t("countedCash")}</label>
          <Input
            type="number"
            step="1"
            min="0"
            inputMode="decimal"
            value={countedInput}
            placeholder="0"
            onChange={(e) => setCountedInput(e.target.value)}
          />
          {countedInput !== "" && closeReport && !Number.isNaN(Number(countedInput)) && (
            <p className={`text-sm font-medium ${Number(countedInput) - closeReport.expectedCash === 0 ? "text-muted-foreground" : Number(countedInput) - closeReport.expectedCash > 0 ? "text-emerald-600" : "text-destructive"}`}>
              {t("differencePreview", { amount: (Number(countedInput) - closeReport.expectedCash).toFixed(2) })}
            </p>
          )}
          <DialogFooter>
            <Button variant="outline" onClick={() => setCloseTarget(null)} disabled={closing}>
              {t("cancel")}
            </Button>
            <Button onClick={handleClose} disabled={closing || !closeReport}>
              {closing && <Loader2 className="ml-2 h-4 w-4 animate-spin" />}
              {t("confirmClose")}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={!!reportFor} onOpenChange={(v) => !v && setReportFor(null)}>
        <DialogContent className="max-w-xl">
          <DialogHeader>
            <DialogTitle>{t("reportTitle", { number: reportFor?.number ?? 0 })}</DialogTitle>
          </DialogHeader>
          {reportLoading || !report ? (
            <div className="py-8 text-center"><Loader2 className="mx-auto h-6 w-6 animate-spin" /></div>
          ) : (
            <>
              <div className="grid grid-cols-2 gap-x-6 gap-y-1 rounded-md border p-3 text-sm">
                <div className="flex justify-between"><span>{t("openingCash")}</span><span>{money(report.openingCash)}</span></div>
                <div className="flex justify-between"><span>{t("cashSales")}</span><span>+{money(report.cashSales)}</span></div>
                <div className="flex justify-between"><span>{t("saleRefunds")}</span><span>−{money(report.saleRefunds)}</span></div>
                <div className="flex justify-between"><span>{t("otherCashIn")}</span><span>+{money(report.otherCashIn)}</span></div>
                <div className="flex justify-between"><span>{t("supplierPaymentsOut")}</span><span>−{money(report.supplierPaymentsOut)}</span></div>
                <div className="flex justify-between"><span>{t("supplierRefundsIn")}</span><span>+{money(report.supplierRefundsIn)}</span></div>
                <div className="col-span-2 flex justify-between border-t pt-1 font-bold">
                  <span>{t("expectedCash")}</span><span>{money(report.expectedCash)}</span>
                </div>
                {report.shift.countedCash != null && (
                  <>
                    <div className="flex justify-between"><span>{t("countedCash")}</span><span>{money(report.shift.countedCash)}</span></div>
                    <div className={`flex justify-between font-bold ${(report.shift.difference ?? 0) >= 0 ? "text-emerald-600" : "text-destructive"}`}>
                      <span>{t("difference")}</span><span>{money(report.shift.difference)}</span>
                    </div>
                  </>
                )}
              </div>
              <div className="max-h-64 overflow-y-auto rounded-md border">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>{t("date")}</TableHead>
                      <TableHead>{t("movement")}</TableHead>
                      <TableHead className="text-left">{t("amount")}</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {report.entries.map((e, i) => (
                      <TableRow key={i}>
                        <TableCell dir="ltr" className="whitespace-nowrap text-muted-foreground">
                          {new Date(e.date).toLocaleTimeString()}
                        </TableCell>
                        <TableCell>{e.description}</TableCell>
                        <TableCell className={`text-left font-medium ${e.amount < 0 ? "text-destructive" : ""}`}>
                          {e.amount.toFixed(2)}
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            </>
          )}
        </DialogContent>
      </Dialog>
    </div>
  )
}
