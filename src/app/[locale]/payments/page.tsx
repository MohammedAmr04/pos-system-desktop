"use client"

import { Payment } from "@/lib/api"
import { useCallback, useEffect, useState } from "react"
import { useTranslations } from "next-intl"
import { api } from "@/lib/api"
import { useAuth } from "@/components/common/auth-context"
import { PERMISSIONS } from "@/lib/constants"
import { AccessDenied } from "@/components/common/access-denied"
import { PaymentsClient } from "./payments-client"

const PAGE_SIZE = 20

export default function PaymentsPage() {
  const t = useTranslations("Payments")
  const { hasPermission } = useAuth()
  const canView = hasPermission(PERMISSIONS.PAYMENTS_VIEW)
  const [items, setItems] = useState<Payment[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (!canView) return
    let cancelled = false
    api.payments
      .listPaged(page, PAGE_SIZE)
      .then((res) => {
        if (cancelled) return
        setItems(res.items)
        setTotal(res.total)
      })
      .catch(() => {
        if (cancelled) return
        setItems([])
        setTotal(0)
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [page, canView])

  const handlePageChange = useCallback((p: number) => {
    setLoading(true)
    setPage(p)
  }, [])

  if (!canView) {
    return <AccessDenied />
  }

  return (
    <div className="flex-1 space-y-4 pt-6">
      <div className="flex items-center justify-between space-y-2">
        <h2 className="text-3xl font-bold tracking-tight">{t("title")}</h2>
      </div>
      <PaymentsClient
        items={items}
        total={total}
        page={page}
        pageSize={PAGE_SIZE}
        loading={loading}
        onPageChange={handlePageChange}
      />
    </div>
  )
}
