using CarFitProject.ViewModel;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace CarFitProject.Models;

public partial class CarFitDbContext : DbContext
{
    public CarFitDbContext()
    {
    }

    public CarFitDbContext(DbContextOptions<CarFitDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Car> Cars { get; set; }

    public virtual DbSet<CarImage> CarImages { get; set; }

    public virtual DbSet<CarListing> CarListings { get; set; }

    public virtual DbSet<InspectionReport> InspectionReports { get; set; }

    public virtual DbSet<InspectionTermsGlossary> InspectionTermsGlossaries { get; set; }

    public virtual DbSet<Mechanic> Mechanics { get; set; }

    public virtual DbSet<RecommendationLog> RecommendationLogs { get; set; }

    public virtual DbSet<SavedResult> SavedResults { get; set; }

    public virtual DbSet<SearchLog> SearchLogs { get; set; }

    public virtual DbSet<Seller> Sellers { get; set; }

    public virtual DbSet<UserProfile> UserProfiles { get; set; }

    public virtual DbSet<VwAvailableCarDetail> VwAvailableCarDetails { get; set; }
    public virtual DbSet<RecommendedCarViewModel> RecommendedCarMatches { get; set; }
    public virtual DbSet<InspectionBooking> InspectionBookings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Car>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Cars__3213E83F43C12A3A");

            entity.HasIndex(e => new { e.Transmission, e.BodyType, e.Year }, "IX_Cars_Matching");
            entity.HasIndex(e => e.Make, "IX_Cars_Make");
            entity.HasIndex(e => e.Model, "IX_Cars_Model");
            entity.HasIndex(e => e.Price, "IX_Cars_Price");
            entity.HasIndex(e => e.Type, "IX_Cars_Type");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BodyType)
                .HasMaxLength(100)
                .HasColumnName("body_type");
            entity.Property(e => e.EngineSize)
                .HasMaxLength(100)
                .HasColumnName("engine_size");
            entity.Property(e => e.ExteriorColor)
                .HasMaxLength(100)
                .HasColumnName("exterior_color");
            entity.Property(e => e.ExteriorOptions).HasColumnName("exterior_options");
            entity.Property(e => e.FuelEfficiency)
                .HasMaxLength(50)
                .HasColumnName("fuel_efficiency");
            entity.Property(e => e.FuelType)
                .HasMaxLength(20)
                .HasColumnName("fuel_type");
            entity.Property(e => e.Images).HasColumnName("images");
            entity.Property(e => e.InteriorColor)
                .HasMaxLength(100)
                .HasColumnName("interior_color");
            entity.Property(e => e.InteriorOptions).HasColumnName("interior_options");
            entity.Property(e => e.Kilometers)
                .HasMaxLength(100)
                .HasColumnName("kilometers");
            entity.Property(e => e.Make)
                .HasMaxLength(50)
                .HasColumnName("make");
            entity.Property(e => e.Model)
                .HasMaxLength(50)
                .HasColumnName("model");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("price");
            entity.Property(e => e.RegionalSpecs)
                .HasMaxLength(100)
                .HasColumnName("regional_specs");
            entity.Property(e => e.ScrapedId).HasColumnName("scraped_id");
            entity.Property(e => e.Seats).HasColumnName("seats");
            entity.Property(e => e.TechnologyOptions).HasColumnName("technology_options");
            entity.Property(e => e.Transmission)
                .HasMaxLength(20)
                .HasColumnName("transmission");
            entity.Property(e => e.Trim)
                .HasMaxLength(100)
                .HasColumnName("trim");
            entity.Property(e => e.Type)
                .HasMaxLength(20)
                .HasColumnName("type");
            entity.Property(e => e.Year).HasColumnName("year");
        });

        modelBuilder.Entity<CarListing>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CarListi__3213E83F8D2FA73D");

            entity.HasIndex(e => e.Status, "IX_CarListings_Status");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active")
                .HasColumnName("status");
            entity.Property(e => e.CarId).HasColumnName("car_id");
            entity.Property(e => e.InstallmentOption)
                .HasDefaultValue(false)
                .HasColumnName("installment_option");
            entity.Property(e => e.ListingPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("listing_price");
            entity.Property(e => e.PaymentMethodAllowed)
                .HasMaxLength(100)
                .HasColumnName("payment_method_allowed");
            entity.Property(e => e.SellerId).HasColumnName("seller_id");

            entity.HasOne(d => d.Car).WithMany(p => p.CarListings)
                .HasForeignKey(d => d.CarId)
                .HasConstraintName("FK__CarListin__car_i__44FF419A");

            entity.HasOne(d => d.Seller).WithMany(p => p.CarListings)
                .HasForeignKey(d => d.SellerId)
                .HasConstraintName("FK__CarListin__selle__45F365D3");
        });

        modelBuilder.Entity<CarImage>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.ToTable("CarImages");

            entity.HasIndex(e => e.CarId, "IX_CarImages_car_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CarId).HasColumnName("car_id");
            entity.Property(e => e.Url)
                .IsRequired()
                .HasMaxLength(2048)
                .HasColumnName("url");
            entity.Property(e => e.SortOrder)
                .HasDefaultValue(0)
                .HasColumnName("sort_order");
            entity.Property(e => e.IsPrimary)
                .HasDefaultValue(false)
                .HasColumnName("is_primary");

            entity.HasOne(d => d.Car).WithMany(c => c.CarImages)
                .HasForeignKey(d => d.CarId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InspectionReport>(entity =>
        {
            entity.HasKey(e => e.CarId).HasName("PK__Inspecti__4C9A0DB3B4E9E2BC");

            entity.Property(e => e.CarId)
                .ValueGeneratedNever()
                .HasColumnName("car_id");
            entity.Property(e => e.BodyCondition)
                .HasMaxLength(255)
                .HasColumnName("body_condition");
            entity.Property(e => e.CalculatedTrustScore)
                .HasColumnType("decimal(3, 2)")
                .HasColumnName("calculated_trust_score");
            entity.Property(e => e.CarseerAttached)
                .HasDefaultValue(false)
                .HasColumnName("carseer_attached");
            entity.Property(e => e.CenterName)
                .HasMaxLength(100)
                .HasColumnName("center_name");
            entity.Property(e => e.Chassis1Status)
                .HasMaxLength(50)
                .HasColumnName("chassis_1_status");
            entity.Property(e => e.Chassis2Status)
                .HasMaxLength(50)
                .HasColumnName("chassis_2_status");
            entity.Property(e => e.Chassis3Status)
                .HasMaxLength(50)
                .HasColumnName("chassis_3_status");
            entity.Property(e => e.Chassis4Status)
                .HasMaxLength(50)
                .HasColumnName("chassis_4_status");
            entity.Property(e => e.DescriptionScore)
                .HasMaxLength(255)
                .HasColumnName("description_score");
            entity.Property(e => e.EngineHealthPercent).HasColumnName("engine_health_percent");
            entity.Property(e => e.EngineSmoke).HasColumnName("engine_smoke");
            entity.Property(e => e.GearboxStatus)
                .HasMaxLength(100)
                .HasColumnName("gearbox_status");
            entity.Property(e => e.InspectionDate).HasColumnName("inspection_date");
            entity.Property(e => e.OverallScore)
                .HasColumnType("decimal(3, 2)")
                .HasColumnName("overall_score");
            entity.Property(e => e.PaintFillerStatus).HasColumnName("paint_filler_status");
            entity.Property(e => e.PaintStatus)
                .HasMaxLength(255)
                .HasColumnName("paint_status");
            entity.Property(e => e.RoofCondition)
                .HasMaxLength(100)
                .HasColumnName("roof_condition");

            entity.HasOne(d => d.Car).WithOne(p => p.InspectionReport)
                .HasForeignKey<InspectionReport>(d => d.CarId)
                .HasConstraintName("FK__Inspectio__car_i__49C3F6B7");
        });

        modelBuilder.Entity<InspectionTermsGlossary>(entity =>
        {
            entity.HasKey(e => e.Term).HasName("PK__Inspecti__E0F9670E11FE1C38");

            entity.ToTable("InspectionTermsGlossary");

            entity.Property(e => e.Term)
                .HasMaxLength(100)
                .HasColumnName("term");
            entity.Property(e => e.ExplanationAr).HasColumnName("explanation_ar");
            entity.Property(e => e.ExplanationEn).HasColumnName("explanation_en");
            entity.Property(e => e.SeverityLevel)
                .HasMaxLength(20)
                .HasColumnName("severity_level");
        });

        modelBuilder.Entity<RecommendationLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Recommen__3213E83F3C2D9495");

            entity.ToTable("RecommendationLog");

            entity.HasIndex(e => e.UserId, "IX_RecommendationLog_user_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.RecommendedCarIds).HasColumnName("recommended_car_ids");
            entity.Property(e => e.Score)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("score");
            entity.Property(e => e.UserId)
                .HasMaxLength(450)
                .HasColumnName("user_id");
        });

        modelBuilder.Entity<SavedResult>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.CarId }).HasName("PK__SavedRes__9D7797D4910E870F");

            entity.Property(e => e.UserId)
                .HasMaxLength(450)
                .HasColumnName("user_id");
            entity.Property(e => e.CarId).HasColumnName("car_id");
            entity.Property(e => e.SavedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("saved_at");

            entity.HasOne(d => d.Car).WithMany(p => p.SavedResults)
                .HasForeignKey(d => d.CarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SavedResu__car_i__5070F446");
        });

        modelBuilder.Entity<Seller>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Sellers__3213E83F4F76A86D");

            entity.HasIndex(e => e.IdentityUserId, "IX_Sellers_IdentityUserId");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.City)
                .HasMaxLength(100)
                .HasColumnName("city");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.IdentityUserId)
                .HasMaxLength(450)
                .HasColumnName("identity_user_id");
            entity.Property(e => e.IsApproved)
                .HasDefaultValue(false)
                .HasColumnName("is_approved");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Neighborhood)
                .HasMaxLength(100)
                .HasColumnName("neighborhood");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.Tier)
                .HasMaxLength(20)
                .HasColumnName("tier");
            entity.Property(e => e.Type)
                .HasMaxLength(20)
                .HasColumnName("type");
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(e => e.ProfileId).HasName("PK__UserProf__AEBB701F5EA739D8");

            entity.Property(e => e.ProfileId).HasColumnName("profile_id");
            entity.Property(e => e.Age).HasColumnName("age");
            entity.Property(e => e.BudgetMax)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("budget_max");
            entity.Property(e => e.BudgetMin)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("budget_min");
            entity.Property(e => e.HasKids)
                .HasDefaultValue(false)
                .HasColumnName("has_kids");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.KidsCount)
                .HasDefaultValue(0)
                .HasColumnName("kids_count");
            entity.Property(e => e.MaritalStatus)
                .HasMaxLength(50)
                .HasColumnName("marital_status");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .HasColumnName("payment_method");
            entity.Property(e => e.ProfileName)
                .HasMaxLength(100)
                .HasDefaultValue("My Fit Profile")
                .HasColumnName("profile_name");
            entity.Property(e => e.Purpose)
                .HasMaxLength(100)
                .HasColumnName("purpose");
            entity.Property(e => e.SizePref)
                .HasMaxLength(50)
                .HasColumnName("size_pref");
            entity.Property(e => e.TransmissionPref)
                .HasMaxLength(20)
                .HasColumnName("transmission_pref");
            entity.Property(e => e.TripType)
                .HasMaxLength(20)
                .HasColumnName("trip_type");
            entity.Property(e => e.ConditionPref)
                .HasMaxLength(20)
                .HasColumnName("condition_pref");
            entity.Property(e => e.InstallmentMonths)
                .HasColumnName("installment_months");
            entity.Property(e => e.UserId)
                .HasMaxLength(450)
                .HasColumnName("user_id");
        });

        modelBuilder.Entity<RecommendedCarViewModel>(entity =>
        {
            entity.Property(e => e.ListingPrice).HasPrecision(18, 2);
            entity.Property(e => e.TrustScore).HasPrecision(18, 2);
        });

        modelBuilder.Entity<VwAvailableCarDetail>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_AvailableCarDetails");

            entity.Property(e => e.BodyCondition)
                .HasMaxLength(255)
                .HasColumnName("body_condition");
            entity.Property(e => e.BodyType)
                .HasMaxLength(100)
                .HasColumnName("body_type");
            entity.Property(e => e.City)
                .HasMaxLength(100)
                .HasColumnName("city");
            entity.Property(e => e.DescriptionScore)
                .HasMaxLength(255)
                .HasColumnName("description_score");
            entity.Property(e => e.EngineSize)
                .HasMaxLength(100)
                .HasColumnName("engine_size");
            entity.Property(e => e.ExteriorColor)
                .HasMaxLength(100)
                .HasColumnName("exterior_color");
            entity.Property(e => e.ExteriorOptions).HasColumnName("exterior_options");
            entity.Property(e => e.FuelType)
                .HasMaxLength(20)
                .HasColumnName("fuel_type");
            entity.Property(e => e.Images).HasColumnName("images");
            entity.Property(e => e.InteriorColor)
                .HasMaxLength(100)
                .HasColumnName("interior_color");
            entity.Property(e => e.InteriorOptions).HasColumnName("interior_options");
            entity.Property(e => e.Kilometers)
                .HasMaxLength(100)
                .HasColumnName("kilometers");
            entity.Property(e => e.ListingPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("listing_price");
            entity.Property(e => e.Make)
                .HasMaxLength(50)
                .HasColumnName("make");
            entity.Property(e => e.Model)
                .HasMaxLength(50)
                .HasColumnName("model");
            entity.Property(e => e.Neighborhood)
                .HasMaxLength(100)
                .HasColumnName("neighborhood");
            entity.Property(e => e.PaintStatus)
                .HasMaxLength(255)
                .HasColumnName("paint_status");
            entity.Property(e => e.PaymentMethodAllowed)
                .HasMaxLength(100)
                .HasColumnName("payment_method_allowed");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("price");
            entity.Property(e => e.RegionalSpecs)
                .HasMaxLength(100)
                .HasColumnName("regional_specs");
            entity.Property(e => e.ScrapedId).HasColumnName("scraped_id");
            entity.Property(e => e.Seats).HasColumnName("seats");
            entity.Property(e => e.SellerName).HasMaxLength(100);
            entity.Property(e => e.TechnologyOptions).HasColumnName("technology_options");
            entity.Property(e => e.Transmission)
                .HasMaxLength(20)
                .HasColumnName("transmission");
            entity.Property(e => e.Trim)
                .HasMaxLength(100)
                .HasColumnName("trim");
            entity.Property(e => e.TrustScore).HasColumnType("decimal(3, 2)");
            entity.Property(e => e.Year).HasColumnName("year");
        });
        modelBuilder.Entity<InspectionBooking>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.CustomerName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.CustomerEmail)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.PackageType)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.PreferredDate)
                .IsRequired();

            entity.Property(e => e.VehicleNotes)
                .HasMaxLength(500);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())");

            entity.HasOne(b => b.CarListing)
                .WithMany()
                .HasForeignKey(b => b.CarListingId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(b => b.Mechanic)
                .WithMany(m => m.InspectionBookings)
                .HasForeignKey(b => b.MechanicId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Mechanic>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Mechanics");

            entity.HasIndex(e => e.City, "IX_Mechanics_City");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.City)
                .HasMaxLength(100)
                .HasColumnName("city");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
        });

        modelBuilder.Entity<SearchLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("SearchLog");

            entity.HasIndex(e => e.CreatedAt, "IX_SearchLog_created_at");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Term)
                .HasMaxLength(255)
                .HasColumnName("term");
            entity.Property(e => e.FiltersJson).HasColumnName("filters_json");
            entity.Property(e => e.UserId)
                .HasMaxLength(450)
                .HasColumnName("user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("getutcdate()")
                .HasColumnName("created_at");
        });
        modelBuilder.Entity<CarImage>(entity =>
        {
            entity.ToTable("car_images"); // or CarImages

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CarId).HasColumnName("car_id");
            entity.Property(e => e.Url).HasColumnName("url");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");
            entity.Property(e => e.IsPrimary).HasColumnName("is_primary");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
