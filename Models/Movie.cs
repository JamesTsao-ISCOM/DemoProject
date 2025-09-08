using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Project01_movie_lease_system.Models;

public partial class Movie
{
    [Key]
    public int Id { get; set; }

    [StringLength(100)]
    public string Title { get; set; } = null!;

    [StringLength(200)]
    public string Description { get; set; } = null!;

    public MovieType Type { get; set; }

    public DateTime ReleaseDate { get; set; }

    [StringLength(100)]
    public string Director { get; set; } = null!;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Price { get; set; }

    public string ImageFileName { get; set; } = null!;

    public string YoutubeTrailerUrl { get; set; } = null!;

    public int Stock { get; set; }

    [InverseProperty("Movie")]
    public virtual ICollection<Lease> Leases { get; set; }

    [InverseProperty("Movie")]
    public virtual ICollection<Review> Reviews { get; set; }
}
