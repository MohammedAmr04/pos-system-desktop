import { request } from "@/lib/api"
import { UserSummary } from "@/types/domain/domain.types"

export function listUsers() {
  return request<UserSummary[]>('/api/users')
}
