"use client"

import * as React from "react"

import { cn } from "@/lib/utils"
import { SearchIcon } from "lucide-react"

function Command({
  className,
  ...props
}: React.ComponentProps<"div">) {
  return (
    <div
      data-slot="command"
      className={cn(
        "flex h-full w-full flex-col overflow-hidden rounded-xl bg-popover text-popover-foreground",
        className
      )}
      {...props}
    />
  )
}

const CommandInput = React.forwardRef<
  HTMLInputElement,
  React.ComponentPropsWithoutRef<"input">
>(({ className, ...props }, ref) => {
  return (
    <div
      data-slot="command-input-wrapper"
      className="flex items-center border-b px-3"
    >
      <SearchIcon className="ml-2 h-4 w-4 shrink-0 opacity-50" />
      <input
        ref={ref}
        data-slot="command-input"
        className={cn(
          "flex h-10 w-full rounded-xl bg-transparent py-3 text-sm outline-none placeholder:text-muted-foreground disabled:cursor-not-allowed disabled:opacity-50",
          className
        )}
        {...props}
      />
    </div>
  )
})
CommandInput.displayName = "CommandInput"

function CommandList({
  className,
  ...props
}: React.ComponentProps<"div">) {
  return (
    <div
      data-slot="command-list"
      className={cn(
        "max-h-[300px] overflow-y-auto overflow-x-hidden",
        className
      )}
      {...props}
    />
  )
}

function CommandEmpty({
  className,
  ...props
}: React.ComponentProps<"div">) {
  return (
    <div
      data-slot="command-empty"
      className={cn("py-6 text-center text-sm", className)}
      {...props}
    />
  )
}

function CommandGroup({
  className,
  ...props
}: React.ComponentProps<"div">) {
  return (
    <div
      data-slot="command-group"
      className={cn(
        "overflow-hidden p-1 text-foreground [&_[cmdk-group-heading]]:px-2 [&_[cmdk-group-heading]]:py-1.5 [&_[cmdk-group-heading]]:text-xs [&_[cmdk-group-heading]]:font-medium [&_[cmdk-group-heading]]:text-muted-foreground",
        className
      )}
      {...props}
    />
  )
}

function CommandItem({
  className,
  ...props
}: React.ComponentProps<"div">) {
  return (
    <div
      data-slot="command-item"
      className={cn(
        "relative flex cursor-default items-center gap-2 rounded-lg px-2 py-1.5 text-sm outline-none select-none aria-selected:bg-accent aria-selected:text-accent-foreground data-disabled:pointer-events-none data-disabled:opacity-50 hover:bg-accent hover:text-accent-foreground [&_svg]:pointer-events-none [&_svg]:size-4 [&_svg]:shrink-0",
        className
      )}
      {...props}
    />
  )
}

function CommandSeparator({
  className,
  ...props
}: React.ComponentProps<"hr">) {
  return (
    <hr
      data-slot="command-separator"
      className={cn("-mx-1 h-px bg-border", className)}
      {...props}
    />
  )
}

export {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
  CommandSeparator,
}
