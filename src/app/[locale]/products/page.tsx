"use client"

import { useEffect, useState } from "react"
import { useTranslations } from "next-intl"
import { ProductsClient } from "./products-client"
import { api, Product } from "@/lib/api"

export default function ProductsPage() {
  const t = useTranslations("Products")
  const [products, setProducts] = useState<Product[]>([])
  const [loading, setLoading] = useState(true)

  const fetchProducts = () => {
    setLoading(true)
    api.products.list().then(setProducts).finally(() => setLoading(false))
  }

  useEffect(() => {
    fetchProducts()
  }, [])

  if (loading) {
    return (
      <div className="flex-1 space-y-4 pt-6">
        <div className="flex items-center justify-between space-y-2">
          <h2 className="text-3xl font-bold tracking-tight">{t("title")}</h2>
        </div>
        <div className="flex justify-center py-12">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
        </div>
      </div>
    )
  }

  return (
    <div className="flex-1 space-y-4 pt-6">
      <div className="flex items-center justify-between space-y-2">
        <h2 className="text-3xl font-bold tracking-tight">{t("title")}</h2>
      </div>
      <ProductsClient data={products} onRefresh={fetchProducts} />
    </div>
  )
}
