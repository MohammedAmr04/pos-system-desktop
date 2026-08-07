"use client"

import { api, Invoice } from "@/lib/api"
import { useEffect, useState } from "react"
import { Button } from "@/components/ui/button"
import { Eye, Loader2 } from "lucide-react"
import {
  ColumnDef,
  SortingState,
  flexRender,
  getCoreRowModel,
  getSortedRowModel,
  useReactTable,
} from "@tanstack/react-table"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { useTranslations } from "next-intl"
import { DatePicker } from "@/components/ui/date-picker"
import { InvoiceDetailsDialog } from "@/components/common/invoice-details-dialog"
import { Input } from "@/components/ui/input"
import { startOfWeek, endOfWeek, startOfMonth, endOfMonth } from "date-fns"

interface InvoicesClientProps {
  items: Invoice[]
  total: number
  page: number
  pageSize: number
  loading: boolean
  totals: { revenue: number; discounts: number }
  query: string
  fromDate?: Date
  toDate?: Date
  onQueryChange: (query: string) => void
  onDateChange: (from?: Date, to?: Date) => void
  onPageChange: (page: number) => void
}

export function InvoicesClient({
  items,
  total,
  page,
  pageSize,
  loading,
  totals,
  query,
  fromDate,
  toDate,
  onQueryChange,
  onDateChange,
  onPageChange,
}: InvoicesClientProps) {
  const t = useTranslations("Invoices")
  const tc = useTranslations("Common")
  const [selectedInvoice, setSelectedInvoice] = useState<Invoice | null>(null)
  const [isDialogOpen, setIsDialogOpen] = useState(false)
  const [sorting, setSorting] = useState<SortingState>([])
  const [searchInput, setSearchInput] = useState(query)

  useEffect(() => {
    const value = searchInput.trim()
    if (value === query) return
    const timer = setTimeout(() => onQueryChange(value), 300)
    return () => clearTimeout(timer)
  }, [searchInput, query, onQueryChange])

  const pageCount = Math.max(1, Math.ceil(total / pageSize))

  const handleView = async (invoice: Invoice) => {
    try {
      const full = await api.invoices.get(invoice.id)
      setSelectedInvoice(full)
      setIsDialogOpen(true)
    } catch {
      // ignore
    }
  }

  const columns: ColumnDef<Invoice>[] = [
    {
      accessorKey: "invoiceNumber",
      header: t("invoiceNumber"),
    },
    {
      accessorKey: "createdAt",
      header: t("time"),
      cell: ({ row }) => new Date(row.original.createdAt).toLocaleTimeString(),
    },
    {
      accessorKey: "totalAmount",
      header: t("total"),
      cell: ({ row }) => `${row.original.totalAmount.toFixed(2)}`,
    },
    {
      accessorKey: "discount",
      header: t("discount"),
      cell: ({ row }) => {
        const inv = row.original
        const type = inv.discountType === 'percentage' ? '%' : inv.discountType === 'fixed' ? '' : ''
        return `${inv.discount.toFixed(2)}${type}`
      }

    },
    {
      id: "actions",
      header: t("actions"),
      cell: ({ row }) => (
        <Button variant="ghost" size="icon" onClick={() => handleView(row.original)}>
          <Eye className="h-4 w-4" />
        </Button>
      ),
    },
  ]

  const table = useReactTable({
    data: items,
    columns,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
    onSortingChange: setSorting,
    manualPagination: true,
    pageCount,
    state: {
      sorting,
      pagination: { pageIndex: page - 1, pageSize },
    },
    onPaginationChange: (updater) => {
      const next =
        typeof updater === "function"
          ? updater({ pageIndex: page - 1, pageSize })
          : updater
      onPageChange(next.pageIndex + 1)
    },
  })

  const handleFromChange = (date: Date | undefined) => {
    onDateChange(date, toDate)
  }

  const handleToChange = (date: Date | undefined) => {
    onDateChange(fromDate, date)
  }

  const quickFilter = (preset: "today" | "thisWeek" | "thisMonth" | "all") => {
    const now = new Date()
    switch (preset) {
      case "today":
        onDateChange(now, undefined)
        break
      case "thisWeek": {
        const weekStart = startOfWeek(now, { weekStartsOn: 6 })
        const weekEnd = endOfWeek(now, { weekStartsOn: 6 })
        onDateChange(weekStart, weekEnd)
        break
      }
      case "thisMonth": {
        const monthStart = startOfMonth(now)
        const monthEnd = endOfMonth(now)
        onDateChange(monthStart, monthEnd)
        break
      }
      case "all":
        onDateChange(undefined, undefined)
        break
    }
  }

  return (
    <>
      <div className="flex flex-wrap items-center gap-3 mb-4">
        <Input
          placeholder={t("searchByNumber")}
          value={searchInput}
          onChange={(e) => setSearchInput(e.target.value)}
          className="max-w-sm"
        />
        <DatePicker
          value={fromDate}
          onChange={handleFromChange}
          placeholder={t("fromDate")}
        />
        <span className="text-muted-foreground text-sm">-</span>
        <DatePicker
          value={toDate}
          onChange={handleToChange}
          placeholder={t("toDate")}
        />
        <div className="flex gap-2 mr-auto">
          <Button variant="outline" size="sm" onClick={() => quickFilter("today")}>
            {t("today")}
          </Button>
          <Button variant="outline" size="sm" onClick={() => quickFilter("thisWeek")}>
            {t("thisWeek")}
          </Button>
          <Button variant="outline" size="sm" onClick={() => quickFilter("thisMonth")}>
            {t("thisMonth")}
          </Button>
          <Button variant="outline" size="sm" onClick={() => quickFilter("all")}>
            {t("all")}
          </Button>
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4 mb-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">{t("totalRevenue")}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{totals.revenue.toFixed(2)}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">{t("totalDiscounts")}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{totals.discounts.toFixed(2)}</div>
          </CardContent>
        </Card>
      </div>

      <div className="rounded-md border bg-card">
        <Table>
          <TableHeader>
            {table.getHeaderGroups().map((headerGroup) => (
              <TableRow key={headerGroup.id}>
                {headerGroup.headers.map((header) => (
                  <TableHead key={header.id}>
                    {header.isPlaceholder
                      ? null
                      : flexRender(
                          header.column.columnDef.header,
                          header.getContext()
                        )}
                  </TableHead>
                ))}
              </TableRow>
            ))}
          </TableHeader>
          <TableBody>
            {loading ? (
              <TableRow>
                <TableCell colSpan={columns.length} className="h-24 text-center">
                  <Loader2 className="mx-auto h-6 w-6 animate-spin" />
                </TableCell>
              </TableRow>
            ) : table.getRowModel().rows?.length ? (
              table.getRowModel().rows.map((row) => (
                <TableRow key={row.id}>
                  {row.getVisibleCells().map((cell) => (
                    <TableCell key={cell.id}>
                      {flexRender(cell.column.columnDef.cell, cell.getContext())}
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : (
              <TableRow>
                <TableCell colSpan={columns.length} className="h-24 text-center">
                  {tc("noResults")}
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>

      <div className="flex items-center justify-between py-4">
        <span className="text-sm text-muted-foreground">
          {t("invoicesCount", { count: total })}
        </span>
        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={() => onPageChange(page - 1)}
            disabled={page <= 1}
          >
            {tc("previous")}
          </Button>
          <span className="text-sm text-muted-foreground whitespace-nowrap">
            {t("pageInfo", { page, pageCount })}
          </span>
          <Button
            variant="outline"
            size="sm"
            onClick={() => onPageChange(page + 1)}
            disabled={page >= pageCount}
          >
            {tc("next")}
          </Button>
        </div>
      </div>

      <InvoiceDetailsDialog
        open={isDialogOpen}
        onOpenChange={setIsDialogOpen}
        invoice={selectedInvoice}
      />
    </>
  )
}
