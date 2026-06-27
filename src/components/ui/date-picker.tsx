"use client"

import * as React from "react"
import { format } from "date-fns"
import { CalendarIcon } from "lucide-react"
import { cn } from "@/lib/utils"
import { Button } from "@/components/ui/button"
import {
  Popover,
  PopoverPopup,
  PopoverPortal,
  PopoverPositioner,
  PopoverTrigger,
} from "@/components/ui/popover"
import { Calendar } from "@/components/ui/calendar"

type DatePickerProps = {
  value?: Date
  onChange: (date: Date | undefined) => void
  placeholder?: string
  className?: string
}

export function DatePicker({ value, onChange, placeholder, className }: DatePickerProps) {
  const [open, setOpen] = React.useState(false)

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger
        render={
          <Button
            variant="outline"
            className={cn("justify-start text-left font-normal", !value && "text-muted-foreground", className)}
          />
        }
      >
        {value ? format(value, "PPP") : (placeholder || "Pick a date")}
        <CalendarIcon className="ml-auto h-4 w-4 opacity-50" />
      </PopoverTrigger>
      <PopoverPortal>
        <PopoverPositioner>
          <PopoverPopup className="p-0 w-auto">
            <Calendar
              selected={value}
              onSelect={(date) => {
                onChange(date)
                setOpen(false)
              }}
            />
          </PopoverPopup>
        </PopoverPositioner>
      </PopoverPortal>
    </Popover>
  )
}
