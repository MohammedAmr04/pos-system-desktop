"use client"

import { useMemo, useState } from "react"
import { useTranslations } from "next-intl"
import { Product } from "@/types/domain/domain.types"
import { useLowStockReport } from "@/hooks/use-reports"
import { Input } from "@/components/ui/input"
import { TableColumn, TableBuilder } from "@/components/common/table-builder"
import { AlertTriangle } from "lucide-react"

export function LowStockClient() {
  const t = useTranslations("LowStock")
  const tc = useTranslations("Common")
  const [searchInput, setSearchInput] = useState("")

  const { data, isPending } = useLowStockReport()
  const products = useMemo(
    () =>
      (data ?? []).filter((p) =>
        searchInput.trim()
          ? (p.name ?? "").toLowerCase().includes(searchInput.trim().toLowerCase())
          : true
      ),
    [data, searchInput]
  )

  const columns: TableColumn<Product>[] = [
    {
      key: "name",
      header: t("productName"),
      cell: (product) => product.name,
    },
    {
      key: "barcode",
      header: t("barcode"),
      cell: (product) => product.barcode,
    },
    {
      key: "stockQuantity",
      header: t("currentStock"),
      cell: (product) => (
        <span className={product.stockQuantity <= 0 ? "text-destructive font-semibold" : ""}>
          {product.stockQuantity}
        </span>
      ),
    },
    {
      key: "lowStockThreshold",
      header: t("threshold"),
      cell: (product) => product.lowStockThreshold,
    },
    {
      key: "salePrice",
      header: t("salePrice"),
      cell: (product) => `${product.salePrice.toFixed(2)}`,
    },
  ]

  return (
    <div className="flex-1 space-y-4 pt-6">
      <div className="flex items-center gap-3">
        <AlertTriangle className="h-6 w-6 text-amber-500" />
        <h2 className="text-3xl font-bold tracking-tight">{t("title")}</h2>
      </div>

      <Input
        placeholder={tc("search")}
        value={searchInput}
        onChange={(e) => setSearchInput(e.target.value)}
        className="max-w-sm mb-4"
      />

      <TableBuilder
        columns={columns}
        data={products}
        rowKey={(product) => product.id}
        loading={isPending}
        emptyMessage={t("empty")}
      />
    </div>
  )
}
