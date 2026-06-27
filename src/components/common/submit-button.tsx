"use client"

import { useFormStatus } from "react-dom"
import { Button } from "@/components/ui/button"
import { Loader2 } from "lucide-react"
import { useTranslations } from "next-intl"

interface SubmitButtonProps extends React.ComponentProps<typeof Button> {
  loadingText?: string
}

export function SubmitButton({ children, ...props }: SubmitButtonProps) {
  const { pending } = useFormStatus()
  const t = useTranslations("Products")

  return (
    <Button type="submit" disabled={pending || props.disabled} {...props}>
      {pending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
      {pending ? t("saving") : children}
    </Button>
  )
}
