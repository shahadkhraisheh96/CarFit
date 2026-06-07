using CarFitProject.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CarFitProject.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>(b =>
            {
                b.Property(u => u.IsActive).HasDefaultValue(true);
                b.Property(u => u.CreatedAt).HasDefaultValueSql("getutcdate()");
                b.Property(u => u.SubscriptionTier)
                    .HasMaxLength(20)
                    .HasDefaultValue("Free");
            });
        }
    }
}
