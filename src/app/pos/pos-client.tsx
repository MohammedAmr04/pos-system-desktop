"use client"

import { Product } from "@prisma/client"
import { usePOSStore } from "@/features/pos/store/usePOSStore"
import { Input } from "@/components/ui/input"
import { createInvoice } from "@/features/invoices/actions"
import { toast } from "sonner"
import { useState } from "react"
import { Loader2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Trash2, Plus, Minus, Search, ArrowLeft } from "lucide-react"
import Link from "next/link"

export function POSClient({ products }: { products: Product[] }) {
  const {
    cartItems,
    searchQuery,
    setSearchQuery,
    addItem,
    removeItem,
    updateQuantity,
    discount,
    setDiscount,
    clearCart
  } = usePOSStore()

  const handleSearch = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter' && searchQuery.trim() !== '') {
      // Find product by barcode or name
      const product = products.find(p => 
        p.barcode === searchQuery.trim() || 
        p.name.toLowerCase().includes(searchQuery.toLowerCase().trim())
      )
      
      if (product) {
        addItem(product)
        setSearchQuery('')
      } else {
        alert("Product not found or out of stock")
      }
    }
  }

  const subtotal = cartItems.reduce((acc, item) => acc + (item.salePrice * item.quantity), 0)
  const total = Math.max(0, subtotal - discount)

  const [isCheckingOut, setIsCheckingOut] = useState(false)

  const handleCheckout = async () => {
    if (cartItems.length === 0) return
    setIsCheckingOut(true)
    try {
      await createInvoice(cartItems, discount)
      toast.success("Checkout successful! Receipt printing...")
      clearCart()
    } catch (e) {
      toast.error("Checkout failed")
    } finally {
      setIsCheckingOut(false)
    }
  }

  return (
    <div className="flex h-full flex-col lg:flex-row gap-4 p-4 lg:p-6 bg-muted/40">
      {/* Left Panel: Search & Products */}
      <div className="flex flex-1 flex-col gap-4">
        <div className="flex items-center gap-4">
          <Link href="/">
            <Button variant="outline" size="icon">
              <ArrowLeft className="h-4 w-4" />
            </Button>
          </Link>
          <div className="relative flex-1">
            <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
            <Input
              type="text"
              placeholder="Scan barcode or search by name (Press Enter)"
              className="pl-8 bg-background text-lg h-12"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              onKeyDown={handleSearch}
              autoFocus
            />
          </div>
        </div>

        <Card className="flex-1 flex flex-col min-h-0">
          <CardHeader className="py-4">
            <CardTitle>Current Cart</CardTitle>
          </CardHeader>
          <CardContent className="flex-1 overflow-y-auto px-4 py-0">
            {cartItems.length === 0 ? (
              <div className="flex h-full items-center justify-center text-muted-foreground">
                Cart is empty. Scan an item to begin.
              </div>
            ) : (
              <div className="space-y-4 py-4">
                {cartItems.map((item) => (
                  <div key={item.id} className="flex items-center justify-between border-b pb-4">
                    <div className="flex-1">
                      <h4 className="font-semibold">{item.name}</h4>
                      <p className="text-sm text-muted-foreground">${item.salePrice.toFixed(2)} each</p>
                    </div>
                    <div className="flex items-center gap-4">
                      <div className="flex items-center gap-2">
                        <Button variant="outline" size="icon" className="h-8 w-8" onClick={() => updateQuantity(item.id, item.quantity - 1)}>
                          <Minus className="h-4 w-4" />
                        </Button>
                        <span className="w-8 text-center font-medium">{item.quantity}</span>
                        <Button variant="outline" size="icon" className="h-8 w-8" onClick={() => updateQuantity(item.id, item.quantity + 1)}>
                          <Plus className="h-4 w-4" />
                        </Button>
                      </div>
                      <div className="w-20 text-right font-semibold">
                        ${(item.salePrice * item.quantity).toFixed(2)}
                      </div>
                      <Button variant="ghost" size="icon" onClick={() => removeItem(item.id)} className="text-destructive">
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Right Panel: Checkout */}
      <div className="w-full lg:w-96 flex flex-col gap-4">
        <Card className="flex-1 flex flex-col">
          <CardHeader>
            <CardTitle>Checkout</CardTitle>
          </CardHeader>
          <CardContent className="flex-1 flex flex-col justify-end gap-6">
            <div className="space-y-4">
              <div className="flex justify-between text-lg">
                <span className="text-muted-foreground">Subtotal</span>
                <span>${subtotal.toFixed(2)}</span>
              </div>
              <div className="flex items-center justify-between gap-4">
                <span className="text-muted-foreground text-lg">Discount</span>
                <div className="relative w-32">
                  <span className="absolute left-3 top-2.5">$</span>
                  <Input 
                    type="number" 
                    min="0" 
                    step="0.01" 
                    className="pl-7 text-right text-lg h-12"
                    value={discount || ''}
                    onChange={(e) => setDiscount(parseFloat(e.target.value) || 0)}
                  />
                </div>
              </div>
              <div className="border-t pt-4 flex justify-between text-3xl font-bold">
                <span>Total</span>
                <span>${total.toFixed(2)}</span>
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4 mt-8">
              <Button variant="outline" className="h-16 text-lg" onClick={clearCart}>
                Clear
              </Button>
              <Button className="h-16 text-lg" size="lg" onClick={handleCheckout} disabled={isCheckingOut || cartItems.length === 0}>
                {isCheckingOut && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                Save & Print
              </Button>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
