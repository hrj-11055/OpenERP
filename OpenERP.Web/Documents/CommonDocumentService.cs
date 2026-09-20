using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace OpenERP.Web.Documents;

public class CommonDocumentService : ICommonDocumentService
{
	private const string DocumentTableName = "SYS_Document";

	private const long MaxFileSizeBytes = 52428800L;

	private static readonly HashSet<string> BlockedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".bat", ".cmd", ".com", ".dll", ".exe", ".js", ".msi", ".ps1", ".sh", ".vbs" };

	private static readonly HashSet<string> ImageFeatureCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "HR_EMPLOYEE_PHOTO" };

	private static readonly HashSet<string> AllowedImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

	private static readonly HashSet<string> AllowedImageContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp", "image/gif" };

	private readonly string _connectionString;

	private readonly string _documentRootDirectory;

	public CommonDocumentService(IConfiguration configuration, IWebHostEnvironment environment)
	{
		_connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("缺少数据库连接字符串：DefaultConnection。");
		_documentRootDirectory = Path.Combine(environment.ContentRootPath, "App_Data", "documents");
	}

	public async Task InitializeAsync()
	{
		await using SqlConnection conn = await OpenConnectionAsync();
		await using SqlCommand cmd = new SqlCommand(BuildInitializeSql(), conn);
		await cmd.ExecuteNonQueryAsync();
	}

	public async Task<List<ManagedDocument>> GetDocumentsAsync(string featureCode, int entityId)
	{
		string normalizedFeatureCode = NormalizeFeatureCode(featureCode);
		if (string.IsNullOrWhiteSpace(normalizedFeatureCode) || entityId <= 0)
		{
			return new List<ManagedDocument>();
		}
		string sql = "SELECT Id, FeatureCode, EntityId, OriginalFileName, StoredFileName, ContentType, FileExtension,\n       FileSizeBytes, StoragePath, UploadedAt, UploadedBy\nFROM dbo.SYS_Document\nWHERE FeatureCode = @FeatureCode\n  AND EntityId = @EntityId\n  AND IsDeleted = 0\nORDER BY UploadedAt DESC, Id DESC;";
		List<ManagedDocument> results = new List<ManagedDocument>();
		List<ManagedDocument> result;
		await using (SqlConnection conn = await OpenConnectionAsync())
		{
			List<ManagedDocument> list2;
			await using (SqlCommand cmd = new SqlCommand(sql, conn))
			{
				cmd.Parameters.AddWithValue("@FeatureCode", normalizedFeatureCode);
				cmd.Parameters.AddWithValue("@EntityId", entityId);
				List<ManagedDocument> list;
				await using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
				{
					while (await reader.ReadAsync())
					{
						results.Add(MapDocument(reader));
					}
					list = results;
				}
				list2 = list;
			}
			result = list2;
		}
		return result;
	}

	public async Task<ManagedDocument> SaveDocumentAsync(string featureCode, int entityId, IFormFile file, string? uploadedBy)
	{
		string normalizedFeatureCode = NormalizeFeatureCode(featureCode);
		if (string.IsNullOrWhiteSpace(normalizedFeatureCode) || entityId <= 0)
		{
			throw new InvalidOperationException("文档关联的业务记录无效。");
		}
		if (file == null || file.Length <= 0)
		{
			throw new InvalidOperationException("请选择要上传的文档。");
		}
		if (file.Length > 52428800)
		{
			throw new InvalidOperationException("单个文档不能超过 50MB。");
		}
		string originalFileName = NormalizeOriginalFileName(file.FileName);
		string extension = Path.GetExtension(originalFileName).Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(extension))
		{
			throw new InvalidOperationException("文档必须包含有效扩展名。");
		}
		if (BlockedExtensions.Contains(extension))
		{
			throw new InvalidOperationException("当前文件类型不允许上传。");
		}
		string contentType = NormalizeContentType(file.ContentType);
		if (ImageFeatureCodes.Contains(normalizedFeatureCode))
		{
			ValidateImageDocument(file, extension, contentType);
		}
		string storageDirectory = EnsureStorageDirectory(normalizedFeatureCode, entityId);
		string storedFileName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}{extension}";
		string storagePath = Path.Combine(storageDirectory, storedFileName);
		await using (FileStream targetStream = File.Create(storagePath))
		{
			await file.CopyToAsync(targetStream);
		}
		ManagedDocument document = new ManagedDocument
		{
			FeatureCode = normalizedFeatureCode,
			EntityId = entityId,
			OriginalFileName = originalFileName,
			StoredFileName = storedFileName,
			ContentType = contentType,
			FileExtension = extension,
			FileSizeBytes = file.Length,
			StoragePath = storagePath,
			UploadedAt = DateTime.Now,
			UploadedBy = uploadedBy
		};
		try
		{
			ManagedDocument managedDocument = document;
			managedDocument.Id = await InsertDocumentAsync(document);
			return document;
		}
		catch
		{
			TryDeleteFile(storagePath);
			throw;
		}
	}

	public async Task<ManagedDocument?> GetDocumentAsync(int documentId)
	{
		if (documentId <= 0)
		{
			return null;
		}
		string sql = "SELECT Id, FeatureCode, EntityId, OriginalFileName, StoredFileName, ContentType, FileExtension,\n       FileSizeBytes, StoragePath, UploadedAt, UploadedBy\nFROM dbo.SYS_Document\nWHERE Id = @Id AND IsDeleted = 0;";
		ManagedDocument result;
		await using (SqlConnection conn = await OpenConnectionAsync())
		{
			ManagedDocument managedDocument2;
			await using (SqlCommand cmd = new SqlCommand(sql, conn))
			{
				cmd.Parameters.AddWithValue("@Id", documentId);
				ManagedDocument managedDocument;
				await using (SqlDataReader reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow))
				{
					managedDocument = ((await reader.ReadAsync()) ? MapDocument(reader) : null);
				}
				managedDocument2 = managedDocument;
			}
			result = managedDocument2;
		}
		return result;
	}

	public async Task<bool> DeleteDocumentAsync(int documentId, string? deletedBy)
	{
		ManagedDocument document = await GetDocumentAsync(documentId);
		if (document == null)
		{
			return false;
		}
		string sql = "UPDATE dbo.SYS_Document\nSET IsDeleted = 1,\n    DeletedAt = SYSDATETIME(),\n    DeletedBy = @DeletedBy\nWHERE Id = @Id AND IsDeleted = 0;";
		bool result;
		await using (SqlConnection conn = await OpenConnectionAsync())
		{
			bool flag;
			await using (SqlCommand cmd = new SqlCommand(sql, conn))
			{
				cmd.Parameters.AddWithValue("@Id", documentId);
				cmd.Parameters.AddWithValue("@DeletedBy", string.IsNullOrWhiteSpace(deletedBy) ? ((IConvertible)DBNull.Value) : ((IConvertible)deletedBy.Trim()));
				if (await cmd.ExecuteNonQueryAsync() <= 0)
				{
					flag = false;
				}
				else
				{
					TryDeleteFile(document.StoragePath);
					flag = true;
				}
			}
			result = flag;
		}
		return result;
	}

	private async Task<int> InsertDocumentAsync(ManagedDocument document)
	{
		string sql = "INSERT INTO dbo.SYS_Document\n(\n    FeatureCode, EntityId, OriginalFileName, StoredFileName, ContentType, FileExtension,\n    FileSizeBytes, StoragePath, UploadedAt, UploadedBy, IsDeleted\n)\nOUTPUT INSERTED.Id\nVALUES\n(\n    @FeatureCode, @EntityId, @OriginalFileName, @StoredFileName, @ContentType, @FileExtension,\n    @FileSizeBytes, @StoragePath, SYSDATETIME(), @UploadedBy, 0\n);";
		int result;
		await using (SqlConnection conn = await OpenConnectionAsync())
		{
			int num;
			await using (SqlCommand cmd = new SqlCommand(sql, conn))
			{
				cmd.Parameters.AddWithValue("@FeatureCode", document.FeatureCode);
				cmd.Parameters.AddWithValue("@EntityId", document.EntityId);
				cmd.Parameters.AddWithValue("@OriginalFileName", document.OriginalFileName);
				cmd.Parameters.AddWithValue("@StoredFileName", document.StoredFileName);
				cmd.Parameters.AddWithValue("@ContentType", document.ContentType);
				cmd.Parameters.AddWithValue("@FileExtension", document.FileExtension);
				cmd.Parameters.AddWithValue("@FileSizeBytes", document.FileSizeBytes);
				cmd.Parameters.AddWithValue("@StoragePath", document.StoragePath);
				cmd.Parameters.AddWithValue("@UploadedBy", string.IsNullOrWhiteSpace(document.UploadedBy) ? ((IConvertible)DBNull.Value) : ((IConvertible)document.UploadedBy.Trim()));
				num = Convert.ToInt32(await cmd.ExecuteScalarAsync());
			}
			result = num;
		}
		return result;
	}

	private static string BuildInitializeSql()
	{
		return "IF OBJECT_ID('dbo.SYS_Document', 'U') IS NULL\nBEGIN\n    CREATE TABLE dbo.SYS_Document\n    (\n        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SYS_Document PRIMARY KEY,\n        FeatureCode NVARCHAR(80) NOT NULL,\n        EntityId INT NOT NULL,\n        OriginalFileName NVARCHAR(260) NOT NULL,\n        StoredFileName NVARCHAR(160) NOT NULL,\n        ContentType NVARCHAR(150) NOT NULL CONSTRAINT DF_SYS_Document_ContentType DEFAULT(N'application/octet-stream'),\n        FileExtension NVARCHAR(20) NOT NULL,\n        FileSizeBytes BIGINT NOT NULL,\n        StoragePath NVARCHAR(1000) NOT NULL,\n        UploadedAt DATETIME2 NOT NULL CONSTRAINT DF_SYS_Document_UploadedAt DEFAULT(SYSDATETIME()),\n        UploadedBy NVARCHAR(80) NULL,\n        DeletedAt DATETIME2 NULL,\n        DeletedBy NVARCHAR(80) NULL,\n        IsDeleted BIT NOT NULL CONSTRAINT DF_SYS_Document_IsDeleted DEFAULT(0)\n    );\n\n    CREATE INDEX IX_SYS_Document_FeatureEntity\n        ON dbo.SYS_Document(FeatureCode, EntityId, IsDeleted, UploadedAt DESC);\nEND;";
	}

	private async Task<SqlConnection> OpenConnectionAsync()
	{
		SqlConnection conn = new SqlConnection(_connectionString);
		await conn.OpenAsync();
		return conn;
	}

	private static ManagedDocument MapDocument(SqlDataReader reader)
	{
		return new ManagedDocument
		{
			Id = reader.GetInt32(0),
			FeatureCode = reader.GetString(1),
			EntityId = reader.GetInt32(2),
			OriginalFileName = reader.GetString(3),
			StoredFileName = reader.GetString(4),
			ContentType = reader.GetString(5),
			FileExtension = reader.GetString(6),
			FileSizeBytes = reader.GetInt64(7),
			StoragePath = reader.GetString(8),
			UploadedAt = reader.GetDateTime(9),
			UploadedBy = (reader.IsDBNull(10) ? null : reader.GetString(10))
		};
	}

	private static string NormalizeFeatureCode(string? featureCode)
	{
		string source = (featureCode ?? string.Empty).Trim().ToUpperInvariant();
		return new string(source.Where(delegate(char character)
		{
			bool flag = char.IsLetterOrDigit(character);
			bool flag2 = flag;
			if (!flag2)
			{
				bool flag3 = ((character == '-' || character == '_') ? true : false);
				flag2 = flag3;
			}
			return flag2;
		}).ToArray());
	}

	private static string NormalizeOriginalFileName(string? originalFileName)
	{
		string text = Path.GetFileName(originalFileName ?? string.Empty).Trim();
		char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
		foreach (char c in invalidFileNameChars)
		{
			text = text.Replace(c.ToString(), string.Empty, StringComparison.Ordinal);
		}
		return string.IsNullOrWhiteSpace(text) ? "未命名文档" : text;
	}

	private static string NormalizeContentType(string? contentType)
	{
		return string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim().ToLowerInvariant();
	}

	private static void ValidateImageDocument(IFormFile file, string extension, string contentType)
	{
		if (!AllowedImageExtensions.Contains(extension) || !AllowedImageContentTypes.Contains(contentType))
		{
			throw new InvalidOperationException("员工图片仅支持 JPG、PNG、WEBP 或 GIF 格式。");
		}
		Span<byte> buffer = stackalloc byte[12];
		using Stream stream = file.OpenReadStream();
		if (!HasExpectedImageSignature(buffer[..stream.Read(buffer)], extension))
		{
			throw new InvalidOperationException("图片内容与文件扩展名不匹配，请重新选择有效图片。");
		}
	}

	private static bool HasExpectedImageSignature(ReadOnlySpan<byte> header, string extension)
	{
		if ((extension == ".jpg" || extension == ".jpeg") ? true : false)
		{
			return header.Length >= 3 && header[0] == byte.MaxValue && header[1] == 216 && header[2] == byte.MaxValue;
		}
		return extension switch
		{
			".png" => header.Length >= 8 && header[0] == 137 && header[1] == 80 && header[2] == 78 && header[3] == 71 && header[4] == 13 && header[5] == 10 && header[6] == 26 && header[7] == 10, 
			".gif" => header.Length >= 6 && header[0] == 71 && header[1] == 73 && header[2] == 70 && header[3] == 56 && (header[4] == 55 || header[4] == 57) && header[5] == 97, 
			".webp" => header.Length >= 12 && header[0] == 82 && header[1] == 73 && header[2] == 70 && header[3] == 70 && header[8] == 87 && header[9] == 69 && header[10] == 66 && header[11] == 80, 
			_ => false, 
		};
	}

	private string EnsureStorageDirectory(string featureCode, int entityId)
	{
		string text = Path.Combine(_documentRootDirectory, featureCode, entityId.ToString());
		Directory.CreateDirectory(text);
		return text;
	}

	private static void TryDeleteFile(string? storagePath)
	{
		if (string.IsNullOrWhiteSpace(storagePath) || !File.Exists(storagePath))
		{
			return;
		}
		try
		{
			File.Delete(storagePath);
		}
		catch (IOException)
		{
		}
		catch (UnauthorizedAccessException)
		{
		}
	}
}
