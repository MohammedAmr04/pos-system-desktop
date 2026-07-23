"use client"

import { useEffect, useState } from "react"
import { api, Product } from "@/lib/api"
import { LowStockClient } from "./low-stock-client"

export default function LowStockPage() {
  const [products, setProducts] = useState<Product[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    api.reports.lowStock().then(setProducts).finally(() => setLoading(false))
  }, [])

  if (loading) {
    return (
      <div className="flex-1 space-y-4 pt-6">
        <div className="flex justify-center py-12">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
        </div>
      </div>
    )
  }

  return <LowStockClient data={products} />
}
