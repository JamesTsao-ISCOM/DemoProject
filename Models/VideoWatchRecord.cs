using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Project01_movie_lease_system.Models;

public partial class VideoWatchRecord
{
    [Key]
    public int Id { get; set; }

    public int AdminId { get; set; }

    public int FileId { get; set; }

    public double LastPosition { get; set; }

    public bool IsCompleted { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime LastWatchedAt { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("AdminId")]
    [InverseProperty("VideoWatchRecords")]
    public virtual Admin Admin { get; set; } = null!;

    [ForeignKey("FileId")]
    [InverseProperty("VideoWatchRecords")]
    public virtual File File { get; set; } = null!;
}
