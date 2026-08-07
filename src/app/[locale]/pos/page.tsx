"use client"

import { POSClient } from "./pos-client"

export default function POSPage() {
  return (
    <div className="flex h-screen flex-col overflow-hidden bg-background">
      <POSClient />
    </div>
  )
}
