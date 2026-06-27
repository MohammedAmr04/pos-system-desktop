"use client"

import * as React from "react"
import { ChevronLeft, ChevronRight } from "lucide-react"
import { cn } from "@/lib/utils"
import { Button } from "@/components/ui/button"
import {
  startOfMonth,
  endOfMonth,
  startOfWeek,
  endOfWeek,
  eachDayOfInterval,
  addMonths,
  subMonths,
  isSameMonth,
  isSameDay,
  isToday,
  format,
} from "date-fns"

export type CalendarProps = {
  selected?: Date
  onSelect: (date: Date) => void
  className?: string
}

const dayNames = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"]

export function Calendar({ selected, onSelect, className }: CalendarProps) {
  const [currentMonth, setCurrentMonth] = React.useState(new Date())

  const days = eachDayOfInterval({
    start: startOfWeek(startOfMonth(currentMonth)),
    end: endOfWeek(endOfMonth(currentMonth)),
  })

  return (
    <div className={cn("p-3", className)}>
      <div className="flex items-center justify-between mb-3">
        <Button
          variant="ghost"
          size="icon-sm"
          onClick={() => setCurrentMonth(subMonths(currentMonth, 1))}
        >
          <ChevronLeft className="h-4 w-4" />
        </Button>
        <div className="text-sm font-medium">
          {format(currentMonth, "MMMM yyyy")}
        </div>
        <Button
          variant="ghost"
          size="icon-sm"
          onClick={() => setCurrentMonth(addMonths(currentMonth, 1))}
        >
          <ChevronRight className="h-4 w-4" />
        </Button>
      </div>
      <div className="grid grid-cols-7 gap-1 text-center text-xs text-muted-foreground mb-2">
        {dayNames.map((name) => (
          <div key={name} className="py-1">{name}</div>
        ))}
      </div>
      <div className="grid grid-cols-7 gap-1">
        {days.map((day) => {
          const isSelected = selected && isSameDay(day, selected)
          const isCurrentMonth = isSameMonth(day, currentMonth)
          return (
            <Button
              key={day.toISOString()}
              variant="ghost"
              size="icon-sm"
              className={cn(
                "h-8 w-8 p-0 text-xs font-normal",
                !isCurrentMonth && "text-muted-foreground opacity-50",
                isToday(day) && "border border-border",
                isSelected && "bg-primary text-primary-foreground hover:bg-primary hover:text-primary-foreground"
              )}
              onClick={() => onSelect(day)}
            >
              {format(day, "d")}
            </Button>
          )
        })}
      </div>
    </div>
  )
}
