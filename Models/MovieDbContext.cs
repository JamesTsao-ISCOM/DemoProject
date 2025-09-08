using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Project01_movie_lease_system.Models;

public partial class MovieDbContext : DbContext
{
    public MovieDbContext()
    {
    }

    public MovieDbContext(DbContextOptions<MovieDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Admin> Admins { get; set; }

    public virtual DbSet<File> Files { get; set; }

    public virtual DbSet<FileCategory> FileCategories { get; set; }

    public virtual DbSet<Lease> Leases { get; set; }

    public virtual DbSet<Member> Members { get; set; }

    public virtual DbSet<Movie> Movies { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<VideoWatchRecord> VideoWatchRecords { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    #warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=MovieDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VideoWatchRecord>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__VideoWat__3214EC07974BCD37");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Admin).WithMany(p => p.VideoWatchRecords)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VideoWatchRecords_Admins");

            entity.HasOne(d => d.File).WithMany(p => p.VideoWatchRecords)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VideoWatchRecords_Videos");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
