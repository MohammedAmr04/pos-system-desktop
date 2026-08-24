"use client"

import { PurchaseInvoice } from "@/lib/api"
import { useCallback, useEffect, useState } from "react"
import { useTranslations } from "next-intl"
import { api } from "@/lib/api"
import { useAuth } from "@/components/common/auth-context"
import { PERMISSIONS } from "@/lib/constants"
import { AccessDenied } from "@/components/common/access-denied"
import { PurchasesClient } from "./purchases-client"

const PAGE_SIZE = 20

export default function PurchasesPage() {
  const t = useTranslations("Purchases")
  const { hasPermission } = useAuth()
  const canView = hasPermission(PERMISSIONS.PURCHASES_VIEW)
  const [items, setItems] = useState<PurchaseInvoice[]>([])
  const [total, setTotal] = useState(0)
  const [postedTotal, setPostedTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [query, setQuery] = useState("")
  const [status, setStatus] = useState("all")
  const [loading, setLoading] = useState(true)
  const [requestId, setRequestId] = useState(0)

  useEffect(() => {
    if (!canView) return
    let cancelled = false
    api.purchases
      .listPaged(page, PAGE_SIZE, { status, q: query.trim() || undefined })
      .then((res) => {
        if (cancelled) return
        setItems(res.items)
        setTotal(res.total)
        setPostedTotal(res.postedTotal)
      })
      .catch(() => {
        if (cancelled) return
        setItems([])
        setTotal(0)
        setPostedTotal(0)
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [page, query, status, requestId, canView])

  const handleQueryChange = useCallback((q: string) => {
    setLoading(true)
    setQuery(q)
    setPage(1)
  }, [])

  const handleStatusChange = useCallback((s: string) => {
    setLoading(true)
    setStatus(s)
    setPage(1)
  }, [])

  const handlePageChange = useCallback((p: number) => {
    setLoading(true)
    setPage(p)
  }, [])

  const refresh = useCallback(() => {
    setLoading(true)
    setRequestId((id) => id + 1)
  }, [])

  if (!canView) {
    return <AccessDenied />
  }

  return (
    <div className="flex-1 space-y-4 pt-6">
      <div className="flex items-center justify-between space-y-2">
        <h2 className="text-3xl font-bold tracking-tight">{t("title")}</h2>
      </div>
      <PurchasesClient
        items={items}
        total={total}
        postedTotal={postedTotal}
        page={page}
        pageSize={PAGE_SIZE}
        loading={loading}
        query={query}
        status={status}
        onQueryChange={handleQueryChange}
        onStatusChange={handleStatusChange}
        onPageChange={handlePageChange}
        onRefresh={refresh}
      />
    </div>
  )
}
