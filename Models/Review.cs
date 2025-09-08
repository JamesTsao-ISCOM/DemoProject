using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Project01_movie_lease_system.Models;

[Index("MemberId", Name = "IX_Reviews_MemberId")]
[Index("MovieId", Name = "IX_Reviews_MovieId")]
public partial class Review
{
    [Key]
    public int Id { get; set; }

    public int MemberId { get; set; }

    public int MovieId { get; set; }

    [StringLength(500)]
    public string Content { get; set; } = null!;

    public int Rating { get; set; }

    public DateTime ReviewDate { get; set; }

    [ForeignKey("MemberId")]
    [InverseProperty("Reviews")]
    public virtual Member Member { get; set; } = null!;

    [ForeignKey("MovieId")]
    [InverseProperty("Reviews")]
    public virtual Movie Movie { get; set; } = null!;
}
