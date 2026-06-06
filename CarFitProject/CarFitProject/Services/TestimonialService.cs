using CarFitProject.Models;
using Microsoft.EntityFrameworkCore;

namespace CarFitProject.Services
{
    /// <summary>
    /// Website testimonials (FR — platform reviews): submission, the public
    /// approved feed for the home page, and the admin moderation queue.
    /// Each user owns at most one review (see the unique index on user_id).
    /// </summary>
    public interface ITestimonialService
    {
        /// <summary>Approved reviews, newest first — used by the home page carousel.</summary>
        Task<List<Testimonial>> GetApprovedAsync(int max = 12);

        /// <summary>Every review (pending first, then newest) — used by the admin management table.</summary>
        Task<List<Testimonial>> GetAllAsync();

        /// <summary>Single review by id — used by the admin details view.</summary>
        Task<Testimonial?> GetByIdAsync(int id);

        /// <summary>The caller's existing review, or null if they haven't reviewed yet.</summary>
        Task<Testimonial?> GetByUserAsync(string userId);

        /// <summary>
        /// Create-or-update the caller's single review. Inserts when none exists;
        /// otherwise updates the existing row. Either way the review is reset to
        /// pending (IsApproved = false) so an admin must re-approve it.
        /// </summary>
        Task SaveAsync(string userId, string reviewerName, int rating, string reviewText);

        /// <summary>Sets IsApproved = true. Returns false if the review no longer exists.</summary>
        Task<bool> ApproveAsync(int id);

        /// <summary>Removes the review. Returns false if it no longer exists.</summary>
        Task<bool> DeleteAsync(int id);
    }

    public class TestimonialService : ITestimonialService
    {
        private readonly CarFitDbContext _context;

        public TestimonialService(CarFitDbContext context)
        {
            _context = context;
        }

        public Task<List<Testimonial>> GetApprovedAsync(int max = 12) =>
            _context.Testimonials
                .AsNoTracking()
                .Where(t => t.IsApproved)
                .OrderByDescending(t => t.CreatedDate)
                .Take(max)
                .ToListAsync();

        public Task<List<Testimonial>> GetAllAsync() =>
            _context.Testimonials
                .AsNoTracking()
                .OrderBy(t => t.IsApproved)          // pending (false) first
                .ThenByDescending(t => t.CreatedDate)
                .ToListAsync();

        public Task<Testimonial?> GetByIdAsync(int id) =>
            _context.Testimonials
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);

        public Task<Testimonial?> GetByUserAsync(string userId) =>
            _context.Testimonials
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == userId);

        public async Task SaveAsync(string userId, string reviewerName, int rating, string reviewText)
        {
            // Tracked lookup (no AsNoTracking) so an update is persisted in place.
            var existing = await _context.Testimonials
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (existing == null)
            {
                // Id is an IDENTITY column — never assign it; let SQL Server
                // generate the value so we don't trip a PK violation.
                _context.Testimonials.Add(new Testimonial
                {
                    ReviewerName = reviewerName.Trim(),
                    Rating = rating,
                    ReviewText = reviewText.Trim(),
                    UserId = userId,
                    IsApproved = false,
                    CreatedDate = DateTime.UtcNow
                });
            }
            else
            {
                existing.ReviewerName = reviewerName.Trim();
                existing.Rating = rating;
                existing.ReviewText = reviewText.Trim();
                // Editing always returns the review to the moderation queue.
                existing.IsApproved = false;
                // CreatedDate is left untouched — it stays the original submission date.
            }

            await _context.SaveChangesAsync();
        }

        public async Task<bool> ApproveAsync(int id)
        {
            var testimonial = await _context.Testimonials.FindAsync(id);
            if (testimonial == null) return false;

            testimonial.IsApproved = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var testimonial = await _context.Testimonials.FindAsync(id);
            if (testimonial == null) return false;

            _context.Testimonials.Remove(testimonial);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
