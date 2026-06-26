import { getProducts } from "@/features/products/actions"
import { ProductsClient } from "./products-client"

export const dynamic = 'force-dynamic'

export default async function ProductsPage() {
  const products = await getProducts()
  
  return (
    <div className="flex-1 space-y-4 pt-6">
      <div className="flex items-center justify-between space-y-2">
        <h2 className="text-3xl font-bold tracking-tight">Products</h2>
      </div>
      <ProductsClient data={products} />
    </div>
  )
}
