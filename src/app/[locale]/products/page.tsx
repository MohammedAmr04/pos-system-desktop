import { getProducts } from "@/features/products/actions"
import { ProductsClient } from "./products-client"
import { getTranslations } from "next-intl/server"

export const dynamic = 'force-dynamic'

export default async function ProductsPage() {
  const products = await getProducts()
  const t = await getTranslations("Products")
  
  return (
    <div className="flex-1 space-y-4 pt-6">
      <div className="flex items-center justify-between space-y-2">
        <h2 className="text-3xl font-bold tracking-tight">{t("title")}</h2>
      </div>
      <ProductsClient data={products} />
    </div>
  )
}
