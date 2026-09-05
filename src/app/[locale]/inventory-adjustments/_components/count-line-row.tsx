"use client"

import { useState } from "react"
import { Check, Eye, Trash2 } from "lucide-react"
import { useTranslations } from "next-intl"
import { toast } from "sonner"
import { removeInventoryCountLine, saveInventoryCountLine } from "@/actions/operations.actions"
import { TooltipIconButton } from "@/components/common/tooltip-icon-button"
import { Input } from "@/components/ui/input"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { TableCell, TableRow } from "@/components/ui/table"
import { useProduct } from "@/hooks/use-products"
import { InventoryAdjustmentLine } from "@/types/domain/domain.types"

export function CountLineRow({
  adjustmentId,
  line,
  editable,
  onDetails,
}: {
  adjustmentId: string
  line: InventoryAdjustmentLine
  editable: boolean
  onDetails: (id: string) => void
}) {
  const t = useTranslations("InventoryCounts")
  const { data: product } = useProduct(line.productId)
  const [count, setCount] = useState(String(line.countedQuantity))
  const [unitCost, setUnitCost] = useState(String(line.unitCost))
  const [unitId, setUnitId] = useState(line.productUnitId)
  const [saving, setSaving] = useState(false)
  const units = product?.units ?? []
  const selectedUnit = units.find((unit) => unit.id === unitId)
  const update = async (
    changes: Partial<{
      countedQuantity: number
      productUnitId: string
      unitCost: number
      isMatched: boolean
    }>,
  ) => {
    const selectedUnit = product?.units?.find((unit) => unit.id === (changes.productUnitId ?? unitId))
    if (!selectedUnit) return
    setSaving(true)
    try {
      await saveInventoryCountLine(adjustmentId, {
        productId: line.productId,
        productUnitId: selectedUnit.id,
        countedQuantity: changes.countedQuantity ?? Number(count),
        unitCost: changes.unitCost ?? Number(unitCost),
        isMatched: changes.isMatched ?? line.isMatched,
      })
      toast.success(t("saved"))
    } catch (error) {
      toast.error(error instanceof Error ? error.message : t("saveFailed"))
    } finally {
      setSaving(false)
    }
  }
  const remove = async () => {
    setSaving(true)
    try {
      await removeInventoryCountLine(adjustmentId, line.id)
      toast.success(t("saved"))
    } catch (error) {
      toast.error(error instanceof Error ? error.message : t("saveFailed"))
    } finally {
      setSaving(false)
    }
  }
  return (
    <TableRow id={`inventory-line-${line.id}`}>
      <TableCell>
        <p className="font-medium">{line.productName}</p>
        <p className="text-xs text-muted-foreground" dir="ltr">{line.barcode ?? "-"}</p>
      </TableCell>
      <TableCell className="text-center" dir="ltr">{line.systemQuantity}</TableCell>
      <TableCell>
        <Select
          value={selectedUnit?.id ?? null}
          onValueChange={(value) => {
            if (value == null) return
            setUnitId(value)
            void update({ productUnitId: value })
          }}
          disabled={!editable || saving || !product?.units?.length}
        >
          <SelectTrigger aria-label={t("unit")} className="w-28">
            <SelectValue placeholder={line.unitName}>{selectedUnit?.unitName ?? line.unitName}</SelectValue>
          </SelectTrigger>
          <SelectContent>
            {units.map((unit) => (
              <SelectItem key={unit.id} value={unit.id}>{unit.unitName}</SelectItem>
            ))}
          </SelectContent>
        </Select>
      </TableCell>
      <TableCell>
        <Input
          disabled={!editable || saving}
          className="w-24"
          dir="ltr"
          type="number"
          min="0"
          step="any"
          value={count}
          onChange={(event) => setCount(event.target.value)}
          onBlur={() => {
            if (count !== String(line.countedQuantity)) void update({ countedQuantity: Number(count) })
          }}
        />
      </TableCell>
      <TableCell className="text-center" dir="ltr">{line.wholesalePrice?.toFixed(2) ?? "-"}</TableCell>
      <TableCell className="text-center" dir="ltr">{line.retailPrice.toFixed(2)}</TableCell>
      <TableCell>
        <Input
          disabled={!editable || saving}
          className="w-24"
          dir="ltr"
          type="number"
          min="0"
          step="any"
          value={unitCost}
          onChange={(event) => setUnitCost(event.target.value)}
          onBlur={() => {
            if (unitCost !== String(line.unitCost)) void update({ unitCost: Number(unitCost) })
          }}
        />
      </TableCell>
      <TableCell className={line.differenceQuantity === 0 ? "text-center" : "text-center font-semibold text-amber-700"} dir="ltr">
        {line.differenceQuantity > 0 ? "+" : ""}{line.differenceQuantity}
      </TableCell>
      <TableCell>
        <TooltipIconButton
          label={t("markMatched")}
          size="icon-sm"
          variant={line.isMatched ? "secondary" : "ghost"}
          disabled={!editable || saving}
          onClick={() => {
            setCount(String(line.systemQuantity / line.quantityFactor))
            void update({ isMatched: true, countedQuantity: line.systemQuantity / line.quantityFactor })
          }}
        >
          <Check className="size-4" />
        </TooltipIconButton>
      </TableCell>
      <TableCell>
        <div className="flex gap-1">
          <TooltipIconButton
            label={t("productDetails")}
            size="icon-sm"
            variant="ghost"
            onClick={() => onDetails(line.productId)}
          >
            <Eye className="size-4" />
          </TooltipIconButton>
          {editable && (
            <TooltipIconButton
              label={t("removeProduct")}
              size="icon-sm"
              variant="ghost"
              disabled={saving}
              onClick={() => void remove()}
            >
              <Trash2 className="size-4 text-destructive" />
            </TooltipIconButton>
          )}
        </div>
      </TableCell>
    </TableRow>
  )
}
