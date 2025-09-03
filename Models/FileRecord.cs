using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Repeated_Files.Models
{
    /// <summary>
    /// 文件记录实体类
    /// </summary>
    public partial class FileRecord : ObservableObject
    {
        /// <summary>
        /// 主键标识
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 文件名
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 文件路径
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// 文件哈希值
        /// </summary>
        [MaxLength(64)]
        public string? FileHash { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 最后修改时间
        /// </summary>
        public DateTime ModifiedAt { get; set; }

        /// <summary>
        /// 是否为重复文件
        /// </summary>
        public bool IsDuplicate { get; set; }

        /// <summary>
        /// 文件扩展名
        /// </summary>
        [MaxLength(10)]
        public string? Extension { get; set; }

        /// <summary>
        /// 是否被选中（用于UI绑定）
        /// </summary>
        [ObservableProperty]
        private bool _isChecked;

        /// <summary>
        /// 重复次数（用于UI显示）
        /// </summary>
        public int DuplicateCount { get; set; }

        /// <summary>
        /// 最后修改时间（格式化显示）
        /// </summary>
        public string LastModified => ModifiedAt.ToString("yyyy-MM-dd HH:mm:ss");

        /// <summary>
        /// 文件大小（格式化显示）
        /// </summary>
        public string FileSizeFormatted => FormatFileSize(FileSize);

        private static string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return string.Format("{0:n1} {1}", number, suffixes[counter]);
        }
    }
}
