using Repeated_Files.Models;

namespace Repeated_Files.Services
{
    /// <summary>
    /// 数据库服务接口
    /// </summary>
    public interface IDatabaseService
    {
        /// <summary>
        /// 初始化数据库
        /// </summary>
        Task InitializeDatabaseAsync();

        /// <summary>
        /// 添加文件记录
        /// </summary>
        Task<FileRecord> AddFileRecordAsync(FileRecord fileRecord);

        /// <summary>
        /// 获取所有文件记录
        /// </summary>
        Task<List<FileRecord>> GetAllFileRecordsAsync();

        /// <summary>
        /// 根据ID获取文件记录
        /// </summary>
        Task<FileRecord?> GetFileRecordByIdAsync(int id);

        /// <summary>
        /// 根据哈希值获取文件记录
        /// </summary>
        Task<List<FileRecord>> GetFileRecordsByHashAsync(string hash);

        /// <summary>
        /// 获取重复文件记录
        /// </summary>
        Task<List<FileRecord>> GetDuplicateFilesAsync();

        /// <summary>
        /// 更新文件记录
        /// </summary>
        Task<FileRecord> UpdateFileRecordAsync(FileRecord fileRecord);

        /// <summary>
        /// 删除文件记录
        /// </summary>
        Task<bool> DeleteFileRecordAsync(int id);

        /// <summary>
        /// 批量删除文件记录
        /// </summary>
        Task<bool> DeleteFileRecordsAsync(List<int> ids);

        /// <summary>
        /// 检查文件是否存在
        /// </summary>
        Task<bool> FileExistsAsync(string filePath);

        /// <summary>
        /// 获取文件统计信息
        /// </summary>
        Task<(int totalFiles, int duplicateFiles, long totalSize)> GetFileStatisticsAsync();

        /// <summary>
        /// 清空所有文件记录
        /// </summary>
        Task ClearAllFileRecordsAsync();

        /// <summary>
        /// 批量添加文件记录
        /// </summary>
        Task BatchAddFileRecordsAsync(List<FileRecord> fileRecords);

        /// <summary>
        /// 批量更新文件记录
        /// </summary>
        Task BatchUpdateFileRecordsAsync(List<FileRecord> fileRecords);
    }
}
