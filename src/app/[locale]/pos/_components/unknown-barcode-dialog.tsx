"use client"

import { useState } from "react"
import { Product } from "@/types/domain/domain.types"
import { useTranslations } from "next-intl"
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
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { X, PackagePlus, Link2 } from "lucide-react"
import { allBarcodes } from "@/lib/barcode"
import { ProductForm, PRODUCT_FORM_ID } from "@/components/common/product-form"

type UnknownFlow = "options" | "create" | "link"

interface UnknownBarcodeDialogProps {
  barcode: string
  products: Product[]
  canCreateProduct: boolean
  canLinkBarcode: boolean
  onClose: () => void
  onConfirmLink: (product: Product) => void
  onCreateSuccess: () => void
}

export function UnknownBarcodeDialog({
  barcode,
  products,
  canCreateProduct,
  canLinkBarcode,
  onClose,
  onConfirmLink,
  onCreateSuccess,
}: UnknownBarcodeDialogProps) {
  const t = useTranslations("POS")
  const [flow, setFlow] = useState<UnknownFlow>("options")
  const [linkQuery, setLinkQuery] = useState("")
  const [selectedLinkProduct, setSelectedLinkProduct] = useState<Product | null>(null)

  const linkedProducts = products.filter((p) => {
    const q = linkQuery.trim().toLowerCase()
    if (!q) return true
    return p.name.toLowerCase().includes(q) || allBarcodes(p).some((b) => b.toLowerCase().includes(q))
  })

  return (
    <Dialog open onOpenChange={(o) => { if (!o) onClose() }}>
      <DialogContent className="sm:max-w-[440px]">
        {flow === "options" && (
          <>
            <DialogHeader>
              <DialogTitle>{t("barcodeNotFoundTitle")}</DialogTitle>
              <DialogDescription>{t("barcodeNotFoundMessage")}</DialogDescription>
            </DialogHeader>
            <div className="py-2">
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <span>{t("scannedBarcode")}:</span>
                <span className="rounded-lg bg-muted px-3 py-1 font-mono text-base text-foreground">{barcode}</span>
              </div>
            </div>
            <DialogFooter className="flex-col gap-2 sm:flex-col">
              {canCreateProduct && (
                <Button onClick={() => setFlow("create")}>
                  <PackagePlus className="mr-2 h-4 w-4" /> {t("createNewProduct")}
                </Button>
              )}
              {canLinkBarcode && (
                <Button variant="outline" onClick={() => setFlow("link")}>
                  <Link2 className="mr-2 h-4 w-4" /> {t("linkToExistingProduct")}
                </Button>
              )}
              {!canCreateProduct && !canLinkBarcode && (
                <p className="text-sm text-muted-foreground">{t("noBarcodeActions")}</p>
              )}
              <Button variant="ghost" onClick={onClose}>
                {t("cancel")}
              </Button>
            </DialogFooter>
          </>
        )}

        {flow === "create" && (
          <>
            <DialogHeader>
              <DialogTitle>{t("createNewProduct")}</DialogTitle>
              <DialogDescription>{t("createNewProductDescription")}</DialogDescription>
            </DialogHeader>
            <div className="py-2">
              <ProductForm
                key={barcode}
                defaultBarcode={barcode}
                onSuccess={onCreateSuccess}
              />
            </div>
            <DialogFooter className="flex-col gap-2 sm:flex-row">
              <Button variant="outline" onClick={() => setFlow("options")}>
                {t("back")}
              </Button>
              <Button type="submit" form={PRODUCT_FORM_ID}>
                {t("save")}
              </Button>
            </DialogFooter>
          </>
        )}

        {flow === "link" && (
          <>
            <DialogHeader>
              <DialogTitle>{t("linkToExistingProduct")}</DialogTitle>
              <DialogDescription>{t("linkToExistingProductDescription")}</DialogDescription>
            </DialogHeader>
            <div className="py-2 space-y-3">
              <div className="flex items-center gap-2 rounded-lg border bg-muted/50 px-3 py-1.5 font-mono text-base">
                {barcode}
              </div>
              <Command className="rounded-lg border">
                <CommandInput
                  placeholder={t("searchProduct")}
                  value={linkQuery}
                  onChange={(e) => setLinkQuery(e.target.value)}
                  autoFocus
                />
                <CommandList>
                  <CommandEmpty>{t("noResults")}</CommandEmpty>
                  <CommandGroup>
                    {linkedProducts.slice(0, 10).map((p) => (
                      <CommandItem
                        key={p.id}
                        onSelect={() => setSelectedLinkProduct(p)}
                        className={selectedLinkProduct?.id === p.id ? "bg-primary/10" : undefined}
                      >
                        <div className="flex flex-1 items-center justify-between">
                          <span>{p.name}</span>
                          <span className="text-muted-foreground text-sm">{p.salePrice.toFixed(2)}</span>
                        </div>
                      </CommandItem>
                    ))}
                  </CommandGroup>
                </CommandList>
              </Command>
              {selectedLinkProduct && (
                <div className="flex items-center justify-between rounded-lg border px-3 py-2 text-sm">
                  <span className="font-medium">{selectedLinkProduct.name}</span>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => setSelectedLinkProduct(null)}
                    aria-label={t("clear")}
                  >
                    <X className="h-4 w-4" />
                  </Button>
                </div>
              )}
            </div>
            <DialogFooter className="flex-col gap-2 sm:flex-row">
              <Button variant="outline" onClick={() => setFlow("options")}>
                {t("back")}
              </Button>
              <Button onClick={() => selectedLinkProduct && onConfirmLink(selectedLinkProduct)} disabled={!selectedLinkProduct}>
                {t("confirmLink")}
              </Button>
            </DialogFooter>
          </>
        )}
      </DialogContent>
    </Dialog>
  )
}
