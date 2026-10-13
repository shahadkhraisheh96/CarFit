# Service Architecture Map
*(A master reference document showing exactly where every service is injected and utilized across the CarFit platform)*

---

### 1. 🧠 Core Engines & Algorithms
**`IRecommendationService`**
* **Where it's used:**
  * `Areas/Buyer/Controllers/DashboardController.cs` *(Generates the custom car matches on the buyer's home screen)*

**`IInspectionScoringService`** (The most widely used core service)
* **Where it's used:**
  * `Services/RecommendationService.cs` *(Applies the 30% inspection penalty to the final match score)*
  * `Services/InspectionReportService.cs` *(Automatically scores reports when saving them to the DB)*
  * `Services/InspectionReportPdfService.cs` *(Pulls the trust score to print on the PDF)*
  * `Controllers/InventoryController.cs` *(Displays the Trust Score on the public car details page)*
  * `Areas/Admin/Controllers/InspectionsController.cs` *(Admin inputs raw data, uses service to calculate the score)*
  * `Areas/Admin/Controllers/ListingsController.cs` *(Admin views score while approving cars)*
  * `Areas/Seller/Controllers/InspectionsController.cs` *(Sellers view their own car scores)*
  * `Areas/Buyer/Controllers/CompareController.cs` *(Scores used in the side-by-side comparison)*

### 2. 🖼️ Media & File Processing
**`IImageStorageService`**
* **Where it's used:**
  * `Areas/Seller/Controllers/InventoryController.cs` *(Resizes and saves images when dealers upload cars)*
  * `Areas/Admin/Controllers/ListingsController.cs` *(Allows admins to manage/delete images)*

**`IInspectionReportPdfService`**
* **Where it's used:**
  * `Controllers/InspectionController.cs` *(Generates the downloadable PDF report for users)*

**`IInspectionUploadService`**
* **Where it's used:**
  * `Areas/Seller/Controllers/InventoryController.cs` *(Dealers uploading scanned mechanic documents)*
  * `Areas/Admin/Controllers/ListingsController.cs` *(Admins verifying those documents)*

### 3. 🚗 Inventory & Data Management
**`IListingService`**
* **Where it's used:**
  * `Areas/Seller/Controllers/InventoryController.cs` *(Dealers creating and managing their car listings)*
  * `Areas/Seller/Controllers/InspectionsController.cs` *(Linking inspections to listings)*
  * `Areas/Admin/Controllers/ListingsController.cs` *(Admins approving, rejecting, or deleting listings)*

**`IInspectionReportService`**
* **Where it's used:**
  * `Areas/Admin/Controllers/InspectionsController.cs` *(Admins creating the database records for inspections)*
  * `Areas/Admin/Controllers/ListingsController.cs` *(Viewing reports tied to pending listings)*
  * `Areas/Seller/Controllers/InspectionsController.cs` *(Sellers tracking their inspection statuses)*

**`ISavedCarsService`**
* **Where it's used:**
  * `Controllers/InventoryController.cs` *(The "Bookmark" button on public car pages)*
  * `Areas/Buyer/Controllers/SavedController.cs` *(The Buyer's saved cars list page)*
  * `Areas/Buyer/Controllers/DashboardController.cs` *(Dashboard summary of saved cars)*

### 4. 👤 Users & Authentication
**`IUserAdminService`**
* **Where it's used:**
  * `Areas/Admin/Controllers/AdminController.cs` *(Admins approving dealer profiles or banning users)*

**`IUserProfileService`**
* **Where it's used:**
  * `Areas/Buyer/Controllers/QuestionnaireController.cs` *(Saves the buyer's 5-step questionnaire progress)*

### 5. 💳 Billing & Subscriptions
**`ISubscriptionService` & `ISellerSubscriptionService`**
* **Where it's used:**
  * `Controllers/BillingController.cs` *(Redirecting users to Stripe based on their plan)*
  * `Controllers/WebhooksController.cs` *(Updating DB statuses when Stripe confirms payment)*
  * `Controllers/InventoryController.cs` *(Checking if a seller is allowed to list more cars)*
  * `Areas/Seller/Controllers/InventoryController.cs` *(Checking dealer tier limits)*
  * `Services/SavedCarsService.cs` & `Areas/Buyer/Controllers/SavedController.cs` *(Pay-per-post checks)*

**`IStripeService`**
* **Where it's used:**
  * `Controllers/BillingController.cs` *(Creating Checkout sessions)*
  * `Controllers/WebhooksController.cs` *(Verifying the cryptographic signature of Stripe webhooks)*

### 6. 📧 Notifications & Misc
**`ISmsService` (Twilio)**
* **Where it's used:**
  * `Controllers/WebhooksController.cs` *(Sending a live SMS text message to the user when their subscription renews or fails)*

**`ITestimonialService`**
* **Where it's used:**
  * `Controllers/HomeController.cs` *(Fetching approved reviews for the home page carousel)*
  * `Controllers/TestimonialsController.cs` *(Users submitting new reviews)*
  * `Areas/Admin/Controllers/TestimonialsController.cs` *(Admins approving or deleting reviews)*
  * `Components/ReviewButtonViewComponent.cs` *(A UI component checking if a user has already reviewed)*
