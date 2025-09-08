using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Project01_movie_lease_system.Models;

[Index("AdminId", Name = "IX_Files_AdminId")]
[Index("FileCategoryId", Name = "IX_Files_FileCategoryId")]
public partial class File
{
    [Key]
    public int Id { get; set; }

    public string FileName { get; set; } = null!;

    public string StoredFileName { get; set; } = null!;

    [StringLength(200)]
    public string Description { get; set; } = null!;

    public string FileType { get; set; } = null!;

    public long FileSize { get; set; }

    public int CategoryId { get; set; }

    public int AdminId { get; set; }

    public DateTime UploadDate { get; set; }

    public int? FileCategoryId { get; set; }
    [NotMapped]
    public string Uploader { get; set; }

    [ForeignKey("AdminId")]
    [InverseProperty("Files")]
    public virtual Admin Admin { get; set; } = null!;

    [ForeignKey("FileCategoryId")]
    [InverseProperty("Files")]
    public virtual FileCategory? FileCategory { get; set; }
    
    // 為了向後相容，添加 Category 屬性（可讀寫）
    [NotMapped]
    public string Category{ get;  set;}
    

    [InverseProperty("File")]
    public virtual ICollection<VideoWatchRecord> VideoWatchRecords { get; set; } = new List<VideoWatchRecord>();
}
