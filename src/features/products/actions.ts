import { api } from "@/lib/api"

export const getProducts = async () => {
  return api.products.list()
}

export async function createProduct(data: {
  barcode?: string | null
  name: string
  buyPrice: number
  salePrice: number
  stockQuantity: number
  notes?: string | null
}) {
  await api.products.create(data as any)
}

export async function updateProduct(
  id: string,
  data: {
    barcode?: string | null
    name: string
    buyPrice: number
    salePrice: number
    stockQuantity: number
    notes?: string | null
  }
) {
  await api.products.update(id, data as any)
}

export async function deleteProduct(id: string) {
  await api.products.delete(id)
}
