# 🍽️ KusinaPOS - Restaurant Point of Sale System

A modern, cross-platform Restaurant Management Application built with .NET MAUI and SQLite. KusinaPOS provides a complete POS solution designed for small to medium-sized restaurants, with offline-first capabilities and a beautiful, intuitive interface.

![Platform](https://img.shields.io/badge/Platform-.NET%20MAUI-512BD4?style=flat-square)
![Language](https://img.shields.io/badge/Language-C%23-239120?style=flat-square)
![Database](https://img.shields.io/badge/Database-SQLite-003B57?style=flat-square)
![Architecture](https://img.shields.io/badge/Architecture-MVVM-00ADD8?style=flat-square)

---

## 📱 Target Platforms

- **Android** (Primary deployment target)
- **Windows** (Development and testing)
- **iOS/macOS** (Future support planned)

---

## 🛠️ Tech Stack

### Core Technologies
- **Framework:** .NET MAUI 8.0+
- **Language:** C# 12
- **Architecture:** MVVM (Model-View-ViewModel)
- **Database:** SQLite with async operations
- **ORM:** SQLite-net-pcl / SQLiteAsyncConnection

### UI & Components
- **Markup:** XAML
- **UI Controls:** Syncfusion MAUI Controls
  - SfListView
  - SfNumericEntry
  - SfButton
- **Styling:** Custom Resource Dictionaries with Dark Theme

### Toolkits & Libraries
- **CommunityToolkit.Mvvm** - Observable properties, commands, and source generators
- **CommunityToolkit.Maui** - Additional MAUI helpers and behaviors
- **SQLite-net-pcl** - SQLite ORM for .NET

---

## 🎯 Project Vision

KusinaPOS aims to provide a **reliable, fast, and user-friendly** point-of-sale experience for restaurants that:
- ✅ Works **offline-first** without internet dependency
- ✅ Handles **high-volume transactions** smoothly
- ✅ Provides **real-time inventory tracking**
- ✅ Offers a **clean, modern UI** that's easy to learn
- ✅ Scales from single terminal to multi-terminal setups

### Future Integrations
- Online payment processors (GCash, PayMaya, QR Ph)
- Cloud synchronization for multi-device support
- Web-based reporting dashboard
- Kitchen display system (KDS)
- Customer loyalty program

---

## ✨ Key Features

### 🏪 POS Terminal
- **Category-based navigation** with vertical sidebar
- **Dynamic menu filtering** by category
- **Grid layout** with adaptive columns (responsive to screen size)
- **Quantity controls** with inline numeric up/down
- **Real-time order management** with live subtotal calculation
- **Cash tendering** with automatic change calculation
- **Touch-optimized UI** for fast order entry

### 🍽️ Menu Management
- Create and organize menu items by category
- Set prices, descriptions, and images
- Link menu items to ingredients
- Enable/disable items (seasonal menus)
- Support for meal types (Dine-in, Takeout, Delivery)

### 🧂 Ingredient & Inventory System
- Track ingredients used in each menu item
- Monitor stock levels in real-time
- Automatic inventory deduction on order completion
- Inventory adjustment interface
- Ingredient transaction history
- Low stock alerts (planned)

### 🧾 Order & Transaction Processing
- Multi-item order creation
- Quantity adjustments in cart
- Order modification before completion
- Transaction history
- Receipt generation (planned)
- Order status tracking (planned)

### 💾 Data Management
- **Offline-first architecture** - works without internet
- **Local SQLite database** for fast, persistent storage
- **Async operations** for smooth UI performance
- **Data integrity** with foreign key constraints
- **Migration support** for database updates

---

## 🏗️ Application Architecture

```
┌─────────────────────────────────────────────┐
│              UI Layer (XAML)                │
│  ┌─────────────┐  ┌──────────────────────┐ │
│  │   Pages     │  │   Custom Controls    │ │
│  └─────────────┘  └──────────────────────┘ │
└──────────────────┬──────────────────────────┘
                   │ Data Binding
┌──────────────────▼──────────────────────────┐
│         ViewModel Layer (MVVM)              │
│  ┌─────────────────────────────────────┐   │
│  │  Observable Properties & Commands   │   │
│  │  Business Logic & State Management  │   │
│  └─────────────────────────────────────┘   │
└──────────────────┬──────────────────────────┘
                   │ Service Calls
┌──────────────────▼──────────────────────────┐
│           Service Layer                     │
│  ┌──────────────┐  ┌────────────────────┐  │
│  │  Category    │  │  MenuItem Service  │  │
│  │  Service     │  │                    │  │
│  └──────────────┘  └────────────────────┘  │
│  ┌──────────────┐  ┌────────────────────┐  │
│  │  Inventory   │  │  Transaction       │  │
│  │  Service     │  │  Service           │  │
│  └──────────────┘  └────────────────────┘  │
└──────────────────┬──────────────────────────┘
                   │ Data Access
┌──────────────────▼──────────────────────────┐
│          Data Layer (SQLite)                │
│  ┌─────────────────────────────────────┐   │
│  │     SQLiteAsyncConnection           │   │
│  │  - Categories                       │   │
│  │  - MenuItems                        │   │
│  │  - Ingredients                      │   │
│  │  - MenuItemIngredients              │   │
│  │  - InventoryTransactions            │   │
│  │  - Orders                           │   │
│  └─────────────────────────────────────┘   │
└─────────────────────────────────────────────┘
```

---

## 📂 Project Structure

```
KusinaPOS/
│
├── 📁 Models/                      # Data models
│   ├── Category.cs
│   ├── MenuItem.cs
│   ├── Ingredient.cs
│   ├── MenuItemIngredient.cs
│   ├── InventoryTransaction.cs
│   ├── Order.cs
│   └── OrderItem.cs
│
├── 📁 ViewModels/                  # MVVM ViewModels
│   ├── POSTerminalViewModel.cs
│   ├── MenuManagementViewModel.cs
│   ├── InventoryViewModel.cs
│   ├── TransactionHistoryViewModel.cs
│   └── SettingsViewModel.cs
│
├── 📁 Views/                       # XAML Pages
│   ├── POSTerminalPage.xaml
│   ├── MenuManagementPage.xaml
│   ├── InventoryPage.xaml
│   ├── TransactionHistoryPage.xaml
│   └── SettingsPage.xaml
│
├── 📁 Services/                    # Business logic services
│   ├── DatabaseService.cs
│   ├── CategoryService.cs
│   ├── MenuItemService.cs
│   ├── IngredientService.cs
│   └── InventoryService.cs
│
├── 📁 Helpers/                     # Utility classes
│   ├── PageHelper.cs
│   └── DatabaseInitializer.cs
│
├── 📁 Converters/                  # Value converters
│   └── InvertedBoolConverter.cs
│
├── 📁 Resources/                   # App resources
│   ├── Styles/
│   │   └── Colors.xaml
│   ├── Images/
│   └── Fonts/
│
└── 📄 MauiProgram.cs              # App configuration
```

---

## 🗄️ Database Schema

### Core Tables

**Categories**
```
┌──────────────────────┐
│ Id (PK)              │
│ Name                 │
│ Description          │
│ IsActive             │
└──────────────────────┘
```

**MenuItems**
```
┌──────────────────────┐
│ Id (PK)              │
│ Name                 │
│ Description          │
│ Category             │
│ Price                │
│ Type                 │
│ ImagePath            │
│ IsActive             │
└──────────────────────┘
```

**Ingredients**
```
┌──────────────────────┐
│ Id (PK)              │
│ Name                 │
│ Unit                 │
│ CurrentStock         │
│ MinimumStock         │
│ Cost                 │
└──────────────────────┘
```

**MenuItemIngredients** (Junction Table)
```
┌──────────────────────┐
│ Id (PK)              │
│ MenuItemId (FK)      │
│ IngredientId (FK)    │
│ Quantity             │
└──────────────────────┘
```

**Orders**
```
┌──────────────────────┐
│ Id (PK)              │
│ OrderNumber          │
│ OrderDate            │
│ TotalAmount          │
│ CashTendered         │
│ Change               │
│ Status               │
└──────────────────────┘
```

**OrderItems**
```
┌──────────────────────┐
│ Id (PK)              │
│ OrderId (FK)         │
│ MenuItemId (FK)      │
│ Quantity             │
│ Price                │
│ Subtotal             │
└──────────────────────┘
```

---

## 🎨 UI/UX Highlights

### Design Principles
- **Dark theme** for reduced eye strain during long shifts
- **Large touch targets** (minimum 36px) for easy tapping
- **Clear visual hierarchy** with proper spacing and typography
- **Responsive grid layouts** that adapt to screen size
- **Real-time feedback** for all user actions

### Key Screens

**POS Terminal**
- Left sidebar: Vertical category navigation
- Center: Grid of menu items with images, prices, and add controls
- Right panel: Current order summary with cash handling

**Menu Management**
- CRUD operations for menu items
- Category assignment
- Image upload
- Ingredient linking

**Inventory**
- Stock level overview
- Quick adjustment interface
- Transaction history

---

## 🧪 Development & Testing

### Debug Features
- Extensive console logging for state tracking
- Debug output for all command executions
- Database query tracing
- Performance monitoring points

### Testing Approach
- Manual testing on Android emulator
- Real device testing on various screen sizes
- Edge case handling (empty states, errors)
- Data validation testing

### Known Issues & Solutions
- ❌ **Issue:** `EventToCommandBehavior` on Entry blocks UI updates
  - ✅ **Solution:** Use `Unfocused` event or code-behind instead
  
- ❌ **Issue:** Menu filtering shows all items
  - ✅ **Solution:** Fixed by comparing `MenuItem.Category` with `SelectedCategoryName`

- ❌ **Issue:** Numeric entry shows decimals for quantity
  - ✅ **Solution:** Set `CustomFormat="0"` on `SfNumericEntry`

---

## 🚀 Getting Started

### Prerequisites
- Visual Studio 2022 (17.8+) or Visual Studio Code
- .NET 8.0 SDK
- .NET MAUI workload installed
- Android SDK (API 21+)
- Syncfusion Community License (free for individuals)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/KusinaPOS.git
   cd KusinaPOS
   ```

2. **Restore NuGet packages**
   ```bash
   dotnet restore
   ```

3. **Register Syncfusion license** (in `MauiProgram.cs`)
   ```csharp
   Syncfusion.Licensing.SyncfusionLicenseProvider
       .RegisterLicense("YOUR_LICENSE_KEY");
   ```

4. **Build the solution**
   ```bash
   dotnet build
   ```

5. **Run on Android**
   - Open in Visual Studio
   - Select Android Emulator or physical device
   - Press F5 to run

### First Run Setup
The app will automatically:
- Create the SQLite database
- Initialize tables with schema
- Seed sample data (categories and menu items)

---

## 🔧 Configuration

### Database Location
- **Android:** `/data/data/com.yourcompany.kusinapos/files/kusina.db3`
- **Windows:** `%LOCALAPPDATA%/KusinaPOS/kusina.db3`

### App Settings
Configure in `appsettings.json` (planned):
- Currency symbol
- Tax rate
- Receipt printer settings
- Theme preferences

---

## 🚧 Current Limitations

- ⚠️ **Single terminal only** - no multi-terminal sync yet
- ⚠️ **Local storage only** - no cloud backup
- ⚠️ **Basic reporting** - limited analytics
- ⚠️ **Android-focused UI** - tablet/desktop layouts need refinement
- ⚠️ **No user authentication** - single user mode
- ⚠️ **Payment integration** - planned but not implemented

---

## 🔮 Roadmap

### Phase 1: Core Improvements (Current)
- [x] POS terminal with order management
- [x] Menu item filtering by category
- [x] Basic inventory tracking
- [ ] Order history and search
- [ ] Receipt printing
- [ ] End-of-day reports

### Phase 2: Enhanced Features
- [ ] User authentication and roles
- [ ] Table management for dine-in
- [ ] Kitchen display system integration
- [ ] Customer management
- [ ] Discount and promo codes
- [ ] Multi-language support

### Phase 3: Cloud & Integration
- [ ] Cloud database sync
- [ ] Web-based admin dashboard
- [ ] Payment gateway integration (GCash, PayMaya)
- [ ] QR code payments
- [ ] Third-party delivery integration
- [ ] Analytics and business intelligence

### Phase 4: Enterprise Features
- [ ] Multi-terminal support
- [ ] Franchise management
- [ ] Employee time tracking
- [ ] Advanced reporting
- [ ] API for external integrations

---

## 🤝 Contributing

Contributions are welcome! This project is both a learning experience and a practical application.

### How to Contribute
1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

### Code Standards
- Follow C# coding conventions
- Use MVVM pattern consistently
- Add XML documentation for public methods
- Include debug logging for important operations
- Write clean, readable code with meaningful variable names

---

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 👨‍💻 Author

**Your Name**
- GitHub: [@yourusername](https://github.com/yourusername)
- Email: your.email@example.com

---

## 🙏 Acknowledgments

- **Syncfusion** for their excellent MAUI controls
- **.NET MAUI Team** for the cross-platform framework
- **CommunityToolkit** for MVVM helpers
- **SQLite** for reliable local storage
- The restaurant industry workers who inspired this project

---

## 📚 Resources & Learning

### Documentation
- [.NET MAUI Documentation](https://learn.microsoft.com/dotnet/maui/)
- [Syncfusion MAUI Controls](https://help.syncfusion.com/maui/introduction/overview)
- [MVVM Toolkit](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)
- [SQLite-net Documentation](https://github.com/praeclarum/sqlite-net)

### Tutorials Used
- Building MVVM apps with .NET MAUI
- SQLite integration in mobile apps
- Restaurant POS system design patterns
- Touch-optimized UI design

---

## 💬 Support

If you encounter any issues or have questions:
1. Check existing [Issues](https://github.com/yourusername/KusinaPOS/issues)
2. Create a new issue with detailed description
3. Join discussions in [Discussions](https://github.com/yourusername/KusinaPOS/discussions)

---

## ⭐ Show Your Support

If this project helped you, please give it a ⭐️! It motivates continued development.

---

<div align="center">

**Built with ❤️ for the restaurant community**

[Report Bug](https://github.com/yourusername/KusinaPOS/issues) · [Request Feature](https://github.com/yourusername/KusinaPOS/issues) · [Documentation](https://github.com/yourusername/KusinaPOS/wiki)

</div>
