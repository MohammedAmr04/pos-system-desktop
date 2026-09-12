param(
    [string]$BaseUrl = "http://localhost:3001",
    [string]$Username = "admin",
    [string]$Password = "1234"
)

$ErrorActionPreference = "Stop"
$BaseUrl = $BaseUrl.TrimEnd('/')

function Invoke-PosRequest([string]$Method, [string]$Path, $Body = $null) {
    $params = @{
        Method = $Method
        Uri = "$BaseUrl/api/$Path"
        Headers = @{ Authorization = "Bearer $script:Token" }
        ContentType = "application/json"
    }
    if ($null -ne $Body) {
        $params.Body = ($Body | ConvertTo-Json -Depth 10)
    }
    try {
        return Invoke-RestMethod @params
    }
    catch {
        throw "POS API request failed: $Method /api/$Path. $($_.Exception.Message)"
    }
}

Write-Host "Connecting to $BaseUrl..."
$login = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/auth/login" -ContentType "application/json" -Body (@{
    username = $Username
    password = $Password
} | ConvertTo-Json)
$script:Token = $login.token
if ([string]::IsNullOrWhiteSpace($script:Token)) {
    throw "Login failed. Check the username and password."
}

$existingProducts = @(Invoke-PosRequest "Get" "products")
if ($existingProducts | Where-Object { $_.name -like "DEMO - *" }) {
    Write-Host "Demo data already exists. No changes were made."
    exit 0
}

function New-Entity([string]$Path, $Body) {
    return Invoke-PosRequest "Post" $Path $Body
}

$categories = @{}
foreach ($item in @(
    @{ name = "DEMO - Beverages"; description = "Demo beverage category" },
    @{ name = "DEMO - Snacks"; description = "Demo snacks category" },
    @{ name = "DEMO - Household"; description = "Demo household category" }
)) {
    $created = New-Entity "categories" $item
    $categories[$item.name] = $created.id
}

$brands = @{}
foreach ($name in @("DEMO - Nile Foods", "DEMO - FreshCo", "DEMO - HomePro")) {
    $created = New-Entity "brands" @{ name = $name }
    $brands[$name] = $created.id
}

$units = @{}
foreach ($name in @("Piece", "Pack", "Carton", "Kilogram")) {
    $created = New-Entity "units" @{ name = "DEMO - $name" }
    $units[$name] = $created.id
}

$supplier = New-Entity "suppliers" @{
    name = "DEMO - Al Baraka Supplies"
    phone = "01000000001"
    address = "15 Demo Street, Cairo"
    notes = "Demo supplier for purchase and statement screens"
}

$client = New-Entity "clients" @{
    name = "DEMO - Ahmed Hassan"
    phone = "01000000002"
    address = "25 Demo Avenue, Giza"
    notes = "Demo client for credit sales and payments"
}

$employee = New-Entity "employees" @{
    name = "DEMO - Sara Mohamed"
    phone = "01000000003"
}

$products = @{}
$productDefinitions = @(
    @{ key = "water"; name = "DEMO - Mineral Water 1.5L"; barcode = "622100000001"; buyPrice = 6; retailPrice = 10; wholesalePrice = 8; stock = 0; category = "DEMO - Beverages"; brand = "DEMO - FreshCo"; unit = "Piece"; threshold = 20; notes = "Fast-moving demo item" },
    @{ key = "juice"; name = "DEMO - Mango Juice 1L"; barcode = "622100000002"; buyPrice = 18; retailPrice = 28; wholesalePrice = 24; stock = 0; category = "DEMO - Beverages"; brand = "DEMO - Nile Foods"; unit = "Piece"; threshold = 10; notes = "Demo product with retail and wholesale prices" },
    @{ key = "chips"; name = "DEMO - Potato Chips"; barcode = "622100000003"; buyPrice = 8; retailPrice = 15; wholesalePrice = 12; stock = 0; category = "DEMO - Snacks"; brand = "DEMO - FreshCo"; unit = "Piece"; threshold = 15; notes = "Demo product with discount enabled" },
    @{ key = "detergent"; name = "DEMO - Liquid Detergent"; barcode = "622100000004"; buyPrice = 35; retailPrice = 52; wholesalePrice = 46; stock = 0; category = "DEMO - Household"; brand = "DEMO - HomePro"; unit = "Piece"; threshold = 8; notes = "Demo low-stock report item" }
)

foreach ($definition in $productDefinitions) {
    $created = New-Entity "products" @{
        name = $definition.name
        productType = "product"
        barcode = $definition.barcode
        buyPrice = $definition.buyPrice
        salePrice = $definition.retailPrice
        retailPrice = $definition.retailPrice
        wholesalePrice = $definition.wholesalePrice
        unitName = $definition.unit
        unitId = $units[$definition.unit]
        stockQuantity = $definition.stock
        notes = $definition.notes
        allowDiscount = $true
        lowStockThreshold = $definition.threshold
        isHiddenFromPOS = $false
        categoryId = $categories[$definition.category]
        brandId = $brands[$definition.brand]
    }
    $products[$definition.key] = $created
}

$packUnit = New-Entity "products/$($products.water.id)/units" @{
    unitName = "Pack"
    unitId = $units.Pack
    quantityFactor = 6
    retailPrice = 54
    wholesalePrice = 45
}
$null = New-Entity "products/$($products.water.id)/units/$($packUnit.id)/barcodes" @{ barcode = "622100000101" }

$bundle = New-Entity "products" @{
    name = "DEMO - Refreshment Bundle"
    productType = "bundle"
    barcode = "622100000099"
    buyPrice = 0
    salePrice = 65
    retailPrice = 65
    wholesalePrice = 58
    unitName = "Piece"
    unitId = $units.Piece
    stockQuantity = 0
    notes = "Demo bundle: water plus juice"
    allowDiscount = $true
    lowStockThreshold = 0
    isHiddenFromPOS = $false
    categoryId = $categories.'DEMO - Beverages'
    brandId = $brands.'DEMO - FreshCo'
    bundleComponents = @(
        @{ productId = $products.water.id; quantity = 1 },
        @{ productId = $products.juice.id; quantity = 1 }
    )
}
$products.bundle = $bundle

$service = New-Entity "products" @{
    name = "DEMO - Gift Wrapping Service"
    productType = "service"
    barcode = "622100000098"
    buyPrice = 2
    salePrice = 10
    retailPrice = 10
    wholesalePrice = 10
    unitName = "Piece"
    unitId = $units.Piece
    stockQuantity = 0
    serviceCost = 2
    notes = "Demo non-stock service"
    allowDiscount = $true
    lowStockThreshold = 0
    isHiddenFromPOS = $false
    categoryId = $categories.'DEMO - Household'
    brandId = $brands.'DEMO - HomePro'
}
$products.service = $service

$purchase = New-Entity "purchases" @{
    supplierId = $supplier.id
    supplierInvoiceNumber = "DEMO-PUR-001"
    date = (Get-Date).AddDays(-3).ToString("yyyy-MM-ddTHH:mm:ss")
    paymentMethod = "credit"
    status = "posted"
    discount = 0
    tax = 0
    notes = "Demo opening stock purchase"
    lines = @(
        @{ productId = $products.water.id; quantity = 120; unitCost = 6; newRetailPrice = 10; newWholesalePrice = 8 },
        @{ productId = $products.juice.id; quantity = 60; unitCost = 18; newRetailPrice = 28; newWholesalePrice = 24 },
        @{ productId = $products.chips.id; quantity = 80; unitCost = 8; newRetailPrice = 15; newWholesalePrice = 12 },
        @{ productId = $products.detergent.id; quantity = 25; unitCost = 35; newRetailPrice = 52; newWholesalePrice = 46 }
    )
}

$shift = New-Entity "shifts" @{ openingCash = 1000; notes = "DEMO - Morning shift" }

$invoice = New-Entity "invoices" @{
    items = @(
        @{ productId = $products.water.id; productUnitId = $packUnit.id; unitName = "Pack"; name = $products.water.name; buyPrice = 36; salePrice = 54; originalUnitPrice = 54; unitPrice = 50; quantity = 2; maxStock = 20; allowDiscount = $true; discountType = "fixed"; discountValue = 4; quantityFactor = 6; priceEditNote = "DEMO - customer discount" },
        @{ productId = $products.chips.id; unitName = "Piece"; name = $products.chips.name; buyPrice = 8; salePrice = 15; originalUnitPrice = 15; unitPrice = 15; quantity = 3; maxStock = 80; allowDiscount = $true; discountType = "percentage"; discountValue = 0; quantityFactor = 1 },
        @{ productId = $products.service.id; unitName = "Piece"; name = $products.service.name; buyPrice = 2; salePrice = 10; originalUnitPrice = 10; unitPrice = 10; quantity = 1; maxStock = 0; allowDiscount = $true; discountType = "percentage"; discountValue = 0; quantityFactor = 1 }
    )
    discount = 0
    discountType = "fixed"
    discountValue = 0
    priceMode = "retail"
    paymentMethod = "cash"
    status = "posted"
    employeeId = $employee.id
}

$creditInvoice = New-Entity "invoices" @{
    items = @(
        @{ productId = $products.juice.id; unitName = "Piece"; name = $products.juice.name; buyPrice = 18; salePrice = 28; originalUnitPrice = 28; unitPrice = 28; quantity = 2; maxStock = 60; allowDiscount = $true; discountType = "percentage"; discountValue = 0; quantityFactor = 1 }
    )
    discount = 0
    discountType = "fixed"
    discountValue = 0
    priceMode = "retail"
    paymentMethod = "credit"
    clientId = $client.id
    status = "posted"
    employeeId = $employee.id
}

$null = New-Entity "payments" @{ amount = 20; paymentMethod = "cash"; date = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ss"); invoiceId = $creditInvoice.id; clientId = $client.id; notes = "DEMO - partial client payment" }
$null = New-Entity "payments" @{ amount = 80; paymentMethod = "cash"; date = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ss"); supplierId = $supplier.id; notes = "DEMO - supplier payment" }

$expenseCategory = New-Entity "expenses/categories" @{ name = "DEMO - Utilities"; isActive = $true }
$null = New-Entity "expenses" @{ categoryId = $expenseCategory.id; amount = 75; paymentMethod = "cash"; date = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ss"); description = "DEMO - shop electricity"; reference = "DEMO-EXP-001" }

Write-Host "Demo data created successfully." -ForegroundColor Green
Write-Host "Products: $($productDefinitions.Count) products, one service, one bundle, and one multi-unit product."
Write-Host "Transactions: one purchase, one cash invoice, one credit invoice, two payments, one expense, and one open shift."
