"use client"

import { useState } from "react"
import { useTranslations } from "next-intl"
import { toast } from "sonner"
import { Loader2 } from "lucide-react"
import { Product } from "@/types/domain/domain.types"
import { printBarcodeLabel } from "@/actions/printing.actions"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"

interface BarcodePrintDialogProps {
  product: Product | null
  onOpenChange: (open: boolean) => void
}

export function BarcodePrintDialog({ product, onOpenChange }: BarcodePrintDialogProps) {
  const t = useTranslations("Products")
  const [count, setCount] = useState(1)
  const [isPrinting, setIsPrinting] = useState(false)

  const handlePrint = async () => {
    if (!product) return
    setIsPrinting(true)
    try {
      await printBarcodeLabel({
        barcode: product.barcode ?? "",
        name: product.name,
        price: product.salePrice,
        count,
      })
      onOpenChange(false)
      toast.success(t("barcodePrinting"))
    } catch {
      toast.error(t("barcodePrintFailed"))
    } finally {
      setIsPrinting(false)
    }
  }

  return (
    <Dialog open={product !== null} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[400px]">
        <DialogHeader>
          <DialogTitle>{t("printBarcode")}</DialogTitle>
          <DialogDescription>
            {t("barcodePrintDescription")}
          </DialogDescription>
        </DialogHeader>
        <div className="py-4">
          {product && (
            <div className="mb-4 text-sm">
              <p><strong>{t("name")}:</strong> {product.name}</p>
              <p><strong>{t("barcode")}:</strong> {product.barcode}</p>
              <p><strong>{t("salePrice")}:</strong> {product.salePrice.toFixed(2)}</p>
            </div>
          )}
          <div className="flex items-center gap-4">
            <label className="text-sm font-medium whitespace-nowrap">{t("barcodePrintCount")}:</label>
            <Input
              type="number"
              min="1"
              max="100"
              value={count}
              onChange={(e) => setCount(parseInt(e.target.value) || 1)}
            />
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            {t("cancel")}
          </Button>
          <Button onClick={handlePrint} disabled={isPrinting}>
            {isPrinting ? <Loader2 className="h-4 w-4 animate-spin" /> : null}
            {t("print")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
