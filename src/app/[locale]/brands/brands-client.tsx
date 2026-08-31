"use client"

import { useEffect, useState } from "react"
import { useTranslations } from "next-intl"
import { useApiError } from "@/lib/api-error"
import { toast } from "sonner"
import { CircleCheck, CircleX, Pencil, Plus, Trash } from "lucide-react"

import { Brand } from "@/types/domain/domain.types"
import { useBrandsPage } from "@/hooks/use-brands"
import { deleteBrand, updateBrand } from "@/actions/brands.actions"
import { useDebouncedCallback } from "@/hooks/use-debounced-callback"
import { useAuth } from "@/components/common/auth-context"
import { PERMISSIONS } from "@/lib/constants"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { TableColumn, TableBuilder } from "@/components/common/table-builder"
import { DataPagination } from "@/components/common/data-pagination"
import { TooltipIconButton } from "@/components/common/tooltip-icon-button"
import { BrandFormDialog } from "./_components/brand-form-dialog"

const PAGE_SIZE = 20

export function BrandsClient() {
  const t = useTranslations("Brands")
  const tc = useTranslations("Common")
  const resolveError = useApiError()
  const { hasPermission } = useAuth()
  const canCreate = hasPermission(PERMISSIONS.BRANDS_CREATE)
  const canUpdate = hasPermission(PERMISSIONS.BRANDS_UPDATE)
  const canDelete = hasPermission(PERMISSIONS.BRANDS_DELETE)

  const [page, setPage] = useState(1)
  const [query, setQuery] = useState("")
  const [searchInput, setSearchInput] = useState("")
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [editingBrand, setEditingBrand] = useState<Brand | null>(null)
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

  const { data, isPending } = useBrandsPage(page, PAGE_SIZE, { q: query })
  const items = data?.items ?? []
  const total = data?.total ?? 0

  const openCreate = () => {
    setEditingBrand(null)
    setFormSession((s) => s + 1)
    setIsFormOpen(true)
  }

  const openEdit = (brand: Brand) => {
    setEditingBrand(brand)
    setFormSession((s) => s + 1)
    setIsFormOpen(true)
  }

  const handleToggleActive = async (brand: Brand) => {
    try {
      await updateBrand(brand.id, { isActive: !brand.isActive })
      toast.success(brand.isActive ? t("deactivated") : t("activated"))
    } catch (e) {
      toast.error(resolveError(e) || t("saveFailed"))
    }
  }

  const handleDelete = async (brand: Brand) => {
    if (!confirm(t("deleteConfirmDescription"))) return
    try {
      await deleteBrand(brand.id)
      toast.success(t("deleted"))
    } catch (e) {
      toast.error(resolveError(e) || t("saveFailed"))
    }
  }

  const columns: TableColumn<Brand>[] = [
    {
      key: "name",
      header: t("name"),
      cell: (brand) => <span className="font-medium">{brand.name}</span>,
    },
    {
      key: "status",
      header: t("status"),
      cell: (brand) => (
        <span
          className={
            brand.isActive
              ? "inline-flex items-center rounded-full bg-emerald-500/10 px-2 py-0.5 text-xs font-medium text-emerald-600"
              : "inline-flex items-center rounded-full bg-destructive/10 px-2 py-0.5 text-xs font-medium text-destructive"
          }
        >
          {brand.isActive ? t("active") : t("inactive")}
        </span>
      ),
    },
    {
      key: "actions",
      header: t("actions"),
      headClassName: "w-32",
      cell: (brand) =>
        (canUpdate || canDelete) && (
          <div className="flex items-center gap-1">
            {canUpdate && (
              <>
                <TooltipIconButton
                  label={brand.isActive ? t("deactivate") : t("activate")}
                  onClick={() => handleToggleActive(brand)}
                >
                  {brand.isActive ? <CircleX className="h-4 w-4" /> : <CircleCheck className="h-4 w-4" />}
                </TooltipIconButton>
                <TooltipIconButton label={t("editBrand")} onClick={() => openEdit(brand)}>
                  <Pencil className="h-4 w-4" />
                </TooltipIconButton>
              </>
            )}
            {canDelete && (
              <TooltipIconButton label={t("confirmDelete")} onClick={() => handleDelete(brand)}>
                <Trash className="h-4 w-4 text-destructive" />
              </TooltipIconButton>
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
            <Plus className="mr-2 h-4 w-4" /> {t("newBrand")}
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
        rowKey={(brand) => brand.id}
        loading={isPending}
        emptyMessage={query ? t("noResults") : t("empty")}
      />

      <DataPagination page={page} pageSize={PAGE_SIZE} total={total} onPageChange={setPage} />

      <BrandFormDialog
        key={formSession}
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        brand={editingBrand}
      />
    </>
  )
}
