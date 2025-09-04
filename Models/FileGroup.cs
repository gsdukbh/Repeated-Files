using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Repeated_Files.Models
{
    /// <summary>
    /// 文件分组模型，用于按文件名分组显示
    /// </summary>
    public partial class FileGroup : ObservableObject
    {
        /// <summary>
        /// 文件名（分组标题）
        /// </summary>
        [ObservableProperty]
        private string _fileName = string.Empty;

        /// <summary>
        /// 分组下的文件数量
        /// </summary>
        [ObservableProperty]
        private int _fileCount;

        /// <summary>
        /// 是否展开分组
        /// </summary>
        [ObservableProperty]
        private bool _isExpanded = true;

        /// <summary>
        /// 分组下的文件列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<FileLocationItem> _files = new();

        /// <summary>
        /// 是否为重复文件组（文件数量大于1）
        /// </summary>
        public bool IsDuplicateGroup => FileCount > 1;
    }

    /// <summary>
    /// 文件位置项模型
    /// </summary>
    public partial class FileLocationItem : ObservableObject
    {
        /// <summary>
        /// 原始文件记录
        /// </summary>
        public FileRecord FileRecord { get; set; } = null!;

        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath => FileRecord.FilePath;

        /// <summary>
        /// 格式化的文件大小
        /// </summary>
        public string FileSizeFormatted => FileRecord.FileSizeFormatted;

        /// <summary>
        /// 最后修改时间
        /// </summary>
        public DateTime LastModified => FileRecord.ModifiedAt;

        /// <summary>
        /// 是否选中
        /// </summary>
        [ObservableProperty]
        private bool _isChecked;
    }
}
