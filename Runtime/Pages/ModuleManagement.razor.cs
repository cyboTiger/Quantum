using Microsoft.AspNetCore.Components.Forms;
using Quantum.Runtime.Models;
using Quantum.Sdk;
using Quantum.Sdk.Utilities;
using System.Globalization;
using System.IO.Compression;

namespace Quantum.Runtime.Pages;

public partial class ModuleManagement(ILogger<ModuleManagement> logger)
{
    // 本地插件管理
    private const string UninstallMarkFile = "TobeUninstalled.Quantum.MarkTag";

    // API基础URL - 从SettingService获取
    private string ApiBaseUrl => ExtensionMarketService.Data.ExtensionMarketUrl;

    // 插件市场数据
    private List<ExtensionDto> _extensions = [];
    private List<ExtensionVersionDto> _extensionVersions = [];
    private ExtensionDto? _selectedExtension;
    private string _searchTerm = string.Empty;
    private bool _isLoading;
    private bool _isLoadingVersions;

    // 模态框状态
    private bool _isCreateExtensionModalVisible;
    private bool _isExtensionDetailsModalVisible;
    private bool _isEditExtensionModalVisible;
    private bool _isAddVersionModalVisible;
    private bool _isExtensionMarketSettingsModalVisible;

    // 表单模型
    private CreateExtensionDto _createExtensionModel = new();
    private UpdateExtensionDto _updateExtensionModel = new();
    private CreateExtensionVersionDto _createVersionModel = new();
    private string _tagsInput = "";
    private string _editTagsInput = "";
    private IBrowserFile? _selectedFile;
    private readonly ExtensionMarketSettingsModel _extensionMarketSettings = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadExtensions();
        // 初始化插件市场设置
        _extensionMarketSettings.Url = ExtensionMarketService.Data.ExtensionMarketUrl;
    }

    #region 本地插件管理

    private async Task InstallModuleFromFile(InputFileChangeEventArgs e)
    {
        try
        {
            var file = e.File;
            if (!Path.GetExtension(file.Name).Equals(".zip", StringComparison.CurrentCultureIgnoreCase))
            {
                _ = MessageService.Error("只能上传ZIP文件");
                return;
            }

            // 创建临时文件保存上传内容
            var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");

            try
            {
                // 保存上传的文件
                await using var fileStream = file.OpenReadStream(maxAllowedSize: 1024 * 1024 * 1024); // 1GB max
                await using var fs = new FileStream(tempFile, FileMode.Create);
                await fileStream.CopyToAsync(fs);

                // 安装模块
                var success = InstallModuleFromZipFile(tempFile);
                if (success)
                {
                    _ = MessageService.Success("插件安装成功，请重启应用以加载新安装的插件");
                }
            }
            finally
            {
                // 清理临时文件
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }
        catch (Exception ex)
        {
            _ = MessageService.Error($"安装失败:{ex.Message}");
        }
    }

    private async Task UninstallModule(IModule module)
    {
        try
        {
            var modulePath = Path.GetDirectoryName(module.GetType().Assembly.Location) ?? throw new InvalidOperationException($"无法获取 {module.ModuleId} 所在的目录");

            await File.WriteAllTextAsync(Path.Combine(modulePath, UninstallMarkFile), DateTime.Now.ToString(CultureInfo.InvariantCulture));
            _ = MessageService.Success("模块将在下次启动时被移除");
        }
        catch (Exception ex)
        {
            _ = MessageService.Error($"卸载失败: {ex.Message}");
        }
    }

    private bool InstallModuleFromZipFile(string zipFilePath)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempPath);

        try
        {
            // 解压到临时目录
            ZipFile.ExtractToDirectory(zipFilePath, tempPath, true);

            // 检查是否只有一个顶层文件夹
            var topLevelItems = Directory.GetFileSystemEntries(tempPath);
            if (topLevelItems.Length != 1 || !Directory.Exists(topLevelItems[0]))
            {
                _ = MessageService.Error("无效的插件包：压缩包必须只包含一个顶层文件夹");
                return false;
            }

            var moduleFolder = topLevelItems[0];
            var moduleName = Path.GetFileName(moduleFolder);
            var pendingPath = Path.Combine(AppContext.BaseDirectory, "PendingModule", moduleName);

            // 确保PendingModule目录存在
            Directory.CreateDirectory(Path.GetDirectoryName(pendingPath)
                                      ?? throw new InvalidOperationException($"无法获取目录名称: {pendingPath}"));

            if (Directory.Exists(pendingPath))
                Directory.Delete(pendingPath, true);

            // 使用复制代替移动，因为可能跨卷
            CopyDirectory(moduleFolder, pendingPath, true);
            Directory.Delete(moduleFolder, true);

            return true;
        }
        finally
        {
            // 清理临时目录
            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, true);
        }
    }

    private static void CopyDirectory(string sourceDir, string targetDir, bool overwrite)
    {
        Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, file);
            var targetPath = Path.Combine(targetDir, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)
                                      ?? throw new InvalidOperationException($"无法获取目录名称: {targetPath}"));
            File.Copy(file, targetPath, overwrite);
        }
    }

    #endregion

    #region 插件市场 - 基础功能

    // 加载插件列表
    private async Task LoadExtensions()
    {
        try
        {
            _isLoading = true;
            var client = RequestClient.Create();
            var response = await client.GetAsync($"{ApiBaseUrl}/Extensions");
            if (response.IsSuccessStatusCode)
            {
                _extensions = await response.Content.ReadFromJsonAsync<List<ExtensionDto>>() ?? [];
            }
            else
            {
                _ = MessageService.Error("获取插件列表失败");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            _ = MessageService.Error($"获取插件列表出错: {ex.Message}");
        }
        finally
        {
            _isLoading = false;
        }
    }

    // 搜索插件
    private async Task SearchExtensions()
    {
        try
        {
            _isLoading = true;
            var url = string.IsNullOrWhiteSpace(_searchTerm)
                ? $"{ApiBaseUrl}/Extensions"
                : $"{ApiBaseUrl}/Extensions/search?term={Uri.EscapeDataString(_searchTerm)}";

            var client = RequestClient.Create();
            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                _extensions = await response.Content.ReadFromJsonAsync<List<ExtensionDto>>() ?? new List<ExtensionDto>();
            }
            else
            {
                _ = MessageService.Error("搜索插件失败");
            }
        }
        catch (Exception ex)
        {
            _ = MessageService.Error($"搜索插件出错: {ex.Message}");
        }
        finally
        {
            _isLoading = false;
        }
    }

    // 查看插件详情
    private async Task ViewExtensionDetails(ExtensionDto extension)
    {
        _selectedExtension = extension;
        _isExtensionDetailsModalVisible = true;
        await LoadExtensionVersions(extension.Id);
    }

    // 加载插件版本
    private async Task LoadExtensionVersions(Guid extensionId)
    {
        try
        {
            _isLoadingVersions = true;

            var client = RequestClient.Create();
            var response = await client.GetAsync($"{ApiBaseUrl}/Extensions/{extensionId}/versions");
            if (response.IsSuccessStatusCode)
            {
                _extensionVersions = await response.Content.ReadFromJsonAsync<List<ExtensionVersionDto>>() ?? new List<ExtensionVersionDto>();
            }
            else
            {
                _ = MessageService.Error("获取版本列表失败");
            }
        }
        catch (Exception ex)
        {
            _ = MessageService.Error($"获取版本列表出错: {ex.Message}");
        }
        finally
        {
            _isLoadingVersions = false;
        }
    }

    // 下载并安装插件
    private async Task DownloadAndInstallExtension(Guid extensionId, string versionNumber)
    {
        try
        {
            // 下载插件
            var client = RequestClient.Create();
            var response = await client.GetAsync($"{ApiBaseUrl}/Extensions/{extensionId}/versions/{versionNumber}/download");
            if (!response.IsSuccessStatusCode)
            {
                await MessageService.Error("下载插件失败");
                return;
            }

            // 创建临时文件保存下载内容
            var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");
            await using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await response.Content.CopyToAsync(fileStream);
            }

            try
            {
                if (InstallModuleFromZipFile(tempFile))
                {
                    await MessageService.Success("插件安装成功，请重启应用以加载新安装的插件");
                }
            }
            finally
            {
                // 清理临时文件
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }
        catch (Exception ex)
        {
            await MessageService.Error($"安装失败: {ex.Message}");
        }
    }

    // 关闭插件详情对话框
    private void CloseExtensionDetailsModal()
    {
        _isExtensionDetailsModalVisible = false;
        _selectedExtension = null;
        _extensionVersions.Clear();
    }

    #endregion

    #region 插件市场 - 设置

    // 显示插件市场设置对话框
    private void ShowExtensionMarketSettingsModal()
    {
        _extensionMarketSettings.Url = ExtensionMarketService.Data.ExtensionMarketUrl;
        _isExtensionMarketSettingsModalVisible = true;
    }

    // 关闭插件市场设置对话框
    private void CloseExtensionMarketSettingsModal()
    {
        _isExtensionMarketSettingsModalVisible = false;
    }

    // 保存插件市场设置
    private async Task SaveExtensionMarketSettings()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_extensionMarketSettings.Url))
            {
                await MessageService.Error("插件市场地址不能为空");
                return;
            }

            // 保存设置
            ExtensionMarketService.Data.ExtensionMarketUrl = _extensionMarketSettings.Url;
            await ExtensionMarketService.SaveUserDataAsync();

            _isExtensionMarketSettingsModalVisible = false;
            await MessageService.Success("插件市场设置已保存");

            // 重新加载插件列表
            await LoadExtensions();
        }
        catch (Exception ex)
        {
            await MessageService.Error($"保存设置失败: {ex.Message}");
        }
    }

    #endregion

    #region 插件管理 - 开发者功能

    // 显示创建插件对话框
    private void ShowCreateExtensionModal()
    {
        _createExtensionModel = new CreateExtensionDto();
        _tagsInput = "";
        _isCreateExtensionModalVisible = true;
    }

    // 关闭创建插件对话框
    private void CloseCreateExtensionModal()
    {
        _isCreateExtensionModalVisible = false;
    }

    // 创建插件
    private async Task CreateExtension()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_createExtensionModel.Name) || string.IsNullOrWhiteSpace(_createExtensionModel.Description))
            {
                await MessageService.Error("插件名称和描述不能为空");
                return;
            }

            // 处理标签
            if (!string.IsNullOrWhiteSpace(_tagsInput))
            {
                _createExtensionModel.Tags = _tagsInput.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .ToList();
            }

            // 获取认证客户端
            var (isSuccess, client, message) = await ExtensionMarketService.GetAuthenticatedClientAsync();
            if (!isSuccess)
            {
                await MessageService.Error(message);
                return;
            }

            var response = await client!.PostAsJsonAsync($"{ApiBaseUrl}/Extensions", _createExtensionModel);
            if (response.IsSuccessStatusCode)
            {
                _isCreateExtensionModalVisible = false;
                await LoadExtensions();
                await MessageService.Success("插件创建成功");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                await MessageService.Error($"创建失败: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            await MessageService.Error($"创建出错: {ex.Message}");
        }
    }

    // 显示我的插件
    private async Task ShowMyExtensions()
    {
        try
        {
            _isLoading = true;

            // 获取认证客户端
            var (isSuccess, client, message) = await ExtensionMarketService.GetAuthenticatedClientAsync();
            if (!isSuccess)
            {
                await MessageService.Error(message);
                _isLoading = false;
                return;
            }

            var response = await client!.GetAsync($"{ApiBaseUrl}/Extensions/author/{ExtensionMarketService.Data.CurrentUser!.Id}");
            if (response.IsSuccessStatusCode)
            {
                _extensions = await response.Content.ReadFromJsonAsync<List<ExtensionDto>>() ?? [];
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                await MessageService.Error($"获取我的插件失败: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            await MessageService.Error($"获取我的插件出错: {ex.Message}");
        }
        finally
        {
            _isLoading = false;
        }
    }

    // 显示编辑插件对话框
    private void ShowEditExtensionModal(ExtensionDto extension)
    {
        _updateExtensionModel = new UpdateExtensionDto
        {
            Name = extension.Name,
            Description = extension.Description,
        };

        _editTagsInput = string.Join(",", extension.Tags);
        _selectedExtension = extension;
        _isEditExtensionModalVisible = true;
    }

    // 关闭编辑插件对话框
    private void CloseEditExtensionModal()
    {
        _isEditExtensionModalVisible = false;
    }

    // 更新插件信息
    private async Task UpdateExtension()
    {
        try
        {
            if (_selectedExtension == null)
                return;

            if (string.IsNullOrWhiteSpace(_updateExtensionModel.Name) || string.IsNullOrWhiteSpace(_updateExtensionModel.Description))
            {
                await MessageService.Error("插件名称和描述不能为空");
                return;
            }

            // 处理标签
            if (!string.IsNullOrWhiteSpace(_editTagsInput))
            {
                _updateExtensionModel.Tags = _editTagsInput.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .ToList();
            }

            // 获取认证客户端
            var (isSuccess, client, message) = await ExtensionMarketService.GetAuthenticatedClientAsync();
            if (!isSuccess)
            {
                await MessageService.Error(message);
                return;
            }

            var response = await client!.PutAsJsonAsync($"{ApiBaseUrl}/Extensions/{_selectedExtension.Id}", _updateExtensionModel);
            if (response.IsSuccessStatusCode)
            {
                _isEditExtensionModalVisible = false;
                await LoadExtensions();
                await MessageService.Success("插件更新成功");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _ = MessageService.Error($"更新失败: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            _ = MessageService.Error($"更新出错: {ex.Message}");
        }
    }

    // 显示添加版本对话框
    private void ShowAddVersionModal(ExtensionDto extension)
    {
        _createVersionModel = new CreateExtensionVersionDto();
        _selectedExtension = extension;
        _selectedFile = null;
        _isAddVersionModalVisible = true;
    }

    // 关闭添加版本对话框
    private void CloseAddVersionModal()
    {
        _isAddVersionModalVisible = false;
        _selectedFile = null;
    }

    // 处理文件选择
    private void OnInputFileChange(InputFileChangeEventArgs e)
    {
        var file = e.File;
        if (!Path.GetExtension(file.Name).Equals(".zip", StringComparison.CurrentCultureIgnoreCase))
        {
            _ = MessageService.Error("只能上传ZIP文件");
            return;
        }

        _selectedFile = file;
        StateHasChanged();
    }

    // 添加扩展版本
    private async Task AddExtensionVersion()
    {
        try
        {
            if (_selectedExtension == null)
                return;

            if (string.IsNullOrWhiteSpace(_createVersionModel.VersionNumber) || _selectedFile == null)
            {
                _ = MessageService.Error("版本号和文件不能为空");
                return;
            }

            // 获取认证客户端
            var (isSuccess, client, message) = await ExtensionMarketService.GetAuthenticatedClientAsync();
            if (!isSuccess)
            {
                _ = MessageService.Error(message);
                return;
            }

            // 创建HttpRequestMessage
            var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiBaseUrl}/Extensions/{_selectedExtension.Id}/versions");

            // 创建MultipartFormDataContent
            using var content = new MultipartFormDataContent();

            // 添加表单字段
            content.Add(new StringContent(_createVersionModel.VersionNumber), "VersionNumber");
            content.Add(new StringContent(_createVersionModel.QuantumVersionSupport), "QuantumVersionSupport");
            content.Add(new StringContent(_createVersionModel.ReleaseNotes), "ReleaseNotes");

            // 添加文件
            await using var fileStream = _selectedFile.OpenReadStream(maxAllowedSize: 1024 * 1024 * 1024); // 1GB max
            using var ms = new MemoryStream();
            await fileStream.CopyToAsync(ms);
            ms.Position = 0;

            var fileContent = new StreamContent(ms);
            content.Add(fileContent, "ExtensionFile", _selectedFile.Name);

            // 设置请求内容
            request.Content = content;

            // 发送请求
            var response = await client!.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                _isAddVersionModalVisible = false;
                if (_selectedExtension != null)
                {
                    await LoadExtensionVersions(_selectedExtension.Id);
                }
                _ = MessageService.Success("版本添加成功");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _ = MessageService.Error($"添加失败: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            _ = MessageService.Error($"添加出错: {ex.Message}");
        }
    }

    #endregion
}

// 插件市场设置模型
public class ExtensionMarketSettingsModel
{
    public string Url { get; set; } = string.Empty;
}
