using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Project01_movie_lease_system.Models;

public partial class Member
{
    [Key]
    public int Id { get; set; }

    [StringLength(50)]
    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    [StringLength(255)]
    public string PasswordHash { get; set; } = null!;

    [NotMapped]
    public string ConfirmPassword { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    [StringLength(15)]
    public string PhoneNumber { get; set; } = null!;

    public bool IsVerified { get; set; }

    public DateTime UpdatedAt { get; set; }

    [InverseProperty("Member")]
    public virtual ICollection<Lease> Leases { get; set; } = new List<Lease>();

    [InverseProperty("Member")]
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}
