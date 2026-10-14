"use client"

import { useQueryClient } from "@tanstack/react-query"
import { useTranslations } from "next-intl"
import { Loader2, RefreshCw } from "lucide-react"
import { useAuth } from "@/components/common/auth-context"
import { AccessDenied } from "@/components/common/access-denied"
import { TooltipIconButton } from "@/components/common/tooltip-icon-button"
import { PERMISSIONS } from "@/lib/constants"
import { syncKeys, usePrintQueueStatus, useSyncStatus } from "@/hooks/use-sync"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"

function formatDate(value?: string | null) {
  return value ? new Date(value).toLocaleString(undefined, { dateStyle: "short", timeStyle: "short" }) : null
}

function Stat({ label, value, tone }: { label: string; value: string; tone?: "default" | "danger" }) {
  return (
    <div className="flex items-center justify-between gap-4 rounded-md border px-3 py-2.5">
      <span className="text-sm text-muted-foreground">{label}</span>
      <span dir="ltr" className={tone === "danger" ? "text-sm font-semibold text-destructive" : "text-sm font-medium"}>
        {value}
      </span>
    </div>
  )
}

export function SyncStatusClient() {
  const t = useTranslations("SyncStatus")
  const { hasPermission } = useAuth()
  const queryClient = useQueryClient()
  const sync = useSyncStatus()
  const print = usePrintQueueStatus()

  if (!hasPermission(PERMISSIONS.SETTINGS_VIEW)) return <AccessDenied />

  const loading = sync.isPending || print.isPending
  const failed = sync.isError || print.isError

  const refresh = () => queryClient.invalidateQueries({ queryKey: syncKeys.all })

  return (
    <div className="flex-1 space-y-4 pt-6 max-w-3xl">
      <div className="flex items-center justify-between gap-4">
        <h2 className="text-3xl font-bold tracking-tight">{t("title")}</h2>
        <TooltipIconButton label={t("refresh")} onClick={refresh} size="icon">
          <RefreshCw className="h-4 w-4" />
        </TooltipIconButton>
      </div>

      {loading ? (
        <div className="flex h-40 items-center justify-center">
          <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
        </div>
      ) : failed ? (
        <div className="flex h-40 items-center justify-center">
          <p className="text-sm text-muted-foreground">{t("loadFailed")}</p>
        </div>
      ) : (
        <div className="grid gap-4 sm:grid-cols-2">
          <Card>
            <CardHeader>
              <CardTitle>{t("branchSync")}</CardTitle>
            </CardHeader>
            <CardContent className="space-y-2">
              <Stat label={t("pending")} value={String(sync.data?.pending ?? 0)} />
              <Stat
                label={t("failed")}
                value={String(sync.data?.failed ?? 0)}
                tone={(sync.data?.failed ?? 0) > 0 ? "danger" : "default"}
              />
              <Stat label={t("lastSyncedAt")} value={formatDate(sync.data?.lastSyncedAt) ?? t("never")} />
              <Stat label={t("lastOccurredAt")} value={formatDate(sync.data?.lastOccurredAt) ?? t("never")} />
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>{t("printQueue")}</CardTitle>
            </CardHeader>
            <CardContent className="space-y-2">
              <Stat label={t("pending")} value={String(print.data?.pending ?? 0)} />
              <Stat
                label={t("failed")}
                value={String(print.data?.failed ?? 0)}
                tone={(print.data?.failed ?? 0) > 0 ? "danger" : "default"}
              />
            </CardContent>
          </Card>
        </div>
      )}
    </div>
  )
}
