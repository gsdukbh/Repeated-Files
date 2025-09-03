using Microsoft.EntityFrameworkCore;
using Repeated_Files.Models;

namespace Repeated_Files.Data
{
    /// <summary>
    /// 应用程序数据库上下文
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// 文件记录数据集
        /// </summary>
        public DbSet<FileRecord> FileRecords { get; set; }

        /// <summary>
        /// 配置模型
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 配置FileRecord实体
            modelBuilder.Entity<FileRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
                entity.Property(e => e.FileHash).HasMaxLength(64);
                entity.Property(e => e.Extension).HasMaxLength(10);
                entity.HasIndex(e => e.FileHash).HasDatabaseName("IX_FileRecord_FileHash");
                entity.HasIndex(e => e.FilePath).HasDatabaseName("IX_FileRecord_FilePath");
            });
        }
    }
}
