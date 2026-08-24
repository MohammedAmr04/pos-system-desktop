"use client"

import { Suspense } from "react"
import { Loader2 } from "lucide-react"
import { EditorClient } from "./editor-client"

export default function PurchaseEditorPage() {
  return (
    <Suspense fallback={<Loader2 className="mx-auto mt-10 h-6 w-6 animate-spin" />}>
      <EditorClient />
    </Suspense>
  )
}
