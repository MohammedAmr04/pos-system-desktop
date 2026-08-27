"use client"

import { useState } from "react"
import { useTranslations } from "next-intl"
import { toast } from "sonner"
import { Boxes, Edit, Plus, Trash } from "lucide-react"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { Product, ProductBarcode, ProductUnit } from "@/types/domain/domain.types"
import {
  addProductBarcode,
  addProductUnit,
  deleteProductUnit,
  removeProductBarcode,
  setDefaultProductBarcode,
  updateProductUnit,
} from "@/actions/products.actions"
import { useAllUnits } from "@/hooks/use-units"
import { Button } from "@/components/ui/button"
import { TooltipIconButton } from "@/components/common/tooltip-icon-button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"

interface UnitFormState {
  unit?: ProductUnit
  unitMasterId: string
  quantityFactor: string
  retailPrice: string
  wholesalePrice: string
}

interface ProductUnitsManagerProps {
  product: Product
  canManageUnits: boolean
  canManageBarcodes: boolean
  onRefresh?: () => void
}

export function ProductUnitsManager({
  product,
  canManageUnits,
  canManageBarcodes,
  onRefresh,
}: ProductUnitsManagerProps) {
  const t = useTranslations("Products")
  const [isUnitFormOpen, setIsUnitFormOpen] = useState(false)
  const [unitForm, setUnitForm] = useState<UnitFormState>({
    unitMasterId: "",
    quantityFactor: "1",
    retailPrice: "",
    wholesalePrice: "",
  })
  const [isAddBarcodeOpen, setIsAddBarcodeOpen] = useState(false)
  const [newBarcode, setNewBarcode] = useState("")
  const [addBarcodeForUnit, setAddBarcodeForUnit] = useState<ProductUnit | null>(null)

  const { data: masterUnits = [] } = useAllUnits()
  const activeMasterUnits = masterUnits.filter((u) => u.isActive)

  const selectedUnitMaster = masterUnits.find((u) => u.id === unitForm.unitMasterId)
  const unitNameFallback = !unitForm.unitMasterId && unitForm.unit ? unitForm.unit.unitName : ""

  const openAddUnit = () => {
    setUnitForm({
      unitMasterId: activeMasterUnits[0]?.id ?? "",
      quantityFactor: "1",
      retailPrice: "",
      wholesalePrice: "",
    })
    setIsUnitFormOpen(true)
  }

  const openEditUnit = (unit: ProductUnit) => {
    setUnitForm({
      unit,
      unitMasterId: unit.unitId ?? "",
      quantityFactor: String(unit.quantityFactor),
      retailPrice: String(unit.retailPrice),
      wholesalePrice: unit.wholesalePrice != null ? String(unit.wholesalePrice) : "",
    })
    setIsUnitFormOpen(true)
  }

  const handleSaveUnit = async () => {
    const quantityFactor = parseFloat(unitForm.quantityFactor)
    const retailPrice = parseFloat(unitForm.retailPrice)
    const wholesalePrice = unitForm.wholesalePrice.trim()
      ? parseFloat(unitForm.wholesalePrice)
      : null
    if (!selectedUnitMaster || isNaN(quantityFactor) || quantityFactor <= 0 || isNaN(retailPrice) || retailPrice <= 0) return
    try {
      if (unitForm.unit) {
        await updateProductUnit(product.id, unitForm.unit.id, {
          unitId: selectedUnitMaster.id,
          unitName: selectedUnitMaster.name,
          quantityFactor,
          retailPrice,
          wholesalePrice,
        })
        toast.success(t("unitUpdated"))
      } else {
        await addProductUnit(product.id, {
          unitId: selectedUnitMaster.id,
          unitName: selectedUnitMaster.name,
          quantityFactor,
          retailPrice,
          wholesalePrice,
        })
        toast.success(t("unitAdded"))
      }
      setIsUnitFormOpen(false)
      onRefresh?.()
    } catch (e) {
      toast.error((e as Error).message || t("unitSaveFailed"))
    }
  }

  const handleDeleteUnit = async (unit: ProductUnit) => {
    if (unit.isBaseUnit) return
    if (!confirm(t("unitDeleteConfirm"))) return
    try {
      await deleteProductUnit(product.id, unit.id)
      toast.success(t("unitDeleted"))
      onRefresh?.()
    } catch (e) {
      toast.error((e as Error).message || t("unitDeleteFailed"))
    }
  }

  const openAddBarcode = (unit: ProductUnit) => {
    setAddBarcodeForUnit(unit)
    setNewBarcode("")
    setIsAddBarcodeOpen(true)
  }

  const handleAddBarcodeConfirm = async () => {
    if (!addBarcodeForUnit || !newBarcode.trim()) return
    try {
      await addProductBarcode(product.id, addBarcodeForUnit.id, newBarcode.trim())
      toast.success(t("barcodeAdded"))
      setNewBarcode("")
      setIsAddBarcodeOpen(false)
      onRefresh?.()
    } catch (e) {
      toast.error((e as Error).message || t("barcodeAddFailed"))
    }
  }

  const handleDeleteBarcode = async (unit: ProductUnit, barcode: ProductBarcode) => {
    if (!confirm(t("deleteBarcodeConfirm"))) return
    try {
      await removeProductBarcode(product.id, unit.id, barcode.id)
      toast.success(t("barcodeDeleted"))
      onRefresh?.()
    } catch (e) {
      toast.error((e as Error).message || t("barcodeDeleteFailed"))
    }
  }

  const handleSetDefault = async (unit: ProductUnit, barcodeId: string) => {
    try {
      await setDefaultProductBarcode(product.id, unit.id, barcodeId)
      toast.success(t("defaultBarcodeUpdated"))
      onRefresh?.()
    } catch (e) {
      toast.error((e as Error).message || t("defaultBarcodeUpdateFailed"))
    }
  }

  return (
    <div className="border-t pt-4 space-y-4">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Boxes className="h-4 w-4 text-muted-foreground" />
          <h4 className="text-sm font-semibold">{t("units")}</h4>
        </div>
        <Button variant="outline" size="sm" onClick={openAddUnit}>
          <Plus className="mr-2 h-4 w-4" /> {t("addUnit")}
        </Button>
      </div>
      {product.units && product.units.length > 0 ? (
        <div className="space-y-3">
          {product.units.map((unit) => (
            <div key={unit.id} className="rounded-lg border p-3">
              <div className="flex items-center justify-between gap-2">
                <div className="min-w-0">
                  <div className="flex items-center gap-2">
                    <span className="font-semibold">{unit.unitName}</span>
                    {unit.isBaseUnit && (
                      <span className="shrink-0 inline-flex items-center rounded bg-primary/10 px-1.5 py-0.5 text-[10px] font-medium text-primary">
                        {t("baseUnit")}
                      </span>
                    )}
                    {unit.quantityFactor !== 1 && (
                      <span className="text-xs text-muted-foreground">
                        {t("factorLabel")}: {unit.quantityFactor}
                      </span>
                    )}
                  </div>
                  <div className="text-xs text-muted-foreground mt-0.5">
                    {t("retailPrice")}: {unit.retailPrice.toFixed(2)}
                    {unit.wholesalePrice != null && (
                      <> | {t("wholesalePrice")}: {unit.wholesalePrice.toFixed(2)}</>
                    )}
                  </div>
                </div>
                {!unit.isBaseUnit && canManageUnits && (
                  <div className="flex items-center gap-1 shrink-0">
                    <TooltipIconButton label={t("edit")} onClick={() => openEditUnit(unit)} size="sm">
                      <Edit className="h-4 w-4" />
                    </TooltipIconButton>
                    <TooltipIconButton label={t("delete")} onClick={() => handleDeleteUnit(unit)}>
                      <Trash className="h-4 w-4 text-destructive" />
                    </TooltipIconButton>
                  </div>
                )}
              </div>
              {canManageBarcodes && (
                <div className="mt-2 space-y-1">
                  {unit.barcodes && unit.barcodes.length > 0 ? (
                    unit.barcodes.map((b) => (
                      <div key={b.id} className="flex items-center justify-between gap-2 rounded border bg-muted/40 px-2 py-1">
                        <div className="flex items-center gap-2 min-w-0">
                          <span className="font-mono text-xs truncate">{b.barcode}</span>
                          {b.isDefault && (
                            <span className="shrink-0 inline-flex items-center rounded bg-primary/10 px-1.5 py-0.5 text-[10px] font-medium text-primary">
                              {t("primary")}
                            </span>
                          )}
                        </div>
                        {!b.isDefault && (
                          <div className="flex items-center gap-1 shrink-0">
                            <Button variant="ghost" size="sm" onClick={() => handleSetDefault(unit, b.id)}>
                              {t("setAsDefault")}
                            </Button>
                            <TooltipIconButton label={t("delete")} onClick={() => handleDeleteBarcode(unit, b)}>
                              <Trash className="h-4 w-4 text-destructive" />
                            </TooltipIconButton>
                          </div>
                        )}
                      </div>
                    ))
                  ) : (
                    <p className="text-xs text-muted-foreground">{t("noBarcodes")}</p>
                  )}
                  <Button variant="ghost" size="sm" onClick={() => openAddBarcode(unit)}>
                    <Plus className="mr-2 h-4 w-4" /> {t("addBarcode")}
                  </Button>
                </div>
              )}
            </div>
          ))}
        </div>
      ) : (
        <p className="text-sm text-muted-foreground">{t("noUnits")}</p>
      )}

      <Dialog open={isAddBarcodeOpen} onOpenChange={setIsAddBarcodeOpen}>
        <DialogContent className="sm:max-w-[400px]">
          <DialogHeader>
            <DialogTitle>{t("addBarcode")}</DialogTitle>
            <DialogDescription>
              {t("addBarcodeDescription")}
            </DialogDescription>
          </DialogHeader>
          <div className="py-4">
            <p className="mb-3 text-sm text-muted-foreground">
              <strong>{t("name")}:</strong> {product.name}
              {addBarcodeForUnit && <> ({addBarcodeForUnit.unitName})</>}
            </p>
            <label className="text-sm font-medium">{t("barcode")} *</label>
            <Input
              className="mt-1 font-mono"
              value={newBarcode}
              onChange={(e) => setNewBarcode(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === "Enter") handleAddBarcodeConfirm()
              }}
              placeholder={t("barcode")}
              autoFocus
            />
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setIsAddBarcodeOpen(false)}>
              {t("cancel")}
            </Button>
            <Button onClick={handleAddBarcodeConfirm} disabled={!newBarcode.trim()}>
              {t("save")}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={isUnitFormOpen} onOpenChange={setIsUnitFormOpen}>
        <DialogContent className="sm:max-w-[440px]">
          <DialogHeader>
            <DialogTitle>{unitForm.unit ? t("editUnit") : t("addUnit")}</DialogTitle>
            <DialogDescription>
              {unitForm.unit?.isBaseUnit ? t("baseUnitLocked") : t("unitDescription")}
            </DialogDescription>
          </DialogHeader>
          <div className="py-4 space-y-4">
            <div className="space-y-2">
              <label className="text-sm font-medium">{t("unitName")} *</label>
              <Select
                value={unitForm.unitMasterId}
                onValueChange={(v) => v != null && setUnitForm({ ...unitForm, unitMasterId: v })}
              >
                <SelectTrigger className="w-full">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {!selectedUnitMaster && (
                    <SelectItem value={unitForm.unitMasterId}>
                      {unitNameFallback || t("selectUnit")}
                    </SelectItem>
                  )}
                  {activeMasterUnits.map((u) => (
                    <SelectItem key={u.id} value={u.id}>
                      {u.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <label className="text-sm font-medium">{t("quantityFactor")} *</label>
                <Input
                  type="number"
                  step="1"
                  min="0.01"
                  value={unitForm.quantityFactor}
                  disabled={!!unitForm.unit?.isBaseUnit}
                  onChange={(e) => setUnitForm({ ...unitForm, quantityFactor: e.target.value })}
                />
              </div>
              <div className="space-y-2">
                <label className="text-sm font-medium">{t("retailPrice")} *</label>
                <Input
                  type="number"
                  step="1"
                  min="0.01"
                  value={unitForm.retailPrice}
                  onChange={(e) => setUnitForm({ ...unitForm, retailPrice: e.target.value })}
                />
              </div>
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t("wholesalePrice")}</label>
              <Input
                type="number"
                step="1"
                min="0"
                value={unitForm.wholesalePrice}
                onChange={(e) => setUnitForm({ ...unitForm, wholesalePrice: e.target.value })}
                placeholder={t("optional")}
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setIsUnitFormOpen(false)}>
              {t("cancel")}
            </Button>
            <Button
              onClick={handleSaveUnit}
              disabled={!selectedUnitMaster || isNaN(parseFloat(unitForm.quantityFactor)) || parseFloat(unitForm.quantityFactor) <= 0 || isNaN(parseFloat(unitForm.retailPrice)) || parseFloat(unitForm.retailPrice) <= 0}
            >
              {t("save")}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
