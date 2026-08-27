"use client"

import { EditorLine } from "../editor-client"
import { Product } from "@/types/domain/domain.types"
import { useTranslations } from "next-intl"
import { Button } from "@/components/ui/button"
import { TooltipIconButton } from "@/components/common/tooltip-icon-button"
import { Input } from "@/components/ui/input"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Plus, Trash2 } from "lucide-react"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"

interface LinesEditorProps {
  products: Product[]
  lines: EditorLine[]
  disabled: boolean
  onAdd: () => void
  onRemove: (key: string) => void
  onUpdate: (key: string, patch: Partial<EditorLine>) => void
}

export function LinesEditor({ products, lines, disabled, onAdd, onRemove, onUpdate }: LinesEditorProps) {
  const t = useTranslations("Purchases")

  const handleProductChange = (line: EditorLine, productId: string) => {
    const product = products.find((p) => p.id === productId)
    const firstUnit = product?.units?.[0]
    onUpdate(line.key, {
      productId,
      productUnitId: firstUnit?.id ?? "",
      unitCost: line.unitCost || String(firstUnit ? product?.buyPrice ?? "" : ""),
    })
  }

  const handleUnitChange = (line: EditorLine, unitId: string) => {
    onUpdate(line.key, { productUnitId: unitId })
  }

  return (
    <div className="rounded-lg border bg-card">
      <div className="flex items-center justify-between px-4 py-3 border-b">
        <span className="font-medium">{t("lines")}</span>
        <Button variant="outline" size="sm" onClick={onAdd} disabled={disabled}>
          <Plus className="mr-1 h-4 w-4" /> {t("addLine")}
        </Button>
      </div>
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>{t("product")}</TableHead>
            <TableHead>{t("unit")}</TableHead>
            <TableHead>{t("quantity")}</TableHead>
            <TableHead>{t("unitCost")}</TableHead>
            <TableHead>{t("newRetailPrice")}</TableHead>
            <TableHead>{t("newWholesalePrice")}</TableHead>
            <TableHead className="w-10" />
          </TableRow>
        </TableHeader>
        <TableBody>
          {lines.map((line) => {
            const product = products.find((p) => p.id === line.productId)
            return (
              <TableRow key={line.key}>
                <TableCell>
                  <Select
                    value={line.productId}
                    onValueChange={(v) => v != null && handleProductChange(line, v)}
                    disabled={disabled}
                    items={{
                      "": t("product"),
                      ...Object.fromEntries(products.map((p) => [p.id, p.name])),
                    }}
                  >
                    <SelectTrigger aria-label={t("product")} className="w-full min-w-[140px]">
                      <SelectValue placeholder={t("product")} />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="">{t("product")}</SelectItem>
                      {products.map((p) => (
                        <SelectItem key={p.id} value={p.id}>
                          {p.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </TableCell>
                <TableCell>
                  <Select
                    value={line.productUnitId}
                    onValueChange={(v) => v != null && handleUnitChange(line, v)}
                    disabled={disabled || !product}
                    items={{
                      ...Object.fromEntries((product?.units ?? []).map((u) => [u.id, u.unitName])),
                    }}
                  >
                    <SelectTrigger aria-label={t("unit")} className="w-full min-w-[100px]">
                      <SelectValue placeholder="—" />
                    </SelectTrigger>
                    <SelectContent>
                      {!product && <SelectItem value="">—</SelectItem>}
                      {(product?.units ?? []).map((u) => (
                        <SelectItem key={u.id} value={u.id}>
                          {u.unitName}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </TableCell>
                <TableCell>
                  <Input
                    type="number"
                    step="1"
                    min="0"
                    dir="ltr"
                    value={line.quantity}
                    onChange={(e) => onUpdate(line.key, { quantity: e.target.value })}
                    disabled={disabled}
                    className="w-20"
                  />
                </TableCell>
                <TableCell>
                  <Input
                    type="number"
                    step="1"
                    min="0"
                    dir="ltr"
                    value={line.unitCost}
                    onChange={(e) => onUpdate(line.key, { unitCost: e.target.value })}
                    disabled={disabled}
                    className="w-24"
                  />
                </TableCell>
                <TableCell>
                  <Input
                    type="number"
                    step="1"
                    min="0"
                    dir="ltr"
                    placeholder="—"
                    value={line.newRetailPrice}
                    onChange={(e) => onUpdate(line.key, { newRetailPrice: e.target.value })}
                    disabled={disabled}
                    className="w-24"
                  />
                </TableCell>
                <TableCell>
                  <Input
                    type="number"
                    step="1"
                    min="0"
                    dir="ltr"
                    placeholder="—"
                    value={line.newWholesalePrice}
                    onChange={(e) => onUpdate(line.key, { newWholesalePrice: e.target.value })}
                    disabled={disabled}
                    className="w-24"
                  />
                </TableCell>
                <TableCell>
                  <TooltipIconButton label={t("removeLine")} onClick={() => onRemove(line.key)} disabled={disabled}>
                    <Trash2 className="h-4 w-4" />
                  </TooltipIconButton>
                </TableCell>
              </TableRow>
            )
          })}
        </TableBody>
      </Table>
    </div>
  )
}
