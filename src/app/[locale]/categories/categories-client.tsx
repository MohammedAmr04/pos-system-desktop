"use client"

import { Category } from "@/lib/api"
import { useEffect, useState } from "react"
import { useDebouncedCallback } from "@/hooks/use-debounced-callback"
import { useAuth } from "@/components/common/auth-context"
import { PERMISSIONS } from "@/lib/constants"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { api } from "@/lib/api"
import { Loader2, Plus, Pencil, FolderCheck, FolderMinus } from "lucide-react"
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
import { CategoryFormDialog } from "./_components/category-form-dialog"

interface CategoriesClientProps {
  items: Category[]
  total: number
  page: number
  pageSize: number
  loading: boolean
  query: string
  onQueryChange: (query: string) => void
  onPageChange: (page: number) => void
  onRefresh?: () => void
}

export function CategoriesClient({
  items,
  total,
  page,
  pageSize,
  loading,
  query,
  onQueryChange,
  onPageChange,
  onRefresh,
}: CategoriesClientProps) {
  const t = useTranslations("Categories")
  const tc = useTranslations("Common")
  const { hasPermission } = useAuth()
  const canCreate = hasPermission(PERMISSIONS.CATEGORIES_CREATE)
  const canUpdate = hasPermission(PERMISSIONS.CATEGORIES_UPDATE)
  const [searchInput, setSearchInput] = useState(query)
  const debouncedQueryChange = useDebouncedCallback(onQueryChange, 300)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [editingCategory, setEditingCategory] = useState<Category | null>(null)
  const [formSession, setFormSession] = useState(0)

  useEffect(() => {
    const value = searchInput.trim()
    if (value === query) return
    debouncedQueryChange(value)
  }, [debouncedQueryChange, query, searchInput])

  const pageCount = Math.max(1, Math.ceil(total / pageSize))

  const openCreate = () => {
    setEditingCategory(null)
    setFormSession((s) => s + 1)
    setIsFormOpen(true)
  }

  const openEdit = (category: Category) => {
    setEditingCategory(category)
    setFormSession((s) => s + 1)
    setIsFormOpen(true)
  }

  const handleToggleActive = async (category: Category) => {
    try {
      await api.categories.update(category.id, { isActive: !category.isActive })
      toast.success(category.isActive ? t("deactivated") : t("activated"))
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
            <Plus className="mr-2 h-4 w-4" /> {t("newCategory")}
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
              <TableHead>{t("desc")}</TableHead>
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
              items.map((category) => (
                <TableRow key={category.id}>
                  <TableCell className="font-medium">{category.name}</TableCell>
                  <TableCell className="text-muted-foreground">{category.description ?? "—"}</TableCell>
                  <TableCell>
                    <span
                      className={
                        category.isActive
                          ? "inline-flex items-center rounded-full bg-emerald-500/10 px-2 py-0.5 text-xs font-medium text-emerald-600"
                          : "inline-flex items-center rounded-full bg-destructive/10 px-2 py-0.5 text-xs font-medium text-destructive"
                      }
                    >
                      {category.isActive ? t("active") : t("inactive")}
                    </span>
                  </TableCell>
                  <TableCell>
                    {canUpdate && (
                      <div className="flex items-center gap-1">
                        <Button
                          variant="ghost"
                          size="icon"
                          aria-label={category.isActive ? t("deactivated") : t("activated")}
                          onClick={() => handleToggleActive(category)}
                        >
                          {category.isActive ? <FolderMinus className="h-4 w-4" /> : <FolderCheck className="h-4 w-4" />}
                        </Button>
                        <Button variant="ghost" size="icon" aria-label={t("editCategory")} onClick={() => openEdit(category)}>
                          <Pencil className="h-4 w-4" />
                        </Button>
                      </div>
                    )}
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

      <CategoryFormDialog
        key={formSession}
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        category={editingCategory}
        onSaved={() => onRefresh?.()}
      />
    </>
  )
}
