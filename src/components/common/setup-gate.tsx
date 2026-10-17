"use client"

import { useEffect } from "react"
import { usePathname, useRouter } from "@/i18n/navigation"
import { useSetupStatus } from "@/hooks/use-branches"

const SETUP_PATH = "/setup/"

export function SetupGate({ children }: { children: React.ReactNode }) {
  const pathname = usePathname()
  const router = useRouter()
  const { data, isLoading, isError } = useSetupStatus()

  const onSetup = pathname === SETUP_PATH

  useEffect(() => {
    if (isLoading || isError || !data) return
    if (!data.setupComplete && !onSetup) {
      router.replace(SETUP_PATH)
    } else if (data.setupComplete && onSetup) {
      router.replace("/")
    }
  }, [data, isLoading, isError, onSetup, router])

  const redirecting =
    !isError && (isLoading || !data || (!data.setupComplete && !onSetup) || (data.setupComplete && onSetup))

  if (redirecting) {
    return (
      <div className="flex h-screen items-center justify-center bg-background">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
      </div>
    )
  }

  return <>{children}</>
}
