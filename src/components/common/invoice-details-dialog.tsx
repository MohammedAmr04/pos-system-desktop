"use client"

import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"

// Simplified type for the client view
type InvoiceWithDetails = any

interface InvoiceDetailsDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  invoice: InvoiceWithDetails | null
}

export function InvoiceDetailsDialog({
  open,
  onOpenChange,
  invoice,
}: InvoiceDetailsDialogProps) {
  if (!invoice) return null

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[600px] max-h-[80vh] flex flex-col">
        <DialogHeader>
          <DialogTitle>Invoice Details</DialogTitle>
          <DialogDescription>
            Invoice #{invoice.id.split('-')[0].toUpperCase()} - {new Date(invoice.createdAt).toLocaleString()}
          </DialogDescription>
        </DialogHeader>
        <div className="flex-1 overflow-y-auto mt-4 pr-2">
           <table className="w-full text-sm mb-4">
             <thead>
               <tr className="border-b">
                 <th className="text-left py-2">Item</th>
                 <th className="text-center py-2">Qty</th>
                 <th className="text-right py-2">Price</th>
                 <th className="text-right py-2">Total</th>
               </tr>
             </thead>
             <tbody>
               {invoice.InvoiceDetail?.map((detail: any) => (
                 <tr key={detail.id} className="border-b last:border-0">
                   <td className="py-2">{detail.product?.name || 'Unknown Product'}</td>
                   <td className="text-center py-2">{detail.quantity}</td>
                   <td className="text-right py-2">${detail.salePrice.toFixed(2)}</td>
                   <td className="text-right py-2">${(detail.salePrice * detail.quantity).toFixed(2)}</td>
                 </tr>
               ))}
             </tbody>
           </table>
           
           <div className="space-y-2 border-t pt-4">
             <div className="flex justify-between text-muted-foreground">
               <span>Subtotal</span>
               <span>${(invoice.totalAmount + invoice.discount).toFixed(2)}</span>
             </div>
             <div className="flex justify-between text-muted-foreground">
               <span>Discount</span>
               <span>${invoice.discount?.toFixed(2)}</span>
             </div>
             <div className="flex justify-between font-bold text-lg pt-2 border-t">
               <span>Total</span>
               <span>${invoice.totalAmount?.toFixed(2)}</span>
             </div>
           </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
