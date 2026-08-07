"use client"

import { useCallback, useEffect, useState } from "react"
import { useTranslations } from "next-intl"
import { ProductsClient } from "./products-client"
import { api, Product } from "@/lib/api"

const PAGE_SIZE = 20

export default function ProductsPage() {
  const t = useTranslations("Products")
  const [products, setProducts] = useState<Product[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [query, setQuery] = useState("")
  const [loading, setLoading] = useState(true)
  const [requestId, setRequestId] = useState(0)

  useEffect(() => {
    let cancelled = false
    api.products
      .listPaged(page, PAGE_SIZE, query.trim() || undefined)
      .then((res) => {
        if (cancelled) return
        setProducts(res.items)
        setTotal(res.total)
      })
      .catch(() => {
        if (cancelled) return
        setProducts([])
        setTotal(0)
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [page, query, requestId])

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

  return (
    <div className="flex-1 space-y-4 pt-6">
      <div className="flex items-center justify-between space-y-2">
        <h2 className="text-3xl font-bold tracking-tight">{t("title")}</h2>
      </div>
      <ProductsClient
        items={products}
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
