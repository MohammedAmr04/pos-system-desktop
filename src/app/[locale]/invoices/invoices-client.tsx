"use client"

import { DataTable } from "@/components/common/data-table"
import { InvoiceDetailsDialog } from "@/components/common/invoice-details-dialog"
import { useState, useCallback, useEffect } from "react"
import { Button } from "@/components/ui/button"
import { Eye, CalendarIcon } from "lucide-react"
import { ColumnDef } from "@tanstack/react-table"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { useTranslations } from "next-intl"
import { DatePicker } from "@/components/ui/date-picker"
import { getFilteredInvoices } from "@/features/invoices/actions"
import { format, startOfWeek, endOfWeek, startOfMonth, endOfMonth } from "date-fns"

type InvoiceWithDetails = any

interface InvoicesClientProps {
  data: InvoiceWithDetails[]
  onRefresh?: () => void
}

export function InvoicesClient({ data: initialData, onRefresh }: InvoicesClientProps) {
  const t = useTranslations("Invoices")
  const [selectedInvoice, setSelectedInvoice] = useState<InvoiceWithDetails | null>(null)
  const [isDialogOpen, setIsDialogOpen] = useState(false)
  const [invoices, setInvoices] = useState<InvoiceWithDetails[]>(initialData)
  const [fromDate, setFromDate] = useState<Date | undefined>(undefined)
  const [toDate, setToDate] = useState<Date | undefined>(undefined)

  useEffect(() => {
    setInvoices(initialData)
  }, [initialData])

  const fetchFiltered = useCallback(async (from?: Date, to?: Date) => {
    const result = await getFilteredInvoices(
      from ? format(from, "yyyy-MM-dd") : undefined,
      to ? format(to, "yyyy-MM-dd") : undefined
    )
    setInvoices(result)
  }, [])

  const handleFromChange = (date: Date | undefined) => {
    setFromDate(date)
    fetchFiltered(date, toDate)
  }

  const handleToChange = (date: Date | undefined) => {
    setToDate(date)
    fetchFiltered(fromDate, date)
  }

  const quickFilter = (preset: "today" | "thisWeek" | "thisMonth" | "all") => {
    const now = new Date()
    switch (preset) {
      case "today":
        setFromDate(now)
        setToDate(undefined)
        fetchFiltered(now, undefined)
        break
      case "thisWeek": {
        const weekStart = startOfWeek(now, { weekStartsOn: 6 })
        const weekEnd = endOfWeek(now, { weekStartsOn: 6 })
        setFromDate(weekStart)
        setToDate(weekEnd)
        fetchFiltered(weekStart, weekEnd)
        break
      }
      case "thisMonth": {
        const monthStart = startOfMonth(now)
        const monthEnd = endOfMonth(now)
        setFromDate(monthStart)
        setToDate(monthEnd)
        fetchFiltered(monthStart, monthEnd)
        break
      }
      case "all":
        setFromDate(undefined)
        setToDate(undefined)
        fetchFiltered(undefined, undefined)
        break
    }
  }

  const handleView = (invoice: InvoiceWithDetails) => {
    setSelectedInvoice(invoice)
    setIsDialogOpen(true)
  }

  const columns: ColumnDef<InvoiceWithDetails>[] = [
    {
      accessorKey: "id",
      header: t("invoiceId"),
      cell: ({ row }) => row.original.id.split('-')[0].toUpperCase()
    },
    {
      accessorKey: "createdAt",
      header: t("time"),
      cell: ({ row }) => new Date(row.original.createdAt).toLocaleTimeString()
    },
    {
      accessorKey: "totalAmount",
      header: t("total"),
      cell: ({ row }) => `${row.original.totalAmount.toFixed(2)}`
    },
    {
      accessorKey: "discount",
      header: t("discount"),
      cell: ({ row }) => `${row.original.discount.toFixed(2)}`
    },
    {
      id: "actions",
      header: t("actions"),
      cell: ({ row }) => (
        <Button variant="ghost" size="icon" onClick={() => handleView(row.original)}>
          <Eye className="h-4 w-4" />
        </Button>
      )
    }
  ]

  const totalSales = invoices.reduce((acc, inv) => acc + inv.totalAmount, 0)
  const totalDiscounts = invoices.reduce((acc, inv) => acc + inv.discount, 0)

  return (
    <>
      <div className="flex flex-wrap items-center gap-3 mb-4">
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
            <div className="text-2xl font-bold">{totalSales.toFixed(2)}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">{t("totalDiscounts")}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{totalDiscounts.toFixed(2)}</div>
          </CardContent>
        </Card>
      </div>

      <DataTable columns={columns} data={invoices} searchKey="id" />

      <InvoiceDetailsDialog 
        open={isDialogOpen} 
        onOpenChange={setIsDialogOpen} 
        invoice={selectedInvoice} 
      />
    </>
  )
}
