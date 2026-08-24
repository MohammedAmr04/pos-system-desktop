"use client"

import { useCallback, useEffect, useState } from "react"
import { useTranslations } from "next-intl"
import { CategoriesClient } from "./categories-client"
import { api, Category } from "@/lib/api"
import { useAuth } from "@/components/common/auth-context"
import { PERMISSIONS, FEATURES } from "@/lib/constants"
import { AccessDenied } from "@/components/common/access-denied"

const PAGE_SIZE = 20

export default function CategoriesPage() {
  const t = useTranslations("Categories")
  const { hasAccess } = useAuth()
  const canView = hasAccess(PERMISSIONS.CATEGORIES_VIEW, FEATURES.CATEGORIES)
  const [items, setItems] = useState<Category[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [query, setQuery] = useState("")
  const [loading, setLoading] = useState(true)
  const [requestId, setRequestId] = useState(0)

  useEffect(() => {
    if (!canView) return
    let cancelled = false
    api.categories
      .listPaged(page, PAGE_SIZE, query.trim() || undefined)
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
  }, [page, query, requestId, canView])

  const handleQueryChange = useCallback((q: string) => {
    setLoading(true)
    setQuery(q)
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
      <CategoriesClient
        items={items}
        total={total}
        page={page}
        pageSize={PAGE_SIZE}
        loading={loading}
        query={query}
        onQueryChange={handleQueryChange}
        onPageChange={handlePageChange}
        onRefresh={refresh}
      />
    </div>
  )
}
