using Project01_movie_lease_system.Models;

namespace Project01_movie_lease_system.Models
{
    public class HomeIndexViewModel
    {
        public List<MovieDetails> FeaturedMovies { get; set; } = new List<MovieDetails>();
        public List<MovieType> MovieTypes { get; set; } = new List<MovieType>();
        public List<Review> RecentReviews { get; set; } = new List<Review>();
    }
}