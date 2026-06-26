"use client"

import { DataTable } from "@/components/common/data-table"
import { InvoiceDetailsDialog } from "@/components/common/invoice-details-dialog"
import { useState } from "react"
import { Button } from "@/components/ui/button"
import { Eye } from "lucide-react"
import { ColumnDef } from "@tanstack/react-table"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"

type InvoiceWithDetails = any

interface InvoicesClientProps {
  data: InvoiceWithDetails[]
}

export function InvoicesClient({ data }: InvoicesClientProps) {
  const [selectedInvoice, setSelectedInvoice] = useState<InvoiceWithDetails | null>(null)
  const [isDialogOpen, setIsDialogOpen] = useState(false)

  const handleView = (invoice: InvoiceWithDetails) => {
    setSelectedInvoice(invoice)
    setIsDialogOpen(true)
  }

  const columns: ColumnDef<InvoiceWithDetails>[] = [
    {
      accessorKey: "id",
      header: "Invoice ID",
      cell: ({ row }) => row.original.id.split('-')[0].toUpperCase()
    },
    {
      accessorKey: "createdAt",
      header: "Time",
      cell: ({ row }) => new Date(row.original.createdAt).toLocaleTimeString()
    },
    {
      accessorKey: "totalAmount",
      header: "Total",
      cell: ({ row }) => `$${row.original.totalAmount.toFixed(2)}`
    },
    {
      accessorKey: "discount",
      header: "Discount",
      cell: ({ row }) => `$${row.original.discount.toFixed(2)}`
    },
    {
      id: "actions",
      cell: ({ row }) => (
        <Button variant="ghost" size="icon" onClick={() => handleView(row.original)}>
          <Eye className="h-4 w-4" />
        </Button>
      )
    }
  ]

  const totalSales = data.reduce((acc, inv) => acc + inv.totalAmount, 0)
  const totalDiscounts = data.reduce((acc, inv) => acc + inv.discount, 0)

  return (
    <>
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4 mb-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Sales Revenue</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">${totalSales.toFixed(2)}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Given Discounts</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">${totalDiscounts.toFixed(2)}</div>
          </CardContent>
        </Card>
      </div>

      <DataTable columns={columns} data={data} searchKey="id" />

      <InvoiceDetailsDialog 
        open={isDialogOpen} 
        onOpenChange={setIsDialogOpen} 
        invoice={selectedInvoice} 
      />
    </>
  )
}
