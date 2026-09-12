"use client"
import { useState } from "react"
import { useTranslations } from "next-intl"
import { useAuditLogs } from "@/hooks/use-operations"
import { useAuth } from "@/components/common/auth-context"
import { PERMISSIONS } from "@/lib/constants"
import { AccessDenied } from "@/components/common/access-denied"
import { TableBuilder, TableColumn } from "@/components/common/table-builder"
import { DataPagination } from "@/components/common/data-pagination"
import { AuditLogEntry } from "@/types/domain/domain.types"

const PAGE_SIZE = 20

export default function AuditLogsPage() {
  const t = useTranslations("AuditLogs")
  const { hasPermission } = useAuth()
  const [page, setPage] = useState(1)
  const { data, isPending } = useAuditLogs(page)
  if (!hasPermission(PERMISSIONS.AUDIT_VIEW)) return <AccessDenied />
  const cols: TableColumn<AuditLogEntry>[] = [
    { key: "createdAt", header: t("date"), cell: (x) => <span dir="ltr">{new Date(x.createdAt).toLocaleString()}</span> },
    { key: "action", header: t("action"), cell: (x) => x.action },
    { key: "entityType", header: t("module"), cell: (x) => x.entityType },
    { key: "summary", header: t("details"), cell: (x) => x.summary },
    { key: "actor", header: t("user"), cell: (x) => x.actorUserId ?? "—" },
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
