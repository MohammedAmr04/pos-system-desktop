"use client"

import { useCallback, useEffect, useImperativeHandle, useRef, useState } from "react"
import { Product, ProductUnit } from "@/types/domain/domain.types"
import { usePOSStore } from "@/store/pos.store"
import { resolveBarcode } from "@/lib/barcode"
import { useTranslations } from "next-intl"
import { useDebouncedCallback } from "@/hooks/use-debounced-callback"
import { Loader2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { useProductSearch } from "@/hooks/use-products"
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

export interface ProductSearchHandle {
  /** Clear the input, close the popup and refocus the trigger. */
  reset: () => void
  /** Close the popup without clearing the input. */
  close: () => void
}

interface ProductSearchPopoverProps {
  products: Product[]
  /** unit is provided when resolved from an exact barcode match. */
  onSelect: (product: Product, unit?: ProductUnit) => void
  onUnknownBarcode: (barcode: string) => void
  ref?: React.Ref<ProductSearchHandle>
}

export function ProductSearchPopover({ products, onSelect, onUnknownBarcode, ref }: ProductSearchPopoverProps) {
  const t = useTranslations("POS")
  const setSearchQuery = usePOSStore((s) => s.setSearchQuery)

  const [open, setOpen] = useState(false)
  const [inputValue, setInputValue] = useState("")
  const [query, setQuery] = useState("")
  const [triggerWidth, setTriggerWidth] = useState(0)
  const inputRef = useRef<HTMLInputElement>(null)
  const triggerRef = useRef<HTMLButtonElement>(null)

  const debouncedSetQuery = useDebouncedCallback((v: string) => setQuery(v), 300)

  const { data: searchResultsData, isFetching: isSearching } = useProductSearch(query, 20)
  const searchResults: Product[] = searchResultsData ?? []

  const focusTrigger = useCallback(() => {
    triggerRef.current?.focus()
  }, [])

  useImperativeHandle(ref, () => ({
    reset: () => {
      setInputValue("")
      setQuery("")
      setSearchQuery("")
      setOpen(false)
      requestAnimationFrame(focusTrigger)
    },
    close: () => setOpen(false),
  }), [setSearchQuery, focusTrigger, debouncedSetQuery])

  const handleOpenChange = useCallback((next: boolean) => {
    if (!next) setInputValue("")
    setOpen(next)
  }, [])

  useEffect(() => {
    if (open && triggerRef.current) {
      setTriggerWidth(triggerRef.current.offsetWidth)
    }
    if (open && inputRef.current) {
      inputRef.current.focus()
    }
  }, [open])

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'F1') {
        e.preventDefault()
        if (!open) setOpen(true)
        requestAnimationFrame(() => inputRef.current?.focus())
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [open])

  return (
    <Popover open={open} onOpenChange={handleOpenChange}>
      <PopoverTrigger
        ref={triggerRef}
        render={
          <Button
            variant="outline"
            className="w-full justify-between bg-background text-lg h-12 font-normal"
          />
        }
      >
        <span className="truncate">{inputValue || t("searchPlaceholder")}</span>
        <kbd className="ml-auto pointer-events-none inline-flex h-5 select-none items-center gap-1 rounded border bg-muted px-1.5 font-mono text-[10px] font-medium text-muted-foreground opacity-60">
          F1
        </kbd>
      </PopoverTrigger>
      <PopoverPortal>
        <PopoverPositioner>
          <PopoverPopup className="p-0 w-full" style={{ width: triggerWidth || undefined }}>
            <Command>
              <CommandInput
                placeholder={t("searchPlaceholder")}
                ref={inputRef}
                value={inputValue}
                onChange={(e) => {
                  setInputValue(e.target.value)
                  debouncedSetQuery(e.target.value)
                }}
                onKeyDown={(e) => {
                  if (e.key === "Enter") {
                    const trimmed = inputValue.trim()
                    if (trimmed) {
                      const exact = resolveBarcode(products, trimmed)
                      if (exact) {
                        onSelect(exact.product, exact.unit)
                        return
                      }
                      if (/^\d{4,}$/.test(trimmed)) {
                        onUnknownBarcode(trimmed)
                        return
                      }
                    }
                    if (searchResults.length > 0) {
                      onSelect(searchResults[0])
                    }
                  } else if (e.key === "Escape") {
                    setOpen(false)
                    focusTrigger()
                  }
                }}
              />
              {inputValue.trim().length >= 1 && (
                <CommandList>
                  {isSearching ? (
                    <div className="flex items-center justify-center py-6">
                      <Loader2 className="h-5 w-5 animate-spin" />
                    </div>
                  ) : searchResults.length === 0 ? (
                    <CommandEmpty>{t("productNotFound")}</CommandEmpty>
                  ) : (
                    <CommandGroup>
                      {searchResults.map((product) => (
                        <CommandItem
                          key={product.id}
                          onClick={() => onSelect(product)}
                          onKeyDown={(e) => {
                            if (e.key === "Enter") {
                              e.preventDefault()
                              onSelect(product)
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
                  )}
                </CommandList>
              )}
            </Command>
          </PopoverPopup>
        </PopoverPositioner>
      </PopoverPortal>
    </Popover>
  )
}
