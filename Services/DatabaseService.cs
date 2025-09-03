using Microsoft.EntityFrameworkCore;
using Repeated_Files.Data;
using Repeated_Files.Models;

namespace Repeated_Files.Services
{
    /// <summary>
    /// 数据库服务实现类
    /// </summary>
    public class DatabaseService : IDatabaseService
    {
        private readonly AppDbContext _context;

        public DatabaseService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 初始化数据库
        /// </summary>
        public async Task InitializeDatabaseAsync()
        {
            await _context.Database.EnsureCreatedAsync();
        }

        /// <summary>
        /// 添加文件记录
        /// </summary>
        public async Task<FileRecord> AddFileRecordAsync(FileRecord fileRecord)
        {
            _context.FileRecords.Add(fileRecord);
            await _context.SaveChangesAsync();
            return fileRecord;
        }

        /// <summary>
        /// 获取所有文件记录
        /// </summary>
        public async Task<List<FileRecord>> GetAllFileRecordsAsync()
        {
            
            return await _context.FileRecords
                .OrderBy(f => f.FileName)
                .ToListAsync();
        }

        /// <summary>
        /// 根据ID获取文件记录
        /// </summary>
        public async Task<FileRecord?> GetFileRecordByIdAsync(int id)
        {
            return await _context.FileRecords.FindAsync(id);
        }

        /// <summary>
        /// 根据哈希值获取文件记录
        /// </summary>
        public async Task<List<FileRecord>> GetFileRecordsByHashAsync(string hash)
        {
            return await _context.FileRecords
                .Where(f => f.FileHash == hash)
                .ToListAsync();
        }

        /// <summary>
        /// 获取重复文件记录
        /// </summary>
        public async Task<List<FileRecord>> GetDuplicateFilesAsync()
        {
            return await _context.FileRecords
                .Where(f => f.IsDuplicate)
                .OrderBy(f => f.FileHash)
                .ThenBy(f => f.FileName)
                .ToListAsync();
        }

        /// <summary>
        /// 更新文件记录
        /// </summary>
        public async Task<FileRecord> UpdateFileRecordAsync(FileRecord fileRecord)
        {
            _context.FileRecords.Update(fileRecord);
            await _context.SaveChangesAsync();
            return fileRecord;
        }

        /// <summary>
        /// 删除文件记录
        /// </summary>
        public async Task<bool> DeleteFileRecordAsync(int id)
        {
            var fileRecord = await _context.FileRecords.FindAsync(id);
            if (fileRecord == null)
                return false;

            _context.FileRecords.Remove(fileRecord);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// 批量删除文件记录
        /// </summary>
        public async Task<bool> DeleteFileRecordsAsync(List<int> ids)
        {
            var fileRecords = await _context.FileRecords
                .Where(f => ids.Contains(f.Id))
                .ToListAsync();

            if (!fileRecords.Any())
                return false;

            _context.FileRecords.RemoveRange(fileRecords);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// 检查文件是否存在
        /// </summary>
        public async Task<bool> FileExistsAsync(string filePath)
        {
            return await _context.FileRecords
                .AnyAsync(f => f.FilePath == filePath);
        }

        /// <summary>
        /// 获取文件统计信息
        /// </summary>
        public async Task<(int totalFiles, int duplicateFiles, long totalSize)> GetFileStatisticsAsync()
        {
            var totalFiles = await _context.FileRecords.CountAsync();
            var duplicateFiles = await _context.FileRecords.CountAsync(f => f.IsDuplicate);
            var totalSize = await _context.FileRecords.SumAsync(f => f.FileSize);

            return (totalFiles, duplicateFiles, totalSize);
        }

        /// <summary>
        /// 清空所有文件记录
        /// </summary>
        public async Task ClearAllFileRecordsAsync()
        {
            _context.FileRecords.RemoveRange(_context.FileRecords);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// 批量添加文件记录
        /// </summary>
        public async Task BatchAddFileRecordsAsync(List<FileRecord> fileRecords)
        {
            if (!fileRecords.Any()) return;

            _context.FileRecords.AddRange(fileRecords);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// 批量更新文件记录
        /// </summary>
        public async Task BatchUpdateFileRecordsAsync(List<FileRecord> fileRecords)
        {
            if (!fileRecords.Any()) return;

            _context.FileRecords.UpdateRange(fileRecords);
            await _context.SaveChangesAsync();
        }
    }
}
