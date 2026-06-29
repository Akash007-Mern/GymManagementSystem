using System;
using System.Collections.Generic;
using GymManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Data;

public partial class GymDbContext : DbContext
{
    public GymDbContext()
    {
    }

    public GymDbContext(DbContextOptions<GymDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Attendance> Attendances { get; set; }

    public virtual DbSet<Member> Members { get; set; }

    public virtual DbSet<MembershipPlan> MembershipPlans { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Trainer> Trainers { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=RONIK;Database=GymManagementDB;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(e => e.AttendanceId).HasName("PK__Attendan__8B69261C9DFB6034");

            entity.ToTable("Attendance");

            entity.Property(e => e.CheckInTime).HasColumnType("datetime");
            entity.Property(e => e.CheckOutTime).HasColumnType("datetime");

            entity.HasOne(d => d.Member).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.MemberId)
                .HasConstraintName("FK_Attendance_Members");
        });

        modelBuilder.Entity<Member>(entity =>
        {
            entity.HasKey(e => e.MemberId).HasName("PK__Members__0CF04B18CFA3D7A3");

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Gender).HasMaxLength(10);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Phone).HasMaxLength(20);

            entity.HasOne(d => d.Plan).WithMany(p => p.Members)
                .HasForeignKey(d => d.PlanId)
                .HasConstraintName("FK_Members_Plans");

            entity.HasOne(d => d.Trainer).WithMany(p => p.Members)
                .HasForeignKey(d => d.TrainerId)
                .HasConstraintName("FK_Members_Trainers");
        });

        modelBuilder.Entity<MembershipPlan>(entity =>
        {
            entity.HasKey(e => e.PlanId).HasName("PK__Membersh__755C22B7DAC80AF1");

            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.PlanName).HasMaxLength(100);
            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payments__9B556A38F38CBEF6");

            entity.Property(e => e.AmountPaid).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Notes).HasMaxLength(255);

            entity.HasOne(d => d.Member).WithMany(p => p.Payments)
                .HasForeignKey(d => d.MemberId)
                .HasConstraintName("FK_Payments_Members");

            entity.HasOne(d => d.Plan).WithMany(p => p.Payments)
                .HasForeignKey(d => d.PlanId)
                .HasConstraintName("FK_Payments_Plans");
        });

        modelBuilder.Entity<Trainer>(entity =>
        {
            entity.HasKey(e => e.TrainerId).HasName("PK__Trainers__366A1A7CB5EEF8A1");

            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Specialty).HasMaxLength(100);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
