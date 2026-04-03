using Microsoft.EntityFrameworkCore;
using TourGuideHCM.API.Data;
using TourGuideHCM.API.Models;

namespace TourGuideHCM.API.Services
{
    public class FavoriteService
    {
        private readonly AppDbContext _context;

        public FavoriteService(AppDbContext context)
        {
            _context = context;
        }

        public List<Favorite> GetAll()
            => _context.Favorites.Include(f => f.User).Include(f => f.POI).ToList();

        public Favorite? GetById(int id)
            => _context.Favorites.Include(f => f.User).Include(f => f.POI).FirstOrDefault(x => x.Id == id);

        public Favorite Add(Favorite favorite)
        {
            favorite.AddedAt = DateTime.UtcNow;
            _context.Favorites.Add(favorite);
            _context.SaveChanges();
            return favorite;
        }

        public bool Delete(int id)
        {
            var fav = _context.Favorites.Find(id);
            if (fav == null) return false;
            _context.Favorites.Remove(fav);
            _context.SaveChanges();
            return true;
        }

        public List<Favorite> GetByUserId(int userId)
            => _context.Favorites.Include(f => f.POI).Where(f => f.UserId == userId).ToList();
    }
}