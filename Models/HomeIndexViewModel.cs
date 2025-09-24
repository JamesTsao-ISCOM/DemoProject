using Project01_movie_lease_system.Models;

namespace Project01_movie_lease_system.Models
{
    public class HomeIndexViewModel
    {
        public List<Movie> FeaturedMovies { get; set; } = new List<Movie>();
        public List<MovieType> MovieTypes { get; set; } = new List<MovieType>();
        public List<Review> RecentReviews { get; set; } = new List<Review>();
    }
}