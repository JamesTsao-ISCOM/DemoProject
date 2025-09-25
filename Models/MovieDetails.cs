using System.ComponentModel.DataAnnotations;

namespace Project01_movie_lease_system.Models;

public class MovieDetails
{
    public int Id { get; set; }
    
    [StringLength(100)]
    public string Title { get; set; } = null!;

    [StringLength(200)]
    public string Description { get; set; } = null!;

    public MovieType Type { get; set; }

    public DateTime ReleaseDate { get; set; }

    [StringLength(100)]
    public string Director { get; set; } = null!;

    public decimal Price { get; set; }

    public string ImageFileName { get; set; } = null!;

    public string YoutubeTrailerUrl { get; set; } = null!;

    public int Stock { get; set; }
    
    // 額外的統計資訊
    public double AverageRating { get; set; }
    
    public int ReviewCount { get; set; }
    
    // 導航屬性
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    
    public string FormattedRating => $"{AverageRating:F1}";
    
    public string RatingStars
    {
        get
        {
            var fullStars = (int)Math.Floor(AverageRating);
            var hasHalfStar = AverageRating - fullStars >= 0.5;
            var emptyStars = 5 - fullStars - (hasHalfStar ? 1 : 0);
            
            return string.Concat(
                string.Concat(Enumerable.Repeat("★", fullStars)),
                hasHalfStar ? "☆" : "",
                string.Concat(Enumerable.Repeat("☆", emptyStars))
            );
        }
    }
}