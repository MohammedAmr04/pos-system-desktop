"use client"

import { useAuth } from "@/components/common/auth-context"
import { PERMISSIONS, FEATURES } from "@/lib/constants"
import { AccessDenied } from "@/components/common/access-denied"
import { LowStockClient } from "./low-stock-client"

export default function LowStockPage() {
  const { hasAccess } = useAuth()
  const canView = hasAccess(PERMISSIONS.REPORTS_VIEW, FEATURES.LOW_STOCK_REPORT)

  if (!canView) {
    return <AccessDenied />
  }

  return <LowStockClient />
}
