"use client"

import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet"

interface ResponsiveSheetProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  title: string
  description?: string
  children: React.ReactNode
  footer?: React.ReactNode
}

export function ResponsiveSheet({
  open,
  onOpenChange,
  title,
  description,
  children,
  footer,
}: ResponsiveSheetProps) {
  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="sm:max-w-[540px] flex flex-col !gap-0">
        <SheetHeader>
          <SheetTitle>{title}</SheetTitle>
          {description && <SheetDescription>{description}</SheetDescription>}
        </SheetHeader>
        <div className="flex-1 overflow-y-auto px-6 py-4">
          {children}
        </div>
        {footer && (
          <div className="border-t px-6 py-4 mt-auto">
            {footer}
          </div>
        )}
      </SheetContent>
    </Sheet>
  )
}
