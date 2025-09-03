using System.IO;
using System.Security.Cryptography;
using System.Text;
using Repeated_Files.Models;
using Repeated_Files.Services;
using System.Buffers; // 新增
using System.IO.Hashing; // 新增 XxHash

namespace Repeated_Files.Services
{
    /// <summary>
    /// 可选文件哈希算法
    /// </summary>
    public enum FileHashAlgorithm
    {
        MD5,
        XxHash64,
        XxHash128
    }

    /// <summary>
    /// 文件扫描服务
    /// </summary>
    public interface IFileScanService
    {
        /// <summary>
        /// 扫描目录中的文件
        /// </summary>
        Task ScanDirectoryAsync(string directoryPath, IProgress<string>? progress = null);

        /// <summary>
        /// 计算文件哈希值
        /// </summary>
        Task<string> CalculateFileHashAsync(string filePath);

        /// <summary>
        /// 查找重复文件
        /// </summary>
        Task FindDuplicateFilesAsync();

        /// <summary>
        /// 设置文件哈希算法
        /// </summary>
        void SetHashAlgorithm(FileHashAlgorithm algorithm); // 新增
    }

    /// <summary>
    /// 文件扫描服务实现
    /// </summary>
    public class FileScanService : IFileScanService
    {
        private readonly IDatabaseService _databaseService;
        private FileHashAlgorithm _hashAlgorithm = FileHashAlgorithm.XxHash128; 

        public FileScanService(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public void SetHashAlgorithm(FileHashAlgorithm algorithm) => _hashAlgorithm = algorithm;

        /// <summary>
        /// 扫描目录中的文件
        /// </summary>
        public async Task ScanDirectoryAsync(string directoryPath, IProgress<string>? progress = null)
        {
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"目录不存在: {directoryPath}");

            progress?.Report("正在获取文件列表...");
            var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
            
            progress?.Report($"发现 {files.Length} 个文件，开始处理...");

            var allFileRecords = new List<FileRecord>();
            int processedCount = 0;

            // 一次性处理所有文件，收集所有文件信息
            foreach (var filePath in files)
            {
                try
                {
                    processedCount++;
                    progress?.Report($"正在处理: {Path.GetFileName(filePath)} ({processedCount}/{files.Length})");
                    var fileInfo = new FileInfo(filePath);
                    
                    // 跳过无法访问的文件
                    if (!fileInfo.Exists)
                        continue;
                    
                    // 跳过已经存在的文件记录
                    if ( await _databaseService.FileExistsAsync(filePath))
                    {
                        continue;
                    }
           
                    var hash = await CalculateFileHashAsync(filePath);
                    
                    var fileRecord = new FileRecord
                    {
                        FileName = fileInfo.Name,
                        FilePath = filePath,
                        FileSize = fileInfo.Length,
                        FileHash = hash,
                        Extension = fileInfo.Extension,
                        CreatedAt = DateTime.Now,
                        ModifiedAt = fileInfo.LastWriteTime
                    };

                    allFileRecords.Add(fileRecord);
                    if (allFileRecords.Count > 50)
                    {
                        await _databaseService.BatchAddFileRecordsAsync(allFileRecords);
                        allFileRecords.Clear();
                    }
                }
                catch (Exception ex)
                {
                    progress?.Report($"处理文件失败 {Path.GetFileName(filePath)}: {ex.Message}");
                }
            }
            
            // 批量保存所有文件记录
            if (allFileRecords.Count > 0)
            {
                await _databaseService.BatchAddFileRecordsAsync(allFileRecords);
            }

            progress?.Report("正在查找重复文件...");
            // 扫描完成后查找重复文件
            await FindDuplicateFilesAsync();
            progress?.Report("重复文件查找完成");
        }

        /// <summary>
        /// 计算文件哈希值（支持 MD5, XxHash64, XxHash128），采用异步顺序读取 + 大缓冲 + ArrayPool 优化大文件性能。
        /// </summary>
        public async Task<string> CalculateFileHashAsync(string filePath)
        {
            const int bufferSize = 1024 * 1024; // 1MB，可按需调大 2~4MB 做 A/B 测试
            var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            try
            {
                // 顺序异步读取优化
                await using var fs = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);

                switch (_hashAlgorithm)
                {
                    case FileHashAlgorithm.MD5:
                        using (var md5 = MD5.Create())
                        {
                            int read;
                            while ((read = await fs.ReadAsync(buffer.AsMemory(0, bufferSize))) > 0)
                            {
                                md5.TransformBlock(buffer, 0, read, null, 0);
                            }
                            md5.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                            return Convert.ToHexString(md5.Hash!);
                        }
                    case FileHashAlgorithm.XxHash64:
                        {
                            var hasher = new XxHash64();
                            int read;
                            while ((read = await fs.ReadAsync(buffer.AsMemory(0, bufferSize))) > 0)
                            {
                                hasher.Append(buffer.AsSpan(0, read));
                            }
                            var hash = hasher.GetCurrentHash();
                            return Convert.ToHexString(hash);
                        }
                    case FileHashAlgorithm.XxHash128:
                        {
                            var hasher = new XxHash128();
                            int read;
                            while ((read = await fs.ReadAsync(buffer.AsMemory(0, bufferSize))) > 0)
                            {
                                hasher.Append(buffer.AsSpan(0, read));
                            }
                            var hash = hasher.GetCurrentHash();
                            return Convert.ToHexString(hash);
                        }
                    default:
                        throw new NotSupportedException($"不支持的哈希算法: {_hashAlgorithm}");
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// 查找重复文件
        /// </summary>
        public async Task FindDuplicateFilesAsync()
        {
            var allFiles = await _databaseService.GetAllFileRecordsAsync();
            var duplicateGroups = allFiles
                .Where(f => !string.IsNullOrEmpty(f.FileHash))
                .GroupBy(f => f.FileHash)
                .Where(g => g.Count() > 1);

            var filesToUpdate = new List<FileRecord>();

            foreach (var group in duplicateGroups)
            {
                foreach (var file in group)
                {
                    file.IsDuplicate = true;
                    filesToUpdate.Add(file);
                }
            }

            // 批量更新重复文件标记
            if (filesToUpdate.Count > 0)
            {
                await _databaseService.BatchUpdateFileRecordsAsync(filesToUpdate);
            }
        }
    }
}
