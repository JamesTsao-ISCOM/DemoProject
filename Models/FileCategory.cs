using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Project01_movie_lease_system.Models;

public partial class FileCategory
{
    [Key]
    public int Id { get; set; }

    public string CategoryName { get; set; } = null!;

    public string Description { get; set; } = null!;

    [InverseProperty("FileCategory")]
    public virtual ICollection<File> Files { get; set; } = new List<File>();
}
