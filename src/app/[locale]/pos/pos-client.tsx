"use client"

import { Product } from "@prisma/client"
import { usePOSStore } from "@/features/pos/store/usePOSStore"
import { Input } from "@/components/ui/input"
import { createInvoice } from "@/features/invoices/actions"
import { toast } from "sonner"
import { useState, useRef, useEffect } from "react"
import { Loader2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Trash2, Plus, Minus, ArrowLeft } from "lucide-react"
import Link from "next/link"
import { useTranslations } from "next-intl"
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from "@/components/ui/command"
import {
  Popover,
  PopoverPopup,
  PopoverPortal,
  PopoverPositioner,
  PopoverTrigger,
} from "@/components/ui/popover"

export function POSClient({ products }: { products: Product[] }) {
  const t = useTranslations("POS")
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

  const [open, setOpen] = useState(false)
  const [inputValue, setInputValue] = useState("")
  const inputRef = useRef<HTMLInputElement>(null)
  const triggerRef = useRef<HTMLButtonElement>(null)

  useEffect(() => {
    if (open && inputRef.current) {
      inputRef.current.focus()
    }
  }, [open])

  const filteredProducts = products.filter((p) =>
    p.name.toLowerCase().includes(inputValue.toLowerCase()) ||
    (p.barcode && p.barcode.includes(inputValue))
  )

  const handleSelect = (product: Product) => {
    addItem(product)
    setInputValue("")
    setSearchQuery("")
    setOpen(false)
    triggerRef.current?.focus()
  }

  const subtotal = cartItems.reduce((acc, item) => acc + (item.salePrice * item.quantity), 0)
  const total = Math.max(0, subtotal - discount)

  const [isCheckingOut, setIsCheckingOut] = useState(false)

  const handleCheckout = async () => {
    if (cartItems.length === 0) return
    setIsCheckingOut(true)
    try {
      await createInvoice(cartItems, discount)
      toast.success(t("checkoutSuccess"))
      clearCart()
    } catch (e) {
      toast.error(t("checkoutFailed"))
    } finally {
      setIsCheckingOut(false)
    }
  }

  return (
    <div className="flex h-full flex-col lg:flex-row gap-4 p-4 lg:p-6 bg-muted/40">
      <div className="flex flex-1 flex-col gap-4">
        <div className="flex items-center gap-4">
          <Link href="/">
            <Button variant="outline" size="icon">
              <ArrowLeft className="h-4 w-4" />
            </Button>
          </Link>
          <Popover open={open} onOpenChange={setOpen}>
            <PopoverTrigger
              ref={triggerRef}
              render={
                <Button
                  variant="outline"
                  className="w-full justify-between bg-background text-lg h-12 font-normal"
                />
              }
            >
              {inputValue || t("searchPlaceholder")}
            </PopoverTrigger>
            <PopoverPortal>
              <PopoverPositioner>
                <PopoverPopup className="p-0 w-full" style={{ width: triggerRef.current?.offsetWidth }}>
                  <Command>
                    <CommandInput
                      placeholder={t("searchPlaceholder")}
                      ref={inputRef}
                      value={inputValue}
                      onChange={(e) => setInputValue(e.target.value)}
                      onKeyDown={(e) => {
                        if (e.key === "Enter" && filteredProducts.length > 0) {
                          handleSelect(filteredProducts[0])
                        }
                        if (e.key === "Escape") {
                          setOpen(false)
                          triggerRef.current?.focus()
                        }
                      }}
                    />
                    <CommandList>
                      <CommandEmpty>{t("productNotFound")}</CommandEmpty>
                      <CommandGroup>
                        {filteredProducts.slice(0, 20).map((product) => (
                          <CommandItem
                            key={product.id}
                            onClick={() => handleSelect(product)}
                            onKeyDown={(e) => {
                              if (e.key === "Enter") {
                                e.preventDefault()
                                handleSelect(product)
                              }
                            }}
                          >
                            <div className="flex flex-1 items-center justify-between">
                              <span>{product.name}</span>
                              <span className="text-muted-foreground text-sm">
                                {product.salePrice.toFixed(2)}
                              </span>
                            </div>
                          </CommandItem>
                        ))}
                      </CommandGroup>
                    </CommandList>
                  </Command>
                </PopoverPopup>
              </PopoverPositioner>
            </PopoverPortal>
          </Popover>
        </div>

        <Card className="flex-1 flex flex-col min-h-0">
          <CardHeader className="py-4">
            <CardTitle>{t("currentCart")}</CardTitle>
          </CardHeader>
          <CardContent className="flex-1 overflow-y-auto px-4 py-0">
            {cartItems.length === 0 ? (
              <div className="flex h-full items-center justify-center text-muted-foreground">
                {t("emptyCart")}
              </div>
            ) : (
              <div className="space-y-4 py-4">
                {cartItems.map((item) => (
                  <div key={item.id} className="flex items-center justify-between border-b pb-4">
                    <div className="flex-1">
                      <h4 className="font-semibold">{item.name}</h4>
                      <p className="text-sm text-muted-foreground">{item.salePrice.toFixed(2)} {t("each")}</p>
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
                        {(item.salePrice * item.quantity).toFixed(2)}
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

      <div className="w-full lg:w-96 flex flex-col gap-4">
        <Card className="flex-1 flex flex-col">
          <CardHeader>
            <CardTitle>{t("checkout")}</CardTitle>
          </CardHeader>
          <CardContent className="flex-1 flex flex-col justify-end gap-6">
            <div className="space-y-4">
              <div className="flex justify-between text-lg">
                <span className="text-muted-foreground">{t("subtotal")}</span>
                <span>{subtotal.toFixed(2)}</span>
              </div>
              <div className="flex items-center justify-between gap-4">
                <span className="text-muted-foreground text-lg">{t("discount")}</span>
                <div className="relative w-32">
                  <Input 
                    type="number" 
                    min="0" 
                    step="0.01" 
                    className="text-right text-lg h-12"
                    value={discount || ""}
                    onChange={(e) => setDiscount(parseFloat(e.target.value) || 0)}
                  />
                </div>
              </div>
              <div className="border-t pt-4 flex justify-between text-3xl font-bold">
                <span>{t("total")}</span>
                <span>{total.toFixed(2)}</span>
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4 mt-8">
              <Button variant="outline" className="h-16 text-lg" onClick={clearCart}>
                {t("clear")}
              </Button>
              <Button className="h-16 text-lg" size="lg" onClick={handleCheckout} disabled={isCheckingOut || cartItems.length === 0}>
                {isCheckingOut && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                {t("saveAndPrint")}
              </Button>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
