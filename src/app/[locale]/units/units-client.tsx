"use client"

import { useEffect, useState } from "react"
import { useTranslations } from "next-intl"
import { toast } from "sonner"
import { CircleCheck, CircleX, Pencil, Plus, Trash } from "lucide-react"

import { MasterUnit } from "@/types/domain/domain.types"
import { useUnitsPage } from "@/hooks/use-units"
import { deleteUnit, updateUnit } from "@/actions/units.actions"
import { useDebouncedCallback } from "@/hooks/use-debounced-callback"
import { useAuth } from "@/components/common/auth-context"
import { PERMISSIONS } from "@/lib/constants"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { TableColumn, TableBuilder } from "@/components/common/table-builder"
import { DataPagination } from "@/components/common/data-pagination"
import { TooltipIconButton } from "@/components/common/tooltip-icon-button"
import { UnitFormDialog } from "./_components/unit-form-dialog"

const PAGE_SIZE = 20

export function UnitsClient() {
  const t = useTranslations("UnitsMaster")
  const tc = useTranslations("Common")
  const { hasPermission } = useAuth()
  const canCreate = hasPermission(PERMISSIONS.UNITS_CREATE)
  const canUpdate = hasPermission(PERMISSIONS.UNITS_UPDATE)
  const canDelete = hasPermission(PERMISSIONS.UNITS_DELETE)

  const [page, setPage] = useState(1)
  const [query, setQuery] = useState("")
  const [searchInput, setSearchInput] = useState("")
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [editingUnit, setEditingUnit] = useState<MasterUnit | null>(null)
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

  const { data, isPending } = useUnitsPage(page, PAGE_SIZE, { q: query })
  const items = data?.items ?? []
  const total = data?.total ?? 0

  const openCreate = () => {
    setEditingUnit(null)
    setFormSession((s) => s + 1)
    setIsFormOpen(true)
  }

  const openEdit = (unit: MasterUnit) => {
    setEditingUnit(unit)
    setFormSession((s) => s + 1)
    setIsFormOpen(true)
  }

  const handleToggleActive = async (unit: MasterUnit) => {
    try {
      await updateUnit(unit.id, { name: unit.name, isActive: !unit.isActive })
      toast.success(unit.isActive ? t("deactivated") : t("activated"))
    } catch (e) {
      toast.error((e as Error).message || t("saveFailed"))
    }
  }

  const handleDelete = async (unit: MasterUnit) => {
    if (!confirm(t("deleteConfirmDescription"))) return
    try {
      await deleteUnit(unit.id)
      toast.success(t("deleted"))
    } catch (e) {
      toast.error((e as Error).message || t("saveFailed"))
    }
  }

  const columns: TableColumn<MasterUnit>[] = [
    {
      key: "name",
      header: t("name"),
      cell: (unit) => <span className="font-medium">{unit.name}</span>,
    },
    {
      key: "status",
      header: t("status"),
      cell: (unit) => (
        <span
          className={
            unit.isActive
              ? "inline-flex items-center rounded-full bg-emerald-500/10 px-2 py-0.5 text-xs font-medium text-emerald-600"
              : "inline-flex items-center rounded-full bg-destructive/10 px-2 py-0.5 text-xs font-medium text-destructive"
          }
        >
          {unit.isActive ? t("active") : t("inactive")}
        </span>
      ),
    },
    {
      key: "actions",
      header: t("actions"),
      headClassName: "w-32",
      cell: (unit) =>
        (canUpdate || canDelete) && (
          <div className="flex items-center gap-1">
            {canUpdate && (
              <>
                <TooltipIconButton
                  label={unit.isActive ? t("deactivate") : t("activate")}
                  onClick={() => handleToggleActive(unit)}
                >
                  {unit.isActive ? <CircleX className="h-4 w-4" /> : <CircleCheck className="h-4 w-4" />}
                </TooltipIconButton>
                <TooltipIconButton label={t("editUnit")} onClick={() => openEdit(unit)}>
                  <Pencil className="h-4 w-4" />
                </TooltipIconButton>
              </>
            )}
            {canDelete && (
              <TooltipIconButton label={t("confirmDelete")} onClick={() => handleDelete(unit)}>
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
            <Plus className="mr-2 h-4 w-4" /> {t("newUnit")}
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
        rowKey={(unit) => unit.id}
        loading={isPending}
        emptyMessage={query ? t("noResults") : t("empty")}
      />

      <DataPagination page={page} pageSize={PAGE_SIZE} total={total} onPageChange={setPage} />

      <UnitFormDialog
        key={formSession}
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        unit={editingUnit}
      />
    </>
  )
}
