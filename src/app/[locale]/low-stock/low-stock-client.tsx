"use client"

import { Product } from "@/lib/api"
import { DataTable } from "@/components/common/data-table"
import { ColumnDef } from "@tanstack/react-table"
import { useTranslations } from "next-intl"
import { AlertTriangle } from "lucide-react"

interface LowStockClientProps {
  data: Product[]
}

export function LowStockClient({ data }: LowStockClientProps) {
  const t = useTranslations("LowStock")

  const columns: ColumnDef<Product>[] = [
    {
      accessorKey: "name",
      header: t("productName"),
    },
    {
      accessorKey: "barcode",
      header: t("barcode"),
    },
    {
      accessorKey: "stockQuantity",
      header: t("currentStock"),
      cell: ({ row }) => (
        <span className={row.original.stockQuantity <= 0 ? "text-destructive font-semibold" : ""}>
          {row.original.stockQuantity}
        </span>
      ),
    },
    {
      accessorKey: "lowStockThreshold",
      header: t("threshold"),
    },
    {
      accessorKey: "salePrice",
      header: t("salePrice"),
      cell: ({ row }) => `${row.original.salePrice.toFixed(2)}`,
    },
  ]

  return (
    <div className="flex-1 space-y-4 pt-6">
      <div className="flex items-center gap-3">
        <AlertTriangle className="h-6 w-6 text-amber-500" />
        <h2 className="text-3xl font-bold tracking-tight">{t("title")}</h2>
      </div>
      {data.length === 0 ? (
        <div className="flex items-center justify-center h-32 text-muted-foreground">
          {t("empty")}
        </div>
      ) : (
        <DataTable columns={columns} data={data} searchKey="name" />
      )}
    </div>
  )
}
