"use client"

import { useTranslations } from "next-intl"
import { Badge } from "@/components/ui/badge"
import { InventoryAdjustmentSummary } from "@/types/domain/domain.types"

const STATUS_VARIANTS = {
  posted: "default",
  cancelled: "destructive",
  draft: "secondary",
  counting: "secondary",
} as const

export function CountStatusBadge({ status }: { status: InventoryAdjustmentSummary["status"] }) {
  const t = useTranslations("InventoryCounts")
  return <Badge variant={STATUS_VARIANTS[status]}>{t(`status.${status}`)}</Badge>
}
