import { queryClient } from "@/lib/query-client"
import { request } from "@/lib/api"
import { CreatePaymentRequest, Payment } from "@/types/domain/domain.types"
import { clientsKeys } from "@/hooks/use-clients"
import { suppliersKeys } from "@/hooks/use-suppliers"
import { paymentsKeys } from "@/hooks/use-payments"

export async function createPayment(data: CreatePaymentRequest) {
  const result = await request<Payment>('/api/payments', { method: 'POST', body: JSON.stringify(data) })
  await queryClient.invalidateQueries({ queryKey: paymentsKeys.all })
  if (data.clientId) {
    await queryClient.invalidateQueries({ queryKey: clientsKeys.all })
  }
  if (data.supplierId) {
    await queryClient.invalidateQueries({ queryKey: suppliersKeys.all })
  }
  return result
}
