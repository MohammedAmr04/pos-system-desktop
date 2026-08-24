"use client"

import { useEffect, useState } from "react"
import { useTranslations } from "next-intl"
import { toast } from "sonner"
import { Loader2, Plus, Settings2 } from "lucide-react"
import { api, type Expense, type ExpenseCategory } from "@/lib/api"
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
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Switch } from "@/components/ui/switch"

const PAGE_SIZE = 20

const selectClass =
  "flex h-9 w-full min-w-0 rounded-lg border border-input bg-transparent px-2.5 py-1 text-base transition-colors outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 md:text-sm dark:bg-input/30"

export function ExpensesClient() {
  const t = useTranslations("Expenses")
  const tc = useTranslations("Common")
  const { hasPermission } = useAuth()
  const canView = hasPermission(PERMISSIONS.EXPENSES_VIEW)
  const canCreate = hasPermission(PERMISSIONS.EXPENSES_CREATE)
  const canManageCategories = hasPermission(PERMISSIONS.EXPENSES_CATEGORIES)

  const [expenses, setExpenses] = useState<Expense[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [loading, setLoading] = useState(true)
  const [categories, setCategories] = useState<ExpenseCategory[]>([])
  const [reloadKey, setReloadKey] = useState(0)

  const [createOpen, setCreateOpen] = useState(false)
  const [categoryInput, setCategoryInput] = useState("")
  const [amountInput, setAmountInput] = useState("")
  const [descriptionInput, setDescriptionInput] = useState("")
  const [referenceInput, setReferenceInput] = useState("")
  const [creating, setCreating] = useState(false)

  const [catsOpen, setCatsOpen] = useState(false)
  const [newCatName, setNewCatName] = useState("")
  const [renamingId, setRenamingId] = useState<string | null>(null)
  const [renameValue, setRenameValue] = useState("")

  const refresh = () => {
    setLoading(true)
    setReloadKey((k) => k + 1)
  }

  useEffect(() => {
    if (!canView) return
    let cancelled = false
    Promise.all([
      api.expenses.listPaged(page, PAGE_SIZE),
      api.expenses.categories(true),
    ])
      .then(([paged, cats]) => {
        if (cancelled) return
        setExpenses(paged.items)
        setTotal(paged.total)
        setCategories(cats)
      })
      .catch(() => {
        if (!cancelled) toast.error(t("loadFailed"))
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [page, reloadKey, canView])

  const pageCount = Math.max(1, Math.ceil(total / PAGE_SIZE))
  const activeCats = categories.filter((c) => c.isActive)

  const handleCreate = async () => {
    const amount = Number(amountInput)
    if (!categoryInput) {
      toast.error(t("invalidCategory"))
      return
    }
    if (Number.isNaN(amount) || amount <= 0) {
      toast.error(t("invalidAmount"))
      return
    }
    setCreating(true)
    try {
      await api.expenses.create({
        categoryId: categoryInput,
        amount,
        paymentMethod: "cash",
        description: descriptionInput.trim() || undefined,
        reference: referenceInput.trim() || undefined,
      })
      toast.success(t("created"))
      setCreateOpen(false)
      setCategoryInput("")
      setAmountInput("")
      setDescriptionInput("")
      setReferenceInput("")
      refresh()
    } catch (e) {
      toast.error((e as Error).message || t("createFailed"))
    } finally {
      setCreating(false)
    }
  }

  const handleAddCategory = async () => {
    const name = newCatName.trim()
    if (!name) return
    try {
      await api.expenses.createCategory(name)
      setNewCatName("")
      const cats = await api.expenses.categories(true)
      setCategories(cats)
    } catch (e) {
      toast.error((e as Error).message || t("categorySaveFailed"))
    }
  }

  const handleRename = async (cat: ExpenseCategory) => {
    const name = renameValue.trim()
    setRenamingId(null)
    if (!name || name === cat.name) return
    try {
      await api.expenses.updateCategory(cat.id, { name, isActive: cat.isActive })
      const cats = await api.expenses.categories(true)
      setCategories(cats)
    } catch (e) {
      toast.error((e as Error).message || t("categorySaveFailed"))
    }
  }

  const handleToggle = async (cat: ExpenseCategory, isActive: boolean) => {
    try {
      await api.expenses.updateCategory(cat.id, { name: cat.name, isActive })
      const cats = await api.expenses.categories(true)
      setCategories(cats)
    } catch (e) {
      toast.error((e as Error).message || t("categorySaveFailed"))
    }
  }

  if (!canView) return <AccessDenied />

  const money = (v: number | null | undefined) => (v ?? 0).toFixed(2)
  const fmtDate = (d: string | null | undefined) =>
    d ? new Date(d).toLocaleString("ar-EG", { dateStyle: "short", timeStyle: "short" }) : "—"
  const catName = (id: string) => categories.find((c) => c.id === id)?.name ?? id

  return (
    <div className="flex-1 space-y-4 pt-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-3xl font-bold tracking-tight">{t("title")}</h2>
        <div className="flex items-center gap-2">
          {canManageCategories && (
            <Button variant="outline" onClick={() => setCatsOpen(true)}>
              <Settings2 className="ml-2 h-4 w-4" />
              {t("manageCategories")}
            </Button>
          )}
          {canCreate && (
            <Button onClick={() => setCreateOpen(true)}>
              <Plus className="ml-2 h-4 w-4" />
              {t("newExpense")}
            </Button>
          )}
        </div>
      </div>

      <div className="rounded-md border bg-card">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{t("date")}</TableHead>
              <TableHead>{t("category")}</TableHead>
              <TableHead>{t("description")}</TableHead>
              <TableHead>{t("method")}</TableHead>
              <TableHead>{t("amount")}</TableHead>
              <TableHead>{t("recordedBy")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              <TableRow>
                <TableCell colSpan={6} className="h-24 text-center">
                  <Loader2 className="mx-auto h-6 w-6 animate-spin" />
                </TableCell>
              </TableRow>
            ) : expenses.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6} className="h-24 text-center text-muted-foreground">
                  {tc("noResults")}
                </TableCell>
              </TableRow>
            ) : (
              expenses.map((exp) => (
                <TableRow key={exp.id}>
                  <TableCell className="whitespace-nowrap">{fmtDate(exp.date)}</TableCell>
                  <TableCell>{exp.categoryName ?? catName(exp.categoryId)}</TableCell>
                  <TableCell className="max-w-xs">
                    <span className="line-clamp-1">{exp.description || "—"}</span>
                    {exp.reference && (
                      <span className="block text-xs text-muted-foreground">{exp.reference}</span>
                    )}
                  </TableCell>
                  <TableCell>{t(`method_${exp.paymentMethod}`)}</TableCell>
                  <TableCell className="font-medium text-red-600">-{money(exp.amount)}</TableCell>
                  <TableCell>{exp.createdBy ?? "—"}</TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      {pageCount > 1 && (
        <div className="flex items-center justify-end gap-2">
          <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => setPage(page - 1)}>
            {tc("previous")}
          </Button>
          <span className="text-sm text-muted-foreground">
            {page} / {pageCount}
          </span>
          <Button
            variant="outline"
            size="sm"
            disabled={page >= pageCount}
            onClick={() => setPage(page + 1)}
          >
            {tc("next")}
          </Button>
        </div>
      )}

      {/* New expense dialog */}
      <Dialog open={createOpen} onOpenChange={setCreateOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{t("newExpenseTitle")}</DialogTitle>
            <DialogDescription>{t("newExpenseHint")}</DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-2">
              <label className="text-sm font-medium">{t("category")}</label>
              <select
                className={selectClass}
                value={categoryInput}
                onChange={(e) => setCategoryInput(e.target.value)}
              >
                <option value="">{t("selectCategory")}</option>
                {activeCats.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.name}
                  </option>
                ))}
              </select>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <label className="text-sm font-medium" htmlFor="expense-amount">
                  {t("amount")}
                </label>
                <Input
                  id="expense-amount"
                  type="number"
                  step="0.01"
                  min="0.01"
                  value={amountInput}
                  onChange={(e) => setAmountInput(e.target.value)}
                  autoFocus
                />
              </div>
              <div className="space-y-2">
                <label className="text-sm font-medium">{t("method")}</label>
                <div className="flex h-9 items-center rounded-lg border bg-muted/40 px-3 text-sm text-muted-foreground">
                  {t("method_cash")}
                </div>
              </div>
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium" htmlFor="expense-desc">
                {t("description")}
              </label>
              <Input
                id="expense-desc"
                value={descriptionInput}
                onChange={(e) => setDescriptionInput(e.target.value)}
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium" htmlFor="expense-ref">
                {t("reference")}
              </label>
              <Input
                id="expense-ref"
                value={referenceInput}
                onChange={(e) => setReferenceInput(e.target.value)}
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setCreateOpen(false)} disabled={creating}>
              {tc("cancel")}
            </Button>
            <Button onClick={handleCreate} disabled={creating}>
              {creating && <Loader2 className="ml-2 h-4 w-4 animate-spin" />}
              {t("save")}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Categories manager dialog */}
      <Dialog open={catsOpen} onOpenChange={setCatsOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{t("categoriesTitle")}</DialogTitle>
            <DialogDescription>{t("categoriesHint")}</DialogDescription>
          </DialogHeader>
          <div className="max-h-72 space-y-2 overflow-y-auto">
            {categories.map((c) => (
              <div key={c.id} className="flex items-center gap-2 rounded-md border px-3 py-2">
                {renamingId === c.id ? (
                  <>
                    <Input
                      value={renameValue}
                      onChange={(e) => setRenameValue(e.target.value)}
                      onKeyDown={(e) => e.key === "Enter" && handleRename(c)}
                      autoFocus
                    />
                    <Button size="sm" variant="outline" onClick={() => handleRename(c)}>
                      ✓
                    </Button>
                  </>
                ) : (
                  <>
                    <button
                      type="button"
                      className={`flex-1 truncate text-right text-sm ${c.isActive ? "" : "text-muted-foreground line-through"}`}
                      onClick={() => {
                        setRenamingId(c.id)
                        setRenameValue(c.name)
                      }}
                    >
                      {c.name}
                    </button>
                    <Switch checked={c.isActive} onCheckedChange={(v) => handleToggle(c, v)} />
                  </>
                )}
              </div>
            ))}
          </div>
          <div className="flex gap-2">
            <Input
              value={newCatName}
              onChange={(e) => setNewCatName(e.target.value)}
              onKeyDown={(e) => e.key === "Enter" && handleAddCategory()}
              placeholder={t("newCategoryPlaceholder")}
            />
            <Button variant="outline" onClick={handleAddCategory}>
              <Plus className="ml-1 h-4 w-4" />
              {t("add")}
            </Button>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setCatsOpen(false)}>
              {tc("cancel")}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
