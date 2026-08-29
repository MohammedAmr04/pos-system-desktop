"use client"

import { useState } from "react"
import { Product } from "@/types/domain/domain.types"
import { useTranslations } from "next-intl"
import { ChevronsUpDown } from "lucide-react"
import { Button } from "@/components/ui/button"
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

interface SearchableProductSelectProps {
  products: Product[]
  value: string
  onValueChange: (productId: string) => void
  disabled?: boolean
}

export function SearchableProductSelect({
  products,
  value,
  onValueChange,
  disabled,
}: SearchableProductSelectProps) {
  const t = useTranslations("Purchases")
  const [open, setOpen] = useState(false)
  const [query, setQuery] = useState("")

  const selected = products.find((p) => p.id === value)

  const filtered = query.trim()
    ? products.filter((p) => p.name.toLowerCase().includes(query.trim().toLowerCase()))
    : products

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger
        render={<Button type="button" variant="outline" disabled={disabled} className="w-full min-w-[140px] justify-between bg-background font-normal" />}
      >
        <span className="truncate">{selected?.name || t("product")}</span>
        <ChevronsUpDown className="ms-auto h-4 w-4 shrink-0 opacity-50" />
      </PopoverTrigger>
      <PopoverPortal>
        <PopoverPositioner align="start" className="w-[var(--anchor-width)]">
          <PopoverPopup className="p-0">
            <Command>
              <CommandInput
                value={query}
                onChange={(e) => setQuery(e.target.value)}
                placeholder={t("searchProduct")}
                className="h-9"
              />
              <CommandList>
                {filtered.length === 0 ? (
                  <CommandEmpty>{t("productNotFound")}</CommandEmpty>
                ) : (
                  <CommandGroup>
                    {filtered.map((p) => (
                      <CommandItem
                        key={p.id}
                        onClick={() => {
                          onValueChange(p.id)
                          setQuery("")
                          setOpen(false)
                        }}
                      >
                        {p.name}
                      </CommandItem>
                    ))}
                  </CommandGroup>
                )}
              </CommandList>
            </Command>
          </PopoverPopup>
        </PopoverPositioner>
      </PopoverPortal>
    </Popover>
  )
}