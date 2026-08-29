export function SummaryCard({
  label,
  value,
  highlight,
  tone,
}: {
  label: string
  value: string
  highlight?: boolean
  tone?: "neutral" | "positive" | "negative"
}) {
  let toneClass = ""
  if (tone === "positive") toneClass = "text-emerald-600"
  else if (tone === "negative") toneClass = "text-red-600"
  return (
    <div className={`rounded-lg border bg-card p-4 ${highlight ? "border-primary/40" : ""}`}>
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className={`mt-1 text-xl font-bold ${toneClass}`} dir="ltr">{value}</p>
    </div>
  )
}
