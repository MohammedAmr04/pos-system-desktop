"use server"

import { prisma } from "@/lib/db"
import { revalidatePath, unstable_cache, updateTag } from "next/cache"
import { CartItem } from "@/features/pos/store/usePOSStore"

export const getInvoices = unstable_cache(
  async () => {
    const today = new Date()
    today.setHours(0, 0, 0, 0)
    
    return await prisma.invoice.findMany({
      where: {
        createdAt: {
          gte: today
        }
      },
      include: {
        InvoiceDetail: {
          include: {
            product: true
          }
        }
      },
      orderBy: { createdAt: 'desc' }
    })
  },
  ['invoices-cache'],
  { tags: ['invoices'] }
)

export async function getFilteredInvoices(from?: string, to?: string) {
  const where: Record<string, unknown> = {}
  if (from) {
    where.createdAt = { ...(where.createdAt as object || {}), gte: new Date(from) }
  }
  if (to) {
    const toDate = new Date(to)
    toDate.setHours(23, 59, 59, 999)
    where.createdAt = { ...(where.createdAt as object || {}), lte: toDate }
  }
  if (!from && !to) {
    const today = new Date()
    today.setHours(0, 0, 0, 0)
    where.createdAt = { gte: today }
  }

  return await prisma.invoice.findMany({
    where,
    include: {
      InvoiceDetail: {
        include: {
          product: true
        }
      }
    },
    orderBy: { createdAt: 'desc' }
  })
}

export async function createInvoice(cartItems: CartItem[], discount: number) {
  const subtotal = cartItems.reduce((acc, item) => acc + (item.salePrice * item.quantity), 0)
  const totalAmount = Math.max(0, subtotal - discount)

  const invoice = await prisma.$transaction(async (tx) => {
    const newInvoice = await tx.invoice.create({
      data: {
        totalAmount,
        discount,
        InvoiceDetail: {
          create: cartItems.map(item => ({
            productId: item.productId,
            quantity: item.quantity,
            buyPrice: item.buyPrice,
            salePrice: item.salePrice
          }))
        }
      },
      include: {
        InvoiceDetail: true
      }
    })

    for (const item of cartItems) {
      await tx.product.update({
        where: { id: item.productId },
        data: {
          stockQuantity: {
            decrement: item.quantity
          }
        }
      })
    }

    return newInvoice
  })

  revalidatePath('/invoices')
  revalidatePath('/products')
  updateTag('invoices')
  updateTag('products')

  try {
    await fetch('http://localhost:3001/api/printing/print', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ invoice })
    })
  } catch (e) {
    console.error("Silent printing failed, printer API might be offline")
  }

  return invoice
}
