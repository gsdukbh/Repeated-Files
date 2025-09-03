using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Repeated_Files.Models;
using Repeated_Files.Services;
using System.Windows;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using MessageBox = System.Windows.MessageBox;

namespace Repeated_Files.ViewModels.Pages
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly IDatabaseService _databaseService;
        private readonly IFileScanService _fileScanService;

        [ObservableProperty] private string _pathToScan = string.Empty;
        [ObservableProperty]
        private ObservableCollection<FileRecord> _fileRecords = new();
        
        [ObservableProperty] private bool _isScanning;
        [ObservableProperty] private string _scanProgress = string.Empty;
        [ObservableProperty] private bool _isAllSelected;
        
        // 分页相关属性
        [ObservableProperty] private int _currentPage = 1;
        [ObservableProperty] private int _pageSize = 20;
        [ObservableProperty] private int _totalPages = 1;
        [ObservableProperty] private int _totalItems = 0;
        [ObservableProperty] private int _jumpToPageNumber = 1;
        [ObservableProperty] private string _pageInfo = "第 1 页，共 1 页";
        
        // 完整的文件记录集合（用于分页）
        private List<FileRecord> _allFileRecords = new();
        
        // 分页相关计算属性
        public bool CanGoToPreviousPage => CurrentPage > 1;
        public bool CanGoToNextPage => CurrentPage < TotalPages;

        // 添加选中的文件记录属性
        [ObservableProperty] private FileRecord? _selectedFileRecord;

        public DashboardViewModel(IDatabaseService databaseService, IFileScanService fileScanService)
        {
            _databaseService = databaseService;
            _fileScanService = fileScanService;
            LoadFileRecords();
        }

        /// <summary>
        /// 加载文件记录
        /// </summary>
        private async void LoadFileRecords()
        {
            try
            {
                var records = await _databaseService.GetDuplicateFilesAsync();
                _allFileRecords = records.ToList();
                UpdateDuplicateCounts();
                UpdatePagingInfo();
                LoadCurrentPageData();
            }
            catch (Exception ex)
            {
                ScanProgress = $"加载文件记录失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 更新重复文件计数
        /// </summary>
        private void UpdateDuplicateCounts()
        {
            var duplicateGroups = _allFileRecords
                .Where(f => !string.IsNullOrEmpty(f.FileHash))
                .GroupBy(f => f.FileHash)
                .Where(g => g.Count() > 1);
            
            foreach (var group in duplicateGroups)
            {
                foreach (var file in group)
                {
                    file.DuplicateCount = group.Count();
                }
            }
        }

        /// <summary>
        /// 更新分页信息
        /// </summary>
        private void UpdatePagingInfo()
        {
            TotalItems = _allFileRecords.Count;
            TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalItems / PageSize));
            
            // 确保当前页在有效范围内
            if (CurrentPage > TotalPages) CurrentPage = TotalPages;
            if (CurrentPage < 1) CurrentPage = 1;
            
            JumpToPageNumber = CurrentPage;
            PageInfo = $"第 {CurrentPage} 页，共 {TotalPages} 页";
            
            // 通知分页按钮状态变化
            OnPropertyChanged(nameof(CanGoToPreviousPage));
            OnPropertyChanged(nameof(CanGoToNextPage));
        }

        /// <summary>
        /// 加载当前页数据
        /// </summary>
        private void LoadCurrentPageData()
        {
            var skip = (CurrentPage - 1) * PageSize;
            var currentPageItems = _allFileRecords.Skip(skip).Take(PageSize).ToList();
            
            FileRecords.Clear();
            foreach (var item in currentPageItems)
            {
                FileRecords.Add(item);
            }
        }

        /// <summary>
        /// 扫描目录路径
        /// </summary>
        [RelayCommand]
        private async Task SelectFolder()
        {
            var dialog = new FolderBrowserDialog();
            var result = dialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                PathToScan = dialog.SelectedPath;
            }
        }

        /// <summary>
        /// 扫描重复文件
        /// </summary>
        [RelayCommand]
        private async Task ScanDiffFiles()
        {
            if (string.IsNullOrEmpty(PathToScan))
            {
                ScanProgress = "请先选择要扫描的目录";
                return;
            }

            if (!System.IO.Directory.Exists(PathToScan))
            {
                ScanProgress = "选择的目录不存在";
                return;
            }

            IsScanning = true;
            ScanProgress = "开始扫描...";

            try
            {
                var progress = new Progress<string>(message => ScanProgress = message);
                await _fileScanService.ScanDirectoryAsync(PathToScan, progress);
                
                ScanProgress = "扫描完成，正在刷新数据...";
                LoadFileRecords();
                ScanProgress = "扫描完成";
            }
            catch (Exception ex)
            {
                ScanProgress = $"扫描失败: {ex.Message}";
            }
            finally
            {
                IsScanning = false;
            }
        }

        /// <summary>
        /// 全选/取消全选
        /// </summary>
        partial void OnIsAllSelectedChanged(bool value)
        {
            foreach (var record in FileRecords)
            {
                record.IsChecked = value;
            }
        }

        /// <summary>
        /// 删除选中的文件
        /// </summary>
        [RelayCommand]
        private async Task DeleteSelectedFiles()
        {
            var selectedFiles = FileRecords.Where(f => f.IsChecked).ToList();
            if (selectedFiles.Count == 0)
            {
                ScanProgress = "请选择要删除的文件";
                return;
            }

            var result = MessageBox.Show(
                $"确定要删除选中的 {selectedFiles.Count} 个文件吗？此操作不可撤销！",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                int deletedCount = 0;
                foreach (var file in selectedFiles)
                {
                    try
                    {
                        if (System.IO.File.Exists(file.FilePath))
                        {
                            System.IO.File.Delete(file.FilePath);
                            await _databaseService.DeleteFileRecordAsync(file.Id);
                            _allFileRecords.Remove(file);
                            deletedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        ScanProgress = $"删除文件失败 {file.FileName}: {ex.Message}";
                    }
                }
                
                ScanProgress = $"成功删除 {deletedCount} 个文件";
                UpdateDuplicateCounts();
                UpdatePagingInfo();
                LoadCurrentPageData();
            }
        }

        /// <summary>
        /// 清空所有记录
        /// </summary>
        [RelayCommand]
        private async Task ClearAllRecords()
        {
            var result = MessageBox.Show(
                "确定要清空所有文件记录吗？",
                "确认清空",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _databaseService.ClearAllFileRecordsAsync();
                    _allFileRecords.Clear();
                    FileRecords.Clear();
                    UpdatePagingInfo();
                    ScanProgress = "已清空所有记录";
                }
                catch (Exception ex)
                {
                    ScanProgress = $"清空记录失败: {ex.Message}";
                }
            }
        }

        /// <summary>
        /// 上一页
        /// </summary>
        [RelayCommand]
        private void PreviousPage()
        {
            if (CanGoToPreviousPage)
            {
                CurrentPage--;
                UpdatePagingInfo();
                LoadCurrentPageData();
            }
        }

        /// <summary>
        /// 下一页
        /// </summary>
        [RelayCommand]
        private void NextPage()
        {
            if (CanGoToNextPage)
            {
                CurrentPage++;
                UpdatePagingInfo();
                LoadCurrentPageData();
            }
        }

        /// <summary>
        /// 第一页
        /// </summary>
        [RelayCommand]
        private void FirstPage()
        {
            if (CurrentPage != 1)
            {
                CurrentPage = 1;
                UpdatePagingInfo();
                LoadCurrentPageData();
            }
        }

        /// <summary>
        /// 最后一页
        /// </summary>
        [RelayCommand]
        private void LastPage()
        {
            if (CurrentPage != TotalPages)
            {
                CurrentPage = TotalPages;
                UpdatePagingInfo();
                LoadCurrentPageData();
            }
        }

        /// <summary>
        /// 跳转到指定页码
        /// </summary>
        [RelayCommand]
        private void JumpToPage()
        {
            if (JumpToPageNumber >= 1 && JumpToPageNumber <= TotalPages && JumpToPageNumber != CurrentPage)
            {
                CurrentPage = JumpToPageNumber;
                UpdatePagingInfo();
                LoadCurrentPageData();
            }
        }

        /// <summary>
        /// 每页条目数变化时更新分页信息
        /// </summary>
        partial void OnPageSizeChanged(int value)
        {
            if (value > 0)
            {
                CurrentPage = 1; // 重置到第一页
                UpdatePagingInfo();
                LoadCurrentPageData();
            }
        }

        /// <summary>
        /// 打开文件所在目录
        /// </summary>
        [RelayCommand]
        private void RightSelect()
        {
            if (SelectedFileRecord?.FilePath == null)
            {
                ScanProgress = "请先选择一个文件";
                return;
            }

            try
            {
                if (File.Exists(SelectedFileRecord.FilePath))
                {
                    // 在资源管理器中打开并选中文件
                    Process.Start("explorer.exe", $"/select,\"{SelectedFileRecord.FilePath}\"");
                }
                else
                {
                    ScanProgress = $"文件不存在: {SelectedFileRecord.FilePath}";
                }
            }
            catch (Exception ex)
            {
                ScanProgress = $"打开文件目录失败: {ex.Message}";
            }
        }
    }
}