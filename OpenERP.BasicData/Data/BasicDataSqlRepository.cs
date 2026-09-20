using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using OpenERP.BasicData.Models;

namespace OpenERP.BasicData.Data;

/// <summary>
/// 基础数据 SQL 仓储实现（负责基础数据类型和选项的持久化）。
/// </summary>
public class BasicDataSqlRepository : IBasicDataRepository
{
    /// <summary>
    /// 默认数据库连接字符串（来自应用配置）。
    /// </summary>
    private readonly string _connectionString;

    public BasicDataSqlRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Missing connection string: DefaultConnection");
    }

    public async Task InitializeAsync()
    {
        await using var conn = await OpenConnectionAsync();

        const string createTypesTableSql = """
            IF OBJECT_ID('dbo.BD_BasicDataType', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.BD_BasicDataType
                (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    TypeCode NVARCHAR(100) NOT NULL,
                    TypeName NVARCHAR(200) NOT NULL,
                    Description NVARCHAR(500) NULL,
                    CreatedAt DATETIME2 NOT NULL,
                    UpdatedAt DATETIME2 NULL,
                    CreatedBy NVARCHAR(50) NULL,
                    UpdatedBy NVARCHAR(50) NULL,
                    IsDeleted BIT NOT NULL
                );
                CREATE UNIQUE INDEX UX_BD_BasicDataType_TypeCode ON dbo.BD_BasicDataType(TypeCode);
            END;
            """;

        const string createItemsTableSql = """
            IF OBJECT_ID('dbo.BD_BasicDataItem', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.BD_BasicDataItem
                (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    TypeId INT NOT NULL,
                    ItemCode NVARCHAR(100) NOT NULL,
                    ItemName NVARCHAR(200) NOT NULL,
                    ParentId INT NULL,
                    SortOrder INT NOT NULL,
                    IsActive BIT NOT NULL,
                    Remark NVARCHAR(500) NULL,
                    CreatedAt DATETIME2 NOT NULL,
                    UpdatedAt DATETIME2 NULL,
                    CreatedBy NVARCHAR(50) NULL,
                    UpdatedBy NVARCHAR(50) NULL,
                    IsDeleted BIT NOT NULL,
                    CONSTRAINT FK_BD_BasicDataItem_BD_BasicDataType_TypeId FOREIGN KEY (TypeId) REFERENCES dbo.BD_BasicDataType(Id)
                );
                CREATE UNIQUE INDEX UX_BD_BasicDataItem_TypeId_ItemCode ON dbo.BD_BasicDataItem(TypeId, ItemCode);
            END;
            """;

        await ExecuteNonQueryAsync(conn, createTypesTableSql);
        await ExecuteNonQueryAsync(conn, createItemsTableSql);

        await SeedDefaultTypesAsync(conn);
        await SeedDefaultItemsAsync(conn);
        await SeedHrPresetTypesAsync(conn);
        await SeedHrPresetItemsAsync(conn);
        await EnsureRegionTypeNamingAsync(conn);
        await EnsureLocationHierarchyAsync(conn);
    }

    public async Task<List<BasicDataType>> GetTypesAsync()
    {
        const string sql = """
            SELECT Id, TypeCode, TypeName, Description, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, IsDeleted
            FROM dbo.BD_BasicDataType
            WHERE IsDeleted = 0
            ORDER BY TypeName;
            """;

        var results = new List<BasicDataType>();
        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            results.Add(new BasicDataType
            {
                Id = reader.GetInt32(0),
                TypeCode = reader.GetString(1),
                TypeName = reader.GetString(2),
                Description = ReadNullableString(reader, 3),
                CreatedAt = reader.GetDateTime(4),
                UpdatedAt = ReadNullableDateTime(reader, 5),
                CreatedBy = ReadNullableString(reader, 6),
                UpdatedBy = ReadNullableString(reader, 7),
                IsDeleted = reader.GetBoolean(8)
            });
        }

        return results;
    }

    public async Task<BasicDataType?> GetTypeByIdAsync(int id)
    {
        const string sql = """
            SELECT Id, TypeCode, TypeName, Description, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, IsDeleted
            FROM dbo.BD_BasicDataType
            WHERE Id = @Id AND IsDeleted = 0;
            """;

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        await using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new BasicDataType
        {
            Id = reader.GetInt32(0),
            TypeCode = reader.GetString(1),
            TypeName = reader.GetString(2),
            Description = ReadNullableString(reader, 3),
            CreatedAt = reader.GetDateTime(4),
            UpdatedAt = ReadNullableDateTime(reader, 5),
            CreatedBy = ReadNullableString(reader, 6),
            UpdatedBy = ReadNullableString(reader, 7),
            IsDeleted = reader.GetBoolean(8)
        };
    }

    /// <summary>
    /// 鎸夌被鍨嬬紪鐮佹煡璇㈠熀纭€鏁版嵁绫诲瀷锛屼緵鈥滃熀纭€鏁版嵁妯″潡鈥濆叆鍙ｈ烦杞娇鐢ㄣ€?    /// </summary>
    public async Task<BasicDataType?> GetTypeByCodeAsync(string typeCode)
    {
        const string sql = """
            SELECT Id, TypeCode, TypeName, Description, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, IsDeleted
            FROM dbo.BD_BasicDataType
            WHERE TypeCode = @TypeCode AND IsDeleted = 0;
            """;

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@TypeCode", typeCode.Trim());
        await using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new BasicDataType
        {
            Id = reader.GetInt32(0),
            TypeCode = reader.GetString(1),
            TypeName = reader.GetString(2),
            Description = ReadNullableString(reader, 3),
            CreatedAt = reader.GetDateTime(4),
            UpdatedAt = ReadNullableDateTime(reader, 5),
            CreatedBy = ReadNullableString(reader, 6),
            UpdatedBy = ReadNullableString(reader, 7),
            IsDeleted = reader.GetBoolean(8)
        };
    }

    public async Task CreateTypeAsync(BasicDataType type, string? userName = null)
    {
        const string sql = """
            INSERT INTO dbo.BD_BasicDataType
            (
                TypeCode, TypeName, Description,
                CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, IsDeleted
            )
            VALUES
            (
                @TypeCode, @TypeName, @Description,
                @CreatedAt, @UpdatedAt, @CreatedBy, @UpdatedBy, @IsDeleted
            );
            """;

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@TypeCode", type.TypeCode.Trim());
        cmd.Parameters.AddWithValue("@TypeName", type.TypeName.Trim());
        cmd.Parameters.AddWithValue("@Description", ToDbValue(type.Description));
        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
        cmd.Parameters.AddWithValue("@UpdatedAt", DBNull.Value);
        cmd.Parameters.AddWithValue("@CreatedBy", ToDbValue(userName));
        cmd.Parameters.AddWithValue("@UpdatedBy", DBNull.Value);
        cmd.Parameters.AddWithValue("@IsDeleted", false);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<bool> UpdateTypeAsync(BasicDataType type, string? userName = null)
    {
        const string sql = """
            UPDATE dbo.BD_BasicDataType
            SET
                TypeCode = @TypeCode,
                TypeName = @TypeName,
                Description = @Description,
                UpdatedAt = @UpdatedAt,
                UpdatedBy = @UpdatedBy
            WHERE Id = @Id AND IsDeleted = 0;
            """;

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", type.Id);
        cmd.Parameters.AddWithValue("@TypeCode", type.TypeCode.Trim());
        cmd.Parameters.AddWithValue("@TypeName", type.TypeName.Trim());
        cmd.Parameters.AddWithValue("@Description", ToDbValue(type.Description));
        cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
        cmd.Parameters.AddWithValue("@UpdatedBy", ToDbValue(userName));

        var affected = await cmd.ExecuteNonQueryAsync();
        return affected > 0;
    }

    public async Task<bool> DeleteTypeAsync(int id, string? userName = null)
    {
        await using var conn = await OpenConnectionAsync();
        await using var tran = await conn.BeginTransactionAsync();

        try
        {
            const string deleteTypeSql = """
                UPDATE dbo.BD_BasicDataType
                SET IsDeleted = 1, UpdatedAt = @UpdatedAt, UpdatedBy = @UpdatedBy
                WHERE Id = @Id AND IsDeleted = 0;
                """;

            await using var typeCmd = new SqlCommand(deleteTypeSql, conn, (SqlTransaction)tran);
            typeCmd.Parameters.AddWithValue("@Id", id);
            typeCmd.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
            typeCmd.Parameters.AddWithValue("@UpdatedBy", ToDbValue(userName));
            var affectedType = await typeCmd.ExecuteNonQueryAsync();

            const string deleteItemsSql = """
                UPDATE dbo.BD_BasicDataItem
                SET IsDeleted = 1, UpdatedAt = @UpdatedAt, UpdatedBy = @UpdatedBy
                WHERE TypeId = @TypeId AND IsDeleted = 0;
                """;

            await using var itemCmd = new SqlCommand(deleteItemsSql, conn, (SqlTransaction)tran);
            itemCmd.Parameters.AddWithValue("@TypeId", id);
            itemCmd.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
            itemCmd.Parameters.AddWithValue("@UpdatedBy", ToDbValue(userName));
            await itemCmd.ExecuteNonQueryAsync();

            await tran.CommitAsync();
            return affectedType > 0;
        }
        catch
        {
            await tran.RollbackAsync();
            throw;
        }
    }

    public async Task<List<BasicDataItem>> GetItemsAsync(int? typeId = null)
    {
        const string sql = """
            SELECT
                i.Id, i.TypeId, t.TypeName, i.ItemCode, i.ItemName, i.ParentId, p.ItemName AS ParentName,
                i.SortOrder, i.IsActive, i.Remark, i.CreatedAt, i.UpdatedAt, i.CreatedBy, i.UpdatedBy, i.IsDeleted
            FROM dbo.BD_BasicDataItem i
            INNER JOIN dbo.BD_BasicDataType t ON i.TypeId = t.Id
            LEFT JOIN dbo.BD_BasicDataItem p ON i.ParentId = p.Id
            WHERE i.IsDeleted = 0
              AND (@TypeId IS NULL OR i.TypeId = @TypeId)
            ORDER BY t.TypeName, i.SortOrder, i.ItemName;
            """;

        var results = new List<BasicDataItem>();
        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@TypeId", typeId.HasValue ? typeId.Value : DBNull.Value);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            results.Add(new BasicDataItem
            {
                Id = reader.GetInt32(0),
                TypeId = reader.GetInt32(1),
                TypeName = reader.GetString(2),
                ItemCode = reader.GetString(3),
                ItemName = reader.GetString(4),
                ParentId = ReadNullableInt(reader, 5),
                ParentName = ReadNullableString(reader, 6),
                SortOrder = reader.GetInt32(7),
                IsActive = reader.GetBoolean(8),
                Remark = ReadNullableString(reader, 9),
                CreatedAt = reader.GetDateTime(10),
                UpdatedAt = ReadNullableDateTime(reader, 11),
                CreatedBy = ReadNullableString(reader, 12),
                UpdatedBy = ReadNullableString(reader, 13),
                IsDeleted = reader.GetBoolean(14)
            });
        }

        return results;
    }

    public async Task<List<BasicDataItem>> GetItemsByTypeIdAsync(int typeId)
    {
        const string sql = """
            SELECT
                Id, TypeId, ItemCode, ItemName, ParentId, SortOrder, IsActive, Remark,
                CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, IsDeleted
            FROM dbo.BD_BasicDataItem
            WHERE TypeId = @TypeId AND IsDeleted = 0
            ORDER BY SortOrder, ItemName;
            """;

        var results = new List<BasicDataItem>();
        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@TypeId", typeId);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            results.Add(new BasicDataItem
            {
                Id = reader.GetInt32(0),
                TypeId = reader.GetInt32(1),
                ItemCode = reader.GetString(2),
                ItemName = reader.GetString(3),
                ParentId = ReadNullableInt(reader, 4),
                SortOrder = reader.GetInt32(5),
                IsActive = reader.GetBoolean(6),
                Remark = ReadNullableString(reader, 7),
                CreatedAt = reader.GetDateTime(8),
                UpdatedAt = ReadNullableDateTime(reader, 9),
                CreatedBy = ReadNullableString(reader, 10),
                UpdatedBy = ReadNullableString(reader, 11),
                IsDeleted = reader.GetBoolean(12)
            });
        }

        return results;
    }

    public async Task<BasicDataItem?> GetItemByIdAsync(int id)
    {
        const string sql = """
            SELECT
                i.Id, i.TypeId, t.TypeName, i.ItemCode, i.ItemName, i.ParentId, p.ItemName AS ParentName,
                i.SortOrder, i.IsActive, i.Remark, i.CreatedAt, i.UpdatedAt, i.CreatedBy, i.UpdatedBy, i.IsDeleted
            FROM dbo.BD_BasicDataItem i
            INNER JOIN dbo.BD_BasicDataType t ON i.TypeId = t.Id
            LEFT JOIN dbo.BD_BasicDataItem p ON i.ParentId = p.Id
            WHERE i.Id = @Id AND i.IsDeleted = 0;
            """;

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        await using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new BasicDataItem
        {
            Id = reader.GetInt32(0),
            TypeId = reader.GetInt32(1),
            TypeName = reader.GetString(2),
            ItemCode = reader.GetString(3),
            ItemName = reader.GetString(4),
            ParentId = ReadNullableInt(reader, 5),
            ParentName = ReadNullableString(reader, 6),
            SortOrder = reader.GetInt32(7),
            IsActive = reader.GetBoolean(8),
            Remark = ReadNullableString(reader, 9),
            CreatedAt = reader.GetDateTime(10),
            UpdatedAt = ReadNullableDateTime(reader, 11),
            CreatedBy = ReadNullableString(reader, 12),
            UpdatedBy = ReadNullableString(reader, 13),
            IsDeleted = reader.GetBoolean(14)
        };
    }

    public async Task CreateItemAsync(BasicDataItem item, string? userName = null)
    {
        const string sql = """
            INSERT INTO dbo.BD_BasicDataItem
            (
                TypeId, ItemCode, ItemName, ParentId, SortOrder, IsActive, Remark,
                CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, IsDeleted
            )
            VALUES
            (
                @TypeId, @ItemCode, @ItemName, @ParentId, @SortOrder, @IsActive, @Remark,
                @CreatedAt, @UpdatedAt, @CreatedBy, @UpdatedBy, @IsDeleted
            );
            """;

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@TypeId", item.TypeId);
        cmd.Parameters.AddWithValue("@ItemCode", item.ItemCode.Trim());
        cmd.Parameters.AddWithValue("@ItemName", item.ItemName.Trim());
        cmd.Parameters.AddWithValue("@ParentId", ToDbValue(item.ParentId));
        cmd.Parameters.AddWithValue("@SortOrder", item.SortOrder);
        cmd.Parameters.AddWithValue("@IsActive", item.IsActive);
        cmd.Parameters.AddWithValue("@Remark", ToDbValue(item.Remark));
        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
        cmd.Parameters.AddWithValue("@UpdatedAt", DBNull.Value);
        cmd.Parameters.AddWithValue("@CreatedBy", ToDbValue(userName));
        cmd.Parameters.AddWithValue("@UpdatedBy", DBNull.Value);
        cmd.Parameters.AddWithValue("@IsDeleted", false);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<bool> UpdateItemAsync(BasicDataItem item, string? userName = null)
    {
        const string sql = """
            UPDATE dbo.BD_BasicDataItem
            SET
                TypeId = @TypeId,
                ItemCode = @ItemCode,
                ItemName = @ItemName,
                ParentId = @ParentId,
                SortOrder = @SortOrder,
                IsActive = @IsActive,
                Remark = @Remark,
                UpdatedAt = @UpdatedAt,
                UpdatedBy = @UpdatedBy
            WHERE Id = @Id AND IsDeleted = 0;
            """;

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", item.Id);
        cmd.Parameters.AddWithValue("@TypeId", item.TypeId);
        cmd.Parameters.AddWithValue("@ItemCode", item.ItemCode.Trim());
        cmd.Parameters.AddWithValue("@ItemName", item.ItemName.Trim());
        cmd.Parameters.AddWithValue("@ParentId", ToDbValue(item.ParentId));
        cmd.Parameters.AddWithValue("@SortOrder", item.SortOrder);
        cmd.Parameters.AddWithValue("@IsActive", item.IsActive);
        cmd.Parameters.AddWithValue("@Remark", ToDbValue(item.Remark));
        cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
        cmd.Parameters.AddWithValue("@UpdatedBy", ToDbValue(userName));

        var affected = await cmd.ExecuteNonQueryAsync();
        return affected > 0;
    }

    public async Task<bool> DeleteItemAsync(int id, string? userName = null)
    {
        const string sql = """
            UPDATE dbo.BD_BasicDataItem
            SET IsDeleted = 1, UpdatedAt = @UpdatedAt, UpdatedBy = @UpdatedBy
            WHERE Id = @Id AND IsDeleted = 0;
            """;

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
        cmd.Parameters.AddWithValue("@UpdatedBy", ToDbValue(userName));

        var affected = await cmd.ExecuteNonQueryAsync();
        return affected > 0;
    }

    private static async Task ExecuteNonQueryAsync(SqlConnection connection, string sql)
    {
        await using var cmd = new SqlCommand(sql, connection);
        await cmd.ExecuteNonQueryAsync();
    }

    private static object ToDbValue(string? value) => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value;
    private static object ToDbValue(int? value) => value.HasValue ? value.Value : DBNull.Value;

    private static string? ReadNullableString(SqlDataReader reader, int index)
        => reader.IsDBNull(index) ? null : reader.GetString(index);

    private static int? ReadNullableInt(SqlDataReader reader, int index)
        => reader.IsDBNull(index) ? null : reader.GetInt32(index);

    private static DateTime? ReadNullableDateTime(SqlDataReader reader, int index)
        => reader.IsDBNull(index) ? null : reader.GetDateTime(index);

    private async Task<SqlConnection> OpenConnectionAsync()
    {
        var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        return conn;
    }

    /// <summary>
    /// 通用基础数据类型预置（用于组织与企业相关下拉字典）。
    /// </summary>
    private static readonly (string TypeCode, string TypeName, string? Description)[] DefaultTypes =
    [
        ("COMPANY_NATURE", "Company Nature", "Company nature options"),
        ("ORG_STATUS", "Organization Status", "Organization status options"),
        ("ENTERPRISE_TYPE", "Enterprise Type", "Enterprise type options"),
        ("REGION", "地域", "地域基础数据，上一级为国家地区"),
        ("CITY", "City", "City options, level 2 under COUNTRY_REGION"),
        ("COUNTY", "County", "County options, level 3 under CITY")
    ];

    /// <summary>
    /// 通用基础数据选项预置（与 DefaultTypes 对应）。
    /// </summary>
    private static readonly (string TypeCode, string ItemCode, string ItemName, int SortOrder)[] DefaultItems =
    [
        ("COMPANY_NATURE", "STATE", "State-owned", 10),
        ("COMPANY_NATURE", "PRIVATE", "Private", 20),
        ("COMPANY_NATURE", "FOREIGN", "Foreign-funded", 30),

        ("ORG_STATUS", "ACTIVE", "Active", 10),
        ("ORG_STATUS", "INACTIVE", "Inactive", 20),

        ("ENTERPRISE_TYPE", "LLC", "Limited Liability Company", 10),
        ("ENTERPRISE_TYPE", "CORP", "Corporation", 20),
        ("ENTERPRISE_TYPE", "PARTNER", "Partnership", 30),

        ("REGION", "EAST", "East", 10),
        ("REGION", "SOUTH", "South", 20),
        ("REGION", "NORTH", "North", 30),

        ("CITY", "SHANGHAI", "Shanghai", 10),
        ("CITY", "BEIJING", "Beijing", 20),
        ("CITY", "GUANGZHOU", "Guangzhou", 30),
        ("CITY", "SHENZHEN", "Shenzhen", 40),
        ("CITY", "FOSHAN", "Foshan", 50),

        ("COUNTY", "PUDONG", "Pudong", 10),
        ("COUNTY", "CHAOYANG", "Chaoyang", 20),
        ("COUNTY", "TIANHE", "Tianhe", 30),
        ("COUNTY", "YUEXIU", "Yuexiu", 40),
        ("COUNTY", "NANSHAN", "Nanshan", 50)
    ];

    /// <summary>
    /// 基础数据模块预置的人资类基础数据类型（用于职位/在职状态/民族/职称/国家地区管理）。
    /// </summary>
    private static readonly (string TypeCode, string TypeName, string? Description)[] HrPresetTypes =
    [
        ("POSITION", "\u804c\u52a1\u6570\u636e", "\u804c\u52a1\u57fa\u7840\u6570\u636e\uff0c\u6765\u81ea\u57fa\u7840\u6570\u636e\u5b57\u5178"),
        ("EMPLOYMENT_STATUS", "\u5728\u804c\u72b6\u6001\u6570\u636e", "\u5458\u5de5\u5728\u804c\u72b6\u6001\u57fa\u7840\u6570\u636e\uff0c\u6765\u81ea\u57fa\u7840\u6570\u636e\u5b57\u5178"),
        ("ETHNICITY", "\u6c11\u65cf\u6570\u636e", "\u6c11\u65cf\u57fa\u7840\u6570\u636e\uff0c\u6765\u81ea\u57fa\u7840\u6570\u636e\u5b57\u5178"),
        ("PROFESSIONAL_TITLE", "\u804c\u79f0\u6570\u636e", "\u4e13\u4e1a\u804c\u79f0\u57fa\u7840\u6570\u636e\uff0c\u6765\u81ea\u57fa\u7840\u6570\u636e\u5b57\u5178"),
        ("ALLOWANCE_PACKAGE", "\u6d25\u8d34\u5f85\u9047\u6570\u636e", "\u5458\u5de5\u6d25\u8d34\u5f85\u9047\u57fa\u7840\u6570\u636e\uff0c\u6765\u81ea\u57fa\u7840\u6570\u636e\u5b57\u5178"),
        ("SCHEDULING_GROUP", "\u6392\u73ed\u7ec4\u522b\u6570\u636e", "\u5458\u5de5\u6392\u73ed\u7ec4\u522b\u57fa\u7840\u6570\u636e\uff0c\u6765\u81ea\u57fa\u7840\u6570\u636e\u5b57\u5178"),
        ("BANK", "\u94f6\u884c\u6570\u636e", "\u5458\u5de5\u6536\u6b3e\u94f6\u884c\u57fa\u7840\u6570\u636e\uff0c\u6765\u81ea\u57fa\u7840\u6570\u636e\u5b57\u5178"),
        ("COUNTRY_REGION", "\u56fd\u5bb6\u5730\u533a\u6570\u636e", "\u56fd\u5bb6\u5730\u533a\u57fa\u7840\u6570\u636e\uff0c\u6765\u81ea\u57fa\u7840\u6570\u636e\u5b57\u5178")
    ];

    /// <summary>
    /// 基础数据模块预置的人资类基础数据选项（用于初始化演示和开箱可用）。
    /// </summary>
    private static readonly (string TypeCode, string ItemCode, string ItemName, int SortOrder)[] HrPresetItems =
    [
        ("POSITION", "GENERAL_MANAGER", "\u603b\u7ecf\u7406", 10),
        ("POSITION", "HR_SPECIALIST", "\u4eba\u4e8b\u4e13\u5458", 20),
        ("POSITION", "ACCOUNTANT", "\u4f1a\u8ba1", 30),

        ("EMPLOYMENT_STATUS", "ON_DUTY", "\u5728\u804c", 10),
        ("EMPLOYMENT_STATUS", "PROBATION", "\u8bd5\u7528", 20),
        ("EMPLOYMENT_STATUS", "LEAVE", "\u79bb\u5c97", 30),
        ("EMPLOYMENT_STATUS", "RESIGNED", "\u79bb\u804c", 40),

        ("ETHNICITY", "HAN", "\u6c49\u65cf", 10),
        ("ETHNICITY", "ZHUANG", "\u58ee\u65cf", 20),
        ("ETHNICITY", "HUI", "\u56de\u65cf", 30),
        ("ETHNICITY", "MAN", "\u6ee1\u65cf", 40),

        ("PROFESSIONAL_TITLE", "JUNIOR", "\u521d\u7ea7", 10),
        ("PROFESSIONAL_TITLE", "INTERMEDIATE", "\u4e2d\u7ea7", 20),
        ("PROFESSIONAL_TITLE", "SENIOR", "\u9ad8\u7ea7", 30),
        ("PROFESSIONAL_TITLE", "PROFESSOR", "\u6b63\u9ad8\u7ea7", 40),

        ("ALLOWANCE_PACKAGE", "JUNIOR", "\u521d\u7ea7", 10),
        ("ALLOWANCE_PACKAGE", "INTERMEDIATE", "\u4e2d\u7ea7", 20),
        ("ALLOWANCE_PACKAGE", "SENIOR", "\u9ad8\u7ea7", 30),

        ("SCHEDULING_GROUP", "DAY_SHIFT", "\u65e5\u73ed\u7ec4", 10),
        ("SCHEDULING_GROUP", "GENERAL", "\u7efc\u5408\u7ec4", 20),
        ("SCHEDULING_GROUP", "ROTATION", "\u8f6e\u73ed\u7ec4", 30),

        ("BANK", "ICBC", "\u4e2d\u56fd\u5de5\u5546\u94f6\u884c", 10),
        ("BANK", "CCB", "\u4e2d\u56fd\u5efa\u8bbe\u94f6\u884c", 20),
        ("BANK", "ABC", "\u4e2d\u56fd\u519c\u4e1a\u94f6\u884c", 30),
        ("BANK", "BOC", "\u4e2d\u56fd\u94f6\u884c", 40),
        ("BANK", "CMB", "\u62db\u5546\u94f6\u884c", 50),

        ("COUNTRY_REGION", "CN", "\u4e2d\u56fd", 10),
        ("COUNTRY_REGION", "HK", "\u4e2d\u56fd\u9999\u6e2f", 20),
        ("COUNTRY_REGION", "MO", "\u4e2d\u56fd\u6fb3\u95e8", 30),
        ("COUNTRY_REGION", "TW", "\u4e2d\u56fd\u53f0\u6e7e", 40),
        ("COUNTRY_REGION", "US", "\u7f8e\u56fd", 50),
        ("COUNTRY_REGION", "JP", "\u65e5\u672c", 60)
    ];

    /// <summary>
    /// 地域及行政区层级映射（地域/城市挂国家地区、县域挂城市）。
    /// </summary>
    private static readonly (string ChildTypeCode, string ChildItemCode, string ParentTypeCode, string ParentItemCode)[] LocationHierarchyMappings =
    [
        ("REGION", "EAST", "COUNTRY_REGION", "CN"),
        ("REGION", "SOUTH", "COUNTRY_REGION", "CN"),
        ("REGION", "NORTH", "COUNTRY_REGION", "CN"),
        ("CITY", "SHANGHAI", "COUNTRY_REGION", "CN"),
        ("CITY", "BEIJING", "COUNTRY_REGION", "CN"),
        ("CITY", "GUANGZHOU", "COUNTRY_REGION", "CN"),
        ("CITY", "SHENZHEN", "COUNTRY_REGION", "CN"),
        ("CITY", "FOSHAN", "COUNTRY_REGION", "CN"),
        ("COUNTY", "PUDONG", "CITY", "SHANGHAI"),
        ("COUNTY", "CHAOYANG", "CITY", "BEIJING"),
        ("COUNTY", "TIANHE", "CITY", "GUANGZHOU"),
        ("COUNTY", "YUEXIU", "CITY", "GUANGZHOU"),
        ("COUNTY", "NANSHAN", "CITY", "SHENZHEN")
    ];

    private static async Task SeedDefaultTypesAsync(SqlConnection conn)
    {
        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM dbo.BD_BasicDataType WHERE TypeCode = @TypeCode)
            BEGIN
                INSERT INTO dbo.BD_BasicDataType
                (
                    TypeCode, TypeName, Description,
                    CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, IsDeleted
                )
                VALUES
                (
                    @TypeCode, @TypeName, @Description,
                    @CreatedAt, @UpdatedAt, @CreatedBy, @UpdatedBy, @IsDeleted
                );
            END
            ELSE
            BEGIN
                UPDATE dbo.BD_BasicDataType
                SET TypeName = @TypeName,
                    Description = @Description,
                    UpdatedAt = @CreatedAt,
                    UpdatedBy = @CreatedBy
                WHERE TypeCode = @TypeCode;
            END;
            """;

        foreach (var item in DefaultTypes)
        {
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TypeCode", item.TypeCode);
            cmd.Parameters.AddWithValue("@TypeName", item.TypeName);
            cmd.Parameters.AddWithValue("@Description", ToDbValue(item.Description));
            cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
            cmd.Parameters.AddWithValue("@UpdatedAt", DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedBy", "system");
            cmd.Parameters.AddWithValue("@UpdatedBy", DBNull.Value);
            cmd.Parameters.AddWithValue("@IsDeleted", false);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static async Task SeedDefaultItemsAsync(SqlConnection conn)
    {
        const string sql = """
            IF NOT EXISTS
            (
                SELECT 1
                FROM dbo.BD_BasicDataItem i
                INNER JOIN dbo.BD_BasicDataType t ON i.TypeId = t.Id
                WHERE t.TypeCode = @TypeCode AND i.ItemCode = @ItemCode
            )
            BEGIN
                INSERT INTO dbo.BD_BasicDataItem
                (
                    TypeId, ItemCode, ItemName, ParentId, SortOrder, IsActive, Remark,
                    CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, IsDeleted
                )
                SELECT
                    t.Id, @ItemCode, @ItemName, NULL, @SortOrder, 1, NULL,
                    @CreatedAt, NULL, @CreatedBy, NULL, 0
                FROM dbo.BD_BasicDataType t
                WHERE t.TypeCode = @TypeCode;
            END;
            """;

        foreach (var item in DefaultItems)
        {
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TypeCode", item.TypeCode);
            cmd.Parameters.AddWithValue("@ItemCode", item.ItemCode);
            cmd.Parameters.AddWithValue("@ItemName", item.ItemName);
            cmd.Parameters.AddWithValue("@SortOrder", item.SortOrder);
            cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
            cmd.Parameters.AddWithValue("@CreatedBy", "system");
            await cmd.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// 鍒濆鍖栧熀纭€鏁版嵁妯″潡鐨勪汉璧勭被鈥滅被鍨嬧€濆瓧鍏搞€?    /// </summary>
    private static async Task SeedHrPresetTypesAsync(SqlConnection conn)
    {
        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM dbo.BD_BasicDataType WHERE TypeCode = @TypeCode)
            BEGIN
                INSERT INTO dbo.BD_BasicDataType
                (
                    TypeCode, TypeName, Description,
                    CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, IsDeleted
                )
                VALUES
                (
                    @TypeCode, @TypeName, @Description,
                    @CreatedAt, @UpdatedAt, @CreatedBy, @UpdatedBy, @IsDeleted
                );
            END;
            """;

        foreach (var item in HrPresetTypes)
        {
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TypeCode", item.TypeCode);
            cmd.Parameters.AddWithValue("@TypeName", item.TypeName);
            cmd.Parameters.AddWithValue("@Description", ToDbValue(item.Description));
            cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
            cmd.Parameters.AddWithValue("@UpdatedAt", DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedBy", "system");
            cmd.Parameters.AddWithValue("@UpdatedBy", DBNull.Value);
            cmd.Parameters.AddWithValue("@IsDeleted", false);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// 鍒濆鍖栧熀纭€鏁版嵁妯″潡鐨勪汉璧勭被鈥滈€夐」鈥濆瓧鍏搞€?    /// </summary>
    private static async Task SeedHrPresetItemsAsync(SqlConnection conn)
    {
        const string sql = """
            IF NOT EXISTS
            (
                SELECT 1
                FROM dbo.BD_BasicDataItem i
                INNER JOIN dbo.BD_BasicDataType t ON i.TypeId = t.Id
                WHERE t.TypeCode = @TypeCode AND i.ItemCode = @ItemCode
            )
            BEGIN
                INSERT INTO dbo.BD_BasicDataItem
                (
                    TypeId, ItemCode, ItemName, ParentId, SortOrder, IsActive, Remark,
                    CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, IsDeleted
                )
                SELECT
                    t.Id, @ItemCode, @ItemName, NULL, @SortOrder, 1, NULL,
                    @CreatedAt, NULL, @CreatedBy, NULL, 0
                FROM dbo.BD_BasicDataType t
                WHERE t.TypeCode = @TypeCode;
            END;
            """;

        foreach (var item in HrPresetItems)
        {
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TypeCode", item.TypeCode);
            cmd.Parameters.AddWithValue("@ItemCode", item.ItemCode);
            cmd.Parameters.AddWithValue("@ItemName", item.ItemName);
            cmd.Parameters.AddWithValue("@SortOrder", item.SortOrder);
            cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
            cmd.Parameters.AddWithValue("@CreatedBy", "system");
            await cmd.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// 修正地域类型命名（将旧“地区/Region”统一改为“地域”）。
    /// </summary>
    private static async Task EnsureRegionTypeNamingAsync(SqlConnection conn)
    {
        const string sql = """
            UPDATE dbo.BD_BasicDataType
            SET
                TypeName = N'地域',
                Description = N'地域基础数据，上一级为国家地区',
                UpdatedAt = SYSDATETIME(),
                UpdatedBy = N'system'
            WHERE TypeCode = N'REGION'
              AND IsDeleted = 0
              AND
              (
                  TypeName <> N'地域'
                  OR ISNULL(Description, N'') <> N'地域基础数据，上一级为国家地区'
              );
            """;

        await ExecuteNonQueryAsync(conn, sql);
    }

    /// <summary>
    /// 修正地域及行政区层级父子关系（确保地域/城市挂国家地区、县域挂城市）。
    /// </summary>
    private static async Task EnsureLocationHierarchyAsync(SqlConnection conn)
    {
        const string sql = """
            UPDATE child
            SET child.ParentId = parent.Id
            FROM dbo.BD_BasicDataItem child
            INNER JOIN dbo.BD_BasicDataType childType ON child.TypeId = childType.Id
            INNER JOIN dbo.BD_BasicDataType parentType ON parentType.TypeCode = @ParentTypeCode
            INNER JOIN dbo.BD_BasicDataItem parent ON parent.TypeId = parentType.Id
            WHERE childType.TypeCode = @ChildTypeCode
              AND child.ItemCode = @ChildItemCode
              AND parent.ItemCode = @ParentItemCode
              AND child.IsDeleted = 0
              AND parent.IsDeleted = 0
              AND (child.ParentId IS NULL OR child.ParentId <> parent.Id);
            """;

        foreach (var mapping in LocationHierarchyMappings)
        {
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ChildTypeCode", mapping.ChildTypeCode);
            cmd.Parameters.AddWithValue("@ChildItemCode", mapping.ChildItemCode);
            cmd.Parameters.AddWithValue("@ParentTypeCode", mapping.ParentTypeCode);
            cmd.Parameters.AddWithValue("@ParentItemCode", mapping.ParentItemCode);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}




