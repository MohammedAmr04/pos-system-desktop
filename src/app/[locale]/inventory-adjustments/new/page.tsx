"use client"

import { useState } from "react"
import { ArrowRight, Plus } from "lucide-react"
import { useTranslations } from "next-intl"
import { toast } from "sonner"
import { createInventoryCount } from "@/actions/operations.actions"
import { useAuth } from "@/components/common/auth-context"
import { AccessDenied } from "@/components/common/access-denied"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { PERMISSIONS } from "@/lib/constants"
import { useRouter } from "@/i18n/navigation"

export default function NewInventoryAdjustmentPage() {
  const t = useTranslations("InventoryCounts")
  const router = useRouter()
  const { hasPermission } = useAuth()
  const [reason, setReason] = useState("")
  const [creating, setCreating] = useState(false)
  if (!hasPermission(PERMISSIONS.INVENTORY_ADJUSTMENTS_CREATE)) return <AccessDenied />
  const create = async () => {
    setCreating(true)
    try {
      const session = await createInventoryCount(reason.trim() || t("defaultReason"))
      window.sessionStorage.setItem("inventory-adjustment-session", session.id)
      router.replace("/inventory-adjustments")
    } catch (error) {
      toast.error(error instanceof Error ? error.message : t("saveFailed"))
    } finally {
      setCreating(false)
    }
  }
  return (
    <div className="flex-1 space-y-6 pt-6">
      <div>
        <Button variant="ghost" onClick={() => router.replace("/inventory-adjustments")}>
          <ArrowRight className="ml-2 size-4" />{t("back")}
        </Button>
        <h2 className="mt-3 text-3xl font-bold">{t("newTitle")}</h2>
        <p className="text-muted-foreground">{t("newDescription")}</p>
      </div>
      <div className="max-w-xl space-y-3 rounded-xl border p-5">
        <label className="text-sm font-medium" htmlFor="inventory-reason">{t("reason")}</label>
        <Input
          id="inventory-reason"
          value={reason}
          onChange={(event) => setReason(event.target.value)}
          placeholder={t("reasonPlaceholder")}
        />
        <p className="text-sm text-muted-foreground">{t("reasonHint")}</p>
        <Button disabled={creating} onClick={() => void create()}>
          <Plus className="ml-2 size-4" />{t("createAndStart")}
        </Button>
      </div>
    </div>
  )
}
