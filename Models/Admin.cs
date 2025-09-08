using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Project01_movie_lease_system.Models;

public partial class Admin
{
    [Key]
    public int Id { get; set; }

    [StringLength(50)]
    public string Username { get; set; } = null!;

    [StringLength(255)]
    public string PasswordHash { get; set; } = null!;

    [StringLength(100)]
    public string Email { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public StaffRole Role { get; set; }

    [InverseProperty("Admin")]
    public virtual ICollection<File> Files { get; set; } = new List<File>();

    [InverseProperty("Admin")]
    public virtual ICollection<VideoWatchRecord> VideoWatchRecords { get; set; } = new List<VideoWatchRecord>();
}
