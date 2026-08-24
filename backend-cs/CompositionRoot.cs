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
        private static ICategoryRepository _categories;
        private static IBrandRepository _brands;
        private static IUnitRepository _masterUnits;
        private static ISupplierRepository _suppliers;
        private static IClientRepository _clients;
        private static IInvoiceRepository _invoices;
        private static IPurchaseRepository _purchases;
        private static IPaymentRepository _payments;
        private static ISaleReturnRepository _saleReturns;
        private static IPurchaseReturnRepository _purchaseReturns;
        private static IShiftRepository _shifts;
        private static IExpenseRepository _expenses;
        private static IReportRepository _reportRepo;
        private static IPrinterSettingsRepository _printerSettings;
        private static PrinterSettingsService _printerSettingsService;
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
        private static CategoryService _categoryService;
        private static BrandService _brandService;
        private static UnitMasterService _unitMasterService;
        private static SupplierService _supplierService;
        private static ClientService _clientService;
        private static PurchaseService _purchaseService;
        private static PaymentService _paymentService;
        private static InvoiceService _invoiceService;
        private static SaleReturnService _saleReturnService;
        private static PurchaseReturnService _purchaseReturnService;
        private static ShiftService _shiftService;
        private static ExpenseService _expenseService;
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
        public static ICategoryRepository CategoriesRepo => Lazy(ref _categories, () => new CategoryRepository());
        public static IBrandRepository BrandsRepo => Lazy(ref _brands, () => new BrandRepository());
        public static IUnitRepository MasterUnitsRepo => Lazy(ref _masterUnits, () => new UnitRepository());
        public static ISupplierRepository SuppliersRepo => Lazy(ref _suppliers, () => new SupplierRepository());
        public static PurchaseService PurchaseService => Lazy(ref _purchaseService,
            () => new PurchaseService(PurchasesRepo));
        public static IClientRepository ClientsRepo => Lazy(ref _clients, () => new ClientRepository());
        public static IInvoiceRepository Invoices => Lazy(ref _invoices, () => new InvoiceRepository());
        public static IPurchaseRepository PurchasesRepo => Lazy(ref _purchases, () => new PurchaseRepository());
        public static IPaymentRepository PaymentsRepo => Lazy(ref _payments, () => new PaymentRepository());
        public static ISaleReturnRepository SaleReturnsRepo => Lazy(ref _saleReturns, () => new SaleReturnRepository());
        public static IPurchaseReturnRepository PurchaseReturnsRepo => Lazy(ref _purchaseReturns, () => new PurchaseReturnRepository());
        public static IShiftRepository ShiftsRepo => Lazy(ref _shifts, () => new ShiftRepository());
        public static IExpenseRepository ExpensesRepo => Lazy(ref _expenses, () => new ExpenseRepository());
        public static IReportRepository ReportsRepo => Lazy(ref _reportRepo, () => new ReportRepository());
        public static IAuthRepository Auth => Lazy(ref _auth, () => new AuthRepository(MachineId));
        public static IUsersRepository UsersRepo => Lazy(ref _users, () => new UsersRepository());
        public static IRolesRepository RolesRepo => Lazy(ref _roles, () => new RolesRepository());
        public static IPermissionsRepository PermissionsRepo => Lazy(ref _permissions, () => new PermissionsRepository());
        public static ITenantFeatureRepository TenantFeaturesRepo => Lazy(ref _tenantFeatures, () => new TenantFeatureRepository());
        public static ILicenseRepository LicenseRepo => Lazy(ref _license, () => new SettingsRepository());
        public static IAccessControl Access => Lazy(ref _access, () => new AccessControl());
        public static IReceiptPrinter Receipts => Lazy(ref _receiptPrinter, () => new ReceiptPrinter(PrinterSettingsRepo));
        public static IBarcodeLabelPrinter BarcodeLabels => Lazy(ref _labelPrinter, () => new BarcodeLabelPrinter(PrinterSettingsRepo));
        public static IPrinterSettingsRepository PrinterSettingsRepo => Lazy(ref _printerSettings, () => new PrinterSettingsRepository());
        public static PrinterSettingsService PrinterSettingsService => Lazy(ref _printerSettingsService,
            () => new PrinterSettingsService(PrinterSettingsRepo));

        public static AuthService AuthService => Lazy(ref _authService,
            () => new AuthService(Auth, RolesRepo, Hasher, Tokens, Throttle));
        public static ProductService ProductService => Lazy(ref _productService,
            () => new ProductService(Products, Units, CategoriesRepo, BrandsRepo, MasterUnitsRepo));
        public static CategoryService CategoryService => Lazy(ref _categoryService,
            () => new CategoryService(CategoriesRepo));
        public static BrandService BrandService => Lazy(ref _brandService,
            () => new BrandService(BrandsRepo));
        public static UnitMasterService UnitMasterService => Lazy(ref _unitMasterService,
            () => new UnitMasterService(MasterUnitsRepo));
        public static SupplierService SupplierService => Lazy(ref _supplierService,
            () => new SupplierService(SuppliersRepo, PaymentsRepo, PurchasesRepo, PurchaseReturnsRepo));
        public static ClientService ClientService => Lazy(ref _clientService,
            () => new ClientService(ClientsRepo, PaymentsRepo, Invoices));
        public static PaymentService PaymentService => Lazy(ref _paymentService,
            () => new PaymentService(PaymentsRepo, ClientsRepo, SuppliersRepo, Invoices, PurchasesRepo));
        public static InvoiceService InvoiceService => Lazy(ref _invoiceService,
            () => new InvoiceService(Invoices, Products, Units, Access, Clock, ClientsRepo));
        public static SaleReturnService SaleReturnService => Lazy(ref _saleReturnService,
            () => new SaleReturnService(SaleReturnsRepo, Invoices));
        public static PurchaseReturnService PurchaseReturnService => Lazy(ref _purchaseReturnService,
            () => new PurchaseReturnService(PurchaseReturnsRepo, PurchasesRepo));
        public static ShiftService ShiftService => Lazy(ref _shiftService,
            () => new ShiftService(ShiftsRepo));
        public static ExpenseService ExpenseService => Lazy(ref _expenseService,
            () => new ExpenseService(ExpensesRepo));
        public static UsersService UsersService => Lazy(ref _usersService,
            () => new UsersService(UsersRepo, Auth, RolesRepo, Hasher, Access));
        public static RolesService RolesService => Lazy(ref _rolesService,
            () => new RolesService(RolesRepo, PermissionsRepo));
        public static TenantFeaturesService TenantFeaturesService => Lazy(ref _tenantFeaturesService,
            () => new TenantFeaturesService(Auth, TenantFeaturesRepo, Access));
        public static LicenseService LicenseService => Lazy(ref _licenseService,
            () => new LicenseService(LicenseRepo, MachineId, Clock));
        public static ReportsService ReportsService => Lazy(ref _reportsService,
            () => new ReportsService(Products, ReportsRepo));
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
