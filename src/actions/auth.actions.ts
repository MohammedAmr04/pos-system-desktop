import { request } from "@/lib/api"
import { LoginResponse } from "@/types/domain/domain.types"

export function login(username: string, password: string) {
  return request<LoginResponse>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify({ username, password }),
  })
}
