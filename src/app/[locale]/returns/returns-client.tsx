"use client"

import { useEffect, useState } from "react"
import { useAuth } from "@/components/common/auth-context"
import { PERMISSIONS } from "@/lib/constants"
import { AccessDenied } from "@/components/common/access-denied"
import { useTranslations } from "next-intl"
import { SalesReturnsSection } from "./_components/sales-returns-section"
import { PurchasesReturnsSection } from "./_components/purchases-returns-section"

type Tab = "sales" | "purchase"

const getInitialTab = (): Tab => {
  if (typeof window === "undefined") return "sales"
  return new URLSearchParams(window.location.search).get("tab") === "purchase" ? "purchase" : "sales"
}

const getInitialPurchaseId = (): string | null => {
  if (typeof window === "undefined") return null
  return new URLSearchParams(window.location.search).get("purchase")
}

export function ReturnsClient() {
  const t = useTranslations("Returns")
  const { hasPermission } = useAuth()
  const canView = hasPermission(PERMISSIONS.INVOICES_VIEW)

  const [activeTab, setActiveTab] = useState<Tab>(getInitialTab)
  const [initialPurchaseId] = useState<string | null>(getInitialPurchaseId)

  useEffect(() => {
    window.history.replaceState({}, "", window.location.pathname)
  }, [])

  if (!canView) return <AccessDenied />

  return (
    <div className="flex-1 space-y-4 pt-6">
      <div className="flex items-center justify-between">
        <h2 className="text-3xl font-bold tracking-tight">{t("title")}</h2>
        <div className="flex gap-1 rounded-lg border bg-card p-1">
          <button
            onClick={() => setActiveTab("sales")}
            className={`rounded-md px-4 py-1.5 text-sm font-medium transition-colors ${
              activeTab === "sales"
                ? "bg-primary text-primary-foreground"
                : "hover:bg-accent"
            }`}
          >
            {t("salesReturns")}
          </button>
          <button
            onClick={() => setActiveTab("purchase")}
            className={`rounded-md px-4 py-1.5 text-sm font-medium transition-colors ${
              activeTab === "purchase"
                ? "bg-primary text-primary-foreground"
                : "hover:bg-accent"
            }`}
          >
            {t("purchaseReturns")}
          </button>
        </div>
      </div>

      {activeTab === "sales" ? (
        <SalesReturnsSection />
      ) : (
        <PurchasesReturnsSection initialPurchaseId={initialPurchaseId} />
      )}
    </div>
  )
}
