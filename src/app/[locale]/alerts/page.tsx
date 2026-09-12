"use client"
import { useState } from "react"
import { useTranslations } from "next-intl"
import { useAlerts } from "@/hooks/use-operations"
import { acknowledgeAlertItem } from "@/actions/operations.actions"
import { useAuth } from "@/components/common/auth-context"
import { PERMISSIONS } from "@/lib/constants"
import { AccessDenied } from "@/components/common/access-denied"
import { Button } from "@/components/ui/button"
import { TableBuilder, TableColumn } from "@/components/common/table-builder"
import { DataPagination } from "@/components/common/data-pagination"
import { AlertItem } from "@/types/domain/domain.types"

const PAGE_SIZE = 20

export default function AlertsPage() {
  const t = useTranslations("Alerts")
  const { hasPermission } = useAuth()
  const [page, setPage] = useState(1)
  const { data, isPending } = useAlerts(page)
  if (!hasPermission(PERMISSIONS.ALERTS_VIEW)) return <AccessDenied />
  const cols: TableColumn<AlertItem>[] = [
    { key: "message", header: t("alert"), cell: (x) => x.message },
    { key: "severity", header: t("severity"), cell: (x) => x.severity },
    {
      key: "created",
      header: t("date"),
      cell: (x) => <span dir="ltr">{new Date(x.createdAt).toLocaleString()}</span>,
    },
    {
      key: "action",
      header: t("actions"),
      cell: (x) =>
        hasPermission(PERMISSIONS.ALERTS_ACKNOWLEDGE) && (
          <Button size="sm" onClick={() => void acknowledgeAlertItem(x.id)}>
            {t("acknowledge")}
          </Button>
        ),
    },
  ]
  return (
    <div className="flex-1 space-y-4 pt-6">
      <h2 className="text-3xl font-bold">{t("title")}</h2>
      <TableBuilder
        columns={cols}
        data={data?.items ?? []}
        rowKey={(x) => x.id}
        loading={isPending}
        emptyMessage={t("empty")}
      />
      <DataPagination page={page} pageSize={PAGE_SIZE} total={data?.total ?? 0} onPageChange={setPage} />
    </div>
  )
}
