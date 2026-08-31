"use client"

import { POSClient } from "./pos-client"

export default function POSPage() {
  return (
    <div className="flex min-h-screen flex-col bg-background lg:h-screen lg:overflow-hidden">
      <POSClient />
    </div>
  )
}
