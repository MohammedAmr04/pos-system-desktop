"use client"

import { useEffect, useState } from "react"
import { POSClient } from "./pos-client"
import { api, Product } from "@/lib/api"

export default function POSPage() {
  const [products, setProducts] = useState<Product[]>([])
  const [loading, setLoading] = useState(true)

  const fetchProducts = () => {
    setLoading(true)
    return api.products.list().then((list) => {
      setProducts(list)
      return list
    })
  }

  useEffect(() => {
    fetchProducts().finally(() => setLoading(false))
  }, [])

  if (loading) {
    return (
      <div className="flex h-screen flex-col items-center justify-center bg-background">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
      </div>
    )
  }

  return (
    <div className="flex h-screen flex-col overflow-hidden bg-background">
      <POSClient products={products} onRefresh={fetchProducts} />
    </div>
  )
}
