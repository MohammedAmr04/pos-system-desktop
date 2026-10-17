import { DashboardLayout } from "@/components/layouts/dashboard-layout";
import { LicenseGate } from "@/components/common/license-lock";
import { AuthGate } from "@/components/common/auth-gate";
import { SetupGate } from "@/components/common/setup-gate";
import { AuthProvider } from "@/components/common/auth-context";
import { QueryProvider } from "@/components/common/query-provider";

export function generateStaticParams() {
  return [{ locale: "ar" }]
}

export default function LocaleLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <QueryProvider>
      <AuthProvider>
        <AuthGate>
          <SetupGate>
            <LicenseGate>
              <DashboardLayout>
                {children}
              </DashboardLayout>
            </LicenseGate>
          </SetupGate>
        </AuthGate>
      </AuthProvider>
    </QueryProvider>
  );
}
