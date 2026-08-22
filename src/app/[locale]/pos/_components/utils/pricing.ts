import type { CartItem } from "@/store/pos.store"

export type DiscountType = 'fixed' | 'percentage'

export const round2 = (n: number): number => Math.round(n * 100) / 100

export const lineSubtotal = (item: CartItem): number => round2(item.unitPrice * item.quantity)

export const lineDiscountAmount = (item: CartItem): number =>
  item.discountType === 'percentage'
    ? round2(lineSubtotal(item) * (item.discountValue ?? 0) / 100)
    : item.discountType === 'fixed'
      ? Math.min(item.discountValue ?? 0, lineSubtotal(item))
      : 0

export const lineFinalTotal = (item: CartItem): number =>
  round2(lineSubtotal(item) - lineDiscountAmount(item))

export interface CartTotals {
  subtotal: number
  itemsDiscount: number
  eligibleSubtotal: number
  effectiveDiscount: number
  total: number
  totalItems: number
}

export const cartTotals = (
  cartItems: CartItem[],
  discount: number,
  discountType: DiscountType
): CartTotals => {
  const subtotal = cartItems.reduce((acc, item) => acc + lineSubtotal(item), 0)
  const itemsDiscount = cartItems.reduce((acc, item) => acc + lineDiscountAmount(item), 0)
  const eligibleSubtotal = cartItems
    .filter(item => item.allowDiscount)
    .reduce((acc, item) => acc + lineFinalTotal(item), 0)

  const effectiveDiscount = discountType === 'percentage'
    ? round2(eligibleSubtotal * (discount / 100))
    : Math.min(discount, eligibleSubtotal)

  return {
    subtotal,
    itemsDiscount,
    eligibleSubtotal,
    effectiveDiscount,
    total: Math.max(0, subtotal - itemsDiscount - effectiveDiscount),
    totalItems: cartItems.reduce((acc, item) => acc + item.quantity, 0),
  }
}
