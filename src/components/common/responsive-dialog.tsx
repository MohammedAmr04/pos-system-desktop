"use client"

import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"

interface ResponsiveDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  title: string
  description?: string
  children: React.ReactNode
  footer?: React.ReactNode
}

/**
 * Shared dialog shell (title/description/scrollable body/footer). Mirrors
 * `ResponsiveSheet`; use for focused create/edit/confirm/details actions.
 */
export function ResponsiveDialog({
  open,
  onOpenChange,
  title,
  description,
  children,
  footer,
}: ResponsiveDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[540px] flex flex-col !gap-0 max-h-[85vh]">
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
          {description && <DialogDescription>{description}</DialogDescription>}
        </DialogHeader>
        <div className="flex-1 overflow-y-auto px-6 py-4">{children}</div>
        {footer && <div className="border-t px-6 py-4 mt-auto">{footer}</div>}
      </DialogContent>
    </Dialog>
  )
}
