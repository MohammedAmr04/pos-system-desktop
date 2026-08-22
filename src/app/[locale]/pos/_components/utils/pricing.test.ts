import { describe, expect, it } from "vitest"
import {
  cartTotals,
  lineDiscountAmount,
  lineFinalTotal,
  lineSubtotal,
  round2,
} from "./pricing"
import type { CartItem } from "@/store/pos.store"

const makeItem = (overrides: Partial<CartItem> = {}): CartItem => ({
  id: "line-1",
  productId: "p1",
  productUnitId: "u1",
  unitName: "piece",
  name: "Item",
  buyPrice: 1,
  retailPrice: 10,
  wholesalePrice: null,
  originalUnitPrice: 10,
  unitPrice: 10,
  quantity: 1,
  maxStock: 100,
  quantityFactor: 1,
  allowDiscount: true,
  overridden: false,
  ...overrides,
})

describe("round2", () => {
  it("rounds to two decimal places", () => {
    expect(round2(10.456)).toBe(10.46)
    expect(round2(10.454)).toBe(10.45)
    expect(round2(1 / 3)).toBe(0.33)
  })

  it("eliminates binary floating point drift", () => {
    expect(round2(0.1 * 3)).toBe(0.3)
  })
})

describe("lineSubtotal", () => {
  it("multiplies unit price by quantity", () => {
    expect(lineSubtotal(makeItem({ unitPrice: 3.33, quantity: 3 }))).toBe(9.99)
  })

  it("rounds away floating point artifacts", () => {
    expect(lineSubtotal(makeItem({ unitPrice: 0.1, quantity: 3 }))).toBe(0.3)
  })

  it("returns 0 for zero quantity", () => {
    expect(lineSubtotal(makeItem({ quantity: 0 }))).toBe(0)
  })
})

describe("lineDiscountAmount", () => {
  it("computes percentage discounts from the line subtotal", () => {
    const item = makeItem({
      unitPrice: 10,
      quantity: 2,
      discountType: "percentage",
      discountValue: 10,
    })
    expect(lineDiscountAmount(item)).toBe(2)
  })

  it("clamps fixed discounts to the line subtotal", () => {
    const item = makeItem({
      unitPrice: 5,
      quantity: 1,
      discountType: "fixed",
      discountValue: 8,
    })
    expect(lineDiscountAmount(item)).toBe(5)
  })

  it("applies fixed discounts below the subtotal as-is", () => {
    const item = makeItem({
      unitPrice: 20,
      quantity: 1,
      discountType: "fixed",
      discountValue: 3,
    })
    expect(lineDiscountAmount(item)).toBe(3)
  })

  it("returns 0 when there is no line discount", () => {
    expect(lineDiscountAmount(makeItem())).toBe(0)
    expect(lineDiscountAmount(makeItem({ discountType: null, discountValue: 5 }))).toBe(0)
  })
})

describe("lineFinalTotal", () => {
  it("subtracts the line discount from the subtotal", () => {
    const item = makeItem({
      unitPrice: 10,
      quantity: 2,
      discountType: "percentage",
      discountValue: 10,
    })
    expect(lineFinalTotal(item)).toBe(18)
  })
})

describe("cartTotals", () => {
  it("returns zeros for an empty cart", () => {
    const totals = cartTotals([], 10, "fixed")
    expect(totals.subtotal).toBe(0)
    expect(totals.itemsDiscount).toBe(0)
    expect(totals.eligibleSubtotal).toBe(0)
    expect(totals.effectiveDiscount).toBe(0)
    expect(totals.total).toBe(0)
    expect(totals.totalItems).toBe(0)
  })

  it("sums subtotals and quantities across lines", () => {
    const cart = [
      makeItem({ id: "a", unitPrice: 10, quantity: 2 }),
      makeItem({ id: "b", unitPrice: 5.5, quantity: 3 }),
    ]
    const totals = cartTotals(cart, 0, "fixed")
    expect(totals.subtotal).toBe(36.5)
    expect(totals.totalItems).toBe(5)
    expect(totals.total).toBe(36.5)
  })

  it("restricts invoice discounts to discountable lines only", () => {
    const cart = [
      makeItem({ id: "eligible", unitPrice: 10, quantity: 1, allowDiscount: true }),
      makeItem({ id: "ineligible", unitPrice: 40, quantity: 1, allowDiscount: false }),
    ]

    const totals = cartTotals(cart, 5, "fixed")
    expect(totals.eligibleSubtotal).toBe(10)
    expect(totals.effectiveDiscount).toBe(5)
    expect(totals.total).toBe(45)

    // Fixed discount larger than the eligible subtotal is capped at it.
    const capped = cartTotals(cart, 99, "fixed")
    expect(capped.effectiveDiscount).toBe(10)
    expect(capped.total).toBe(40)
  })

  it("derives percentage invoice discounts from the eligible subtotal", () => {
    const cart = [
      makeItem({ id: "eligible", unitPrice: 10, quantity: 3, allowDiscount: true }),
      makeItem({ id: "ineligible", unitPrice: 70, quantity: 1, allowDiscount: false }),
    ]

    const totals = cartTotals(cart, 10, "percentage")
    expect(totals.eligibleSubtotal).toBe(30)
    expect(totals.effectiveDiscount).toBe(3)
    expect(totals.total).toBe(97)
  })

  it("combines line-level and invoice-level discounts", () => {
    const cart = [
      makeItem({
        id: "with-line-discount",
        unitPrice: 20,
        quantity: 1,
        allowDiscount: true,
        discountType: "fixed",
        discountValue: 2,
      }),
      makeItem({ id: "plain", unitPrice: 10, quantity: 1, allowDiscount: true }),
    ]

    const totals = cartTotals(cart, 10, "percentage")
    expect(totals.subtotal).toBe(30)
    expect(totals.itemsDiscount).toBe(2)
    expect(totals.eligibleSubtotal).toBe(28)
    expect(totals.effectiveDiscount).toBe(2.8)
    expect(totals.total).toBe(25.2)
  })
})
