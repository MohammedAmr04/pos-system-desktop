"use client"

import { usePathname } from "next/navigation"
import { useTranslations } from "next-intl"
import { AccessDenied } from "@/components/common/access-denied"
import { Button } from "@/components/ui/button"
import { useAuth } from "@/components/common/auth-context"
import { useRouter } from "@/i18n/navigation"
import { PERMISSIONS } from "@/lib/constants"
import { InventorySession } from "../_components/inventory-session"

export function InventoryAdjustmentSessionPage() {
  const t = useTranslations("InventoryCounts")
  const { hasPermission } = useAuth()
  const router = useRouter()
  const pathname = usePathname()
  const pathParts = pathname.split("/").filter(Boolean)
  const sessionId = pathParts[pathParts.length - 1]

  if (!hasPermission(PERMISSIONS.INVENTORY_ADJUSTMENTS_VIEW)) return <AccessDenied />
  if (!sessionId || sessionId === "__session__") {
    return (
      <div className="flex-1 space-y-4 pt-6">
        <p className="text-muted-foreground">{t("missingSession")}</p>
        <Button onClick={() => router.replace("/inventory-adjustments")}>{t("back")}</Button>
      </div>
    )
  }

  return <InventorySession id={sessionId} onBack={() => router.replace("/inventory-adjustments")} />
}
