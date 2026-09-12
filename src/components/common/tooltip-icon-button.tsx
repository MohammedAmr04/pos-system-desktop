"use client"

import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip"
import { Button } from "@/components/ui/button"

type ButtonSize = React.ComponentProps<typeof Button>["size"]
type ButtonVariant = React.ComponentProps<typeof Button>["variant"]

interface TooltipIconButtonProps extends Omit<React.ComponentProps<"button">, "aria-label" | "title"> {
  label: string
  size?: ButtonSize
  variant?: ButtonVariant
}

/**
 * Icon-only button with a mandatory accessible label and a tooltip — use this
 * instead of a bare `size="icon"` Button anywhere in the app.
 */
export function TooltipIconButton({ label, size = "icon", variant = "ghost", ...props }: TooltipIconButtonProps) {
  return (
    <TooltipProvider>
      <Tooltip>
        <TooltipTrigger
          render={
            <Button size={size} variant={variant} aria-label={label} {...props} />
          }
        />
        <TooltipContent>{label}</TooltipContent>
      </Tooltip>
    </TooltipProvider>
  )
}
