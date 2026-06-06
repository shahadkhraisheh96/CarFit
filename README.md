# CarFit
# CarFitProject

CarFitProject is a modern, bilingual (English/Arabic) automotive marketplace and inspection platform built with ASP.NET Core 10.0 MVC and Razor Pages. It connects buyers, dealers (sellers), and administrators, providing a seamless experience for listing vehicles, generating inspection reports, and managing subscriptions.

## 🌟 Key Features

* **Role-Based Architecture:** Dedicated portal areas for Admin, Dealer (Seller), and Buyer.
* **Bilingual Support:** Full English (en) and Arabic (ar) localization using Resource files and sticky cookies.
* **Automotive Inspection Reports:** Generates rich, localized PDF inspection reports using QuestPDF.
* **Subscription & Billing:** Integrated with Stripe for managing dealer subscription plans and billing.
* **SMS Notifications:** Twilio integration for sending real-time alerts and notifications.
* **Secure Authentication:** Built on ASP.NET Core Identity with BCrypt password hashing and custom token lifespans.
* **Data Seeding:** Automatically seeds administrative accounts, vehicle makes, mechanics, glossaries, and subscription plans on startup.

## 🛠️ Technology Stack

* **Framework:** .NET 10.0 (ASP.NET Core MVC & Razor Pages)
* **Database:** SQL Server via Entity Framework Core 10
* **Authentication:** ASP.NET Core Identity (Customized with BCrypt.Net-Next)
* **PDF Generation:** QuestPDF (Community License)
* **Image Processing:** SixLabors.ImageSharp
* **Payments:** Stripe.net
* **SMS:** Twilio
* **Other Tools:** CsvHelper for data import/export

## 📂 Project Structure

* **`/Areas`**: Contains role-specific controllers and views (`Admin`, `Dealer/Seller`, `Buyer`, `Identity`).
* **`/Controllers` & `/Views`**: The public-facing MVC components.
* **`/Data`**: Entity Framework `ApplicationDbContext` (Identity) and `CarFitDbContext` (Business entities).
* **`/Models` & `/ViewModel`**: Domain entities and view-specific data models.
* **`/Services`**: Contains business logic (Recommendations, Listings, Inspection Scoring, Stripe, Twilio, etc.).
* **`/Resources`**: Contains `.resx` files for English and Arabic localizations.
* **`/wwwroot`**: Static assets including CSS, JS, images, and Arabic fonts required for PDF generation.

## 🚀 Getting Started

### Prerequisites

* [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
* SQL Server (LocalDB or a full instance)
* Visual Studio 2022, JetBrains Rider, or VS Code

### Configuration

Before running the application, ensure the necessary configuration secrets are set in `appsettings.Development.json` or via `dotnet user-secrets`.

1. **Database Connection:**
   Ensure the `DefaultConnection` string is pointing to your SQL Server instance.

2. **Stripe & Twilio:**
   Configure your Stripe and Twilio API keys.
   ```json
   "Stripe": {
     "SecretKey": "sk_test_...",
     "PublishableKey": "pk_test_...",
     "WebhookSecret": "whsec_..."
   },
   "Twilio": {
     "AccountSid": "...",
     "AuthToken": "...",
     "FromNumber": "..."
   }
   ```

3. **Email Settings:**
   Configure SMTP settings under `EmailSettings`. (In Development, emails will log to the console).

### Running the Application

1. **Clone the repository.**
2. **Navigate to the project directory:**
   ```bash
   cd CarFitProject/CarFitProject
   ```
3. **Restore dependencies:**
   ```bash
   dotnet restore
   ```
4. **Apply Entity Framework Migrations (if needed):**
   ```bash
   dotnet ef database update -c ApplicationDbContext
   dotnet ef database update -c CarFitDbContext
   ```
   *(Note: The app will seed initial data including the Admin user automatically on the first run).*
5. **Run the project:**
   ```bash
   dotnet run
   ```

### Default Accounts

On startup, the system seeds the following default Admin user:
* **Email:** `admin@carfit.local`
* **Password:** `ChangeMe!Admin#2026`
*(Make sure to change the default password in production, or configure different seed settings in your environment variables).*

## 📄 License

This project relies on several third-party libraries including QuestPDF (Community License). Please review the respective licenses for dependencies used.
