import { request } from "@/lib/api"
import { AccessBundle } from "@/types/domain/domain.types"

export function getMe() {
  return request<AccessBundle>('/api/auth/me')
}
