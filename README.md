# 📦 Inventory Management System

A **full-stack inventory tracking application** with real-time data synchronization across multiple warehouse locations. Features an **automated reorder alert system**, **KPI dashboard**, and comprehensive **supplier performance tracking**.

> Built with **C# .NET 8**, **Entity Framework Core**, and **SQLite** — designed with clean architecture, repository pattern, and dependency injection.

---

## ✨ Features

### 📦 Product Management
- Full CRUD operations for products with SKU, name, description, category, and unit price
- Search and filter by name, SKU, category, or description
- Configurable reorder points and quantities per product

### 🏭 Multi-Location Warehouse Tracking
- Manage multiple warehouse locations with capacity tracking
- Real-time stock levels per product per warehouse
- Utilization monitoring with visual progress bars

### 📊 Inventory Tracking & Synchronization
- Track stock levels across all warehouse locations in real-time
- Color-coded stock health indicators (🟢 Healthy | 🟡 Low | 🔴 Critical)
- Composite unique tracking: every product × warehouse combination

### 🤝 Supplier Management
- Complete supplier profiles with contact info, ratings, and lead times
- Link products to suppliers for supply chain visibility
- Performance metrics (★ rating system, delivery tracking)

### 🔔 Automated Reorder Alerts
- Automatic detection when stock falls below configurable reorder points
- Recommended order quantities calculated per product
- Alert lifecycle management (pending → resolved)
- Real-time alert generation on stock movements

### 📜 Stock Transaction Ledger
- Immutable audit trail for every stock movement
- Transaction types: **Received** | **Shipped** | **Transferred** | **Adjusted** | **Returned**
- Color-coded transaction display with timestamps and reference numbers
- Inter-warehouse transfer tracking with source/destination

### 📈 KPI Dashboard
- **Total Inventory Value** — real-time valuation across all locations
- **Inventory Turnover Rate** — annualized metric based on shipment history
- **Stock Levels by Warehouse** — capacity utilization and value breakdown
- **Top Products by Value** — highest-value inventory items
- **Items Below Reorder Point** — actionable low-stock alerts
- **Supplier Performance** — product count, value, ratings, lead times
- **Category Analysis** — inventory distribution by product category

### 🔍 Search & Filter
- Full-text search across product names, SKUs, and descriptions
- Filter by category, warehouse, or stock level
- Quick-reference displays during stock operations

---

## 🏗️ Architecture

```
src/
├── InventoryManagement.Core/          # Domain models, interfaces, enums
│   ├── Models/
│   │   ├── Product.cs                 # SKU, price, reorder config
│   │   ├── Warehouse.cs              # Location, capacity tracking
│   │   ├── InventoryItem.cs          # Product × Warehouse stock level
│   │   ├── Supplier.cs              # Vendor management
│   │   ├── StockTransaction.cs      # Immutable audit trail
│   │   └── ReorderAlert.cs          # Automated low-stock alerts
│   ├── Interfaces/
│   │   ├── IRepository.cs           # Generic CRUD operations
│   │   ├── IProductRepository.cs    # Product-specific queries
│   │   ├── IInventoryService.cs     # Stock operations
│   │   ├── IReorderService.cs       # Alert management
│   │   └── IReportingService.cs     # KPI calculations + DTOs
│   └── Enums/
│       ├── TransactionType.cs        # Stock movement types
│       └── StockLevel.cs            # Health indicators
│
├── InventoryManagement.Data/          # Entity Framework Core layer
│   ├── AppDbContext.cs               # Normalized schema with indexes
│   ├── Repositories/
│   │   ├── Repository.cs            # Generic repository implementation
│   │   └── ProductRepository.cs     # Optimized product queries
│   └── DataSeeder.cs                # Sample data (5 warehouses, 25 products)
│
├── InventoryManagement.Services/      # Business logic layer
│   ├── InventoryService.cs          # Stock operations + validation
│   ├── ReorderService.cs            # Automated alert calculations
│   └── ReportingService.cs          # KPI metric calculations
│
└── InventoryManagement.Console/       # Presentation layer
    ├── Program.cs                    # DI container + app bootstrap
    └── UI/
        ├── ConsoleHelper.cs          # Color-coded output utilities
        ├── MainMenu.cs              # Application navigation hub
        ├── ProductMenu.cs           # Product & supplier CRUD
        ├── WarehouseMenu.cs         # Warehouse management
        ├── InventoryMenu.cs         # Stock operations + alerts
        └── ReportsMenu.cs          # KPI dashboard + reports
```

### Design Patterns
- **Repository Pattern** — abstracts data access behind interfaces
- **Dependency Injection** — Microsoft.Extensions.DependencyInjection
- **Service Layer** — business logic separated from data and UI
- **Domain Model** — rich entities with validation attributes

---

## 🗃️ Database Schema

```
┌──────────────┐     ┌──────────────────┐     ┌──────────────┐
│   Suppliers   │     │    Products      │     │  Warehouses  │
├──────────────┤     ├──────────────────┤     ├──────────────┤
│ Id           │◄────│ SupplierId (FK)  │     │ Id           │
│ Name         │     │ Id               │     │ Name         │
│ ContactName  │     │ SKU (unique)     │     │ Address      │
│ Email        │     │ Name             │     │ City         │
│ Phone        │     │ Description      │     │ State        │
│ Rating       │     │ Category         │     │ Capacity     │
│ LeadTimeDays │     │ UnitPrice        │     │ IsActive     │
└──────────────┘     │ ReorderPoint     │     └──────┬───────┘
                     │ ReorderQuantity  │            │
                     └────────┬─────────┘            │
                              │                      │
                     ┌────────┴──────────────────────┴────────┐
                     │          InventoryItems                 │
                     ├─────────────────────────────────────────┤
                     │ Id                                      │
                     │ ProductId (FK) ──── unique composite    │
                     │ WarehouseId (FK) ─┘                    │
                     │ Quantity                                │
                     │ LastUpdated                             │
                     └─────────────────────────────────────────┘
                              │
                     ┌────────┴────────────┐    ┌──────────────────┐
                     │ StockTransactions    │    │  ReorderAlerts   │
                     ├─────────────────────┤    ├──────────────────┤
                     │ Id                  │    │ Id               │
                     │ ProductId (FK)      │    │ ProductId (FK)   │
                     │ WarehouseId (FK)    │    │ WarehouseId (FK) │
                     │ Type (enum)         │    │ CurrentStock     │
                     │ Quantity (+/-)      │    │ ReorderPoint     │
                     │ ReferenceNumber     │    │ RecommendedQty   │
                     │ Notes               │    │ IsResolved       │
                     │ TransactionDate     │    │ CreatedAt        │
                     │ DestWarehouseId     │    │ ResolvedAt       │
                     └─────────────────────┘    └──────────────────┘
```

---

## 🚀 Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later

### Run the Application
```bash
# Clone the repository
git clone https://github.com/tukkkie231/ThuDuong_InventoryManagement.git
cd ThuDuong_InventoryManagement

# Build the solution
cd src
dotnet build

# Run the application
dotnet run --project InventoryManagement.Console
```

The application automatically creates a SQLite database and seeds it with sample data on first run:
- **5 Warehouses** across the United States
- **25 Products** across 5 categories (Electronics, Office Supplies, Industrial, Safety, Packaging)
- **5 Suppliers** with ratings and lead times
- **60+ Sample Transactions** for realistic reporting data

---

## 🛠️ Tech Stack

| Component | Technology |
|-----------|-----------|
| **Language** | C# 12 |
| **Framework** | .NET 8 |
| **ORM** | Entity Framework Core 8 |
| **Database** | SQLite (portable, zero-config) |
| **DI Container** | Microsoft.Extensions.DependencyInjection |
| **Architecture** | Clean Architecture, Repository Pattern |

---

## 📸 Screenshots

### Main Menu
```
╔══════════════════════════════════════════════════════╗
║     📦  I N V E N T O R Y   M A N A G E M E N T    ║
║     🏭  Multi-Location Warehouse Tracking System    ║
╚══════════════════════════════════════════════════════╝

  [1] 📦  Product Management
  [2] 🏭  Warehouse Management
  [3] 📊  Inventory Operations
  [4] 📈  KPI Dashboard & Reports
  [5] 🔔  Reorder Alerts
  [6] 🔍  Search & Filter
  [0] Back / Exit
```

### KPI Dashboard
```
  ── KEY PERFORMANCE INDICATORS ──

  ┌─────────────────────────────┐
  │ Total Inventory Value       │
  │ $234,567.89                 │
  └─────────────────────────────┘
  ┌─────────────────────────────┐
  │ Turnover Rate (Annual)      │
  │ 4.23x                       │
  └─────────────────────────────┘
```

### Color-Coded Stock Levels
```
  Product              SKU         Warehouse              Qty    Status
  ────────────────────────────────────────────────────────────────────
  Wireless Keyboard    ELEC-001    East Coast Hub         45     🟢 Healthy
  USB-C Hub            ELEC-002    West Coast Hub         12     🟡 Low
  Safety Goggles       SAFE-001    Central Distribution    3     🔴 Critical
```

---

## 👩‍💻 Author

**Thu Duong**

---

## 📄 License

This project is open source and available under the [MIT License](LICENSE).
