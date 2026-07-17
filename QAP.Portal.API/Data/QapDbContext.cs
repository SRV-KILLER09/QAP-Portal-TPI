using Microsoft.EntityFrameworkCore;
using QAP.Portal.API.Models;

namespace QAP.Portal.API.Data
{
    public class QapDbContext : DbContext
    {
        public QapDbContext(DbContextOptions<QapDbContext> options)
            : base(options)
        {
        }

        public DbSet<QapLineGroup> QapLineGroups { get; set; } = null!;

        public DbSet<QapGroupItem> QapGroupItems { get; set; } = null!;

        public DbSet<PoDocument> PoDocuments { get; set; } = null!;

        public DbSet<GroupActionLog> GroupActionLogs { get; set; } = null!;

        public DbSet<SapPoMaster> SapPoMasters { get; set; } = null!;

        public DbSet<MbaPoDetails> MbaPoDetails { get; set; } = null!;


        // Existing SQL table: ADMIN_USERS
        public DbSet<AdminUser> AdminUsers { get; set; } = null!;

        public DbSet<QapUser> QapUsers { get; set; } = null!;


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<QapGroupItem>()
                .HasKey(x => new
                {
                    x.Po,
                    x.Line,
                    x.ItemNo
                });


            modelBuilder.Entity<MbaPoDetails>()
                .HasKey(x => new
                {
                    x.PurchaseOrder,
                    x.Item,
                    x.Line
                });


            // ADMIN_USERS table mapping
            modelBuilder.Entity<AdminUser>(entity =>
            {
                entity.ToTable("ADMIN_USERS");

                entity.HasKey(x => x.ADMIN_ID);

                entity.Property(x => x.ADMIN_ID)
                    .HasColumnName("ADMIN_ID");

                entity.Property(x => x.ADMIN_NAME)
                    .HasColumnName("ADMIN_NAME");

                entity.Property(x => x.EMAIL)
                    .HasColumnName("EMAIL");

                entity.Property(x => x.PASSWORD_HASH)
                    .HasColumnName("PASSWORD_HASH");

                entity.Property(x => x.STATUS)
                    .HasColumnName("STATUS");

                entity.Property(x => x.CREATED_ON)
                    .HasColumnName("CREATED_ON");
            });

            // QAP_USERS table mapping
            modelBuilder.Entity<QapUser>(entity =>
            {
                entity.ToTable("QAP_USERS");
                entity.HasKey(x => x.Email);
                entity.Property(x => x.Email).HasColumnName("EMAIL");
                entity.Property(x => x.DisplayName).HasColumnName("DISPLAY_NAME");
                entity.Property(x => x.Role).HasColumnName("ROLE");
                entity.Property(x => x.PasswordHash).HasColumnName("PASSWORD_HASH");
                entity.Property(x => x.IsActive).HasColumnName("IS_ACTIVE");
                entity.Property(x => x.CreatedOn).HasColumnName("CREATED_ON");
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}