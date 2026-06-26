"use server"

import { prisma } from "@/lib/db"
import { revalidatePath, unstable_cache } from "next/cache"

export const getProducts = unstable_cache(
  async () => {
    return await prisma.product.findMany({
      orderBy: { createdAt: 'desc' }
    })
  },
  ['products-cache'],
  { tags: ['products'] }
)

export async function createProduct(data: { barcode?: string | null, name: string, buyPrice: number, salePrice: number, stockQuantity: number }) {
  await prisma.product.create({
    data
  })
  revalidatePath('/products')
}

export async function updateProduct(id: string, data: { barcode?: string | null, name: string, buyPrice: number, salePrice: number, stockQuantity: number }) {
  await prisma.product.update({
    where: { id },
    data
  })
  revalidatePath('/products')
}

export async function deleteProduct(id: string) {
  await prisma.product.delete({
    where: { id }
  })
  revalidatePath('/products')
}
