using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Project01_movie_lease_system.Models;

[Index("MemberId", Name = "IX_Leases_MemberId")]
[Index("MovieId", Name = "IX_Leases_MovieId")]
public partial class Lease
{
    [Key]
    public int Id { get; set; }

    public int MemberId { get; set; }

    public int MovieId { get; set; }

    public DateTime LeaseDate { get; set; }

    public DateTime? ReturnDate { get; set; }

    public int Status { get; set; }

    [ForeignKey("MemberId")]
    [InverseProperty("Leases")]
    public virtual Member Member { get; set; } = null!;

    [ForeignKey("MovieId")]
    [InverseProperty("Leases")]
    public virtual Movie Movie { get; set; } = null!;
}
