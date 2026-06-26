import { getProducts } from "@/features/products/actions"
import { POSClient } from "./pos-client"

export const dynamic = 'force-dynamic'

export default async function POSPage() {
  const products = await getProducts()
  
  return (
    <div className="flex h-screen flex-col overflow-hidden bg-background">
      <POSClient products={products} />
    </div>
  )
}
