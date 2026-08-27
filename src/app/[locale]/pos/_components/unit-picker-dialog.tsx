"use client"

import { Product, ProductUnit } from "@/types/domain/domain.types"
import { useTranslations } from "next-intl"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Boxes } from "lucide-react"

export interface UnitPickerState {
  product: Product
  onPick: (unit: ProductUnit) => void
}

interface UnitPickerDialogProps {
  picker: UnitPickerState | null
  onClose: () => void
}

export function UnitPickerDialog({ picker, onClose }: UnitPickerDialogProps) {
  const t = useTranslations("POS")

  return (
    <Dialog open={picker !== null} onOpenChange={(o) => { if (!o) onClose() }}>
      <DialogContent className="sm:max-w-[440px]">
        <DialogHeader>
          <DialogTitle>{t("chooseUnit")}</DialogTitle>
          <DialogDescription>{t("chooseUnitDescription")}</DialogDescription>
        </DialogHeader>
        <div className="py-2 space-y-2">
          {picker?.product.units?.map((unit) => (
            <Button
              key={unit.id}
              variant="outline"
              className="w-full h-auto py-3 justify-between"
              onClick={() => picker.onPick(unit)}
            >
              <div className="flex items-center gap-3">
                <Boxes className="h-5 w-5 text-muted-foreground" />
                <div className="text-left">
                  <div className="font-semibold">
                    {unit.unitName}
                    {unit.isBaseUnit && <span className="mr-2 text-[10px] text-muted-foreground">({t("baseUnit")})</span>}
                  </div>
                  <div className="text-xs text-muted-foreground">
                    {unit.quantityFactor === 1
                      ? `1 ${unit.unitName}`
                      : `1 ${unit.unitName} = ${unit.quantityFactor}`}
                  </div>
                </div>
              </div>
              <div className="text-right">
                <div className="font-semibold">{unit.retailPrice.toFixed(2)}</div>
                {unit.wholesalePrice != null && (
                  <div className="text-xs text-muted-foreground">{t("wholesale")}: {unit.wholesalePrice.toFixed(2)}</div>
                )}
              </div>
            </Button>
          ))}
        </div>
      </DialogContent>
    </Dialog>
  )
}
