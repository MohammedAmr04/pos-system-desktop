import { useCallback } from "react"
import { useLocale, useTranslations } from "next-intl"

const DYNAMIC_KEYS: Array<[RegExp, string]> = [
  [/^no selling unit found for product .+$/i, "no-selling-unit-found-for-product"],
  [/^product not found: .+$/i, "product-not-found"],
  [/^quantity must be greater than zero for .+$/i, "quantity-must-be-greater-than-zero"],
  [/^unit price cannot be negative for .+$/i, "unit-price-cannot-be-negative"],
  [/^line discount cannot exceed line total for .+$/i, "line-discount-cannot-exceed-line-total"],
  [/^profit protection: .+ would sell below cost price$/i, "profit-protection-below-cost"],
  [/^password must be \d+-\d+ characters$/i, "password-length-range"],
  [/^username already taken: .+$/i, "username-already-taken"],
  [/^unknown role\(s\): .+$/i, "unknown-roles"],
  [/^unknown permission\(s\): .+$/i, "unknown-permissions"],
  [/^unknown feature: .+$/i, "unknown-feature"],
  [/^invalid return quantity; maximum available is .+$/i, "invalid-return-quantity"],
  [/^invalid unit for product .+$/i, "invalid-unit-for-product"],
  [/^invalid return quantity for product .+$/i, "invalid-return-quantity-for-product"],
  [/^failed to .+/i, "failed"],
]

export function apiErrorKey(message: string): string {
  const trimmed = message.trim()
  for (const [re, key] of DYNAMIC_KEYS) {
    if (re.test(trimmed)) {
      return key
    }
  }
  return trimmed
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "")
}

export function useApiError() {
  const locale = useLocale()
  const t = useTranslations("Errors")

  return useCallback(
    (error: unknown): string | undefined => {
      if (!(error instanceof Error) || !error.message) {
        return undefined
      }
      if (locale !== "ar") {
        return error.message
      }
      const key = apiErrorKey(error.message)
      return t.has(key) ? t(key) : error.message
    },
    [locale, t],
  )
}