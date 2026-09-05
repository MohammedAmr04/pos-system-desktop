"use client"

import { useTranslations } from "next-intl"
import { Button } from "@/components/ui/button"
import { TableColumn } from "@/components/common/table-builder"
import { InventoryAdjustmentSummary } from "@/types/domain/domain.types"
import { CountStatusBadge } from "./count-status-badge"

export function useAdjustmentColumns(onOpen: (id: string) => void) {
  const t = useTranslations("InventoryCounts")
  const columns: TableColumn<InventoryAdjustmentSummary>[] = [
    { key: "number", header: t("number"), cell: (item) => `#${item.number}` },
    { key: "reason", header: t("reason"), cell: (item) => item.reason },
    {
      key: "status",
      header: t("statusLabel"),
      cell: (item) => <CountStatusBadge status={item.status} />,
    },
    { key: "lines", header: t("products"), cell: (item) => item.lineCount },
    {
      key: "differences",
      header: t("differences"),
      cell: (item) => item.differenceCount,
    },
    {
      key: "created",
      header: t("createdAt"),
      cell: (item) => <span dir="ltr">{new Date(item.createdAt).toLocaleString()}</span>,
    },
    {
      key: "open",
      header: t("actions"),
      cell: (item) => (
        <Button variant="outline" size="sm" onClick={() => onOpen(item.id)}>
          {t("open")}
        </Button>
      ),
    },
  ]
  return columns
}
