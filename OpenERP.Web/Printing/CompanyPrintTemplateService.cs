using Microsoft.AspNetCore.Http;
using OpenERP.HR.Models.Entities;
using OpenERP.Web.Data.HR;

namespace OpenERP.Web.Printing;

/// <summary>
/// 公司打印模板服务（统一维护公司级 LOGO、页头与页脚，并为打印页面生成公共模板配置）。
/// </summary>
public class CompanyPrintTemplateService : ICompanyPrintTemplateService
{
    /// <summary>
    /// 允许上传的 LOGO 扩展名（仅支持 JPG/JPEG/PNG）。
    /// </summary>
    private static readonly string[] AllowedLogoExtensions = [".png", ".jpg", ".jpeg"];

    /// <summary>
    /// 图片内容类型映射（按扩展名返回对应 MIME Type）。
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> LogoContentTypeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg"
    };

    /// <summary>
    /// 人资仓储（用于读取公司组织资料与组织编号）。
    /// </summary>
    private readonly IHrRepository _hrRepository;

    /// <summary>
    /// 初始化公司打印模板服务。
    /// </summary>
    public CompanyPrintTemplateService(IHrRepository hrRepository)
    {
        _hrRepository = hrRepository;
    }

    /// <summary>
    /// 按公司组织ID获取打印模板设置。
    /// </summary>
    public async Task<CompanyPrintTemplateSettings?> GetSettingsAsync(int companyOrganizationId)
    {
        if (companyOrganizationId <= 0)
        {
            return null;
        }

        var organization = await _hrRepository.GetCompanyOrganizationByIdAsync(companyOrganizationId);
        return organization is null ? null : BuildSettings(organization);
    }

    /// <summary>
    /// 按公司组织编号获取打印模板设置。
    /// </summary>
    public async Task<CompanyPrintTemplateSettings?> GetSettingsByOrganizationCodeAsync(string? organizationCode)
    {
        var requestedOrganizationCode = organizationCode?.Trim();
        if (string.IsNullOrWhiteSpace(requestedOrganizationCode))
        {
            return null;
        }

        var organization = await _hrRepository.GetCompanyOrganizationByCodeAsync(requestedOrganizationCode);
        return organization is null ? null : BuildSettings(organization);
    }

    /// <summary>
    /// 保存公司 LOGO 文件，并返回最新打印模板设置。
    /// </summary>
    public async Task<CompanyPrintTemplateSettings?> SaveLogoAsync(int companyOrganizationId, IFormFile logoFile)
    {
        if (companyOrganizationId <= 0)
        {
            throw new InvalidOperationException("未指定有效的公司组织。");
        }

        if (logoFile is null || logoFile.Length <= 0)
        {
            throw new InvalidOperationException("请选择要上传的 LOGO 图片。");
        }

        var organization = await _hrRepository.GetCompanyOrganizationByIdAsync(companyOrganizationId)
            ?? throw new InvalidOperationException("未找到对应的公司组织资料。");

        var extension = NormalizeAndValidateExtension(logoFile.FileName);
        var storageDirectory = EnsureLogoStorageDirectory();
        var normalizedOrganizationCode = NormalizeOrganizationCodeForFileName(organization.OrganizationCode);
        var destinationPath = Path.Combine(storageDirectory, $"{normalizedOrganizationCode}{extension}");

        DeleteLogoFiles(normalizedOrganizationCode);
        await using (var targetStream = File.Create(destinationPath))
        {
            await logoFile.CopyToAsync(targetStream);
        }

        return BuildSettings(organization);
    }

    /// <summary>
    /// 删除公司 LOGO 文件。
    /// </summary>
    public async Task<bool> DeleteLogoAsync(int companyOrganizationId)
    {
        if (companyOrganizationId <= 0)
        {
            return false;
        }

        var organization = await _hrRepository.GetCompanyOrganizationByIdAsync(companyOrganizationId);
        if (organization is null)
        {
            return false;
        }

        var normalizedOrganizationCode = NormalizeOrganizationCodeForFileName(organization.OrganizationCode);
        return DeleteLogoFiles(normalizedOrganizationCode) > 0;
    }

    /// <summary>
    /// 当组织编号变更时同步重命名 LOGO 文件。
    /// </summary>
    public Task<bool> RenameLogoAsync(string? oldOrganizationCode, string? newOrganizationCode)
    {
        var oldCode = NormalizeOrganizationCodeForFileName(oldOrganizationCode);
        var newCode = NormalizeOrganizationCodeForFileName(newOrganizationCode);

        if (string.IsNullOrWhiteSpace(oldCode) || string.IsNullOrWhiteSpace(newCode) || string.Equals(oldCode, newCode, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(true);
        }

        var sourcePath = FindLogoFilePath(oldCode);
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return Task.FromResult(true);
        }

        var storageDirectory = EnsureLogoStorageDirectory();
        var extension = Path.GetExtension(sourcePath);
        var destinationPath = Path.Combine(storageDirectory, $"{newCode}{extension}");

        DeleteLogoFiles(newCode);
        File.Move(sourcePath, destinationPath, true);
        return Task.FromResult(true);
    }

    /// <summary>
    /// 读取公司 LOGO 文件信息。
    /// </summary>
    public Task<CompanyPrintLogoFileResult?> GetLogoFileAsync(string organizationCode)
    {
        var normalizedOrganizationCode = NormalizeOrganizationCodeForFileName(organizationCode);
        if (string.IsNullOrWhiteSpace(normalizedOrganizationCode))
        {
            return Task.FromResult<CompanyPrintLogoFileResult?>(null);
        }

        var filePath = FindLogoFilePath(normalizedOrganizationCode);
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return Task.FromResult<CompanyPrintLogoFileResult?>(null);
        }

        var extension = Path.GetExtension(filePath);
        var contentType = LogoContentTypeMap.TryGetValue(extension, out var resolvedContentType)
            ? resolvedContentType
            : "application/octet-stream";

        return Task.FromResult<CompanyPrintLogoFileResult?>(new CompanyPrintLogoFileResult
        {
            FilePath = filePath,
            ContentType = contentType,
            FileName = Path.GetFileName(filePath)
        });
    }

    /// <summary>
    /// 构建公司打印模板设置。
    /// </summary>
    private static CompanyPrintTemplateSettings BuildSettings(CompanyOrganization organization)
    {
        var normalizedOrganizationCode = NormalizeOrganizationCodeForFileName(organization.OrganizationCode);
        var logoPath = FindLogoFilePath(normalizedOrganizationCode);
        var logoTicks = !string.IsNullOrWhiteSpace(logoPath)
            ? File.GetLastWriteTimeUtc(logoPath).Ticks
            : 0L;

        return new CompanyPrintTemplateSettings
        {
            CompanyOrganizationId = organization.Id,
            OrganizationCode = organization.OrganizationCode,
            OrganizationName = organization.OrganizationName,
            HasLogo = !string.IsNullOrWhiteSpace(logoPath),
            LogoUrl = !string.IsNullOrWhiteSpace(logoPath)
                ? $"/api/companyorganizationprinttemplate/logo/{Uri.EscapeDataString(normalizedOrganizationCode)}?v={logoTicks}"
                : null,
            LogoStoragePath = logoPath,
            PrintHeaderContent = organization.PrintHeaderContent,
            PrintFooterContent = organization.PrintFooterContent
        };
    }

    /// <summary>
    /// 确保 LOGO 存储目录已存在（位于系统安装目录下的 print-assets/company-logos）。
    /// </summary>
    private static string EnsureLogoStorageDirectory()
    {
        var storageDirectory = Path.Combine(AppContext.BaseDirectory, "print-assets", "company-logos");
        Directory.CreateDirectory(storageDirectory);
        return storageDirectory;
    }

    /// <summary>
    /// 查找当前组织编号对应的 LOGO 文件物理路径。
    /// </summary>
    private static string? FindLogoFilePath(string? organizationCode)
    {
        var normalizedOrganizationCode = NormalizeOrganizationCodeForFileName(organizationCode);
        if (string.IsNullOrWhiteSpace(normalizedOrganizationCode))
        {
            return null;
        }

        var storageDirectory = EnsureLogoStorageDirectory();
        foreach (var extension in AllowedLogoExtensions)
        {
            var filePath = Path.Combine(storageDirectory, $"{normalizedOrganizationCode}{extension}");
            if (File.Exists(filePath))
            {
                return filePath;
            }
        }

        return null;
    }

    /// <summary>
    /// 删除指定组织编号下所有允许类型的 LOGO 文件。
    /// </summary>
    private static int DeleteLogoFiles(string? organizationCode)
    {
        var normalizedOrganizationCode = NormalizeOrganizationCodeForFileName(organizationCode);
        if (string.IsNullOrWhiteSpace(normalizedOrganizationCode))
        {
            return 0;
        }

        var storageDirectory = EnsureLogoStorageDirectory();
        var deletedCount = 0;
        foreach (var extension in AllowedLogoExtensions)
        {
            var filePath = Path.Combine(storageDirectory, $"{normalizedOrganizationCode}{extension}");
            if (!File.Exists(filePath))
            {
                continue;
            }

            File.Delete(filePath);
            deletedCount += 1;
        }

        return deletedCount;
    }

    /// <summary>
    /// 规范并校验上传扩展名（仅允许 PNG/JPG/JPEG）。
    /// </summary>
    private static string NormalizeAndValidateExtension(string? originalFileName)
    {
        var extension = Path.GetExtension(originalFileName ?? string.Empty);
        if (string.IsNullOrWhiteSpace(extension))
        {
            throw new InvalidOperationException("LOGO 图片必须为 JPG 或 PNG 格式。");
        }

        var normalizedExtension = extension.Trim().ToLowerInvariant();
        if (!AllowedLogoExtensions.Contains(normalizedExtension, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("LOGO 图片仅支持 JPG、JPEG 或 PNG 格式。");
        }

        return normalizedExtension;
    }

    /// <summary>
    /// 规范组织编号为文件名（移除首尾空白与非法文件名字符）。
    /// </summary>
    private static string NormalizeOrganizationCodeForFileName(string? organizationCode)
    {
        if (string.IsNullOrWhiteSpace(organizationCode))
        {
            return string.Empty;
        }

        var normalized = organizationCode.Trim();
        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            normalized = normalized.Replace(invalidChar.ToString(), string.Empty, StringComparison.Ordinal);
        }

        return normalized;
    }
}
