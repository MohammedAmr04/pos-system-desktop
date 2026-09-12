"use client"

import { useTranslations } from "next-intl"
import { toast } from "sonner"
import { useAuth } from "@/components/common/auth-context"
import { AccessDenied } from "@/components/common/access-denied"
import { PERMISSIONS } from "@/lib/constants"
import { useBackups } from "@/hooks/use-operations"
import { createDatabaseBackup, restoreDatabaseBackup } from "@/actions/operations.actions"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { TableBuilder, TableColumn } from "@/components/common/table-builder"
import { BackupSummary } from "@/types/domain/domain.types"

function formatBytes(value: number) {
  if (value < 1024) return `${value} B`
  if (value < 1024 * 1024) return `${(value / 1024).toFixed(1)} KB`
  return `${(value / (1024 * 1024)).toFixed(1)} MB`
}

export default function BackupsPage() {
  const t = useTranslations("Backups")
  const { hasPermission } = useAuth()
  const { data = [], isPending } = useBackups()

  if (!hasPermission(PERMISSIONS.BACKUPS_MANAGE)) return <AccessDenied />

  const create = async () => {
    try { await createDatabaseBackup(); toast.success(t("created")) } catch { toast.error(t("failed")) }
  }

  const restore = async (backup: BackupSummary) => {
    if (!window.confirm(t("confirmRestore"))) return
    try { await restoreDatabaseBackup(backup.fileName); toast.success(t("restored")) } catch { toast.error(t("failed")) }
  }

  const columns: TableColumn<BackupSummary>[] = [
    { key: "fileName", header: t("file"), cell: (x) => <span dir="ltr">{x.fileName}</span> },
    { key: "createdAt", header: t("date"), cell: (x) => <span dir="ltr">{new Date(x.createdAt).toLocaleString()}</span> },
    { key: "sizeBytes", header: t("size"), cell: (x) => <span dir="ltr">{formatBytes(x.sizeBytes)}</span> },
    { key: "actions", header: "", cell: (x) => <Button variant="outline" onClick={() => restore(x)}>{t("restore")}</Button> },
  ]

  return (
    <div className="flex-1 space-y-4 pt-6">
      <div className="flex items-center justify-between gap-4">
        <h2 className="text-3xl font-bold">{t("title")}</h2>
        <Button onClick={create}>{t("create")}</Button>
      </div>
      <Card>
        <CardHeader><CardTitle>{t("title")}</CardTitle></CardHeader>
        <CardContent><TableBuilder columns={columns} data={data} rowKey={(x) => x.fileName} loading={isPending} emptyMessage={t("empty")} /></CardContent>
      </Card>
    </div>
  )
}
