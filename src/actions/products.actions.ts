import { queryClient } from "@/lib/query-client"
import { request } from "@/lib/api"
import { Product, ProductBarcode, ProductUnit } from "@/types/domain/domain.types"
import { productsKeys } from "@/hooks/use-products"

export async function createProduct(data: {
  barcode?: string | null
  name: string
  retailPrice: number
  wholesalePrice?: number | null
  unitName?: string | null
  unitId?: string | null
  categoryId?: string | null
  brandId?: string | null
  allowDiscount?: boolean
  lowStockThreshold?: number
  isHiddenFromPOS?: boolean
  notes?: string | null
  productType?: 'product' | 'service' | 'bundle'
  serviceCost?: number
  bundleComponents?: Array<{ productId: string; quantity: number }>
}) {
  const result = await request<Product>('/api/products', { method: 'POST', body: JSON.stringify(data) })
  await queryClient.invalidateQueries({ queryKey: productsKeys.all })
  return result
}

export async function updateProduct(
  id: string,
  data: {
    barcode?: string | null
    name: string
    retailPrice: number
    wholesalePrice?: number | null
    unitName?: string | null
    unitId?: string | null
    categoryId?: string | null
    brandId?: string | null
    allowDiscount?: boolean
    lowStockThreshold?: number
    isHiddenFromPOS?: boolean
    notes?: string | null
    productType?: 'product' | 'service' | 'bundle'
    serviceCost?: number
    bundleComponents?: Array<{ productId: string; quantity: number }>
  }
) {
  await request(`/api/products/${id}`, { method: 'PUT', body: JSON.stringify(data) })
  await queryClient.invalidateQueries({ queryKey: productsKeys.all })
}

export async function deleteProduct(id: string) {
  await request<{ success: boolean }>(`/api/products/${id}`, { method: 'DELETE' })
  await queryClient.invalidateQueries({ queryKey: productsKeys.all })
}

export async function addProductUnit(
  productId: string,
  data: { unitId?: string | null; unitName: string; quantityFactor: number; retailPrice: number; wholesalePrice?: number | null }
) {
  const result = await request<ProductUnit>(`/api/products/${productId}/units`, {
    method: 'POST',
    body: JSON.stringify(data),
  })
  await queryClient.invalidateQueries({ queryKey: productsKeys.all })
  return result
}

export async function updateProductUnit(
  productId: string,
  unitId: string,
  data: { unitId?: string | null; unitName?: string; quantityFactor?: number; retailPrice?: number; wholesalePrice?: number | null }
) {
  const result = await request<ProductUnit>(`/api/products/${productId}/units/${unitId}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  })
  await queryClient.invalidateQueries({ queryKey: productsKeys.all })
  return result
}

export async function deleteProductUnit(productId: string, unitId: string) {
  await request<{ success: boolean }>(`/api/products/${productId}/units/${unitId}`, {
    method: 'DELETE',
  })
  await queryClient.invalidateQueries({ queryKey: productsKeys.all })
}

export async function addProductBarcode(productId: string, unitId: string, barcode: string) {
  const result = await request<ProductBarcode>(`/api/products/${productId}/units/${unitId}/barcodes`, {
    method: 'POST',
    body: JSON.stringify({ barcode }),
  })
  await queryClient.invalidateQueries({ queryKey: productsKeys.all })
  return result
}

export async function removeProductBarcode(productId: string, unitId: string, barcodeId: string) {
  await request<{ success: boolean }>(`/api/products/${productId}/units/${unitId}/barcodes/${barcodeId}`, {
    method: 'DELETE',
  })
  await queryClient.invalidateQueries({ queryKey: productsKeys.all })
}

export async function setDefaultProductBarcode(productId: string, unitId: string, barcodeId: string) {
  await request<{ success: boolean }>(`/api/products/${productId}/units/${unitId}/barcodes/${barcodeId}/default`, {
    method: 'PUT',
  })
  await queryClient.invalidateQueries({ queryKey: productsKeys.all })
}
