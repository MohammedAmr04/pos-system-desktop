import { DashboardLayout } from "@/components/layouts/dashboard-layout";
import { LicenseGate } from "@/components/common/license-lock";
import { AuthGate } from "@/components/common/auth-gate";
import { AuthProvider } from "@/features/auth/auth-context";

export function generateStaticParams() {
  return [{ locale: "ar" }]
}

export default function LocaleLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <AuthProvider>
      <AuthGate>
        <LicenseGate>
          <DashboardLayout>
            {children}
          </DashboardLayout>
        </LicenseGate>
      </AuthGate>
    </AuthProvider>
  );
}
