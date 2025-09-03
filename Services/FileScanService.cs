using System.IO;
using System.Security.Cryptography;
using System.Text;
using Repeated_Files.Models;
using Repeated_Files.Services;

namespace Repeated_Files.Services
{
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
    }

    /// <summary>
    /// 文件扫描服务实现
    /// </summary>
    public class FileScanService : IFileScanService
    {
        private readonly IDatabaseService _databaseService;

        public FileScanService(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

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
        /// 计算文件哈希值
        /// </summary>
        public async Task<string> CalculateFileHashAsync(string filePath)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filePath);
            var hashBytes = await Task.Run(() => md5.ComputeHash(stream));
            return Convert.ToHexString(hashBytes);
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
