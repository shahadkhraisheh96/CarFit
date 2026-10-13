using CarFitProject.Data;
using CarFitProject.Models;
using CarFitProject.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

var builder = WebApplication.CreateBuilder(args);

// CLI tool to export local SQL Server database to SQLite carfit.db
if (args.Contains("--export-sqlite"))
{
    await ExportDataToSqliteAsync(builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Server=localhost;Database=CarFit;Trusted_Connection=True;TrustServerCertificate=True;");
    return;
}

// QuestPDF community license (free for our use). Required before any PDF generation.
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var isAzure = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME"));
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// On Azure, if connection string is missing or pointing to local SQL Server, force SQLite in persistent storage
if (isAzure && (string.IsNullOrWhiteSpace(connectionString)
    || connectionString.Contains("shahadkh96", StringComparison.OrdinalIgnoreCase)
    || connectionString.Contains("localhost", StringComparison.OrdinalIgnoreCase)
    || connectionString.Contains("127.0.0.1")
    || connectionString.Contains("(localdb)", StringComparison.OrdinalIgnoreCase)))
{
    var homeDir = Environment.GetEnvironmentVariable("HOME") ?? "D:\\home";
    connectionString = $"Data Source={Path.Combine(homeDir, "carfit.db")}";
}
else if (string.IsNullOrWhiteSpace(connectionString))
{
    connectionString = "Server=shahadkh96;Database=CarFit;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";
}

var useSqlite = connectionString.StartsWith("Data Source", StringComparison.OrdinalIgnoreCase);

// Trust the dev SQL Server self-signed cert in Development only.
if (!useSqlite && builder.Environment.IsDevelopment()
    && !connectionString.Contains("TrustServerCertificate=", StringComparison.OrdinalIgnoreCase))
{
    if (!connectionString.EndsWith(";")) connectionString += ";";
    connectionString += "TrustServerCertificate=True;";
}

if (useSqlite)
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(connectionString));
    builder.Services.AddDbContext<CarFitDbContext>(options =>
        options.UseSqlite(connectionString));
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));
    builder.Services.AddDbContext<CarFitDbContext>(options =>
        options.UseSqlServer(connectionString));
}

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 10;
    options.Password.RequiredUniqueChars = 4;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// Swap Identity's default PBKDF2 hasher for BCrypt cost 12.
builder.Services.AddScoped<IPasswordHasher<ApplicationUser>, BCryptPasswordHasher<ApplicationUser>>();

// Email: SMTP for non-Development; in Development write to the logger so the
// password-reset / email-confirmation links are clickable without an SMTP server.
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddTransient<IEmailSender, LoggingEmailSender>();
}
else
{
    builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();
}

// Password-reset / email-confirmation token lifespan: 30 minutes (FR-1.3).
builder.Services.Configure<DataProtectionTokenProviderOptions>(o =>
    o.TokenLifespan = TimeSpan.FromMinutes(30));

builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddScoped<IUserAdminService, UserAdminService>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<IListingService, ListingService>();
builder.Services.AddScoped<IImageStorageService, ImageStorageService>();
builder.Services.AddScoped<IInspectionUploadService, InspectionUploadService>();
builder.Services.AddSingleton<IInspectionScoringService, InspectionScoringService>();
builder.Services.AddScoped<IInspectionReportService, InspectionReportService>();
builder.Services.AddScoped<IInspectionReportPdfService, InspectionReportPdfService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<ISavedCarsService, SavedCarsService>();
builder.Services.AddScoped<ITestimonialService, TestimonialService>();

// Phase 9: Stripe billing + Twilio SMS. Keys come from user-secrets / environment.
builder.Services.Configure<CarFitProject.Services.Billing.StripeSettings>(builder.Configuration.GetSection("Stripe"));
builder.Services.Configure<CarFitProject.Services.Billing.TwilioSettings>(builder.Configuration.GetSection("Twilio"));
builder.Services.AddScoped<CarFitProject.Services.Billing.IStripeService, CarFitProject.Services.Billing.StripeService>();
builder.Services.AddScoped<CarFitProject.Services.Billing.ISmsService, CarFitProject.Services.Billing.TwilioSmsService>();
builder.Services.AddScoped<CarFitProject.Services.Billing.ISellerSubscriptionService, CarFitProject.Services.Billing.SellerSubscriptionService>();

// Session is used by the Buyer questionnaire wizard to persist partial state
// across step transitions without writing an unfinished UserProfile row to SQL.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Bilingual EN / AR (NFR-U1). All localizable strings resolve through
// Resources/SharedResource.{en|ar}.resx via IStringLocalizer<SharedResource>
// / IHtmlLocalizer<SharedResource>; data-annotation messages on view models
// look up against the same shared file. Culture is sticky via a cookie set
// by /Language/Set.
builder.Services.AddLocalization();
builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options =>
    {
        options.DataAnnotationLocalizerProvider = (type, factory) =>
            factory.Create(typeof(CarFitProject.Resources.SharedResource));
    });

var supportedCultures = new[] { "en", "ar" };
builder.Services.Configure<Microsoft.AspNetCore.Builder.RequestLocalizationOptions>(options =>
{
    options.SetDefaultCulture("en");
    options.AddSupportedCultures(supportedCultures);
    options.AddSupportedUICultures(supportedCultures);
    options.RequestCultureProviders.Insert(0,
        new Microsoft.AspNetCore.Localization.CookieRequestCultureProvider());
});

var app = builder.Build();

// Register the Arabic PDF font if present. The inspection PDF renders Arabic correctly the
// moment Cairo-Regular.ttf / Cairo-Bold.ttf are dropped into wwwroot/fonts/ (until then,
// English PDFs work and Arabic falls back to the default Latin font).
var fontsDir = Path.Combine(app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"), "fonts");
foreach (var ttf in new[] { "Cairo-Regular.ttf", "Cairo-Bold.ttf" })
{
    var fontPath = Path.Combine(fontsDir, ttf);
    if (File.Exists(fontPath))
    {
        using var fs = File.OpenRead(fontPath);
        QuestPDF.Drawing.FontManager.RegisterFont(fs);
    }
}

// On Azure, if a pre-populated carfit.db template was deployed, copy it to persistent storage
if (isAzure)
{
    var targetDb = Path.Combine(Environment.GetEnvironmentVariable("HOME") ?? "D:\\home", "carfit.db");
    var seedDb = Path.Combine(app.Environment.ContentRootPath, "carfit.db");
    if (File.Exists(seedDb))
    {
        if (!File.Exists(targetDb) || new FileInfo(targetDb).Length < 100000)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(targetDb)!);
                File.Copy(seedDb, targetDb, overwrite: true);
            }
            catch (Exception ex)
            {
                var logger = app.Services.GetRequiredService<ILogger<Program>>();
                logger.LogWarning(ex, "Could not copy pre-populated carfit.db to persistent storage.");
            }
        }
    }
}

// For SQLite, auto-create the database and tables on first run.
if (useSqlite)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var appDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        appDb.Database.EnsureCreated();

        // Create CarFit business tables in the same SQLite file.
        var carFitDb = scope.ServiceProvider.GetRequiredService<CarFitDbContext>();
        try
        {
            var creator = carFitDb.Database.GetService<IRelationalDatabaseCreator>();
            creator.CreateTables();
        }
        catch
        {
            // Tables already exist or partially created
        }

        // Seed cars, listings, makes, sellers immediately!
        var seedLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("CarFitDataSeed");
        await CarFitDataSeed.SeedAsync(carFitDb, app.Environment.ContentRootPath, seedLogger);
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Failed to initialize SQLite database.");
    }
}

try
{
    await SeedIdentityAsync(app);
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning(ex, "SeedIdentityAsync warning.");
}

try
{
    await SeedGlossaryAsync(app);
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning(ex, "SeedGlossaryAsync warning.");
}

try
{
    await SeedBillingAsync(app);
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning(ex, "SeedBillingAsync warning.");
}

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

var locOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Builder.RequestLocalizationOptions>>().Value;
app.UseRequestLocalization(locOptions);

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages()
   .WithStaticAssets();

app.Run();

static async Task SeedIdentityAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var config = services.GetRequiredService<IConfiguration>();
    var env = services.GetRequiredService<IHostEnvironment>();

    string[] roleNames = { "Admin", "Dealer", "Buyer" };
    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    // Migrate any users still on the legacy "Seller" role across to "Dealer",
    // then drop the empty role. Idempotent — does nothing once "Seller" is gone.
    if (await roleManager.RoleExistsAsync("Seller"))
    {
        var legacySellerUsers = await userManager.GetUsersInRoleAsync("Seller");
        foreach (var legacyUser in legacySellerUsers)
        {
            if (!await userManager.IsInRoleAsync(legacyUser, "Dealer"))
            {
                await userManager.AddToRoleAsync(legacyUser, "Dealer");
            }
            await userManager.RemoveFromRoleAsync(legacyUser, "Seller");
        }

        var sellerRole = await roleManager.FindByNameAsync("Seller");
        if (sellerRole != null)
        {
            await roleManager.DeleteAsync(sellerRole);
        }
    }

    var adminEmail ="admin@carfit.local";
    var adminPassword = "ChangeMe!Admin#2026";

    if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
    {
        if (!env.IsDevelopment())
        {
            throw new InvalidOperationException(
                "AdminSeed:Email and AdminSeed:Password must be configured (user secrets or environment variables) in non-Development environments.");
        }
        return;
    }

    if (await userManager.FindByEmailAsync(adminEmail) != null)
    {
        return;
    }

    var newAdmin = new ApplicationUser
    {
        UserName = adminEmail,
        Email = adminEmail,
        EmailConfirmed = true,
        FullName = "CarFit Admin",
        IsActive = true
    };

    var createResult = await userManager.CreateAsync(newAdmin, adminPassword);
    if (createResult.Succeeded)
    {
        await userManager.AddToRoleAsync(newAdmin, "Admin");
    }
    else
    {
        var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
        throw new InvalidOperationException($"Failed to seed admin user: {errors}");
    }
}

static async Task SeedGlossaryAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<CarFitDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("CarFitDataSeed");
    await CarFitDataSeed.SeedAsync(context, app.Environment.ContentRootPath, logger);
    await InspectionGlossarySeed.SeedAsync(context);
    await MechanicSeed.SeedAsync(context);
    await MakeSeed.SeedAsync(context);
}

static async Task SeedBillingAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<CarFitDbContext>();
    var stripe = scope.ServiceProvider.GetRequiredService<CarFitProject.Services.Billing.IStripeService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
        .CreateLogger("SubscriptionPlanSeed");
    await CarFitProject.Services.Billing.SubscriptionPlanSeed.SeedAsync(context, stripe, logger);
}

static async Task ExportDataToSqliteAsync(string sqlServerConn)
{
    var targetFile = Path.Combine(Directory.GetCurrentDirectory(), "carfit.db");
    if (File.Exists(targetFile)) File.Delete(targetFile);

    var sqliteConn = $"Data Source={targetFile}";
    Console.WriteLine($"Exporting from SQL Server to SQLite: {targetFile}...");

    var sqlAppOpt = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(sqlServerConn).Options;
    var sqlFitOpt = new DbContextOptionsBuilder<CarFitDbContext>().UseSqlServer(sqlServerConn).Options;

    var litAppOpt = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(sqliteConn).Options;
    var litFitOpt = new DbContextOptionsBuilder<CarFitDbContext>().UseSqlite(sqliteConn).Options;

    using var srcApp = new ApplicationDbContext(sqlAppOpt);
    using var srcFit = new CarFitDbContext(sqlFitOpt);

    using var dstApp = new ApplicationDbContext(litAppOpt);
    using var dstFit = new CarFitDbContext(litFitOpt);

    dstApp.Database.EnsureCreated();
    var creator = dstFit.Database.GetService<IRelationalDatabaseCreator>();
    creator.CreateTables();

    // Export Identity users & roles
    try
    {
        var users = await srcApp.Users.AsNoTracking().ToListAsync();
        var roles = await srcApp.Roles.AsNoTracking().ToListAsync();
        var userRoles = await srcApp.UserRoles.AsNoTracking().ToListAsync();
        await dstApp.Users.AddRangeAsync(users);
        await dstApp.Roles.AddRangeAsync(roles);
        await dstApp.UserRoles.AddRangeAsync(userRoles);
        await dstApp.SaveChangesAsync();
        Console.WriteLine($"Exported {users.Count} users and {roles.Count} roles.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Note on Identity export: {ex.Message}");
    }

    // Export CarFit entities
    try
    {
        var makes = await srcFit.Makes.AsNoTracking().ToListAsync();
        dstFit.Makes.AddRange(makes);

        var mechanics = await srcFit.Mechanics.AsNoTracking().ToListAsync();
        dstFit.Mechanics.AddRange(mechanics);

        var glossaries = await srcFit.InspectionTermsGlossaries.AsNoTracking().ToListAsync();
        dstFit.InspectionTermsGlossaries.AddRange(glossaries);

        var sellers = await srcFit.Sellers.AsNoTracking().ToListAsync();
        dstFit.Sellers.AddRange(sellers);

        var cars = await srcFit.Cars.AsNoTracking().ToListAsync();
        dstFit.Cars.AddRange(cars);

        var listings = await srcFit.CarListings.AsNoTracking().ToListAsync();
        dstFit.CarListings.AddRange(listings);

        var images = await srcFit.CarImages.AsNoTracking().ToListAsync();
        dstFit.CarImages.AddRange(images);

        var reports = await srcFit.InspectionReports.AsNoTracking().ToListAsync();
        dstFit.InspectionReports.AddRange(reports);

        var testimonials = await srcFit.Testimonials.AsNoTracking().ToListAsync();
        dstFit.Testimonials.AddRange(testimonials);

        var plans = await srcFit.SubscriptionPlans.AsNoTracking().ToListAsync();
        dstFit.SubscriptionPlans.AddRange(plans);

        await dstFit.SaveChangesAsync();
        Console.WriteLine($"Exported {cars.Count} cars, {listings.Count} listings, {makes.Count} makes, {sellers.Count} sellers, {images.Count} images.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error exporting CarFit entities: {ex.Message}");
    }

    Console.WriteLine("Export completed successfully!");
}

