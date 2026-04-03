using Microsoft.EntityFrameworkCore;
using TourGuideHCM.API.Data;
using TourGuideHCM.API.Models;

namespace TourGuideHCM.API.Services
{
    public class ReviewService
    {
        private readonly AppDbContext _context;

        public ReviewService(AppDbContext context)
        {
            _context = context;
        }

        public List<Review> GetAll()
            => _context.Reviews.Include(r => r.User).Include(r => r.POI).ToList();

        public Review? GetById(int id)
            => _context.Reviews.Include(r => r.User).Include(r => r.POI).FirstOrDefault(x => x.Id == id);

        public Review Add(Review review)
        {
            review.CreatedAt = DateTime.UtcNow;
            _context.Reviews.Add(review);
            _context.SaveChanges();
            return review;
        }

        public bool Update(int id, Review updated)
        {
            var review = _context.Reviews.Find(id);
            if (review == null) return false;

            review.Rating = updated.Rating;
            review.Comment = updated.Comment;
            _context.SaveChanges();
            return true;
        }

        public bool Delete(int id)
        {
            var review = _context.Reviews.Find(id);
            if (review == null) return false;
            _context.Reviews.Remove(review);
            _context.SaveChanges();
            return true;
        }
    }
}