using PosCs.Application.Ports;
using PosCs.Application.Services;
using PosCs.Infrastructure.Devices;
using PosCs.Infrastructure.Persistence;
using PosCs.Infrastructure.Printing;
using PosCs.Infrastructure.Security;
using PosCs.Infrastructure.SystemTime;

namespace PosCs
{
    /// <summary>
    /// Composition root: the only place where concrete implementations are chosen.
    /// Controllers and middleware resolve dependencies from here.
    /// </summary>
    public static class CompositionRoot
    {
        private static readonly object _lock = new object();

        private static IClock _clock;
        private static IPasswordHasher _hasher;
        private static ITokenService _tokens;
        private static IMachineIdProvider _machineId;
        private static LoginThrottle _throttle;

        private static IProductRepository _products;
        private static IProductUnitRepository _units;
        private static IInvoiceRepository _invoices;
        private static IAuthRepository _auth;
        private static IUsersRepository _users;
        private static IRolesRepository _roles;
        private static IPermissionsRepository _permissions;
        private static ITenantFeatureRepository _tenantFeatures;
        private static ILicenseRepository _license;
        private static IAccessControl _access;
        private static IReceiptPrinter _receiptPrinter;
        private static IBarcodeLabelPrinter _labelPrinter;

        private static AuthService _authService;
        private static ProductService _productService;
        private static InvoiceService _invoiceService;
        private static UsersService _usersService;
        private static RolesService _rolesService;
        private static TenantFeaturesService _tenantFeaturesService;
        private static LicenseService _licenseService;
        private static ReportsService _reportsService;
        private static PrintingService _printingService;

        public static IClock Clock => Lazy(ref _clock, () => new SystemClock());
        public static IPasswordHasher Hasher => Lazy(ref _hasher, () => new PasswordHasher());
        public static ITokenService Tokens => Lazy(ref _tokens, () => new TokenService());
        public static IMachineIdProvider MachineId => Lazy(ref _machineId, () => new MachineIdProvider());
        public static LoginThrottle Throttle => Lazy(ref _throttle, () => new LoginThrottle(Clock));

        public static IProductRepository Products => Lazy(ref _products, () => new ProductRepository());
        public static IProductUnitRepository Units => Lazy(ref _units, () => new ProductUnitRepository());
        public static IInvoiceRepository Invoices => Lazy(ref _invoices, () => new InvoiceRepository());
        public static IAuthRepository Auth => Lazy(ref _auth, () => new AuthRepository(MachineId));
        public static IUsersRepository UsersRepo => Lazy(ref _users, () => new UsersRepository());
        public static IRolesRepository RolesRepo => Lazy(ref _roles, () => new RolesRepository());
        public static IPermissionsRepository PermissionsRepo => Lazy(ref _permissions, () => new PermissionsRepository());
        public static ITenantFeatureRepository TenantFeaturesRepo => Lazy(ref _tenantFeatures, () => new TenantFeatureRepository());
        public static ILicenseRepository LicenseRepo => Lazy(ref _license, () => new SettingsRepository());
        public static IAccessControl Access => Lazy(ref _access, () => new AccessControl());
        public static IReceiptPrinter Receipts => Lazy(ref _receiptPrinter, () => new ReceiptPrinter());
        public static IBarcodeLabelPrinter BarcodeLabels => Lazy(ref _labelPrinter, () => new BarcodeLabelPrinter());

        public static AuthService AuthService => Lazy(ref _authService,
            () => new AuthService(Auth, RolesRepo, Hasher, Tokens, Throttle));
        public static ProductService ProductService => Lazy(ref _productService,
            () => new ProductService(Products, Units));
        public static InvoiceService InvoiceService => Lazy(ref _invoiceService,
            () => new InvoiceService(Invoices, Products, Units, Access, Clock));
        public static UsersService UsersService => Lazy(ref _usersService,
            () => new UsersService(UsersRepo, Auth, RolesRepo, Hasher, Access));
        public static RolesService RolesService => Lazy(ref _rolesService,
            () => new RolesService(RolesRepo, PermissionsRepo));
        public static TenantFeaturesService TenantFeaturesService => Lazy(ref _tenantFeaturesService,
            () => new TenantFeaturesService(Auth, TenantFeaturesRepo, Access));
        public static LicenseService LicenseService => Lazy(ref _licenseService,
            () => new LicenseService(LicenseRepo, MachineId, Clock));
        public static ReportsService ReportsService => Lazy(ref _reportsService,
            () => new ReportsService(Products));
        public static PrintingService PrintingService => Lazy(ref _printingService,
            () => new PrintingService(Receipts, BarcodeLabels));

        private static T Lazy<T>(ref T field, System.Func<T> factory) where T : class
        {
            if (field != null) return field;
            lock (_lock)
            {
                if (field == null)
                    field = factory();
                return field;
            }
        }
    }
}
