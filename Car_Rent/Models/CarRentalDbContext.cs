using System;
using System.Collections.Generic;
using Car_Rent.Infrastructure.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace Car_Rent.Models;

public partial class CarRentalDbContext : DbContext
{
    private readonly ITenantProvider _tenant;
    public CarRentalDbContext()
    {
    }

    public CarRentalDbContext(DbContextOptions<CarRentalDbContext> options, ITenantProvider tenant)
        : base(options)
    {
        _tenant = tenant;
    }

    public virtual DbSet<Comment> Comments { get; set; }

    public virtual DbSet<Blog> Blogs { get; set; }

    public virtual DbSet<Car> Cars { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Contact> Contacts { get; set; }

    public virtual DbSet<MaintenanceRecord> MaintenanceRecords { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Promotion> Promotions { get; set; }

    public virtual DbSet<Reservation> Reservations { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }
    public virtual DbSet<TenantCategory> TenantCategories { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Role> Roles { get; set; } = null!;

    public virtual DbSet<Location> Locations { get; set; }

    public virtual DbSet<Conversation> Conversations { get; set; }

    public virtual DbSet<ChatMessage> ChatMessages { get; set; }

    public virtual DbSet<ConversationReadState> ConversationReadStates { get; set; }

    public virtual DbSet<Tenant> Tenants { get; set; }
    public virtual DbSet<TenantMemberships> TenantMemberships { get; set; }

    public virtual DbSet<UserBranch> UserBranches {  get; set; }
    public virtual DbSet<UserConversationVisibility> UserConversationVisibilities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Global Query Filters theo TenantId
        modelBuilder.Entity<Car>().HasQueryFilter(x =>
            _tenant.IsEndUser || x.TenantId == _tenant.TenantId);

        modelBuilder.Entity<Reservation>().HasQueryFilter(x =>
            _tenant.IsEndUser || x.TenantId == _tenant.TenantId);

        modelBuilder.Entity<Payment>().HasQueryFilter(x =>
            _tenant.IsEndUser || x.TenantId == _tenant.TenantId);

        modelBuilder.Entity<Review>().HasQueryFilter(x =>
            _tenant.IsEndUser || x.TenantId == _tenant.TenantId);

        modelBuilder.Entity<Location>().HasQueryFilter(x =>
            _tenant.IsEndUser || x.TenantId == _tenant.TenantId);

        // Ràng buộc độ dài/unique (unique (TenantId, LicensePlate) đã tạo ở SQL)
        modelBuilder.Entity<Car>()
          .Property(x => x.TransmissionType).HasMaxLength(20);

        modelBuilder.Entity<Car>()
          .Property(x => x.VehicleType).HasMaxLength(20);


        modelBuilder.Entity<Blog>(entity =>
        {
            entity.HasKey(e => e.BlogId).HasName("PK__Blog__54379E30930C48EE");

            entity.ToTable("Blog");

            entity.Property(e => e.ImageUrl).HasMaxLength(255);
            entity.Property(e => e.PublishedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Draft");
            entity.Property(e => e.Title).HasMaxLength(255);

            entity.HasOne(d => d.Author).WithMany(p => p.Blogs)
                .HasForeignKey(d => d.AuthorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Blog__AuthorId__628FA481");
        });

        modelBuilder.Entity<Car>(entity =>
        {
            entity.HasKey(e => e.CarId).HasName("PK__Cars__68A0342EFCD139E9");

            entity.HasIndex(e => e.LicensePlate, "UQ__Cars__026BC15CB5A23E2A").IsUnique();

            entity.Property(e => e.Brand).HasMaxLength(50);
            entity.Property(e => e.CarName).HasMaxLength(100);
            entity.Property(e => e.EnergyType).HasMaxLength(20);
            entity.Property(e => e.EngineType).HasMaxLength(20);
            entity.Property(e => e.ImageUrl).HasMaxLength(255);
            entity.Property(e => e.LicensePlate).HasMaxLength(20);
            entity.Property(e => e.Model).HasMaxLength(50);
            entity.Property(e => e.RentalPricePerDay).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Available");
            entity.Property(e => e.TransmissionType).HasMaxLength(5);

            entity.HasOne(d => d.Category).WithMany(p => p.Cars)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Cars__CategoryId__412EB0B6");

            entity.HasOne(d => d.BaseLocation)
          .WithMany(p => p.Cars)
          .HasForeignKey(d => d.BaseLocationId)
          .OnDelete(DeleteBehavior.Restrict)
          .HasConstraintName("FK_Cars_Locations_BaseLocationId");

        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(x => x.CategoryName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.VehicleType).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(120);

            entity.HasOne(x => x.ParentCategory)
             .WithMany(x => x.SubCategories)
             .HasForeignKey(x => x.ParentCategoryId)
             .OnDelete(DeleteBehavior.Restrict);

            entity.HasKey(e => e.CategoryId).HasName("PK__Categori__19093A0BDF788EA5");

            entity.HasIndex(e => e.CategoryName, "UQ__Categori__8517B2E0F39EC167").IsUnique();

            entity.Property(e => e.CategoryName).HasMaxLength(50);
        });

        modelBuilder.Entity<Contact>(entity =>
        {
            entity.HasKey(e => e.ContactId).HasName("PK__Contact__5C66259B5FDEEF7E");

            entity.ToTable("Contact");

            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(15);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");
            entity.Property(e => e.SubmittedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<MaintenanceRecord>(entity =>
        {
            entity.HasKey(e => e.RecordId).HasName("PK__Maintena__FBDF78E9D1F84E3D");

            entity.Property(e => e.Cost).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.MaintenanceDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Car).WithMany(p => p.MaintenanceRecords)
                .HasForeignKey(d => d.CarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Maintenan__CarId__534D60F1");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payments__9B556A3856F28699");

            entity.Property(e => e.Amount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.PaymentDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PaymentMethod).HasMaxLength(20);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Unpaid");

            entity.HasOne(d => d.Reservation).WithMany(p => p.Payments)
                .HasForeignKey(d => d.ReservationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Payments__Reserv__4CA06362");
        });

        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.HasKey(e => e.PromotionId).HasName("PK__Promotio__52C42FCFDB38B80D");

            entity.HasIndex(e => e.Code, "UQ__Promotio__A25C5AA7537857ED").IsUnique();

            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.DiscountPercent).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.StartDate).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active");
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasKey(e => e.ReservationId).HasName("PK__Reservat__B7EE5F24B88A996B");

            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.ReservationDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.StartDate).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Car).WithMany(p => p.Reservations)
                .HasForeignKey(d => d.CarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Reservati__CarId__46E78A0C");

            entity.HasOne(d => d.User).WithMany(p => p.Reservations)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Reservati__UserI__45F365D3");

            entity.HasOne(d => d.PickupLocation)
          .WithMany(p => p.PickupReservations)
          .HasForeignKey(d => d.PickupLocationId)
          .OnDelete(DeleteBehavior.Restrict)
          .HasConstraintName("FK_Reservations_Locations_Pickup");

            entity.HasOne(d => d.DropoffLocation)
                  .WithMany(p => p.DropoffReservations)
                  .HasForeignKey(d => d.DropoffLocationId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .HasConstraintName("FK_Reservations_Locations_Dropoff");

        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.ReviewId).HasName("PK__Reviews__74BC79CE5D90B607");

            entity.Property(e => e.JobName).HasMaxLength(30);
            entity.Property(e => e.ReviewDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Car).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.CarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Reviews__CarId__571DF1D5");

            entity.HasOne(d => d.User).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Reviews__UserId__5812160E");
        });

        modelBuilder.Entity<TenantCategory>(entity =>
        {
            entity.HasKey(x => new { x.TenantId, x.CategoryId });
            entity.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId)
              .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId)
              .OnDelete(DeleteBehavior.Cascade);
        });


        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4C90EA71FB");

            entity.HasIndex(e => e.Username, "UQ__Users__536C85E48F8E5FC4").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Users__A9D10534479BCCE5").IsUnique();

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasMaxLength(15);
            entity.Property(e => e.Username).HasMaxLength(50);
            entity.Property(e => e.IsBlocked).HasDefaultValue(false);


            entity.HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
            .HasConstraintName("FK_Users_Roles_RoleId")
            .OnDelete(DeleteBehavior.SetNull); // Hoặc Restrict
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId);
            entity.Property(e => e.RoleName).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<Location>(entity =>
        {
            entity.HasKey(e => e.LocationId);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.TimeZone).HasMaxLength(50);
        });

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.ConversationId);
            entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("Open");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime2");
            entity.Property(e => e.LastMessageAt).HasColumnType("datetime2");

            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Conversations_Customer");

            entity.HasOne(e => e.Staff)
                .WithMany()
                .HasForeignKey(e => e.StaffId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Conversations_Staff");

            entity.HasOne(c => c.Tenant)
              .WithMany() 
              .HasForeignKey(c => c.TenantId) 
              .IsRequired(false) 
              .OnDelete(DeleteBehavior.SetNull)
              .HasConstraintName("FK_Conversations_Tenants");
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.ChatMessageId);
            entity.Property(e => e.Content).HasMaxLength(4000);
            entity.Property(e => e.SentAt).HasColumnType("datetime2");

            entity.HasOne(e => e.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(e => e.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Sender)
                .WithMany()
                .HasForeignKey(e => e.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ConversationReadState>().HasKey(x => new { x.ConversationId, x.UserId });

        modelBuilder.Entity<TenantMemberships>(e =>
        {
            e.HasKey(x => new { x.TenantId, x.UserId });

            e.HasOne(x => x.Tenant)
             .WithMany(t => t.Memberships)             
             .HasForeignKey(x => x.TenantId)
             .OnDelete(DeleteBehavior.Cascade);

            // FK -> Users (tường minh)
            e.HasOne(x => x.User)
             .WithMany(t => t.Memberships)               
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });


        modelBuilder.Entity<Car>()
          .HasOne(c => c.Tenant)
          .WithMany()
          .HasForeignKey(c => c.TenantId)
          .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Reservation>()
         .HasOne(r => r.Tenant)
         .WithMany()
         .HasForeignKey(r => r.TenantId)
         .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Payment>()
         .HasOne(p => p.Tenant)
         .WithMany()
         .HasForeignKey(p => p.TenantId)
         .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Location>()
         .HasOne(l => l.Tenant)
         .WithMany()
         .HasForeignKey(l => l.TenantId)
         .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Review>()
         .HasOne(rv => rv.Tenant)
         .WithMany()
         .HasForeignKey(rv => rv.TenantId)
         .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserBranch>(e =>
        {
            e.HasKey(x => new { x.UserId, x.TenantId, x.LocationId });
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Location).WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserConversationVisibility>()
            .HasKey(ucv => new { ucv.UserId, ucv.ConversationId });

        modelBuilder.Entity<UserConversationVisibility>()
            .HasOne(ucv => ucv.User)
            .WithMany()
            .HasForeignKey(ucv => ucv.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserConversationVisibility>()
            .HasOne(ucv => ucv.Conversation)
            .WithMany()
            .HasForeignKey(ucv => ucv.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);


        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
