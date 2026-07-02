"use server"

import { prisma } from "@/lib/db"
import { revalidatePath, unstable_cache, updateTag } from "next/cache"

function generateBarcode(): string {
  let barcode = ""
  for (let i = 0; i < 12; i++) {
    barcode += Math.floor(Math.random() * 10).toString()
  }
  return barcode
}

export const getProducts = unstable_cache(
  async () => {
    return await prisma.product.findMany({
      orderBy: { createdAt: 'desc' }
    })
  },
  ['products-cache'],
  { tags: ['products'] }
)

export async function createProduct(data: { barcode?: string | null, name: string, buyPrice: number, salePrice: number, stockQuantity: number, notes?: string | null }) {
  const barcode = data.barcode || generateBarcode()
  await prisma.product.create({
    data: {
      ...data,
      barcode,
    }
  })
  revalidatePath('/products')
  updateTag('products')
}

export async function updateProduct(id: string, data: { barcode?: string | null, name: string, buyPrice: number, salePrice: number, stockQuantity: number, notes?: string | null }) {
  await prisma.product.update({
    where: { id },
    data
  })
  revalidatePath('/products')
  updateTag('products')
}

export async function deleteProduct(id: string) {
  await prisma.product.delete({
    where: { id }
  })
  revalidatePath('/products')
  updateTag('products')
}
