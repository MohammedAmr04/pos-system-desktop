import { create } from 'zustand'

export interface CartItem {
  id: string
  productId: string
  name: string
  buyPrice: number
  salePrice: number
  quantity: number
  maxStock: number
  allowDiscount: boolean
}

interface POSStore {
  cartItems: CartItem[]
  discount: number
  discountType: 'fixed' | 'percentage'
  searchQuery: string
  setSearchQuery: (query: string) => void
  addItem: (product: { id: string, name: string, buyPrice: number, salePrice: number, stockQuantity: number, allowDiscount?: boolean }) => void
  removeItem: (id: string) => void
  updateQuantity: (id: string, quantity: number) => void
  setDiscount: (discount: number) => void
  setDiscountType: (type: 'fixed' | 'percentage') => void
  toggleDiscountType: () => void
  clearCart: () => void
}

export const usePOSStore = create<POSStore>((set, get) => ({
  cartItems: [],
  discount: 0,
  discountType: 'fixed',
  searchQuery: '',

  setSearchQuery: (query) => set({ searchQuery: query }),

  addItem: (product) => {
    const { cartItems } = get()
    const existing = cartItems.find((item) => item.productId === product.id)
    if (existing) {
      if (existing.quantity < product.stockQuantity) {
        set({
          cartItems: cartItems.map((item) =>
            item.productId === product.id
              ? { ...item, quantity: item.quantity + 1 }
              : item
          ),
        })
      }
    } else {
      if (product.stockQuantity > 0) {
        set({
          cartItems: [
            ...cartItems,
            {
              id: crypto.randomUUID(),
              productId: product.id,
              name: product.name,
              buyPrice: product.buyPrice,
              salePrice: product.salePrice,
              quantity: 1,
              maxStock: product.stockQuantity,
              allowDiscount: product.allowDiscount ?? true,
            },
          ],
        })
      }
    }
  },

  removeItem: (id) => set((state) => ({
    cartItems: state.cartItems.filter((item) => item.id !== id)
  })),

  updateQuantity: (id, quantity) => set((state) => ({
    cartItems: state.cartItems.map((item) =>
      item.id === id ? { ...item, quantity: Math.min(Math.max(1, quantity), item.maxStock) } : item
    )
  })),

  setDiscount: (discount) => set({ discount }),

  setDiscountType: (type) => set({ discountType: type }),

  toggleDiscountType: () => set((state) => ({
    discountType: state.discountType === 'fixed' ? 'percentage' : 'fixed'
  })),

  clearCart: () => set({ cartItems: [], discount: 0, discountType: 'fixed', searchQuery: '' }),
}))
