"use client"

import { useEffect, useState } from "react"
import { useTranslations } from "next-intl"
import { toast } from "sonner"
import { FolderCheck, FolderMinus, Pencil, Plus } from "lucide-react"

import { Category } from "@/types/domain/domain.types"
import { useCategoriesPage } from "@/hooks/use-categories"
import { updateCategory } from "@/actions/categories.actions"
import { useDebouncedCallback } from "@/hooks/use-debounced-callback"
import { useAuth } from "@/components/common/auth-context"
import { PERMISSIONS } from "@/lib/constants"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { TableColumn, TableBuilder } from "@/components/common/table-builder"
import { DataPagination } from "@/components/common/data-pagination"
import { TooltipIconButton } from "@/components/common/tooltip-icon-button"
import { CategoryFormDialog } from "./_components/category-form-dialog"

const PAGE_SIZE = 20

export function CategoriesClient() {
  const t = useTranslations("Categories")
  const tc = useTranslations("Common")
  const { hasPermission } = useAuth()
  const canCreate = hasPermission(PERMISSIONS.CATEGORIES_CREATE)
  const canUpdate = hasPermission(PERMISSIONS.CATEGORIES_UPDATE)

  const [page, setPage] = useState(1)
  const [query, setQuery] = useState("")
  const [searchInput, setSearchInput] = useState("")
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [editingCategory, setEditingCategory] = useState<Category | null>(null)
  const [formSession, setFormSession] = useState(0)

  const debouncedQueryChange = useDebouncedCallback((value: string) => {
    setQuery(value)
    setPage(1)
  }, 300)

  useEffect(() => {
    const value = searchInput.trim()
    if (value === query) return
    debouncedQueryChange(value)
  }, [debouncedQueryChange, query, searchInput])

  const { data, isPending } = useCategoriesPage(page, PAGE_SIZE, { q: query })
  const items = data?.items ?? []
  const total = data?.total ?? 0

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
      await updateCategory(category.id, { isActive: !category.isActive })
      toast.success(category.isActive ? t("deactivated") : t("activated"))
    } catch (e) {
      toast.error((e as Error).message || t("saveFailed"))
    }
  }

  const columns: TableColumn<Category>[] = [
    {
      key: "name",
      header: t("name"),
      cell: (category) => <span className="font-medium">{category.name}</span>,
    },
    {
      key: "description",
      header: t("desc"),
      cell: (category) => (
        <span className="text-muted-foreground">{category.description ?? "—"}</span>
      ),
    },
    {
      key: "status",
      header: t("status"),
      cell: (category) => (
        <span
          className={
            category.isActive
              ? "inline-flex items-center rounded-full bg-emerald-500/10 px-2 py-0.5 text-xs font-medium text-emerald-600"
              : "inline-flex items-center rounded-full bg-destructive/10 px-2 py-0.5 text-xs font-medium text-destructive"
          }
        >
          {category.isActive ? t("active") : t("inactive")}
        </span>
      ),
    },
    {
      key: "actions",
      header: t("actions"),
      headClassName: "w-24",
      cell: (category) =>
        canUpdate && (
          <div className="flex items-center gap-1">
            <TooltipIconButton
              label={category.isActive ? t("deactivate") : t("activate")}
              onClick={() => handleToggleActive(category)}
            >
              {category.isActive ? <FolderMinus className="h-4 w-4" /> : <FolderCheck className="h-4 w-4" />}
            </TooltipIconButton>
            <TooltipIconButton label={t("editCategory")} onClick={() => openEdit(category)}>
              <Pencil className="h-4 w-4" />
            </TooltipIconButton>
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

      <TableBuilder
        columns={columns}
        data={items}
        rowKey={(category) => category.id}
        loading={isPending}
        emptyMessage={query ? t("noResults") : t("empty")}
      />

      <DataPagination page={page} pageSize={PAGE_SIZE} total={total} onPageChange={setPage} />

      <CategoryFormDialog
        key={formSession}
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        category={editingCategory}
      />
    </>
  )
}
