import { DashboardLayout } from "@/components/layouts/dashboard-layout";
import { LicenseGate } from "@/components/common/license-lock";

export default function LocaleLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <DashboardLayout>
      <LicenseGate>
        {children}
      </LicenseGate>
    </DashboardLayout>
  );
}
