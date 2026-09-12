"use client"

import { useEffect, useState } from "react"
import { useTranslations } from "next-intl"
import { useApiError } from "@/lib/api-error"
import { toast } from "sonner"
import { CircleCheck, CircleX, Pencil, Plus } from "lucide-react"

import { Employee } from "@/types/domain/domain.types"
import { useEmployeesPage } from "@/hooks/use-employees"
import { updateEmployee } from "@/actions/employees.actions"
import { useDebouncedCallback } from "@/hooks/use-debounced-callback"
import { useAuth } from "@/components/common/auth-context"
import { PERMISSIONS } from "@/lib/constants"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { TableColumn, TableBuilder } from "@/components/common/table-builder"
import { DataPagination } from "@/components/common/data-pagination"
import { TooltipIconButton } from "@/components/common/tooltip-icon-button"
import { EmployeeFormDialog } from "./_components/employee-form-dialog"

const PAGE_SIZE = 20

export function EmployeesClient() {
  const t = useTranslations("Employees")
  const tc = useTranslations("Common")
  const resolveError = useApiError()
  const { hasPermission } = useAuth()
  const canManage = hasPermission(PERMISSIONS.EMPLOYEES_MANAGE)

  const [page, setPage] = useState(1)
  const [query, setQuery] = useState("")
  const [searchInput, setSearchInput] = useState("")
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [editingEmployee, setEditingEmployee] = useState<Employee | null>(null)
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

  const { data, isPending } = useEmployeesPage(page, PAGE_SIZE, { q: query })
  const items = data?.items ?? []
  const total = data?.total ?? 0

  const openCreate = () => {
    setEditingEmployee(null)
    setFormSession((s) => s + 1)
    setIsFormOpen(true)
  }

  const openEdit = (employee: Employee) => {
    setEditingEmployee(employee)
    setFormSession((s) => s + 1)
    setIsFormOpen(true)
  }

  const handleToggleActive = async (employee: Employee) => {
    try {
      await updateEmployee(employee.id, { isActive: !employee.isActive })
      toast.success(employee.isActive ? t("deactivated") : t("activated"))
    } catch (e) {
      toast.error(resolveError(e) || t("saveFailed"))
    }
  }

  const columns: TableColumn<Employee>[] = [
    {
      key: "name",
      header: t("name"),
      cell: (employee) => <span className="font-medium">{employee.name}</span>,
    },
    {
      key: "phone",
      header: t("phone"),
      cell: (employee) => (
        <span dir="ltr" className="text-muted-foreground">{employee.phone ?? "—"}</span>
      ),
    },
    {
      key: "status",
      header: t("status"),
      cell: (employee) => (
        <span
          className={
            employee.isActive
              ? "inline-flex items-center rounded-full bg-emerald-500/10 px-2 py-0.5 text-xs font-medium text-emerald-600"
              : "inline-flex items-center rounded-full bg-destructive/10 px-2 py-0.5 text-xs font-medium text-destructive"
          }
        >
          {employee.isActive ? t("active") : t("inactive")}
        </span>
      ),
    },
    {
      key: "actions",
      header: t("actions"),
      headClassName: "w-40",
      cell: (employee) => (
        <div className="flex items-center gap-1">
          {canManage && (
            <>
              <TooltipIconButton
                label={employee.isActive ? t("deactivate") : t("activate")}
                onClick={() => handleToggleActive(employee)}
              >
                {employee.isActive ? <CircleX className="h-4 w-4" /> : <CircleCheck className="h-4 w-4" />}
              </TooltipIconButton>
              <TooltipIconButton label={t("editEmployee")} onClick={() => openEdit(employee)}>
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
        {canManage && (
          <Button onClick={openCreate}>
            <Plus className="mr-2 h-4 w-4" /> {t("newEmployee")}
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
        rowKey={(employee) => employee.id}
        loading={isPending}
        emptyMessage={query ? t("noResults") : t("empty")}
      />

      <DataPagination page={page} pageSize={PAGE_SIZE} total={total} onPageChange={setPage} />

      <EmployeeFormDialog
        key={formSession}
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        employee={editingEmployee}
      />
    </>
  )
}
