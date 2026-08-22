import { create } from 'zustand'
import { Product, ProductUnit, PriceMode } from '@/lib/api'

export interface CartItem {
  id: string
  productId: string
  productUnitId: string
  unitName: string
  name: string
  buyPrice: number
  retailPrice: number
  wholesalePrice: number | null
  originalUnitPrice: number
  unitPrice: number
  quantity: number
  maxStock: number
  quantityFactor: number
  allowDiscount: boolean
  discountType?: 'percentage' | 'fixed' | null
  discountValue?: number
  overridden: boolean
  priceEditNote?: string
}

export const priceFor = (unit: ProductUnit, mode: PriceMode): number => {
  if (mode === 'wholesale' && unit.wholesalePrice != null) return unit.wholesalePrice
  return unit.retailPrice
}

interface POSStore {
  cartItems: CartItem[]
  discount: number
  discountType: 'fixed' | 'percentage'
  priceMode: PriceMode
  searchQuery: string
  setSearchQuery: (query: string) => void
  setPriceMode: (mode: PriceMode) => void
  togglePriceMode: () => void
  addItem: (product: Product, unit: ProductUnit) => 'added' | 'out' | 'max'
  removeItem: (id: string) => void
  updateQuantity: (id: string, quantity: number) => void
  updateUnitPrice: (id: string, unitPrice: number, note?: string) => void
  setLineDiscount: (id: string, type: 'percentage' | 'fixed', value: number) => void
  clearLineDiscount: (id: string) => void
  setDiscount: (discount: number) => void
  setDiscountType: (type: 'fixed' | 'percentage') => void
  toggleDiscountType: () => void
  clearCart: () => void
}

export const usePOSStore = create<POSStore>((set, get) => ({
  cartItems: [],
  discount: 0,
  discountType: 'fixed',
  priceMode: 'retail',
  searchQuery: '',

  setSearchQuery: (query) => set({ searchQuery: query }),

  setPriceMode: (mode) => {
    const { priceMode, cartItems } = get()
    if (mode === priceMode) return
    const nextItems = cartItems.map((item) => {
      const newPrice = mode === 'wholesale' && item.wholesalePrice != null
        ? item.wholesalePrice
        : item.retailPrice
      return {
        ...item,
        originalUnitPrice: newPrice,
        unitPrice: newPrice,
        overridden: false,
      }
    })
    set({ priceMode: mode, cartItems: nextItems })
  },

  togglePriceMode: () => {
    const { priceMode } = get()
    get().setPriceMode(priceMode === 'retail' ? 'wholesale' : 'retail')
  },

  addItem: (product, unit) => {
    const { cartItems, priceMode } = get()
    const unitMaxStock = unit.quantityFactor > 0 ? product.stockQuantity / unit.quantityFactor : 0
    if (unitMaxStock <= 0) return 'out'
    const existing = cartItems.find(
      (item) => item.productId === product.id && item.productUnitId === unit.id
    )
    if (existing) {
      if (existing.quantity >= unitMaxStock) return 'max'
      set({
        cartItems: cartItems.map((item) =>
          item.id === existing.id ? { ...item, quantity: item.quantity + 1 } : item
        ),
      })
      return 'added'
    }
    const unitPrice = priceFor(unit, priceMode)
    set({
      cartItems: [
        ...cartItems,
        {
          id: crypto.randomUUID(),
          productId: product.id,
          productUnitId: unit.id,
          unitName: unit.unitName,
          name: product.name,
          buyPrice: product.buyPrice,
          retailPrice: unit.retailPrice,
          wholesalePrice: unit.wholesalePrice ?? null,
          originalUnitPrice: unitPrice,
          unitPrice,
          quantity: 1,
          maxStock: product.stockQuantity,
          quantityFactor: unit.quantityFactor,
          allowDiscount: product.allowDiscount,
          discountType: null,
          discountValue: 0,
          overridden: false,
        },
      ],
    })
    return 'added'
  },

  removeItem: (id) => set((state) => ({
    cartItems: state.cartItems.filter((item) => item.id !== id)
  })),

  updateQuantity: (id, quantity) => set((state) => ({
    cartItems: state.cartItems.map((item) => {
      if (item.id !== id) return item
      const cap = item.quantityFactor > 0 ? item.maxStock / item.quantityFactor : item.maxStock
      return { ...item, quantity: Math.min(Math.max(1, quantity), Math.max(1, cap)) }
    })
  })),

  updateUnitPrice: (id, unitPrice, note) => set((state) => ({
    cartItems: state.cartItems.map((item) => {
      if (item.id !== id) return item
      const overridden = unitPrice !== item.originalUnitPrice
      return {
        ...item,
        unitPrice: Math.max(0, unitPrice),
        overridden,
        priceEditNote: overridden ? (note ?? item.priceEditNote) : undefined,
      }
    })
  })),

  setLineDiscount: (id, type, value) => set((state) => ({
    cartItems: state.cartItems.map((item) =>
      item.id === id ? { ...item, discountType: type, discountValue: value } : item
    )
  })),

  clearLineDiscount: (id) => set((state) => ({
    cartItems: state.cartItems.map((item) =>
      item.id === id ? { ...item, discountType: null, discountValue: 0 } : item
    )
  })),

  setDiscount: (discount) => set({ discount }),

  setDiscountType: (type) => set({ discountType: type }),

  toggleDiscountType: () => set((state) => ({
    discountType: state.discountType === 'fixed' ? 'percentage' : 'fixed'
  })),

  clearCart: () => set({ cartItems: [], discount: 0, discountType: 'fixed', searchQuery: '' }),
}))
