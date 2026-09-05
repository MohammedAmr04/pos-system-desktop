"use client"

import { useDeferredValue, useState } from "react"
import { useTranslations } from "next-intl"
import { toast } from "sonner"
import { cancelInventoryCount, postInventoryCount, saveInventoryCountLine } from "@/actions/operations.actions"
import { getProduct } from "@/api/products"
import { useAuth } from "@/components/common/auth-context"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { useProductSearch } from "@/hooks/use-products"
import { useInventoryAdjustment } from "@/hooks/use-operations"
import { PERMISSIONS } from "@/lib/constants"
import { Product } from "@/types/domain/domain.types"
import { CountLineRow } from "./count-line-row"
import { CountStatusBadge } from "./count-status-badge"
import { ProductDetailsSheet } from "./product-details-sheet"

export function InventorySession({ id, onBack }: { id: string; onBack: () => void }) {
  const t = useTranslations("InventoryCounts")
  const { hasPermission } = useAuth()
  const canEdit = hasPermission(PERMISSIONS.INVENTORY_ADJUSTMENTS_CREATE)
  const { data: session, isPending } = useInventoryAdjustment(id)
  const [search, setSearch] = useState("")
  const deferredSearch = useDeferredValue(search)
  const { data: matches = [] } = useProductSearch(
    deferredSearch,
    10,
    canEdit && !!session && session.status !== "posted" && session.status !== "cancelled",
  )
  const [detailProductId, setDetailProductId] = useState<string | null>(null)
  const [posting, setPosting] = useState(false)
  if (isPending || !session) return <div className="pt-6 text-muted-foreground">{t("loading")}</div>
  const editable = canEdit && (session.status === "draft" || session.status === "counting")
  const addProduct = async (match: Product) => {
    const existing = session.lines.find((line) => line.productId === match.id)
    if (existing) {
      document.getElementById(`inventory-line-${existing.id}`)?.scrollIntoView({ behavior: "smooth", block: "center" })
      return
    }
    try {
      const product = await getProduct(match.id)
      const baseUnit = product.units?.find((unit) => unit.isBaseUnit) ?? product.units?.[0]
      if (!baseUnit) throw new Error(t("missingUnit"))
      await saveInventoryCountLine(id, {
        productId: product.id,
        productUnitId: baseUnit.id,
        countedQuantity: product.stockQuantity / baseUnit.quantityFactor,
        unitCost: product.buyPrice,
        isMatched: false,
      })
      setSearch("")
      toast.success(t("productAdded"))
    } catch (error) {
      toast.error(error instanceof Error ? error.message : t("saveFailed"))
    }
  }
  const post = async () => {
    if (!confirm(t("postConfirm", { count: session.lineCount, differences: session.differenceCount }))) return
    setPosting(true)
    try {
      await postInventoryCount(id)
      toast.success(t("posted"))
    } catch (error) {
      toast.error(error instanceof Error ? error.message : t("postFailed"))
    } finally {
      setPosting(false)
    }
  }
  const cancel = async () => {
    if (!confirm(t("cancelConfirm"))) return
    try {
      await cancelInventoryCount(id)
      toast.success(t("cancelled"))
      onBack()
    } catch (error) {
      toast.error(error instanceof Error ? error.message : t("saveFailed"))
    }
  }
  return (
    <div className="flex-1 space-y-5 pt-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <Button variant="ghost" onClick={onBack}>{t("back")}</Button>
          <h2 className="mt-2 text-3xl font-bold">{t("sessionNumber", { number: session.number })}</h2>
          <p className="text-muted-foreground">{session.reason} · <CountStatusBadge status={session.status} /></p>
        </div>
        <div className="flex gap-2">
          {editable && (
            <>
              <Button variant="outline" onClick={() => void cancel()}>{t("cancelSession")}</Button>
              <Button disabled={posting || session.lineCount === 0} onClick={() => void post()}>{t("post")}</Button>
            </>
          )}
        </div>
      </div>
      {editable && (
        <div className="relative max-w-2xl">
          <Input
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder={t("searchPlaceholder")}
          />
          {deferredSearch.trim() && (
            <div className="absolute z-20 mt-1 w-full rounded-lg border bg-popover p-1 shadow-lg">
              {matches.length ? (
                matches.map((product) => (
                  <Button
                    key={product.id}
                    variant="ghost"
                    className="flex w-full items-center justify-between px-3 py-2 text-end font-normal"
                    onClick={() => void addProduct(product)}
                  >
                    <span>{product.name}</span>
                    <span className="text-xs text-muted-foreground" dir="ltr">{product.barcode ?? ""}</span>
                  </Button>
                ))
              ) : (
                <p className="px-3 py-2 text-sm text-muted-foreground">{t("noProducts")}</p>
              )}
            </div>
          )}
        </div>
      )}
      <div className="overflow-x-auto rounded-xl border">
        <Table className="min-w-[1100px]">
          <TableHeader className="bg-muted/50">
            <TableRow>
              <TableHead className="text-center">{t("product")}</TableHead>
              <TableHead className="text-center">{t("recorded")}</TableHead>
              <TableHead className="text-center">{t("unit")}</TableHead>
              <TableHead className="text-center">{t("current")}</TableHead>
              <TableHead className="text-center">{t("wholesale")}</TableHead>
              <TableHead className="text-center">{t("retail")}</TableHead>
              <TableHead className="text-center">{t("cost")}</TableHead>
              <TableHead className="text-center">{t("difference")}</TableHead>
              <TableHead className="text-center">{t("matched")}</TableHead>
              <TableHead className="text-center">{t("actions")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {session.lines.map((line) => (
              <CountLineRow
                key={line.id}
                adjustmentId={id}
                line={line}
                editable={editable}
                onDetails={setDetailProductId}
              />
            ))}
            {!session.lines.length && (
              <TableRow>
                <TableCell colSpan={10} className="py-12 text-center text-muted-foreground">
                  {t("emptyLines")}
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>
      <ProductDetailsSheet
        productId={detailProductId}
        open={!!detailProductId}
        onOpenChange={(open) => {
          if (!open) setDetailProductId(null)
        }}
      />
    </div>
  )
}
