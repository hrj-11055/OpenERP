using Microsoft.Data.SqlClient;
using OpenERP.HR.Models.Entities;
using System.Data;

namespace OpenERP.Web.Data.HR;

public class HrSqlRepository : IHrRepository
{
    /// <summary>
    /// 公司组织表名（人资模块，遵循 HR_ 实体命名规则）。
    /// </summary>
    private const string CompanyOrganizationTableName = "HR_CompanyOrganization";

    /// <summary>
    /// 员工资料表名（人资模块员工主数据表）。
    /// </summary>
    private const string EmployeeTableName = "HR_Employee";

    /// <summary>
    /// 部门资料表名（人资模块部门主数据表）。
    /// </summary>
    private const string DepartmentTableName = "HR_Department";

    /// <summary>
    /// 职位资料表名（HR 模块职位主数据表）。
    /// </summary>
    private const string PositionTableName = "HR_Position";

    /// <summary>
    /// 旧版员工资料表名（兼容历史结构迁移）。
    /// </summary>
    private const string LegacyEmployeeTableName = "Employees";

    /// <summary>
    /// 旧版部门资料表名（兼容历史结构迁移）。
    /// </summary>
    private const string LegacyDepartmentTableName = "Departments";

    /// <summary>
    /// 旧版职位资料表名（兼容历史结构迁移）。
    /// </summary>
    private const string LegacyPositionTableName = "Positions";

    /// <summary>
    /// 角色表名（权限角色定义）。
    /// </summary>
    private const string RoleTableName = "HR_Role";

    /// <summary>
    /// 功能权限表名（系统功能模块访问权限定义）。
    /// </summary>
    private const string PermissionTableName = "HR_Permission";

    /// <summary>
    /// 角色权限关联表名（角色与权限多对多关系）。
    /// </summary>
    private const string RolePermissionTableName = "HR_RolePermission";

    /// <summary>
    /// 用户权限关联表名（用户个人权限覆盖）。
    /// </summary>
    private const string UserPermissionTableName = "HR_UserPermission";

    /// <summary>
    /// 用户管辖公司关联表名（用户可登录的公司组织）。
    /// </summary>
    private const string UserCompanyTableName = "HR_UserCompany";

    /// <summary>
    /// 通用任务跟进表名（共享功能表，按 FeatureCode + EntityId 关联业务资料）。
    /// </summary>
    private const string TaskFollowUpTableName = "SYS_TaskFollowUp";

    /// <summary>
    /// 员工培训历程表名（记录员工工作经历、培训经历与教育经历）。
    /// </summary>
    private const string EmployeeTrainingExperienceTableName = "HR_EmployeeTrainingExperience";

    /// <summary>
    /// 公司组织银行账号表名（公司组织账务信息中的银行账号明细表）。
    /// </summary>
    private const string CompanyOrganizationBankAccountTableName = "HR_CompanyOrganizationBankAccount";

    /// <summary>
    /// 公司组织单号规则表名（按公司维护各单据功能的编号生成规则）。
    /// </summary>
    private const string CompanyOrganizationDocumentNumberRuleTableName = "HR_CompanyOrganizationDocumentNumberRule";

    /// <summary>
    /// 公司组织单号规则定义列表（描述当前系统已接入的单据功能默认规则）。
    /// </summary>
    private static readonly IReadOnlyList<CompanyOrganizationDocumentNumberRuleDefinition> DocumentNumberRuleDefinitions =
    [
        new()
        {
            DocumentTypeCode = "SALES_ORDER",
            DocumentTypeName = "销售订单",
            DefaultPrefix = "SO",
            DefaultDateFormatCode = "yyMM",
            DefaultSequenceLength = 5
        },
        new()
        {
            DocumentTypeCode = "PURCHASE_ORDER",
            DocumentTypeName = "采购订单",
            DefaultPrefix = "PO",
            DefaultDateFormatCode = "yyMM",
            DefaultSequenceLength = 5
        },
        new()
        {
            DocumentTypeCode = "PRODUCTION_ORDER",
            DocumentTypeName = "生产工单",
            DefaultPrefix = "MO",
            DefaultDateFormatCode = "yyMM",
            DefaultSequenceLength = 5
        }
    ];

    private readonly string _connectionString;

    public HrSqlRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Missing connection string: DefaultConnection");
    }

    /// <summary>
    /// 初始化人资模块表结构（包含公司组织表与员工资料表）。
    /// </summary>
    public async Task InitializeAsync()
    {
        await using var conn = await OpenConnectionAsync();
        await ExecuteNonQueryAsync(conn, BuildCompanyOrganizationInitializeSql());
        await ExecuteNonQueryAsync(conn, BuildCompanyOrganizationBankAccountInitializeSql());
        await ExecuteNonQueryAsync(conn, BuildCompanyOrganizationDocumentNumberRuleInitializeSql());
        await ExecuteNonQueryAsync(conn, BuildDepartmentInitializeSql());
        await ExecuteNonQueryAsync(conn, BuildPositionInitializeSql());
        await ExecuteNonQueryAsync(conn, BuildEmployeeInitializeSql());
        await ExecuteNonQueryAsync(conn, BuildDepartmentMigrationSql());
        await ExecuteNonQueryAsync(conn, BuildPositionMigrationSql());
        await ExecuteNonQueryAsync(conn, BuildEmployeeMigrationSql());
        await SeedCompanyOrganizationsAsync(conn);
        await SeedEmployeesAsync(conn);
        await ExecuteNonQueryAsync(conn, BuildEmployeeCredentialDefaultSql());
        await ExecuteNonQueryAsync(conn, BuildRoleInitializeSql());
        await ExecuteNonQueryAsync(conn, BuildPermissionInitializeSql());
        await ExecuteNonQueryAsync(conn, BuildRolePermissionInitializeSql());
        await ExecuteNonQueryAsync(conn, BuildUserPermissionInitializeSql());
        await ExecuteNonQueryAsync(conn, BuildUserCompanyInitializeSql());
        await ExecuteNonQueryAsync(conn, BuildTaskFollowUpInitializeSql());
        await ExecuteNonQueryAsync(conn, BuildEmployeeTrainingExperienceInitializeSql());
        await SeedRolesAsync(conn);
        await SeedPermissionsAsync(conn);
        await SeedRolePermissionsAsync(conn);
        await EnsureLocationDictionaryCompatibilityAsync(conn);
    }

    /// <summary>
    /// 获取公司组织列表（用于公司组织页面展示）。
    /// </summary>
    public async Task<List<CompanyOrganization>> GetCompanyOrganizationsAsync()
    {
        var sql = $"""
            {BuildCompanyOrganizationSelectSql()}
            WHERE IsDeleted = 0
            ORDER BY ISNULL(UpdatedAt, CreatedAt) DESC, Id DESC;
            """;

        var results = new List<CompanyOrganization>();
        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            results.Add(MapCompanyOrganization(reader));
        }

        return results;
    }

    /// <summary>
    /// 按公司组织ID获取公司组织资料。
    /// </summary>
    public async Task<CompanyOrganization?> GetCompanyOrganizationByIdAsync(int id)
    {
        var sql = $"""
            {BuildCompanyOrganizationSelectSql()}
            WHERE Id = @Id AND IsDeleted = 0;
            """;

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@Id", id));
        await using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return MapCompanyOrganization(reader);
    }

    /// <summary>
    /// 按组织编码获取公司组织资料（供按当前登录公司生成单号时解析所属组织）。
    /// </summary>
    public async Task<CompanyOrganization?> GetCompanyOrganizationByCodeAsync(string organizationCode)
    {
        var normalizedCode = organizationCode?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            return null;
        }

        var sql = $"""
            {BuildCompanyOrganizationSelectSql()}
            WHERE OrganizationCode = @OrganizationCode AND IsDeleted = 0;
            """;

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@OrganizationCode", normalizedCode));
        await using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return MapCompanyOrganization(reader);
    }

    /// <summary>
    /// 新增公司组织资料。
    /// </summary>
    public async Task<int> CreateCompanyOrganizationAsync(CompanyOrganization organization)
    {
        var sql = $"""
            INSERT INTO dbo.{CompanyOrganizationTableName}
                (
                    OrganizationCode,
                    OrganizationName,
                    CompanyNatureId,
                    StatusId,
                    EnterpriseTypeId,
                    BusinessRegistrationNumber,
                    BusinessRegistrationExpiryDate,
                    RegionId,
                    CityId,
                    CountyId,
                    Address,
                    Principal,
                    Phone,
                    Fax,
                    Email,
                    Website,
                    WeeklyWorkDays,
                    LeaveCountBasisType,
                    HolidayType,
                    AnnualLeaveCalculationMonthDay,
                    AnnualLeaveGrantRule,
                    AnnualLeaveGrantMonthDay,
                    IsAnnualLeaveClearEnabled,
                    AnnualLeaveClearMonthDay,
                    IsCarryForwardAnnualLeaveAllowed,
                    BaseAnnualLeaveDays,
                    AnnualLeaveIncrementStartYears,
                    AnnualLeaveIncrementPerYearDays,
                    AnnualLeaveCapDays,
                    AnnualLeaveMaxAccumulatedDays,
                    PaidSickLeaveDaysPerYear,
                    PaidSickLeaveSalaryRatio,
                    PaidSickLeaveCalculationMonthDay,
                    IsPaidSickLeaveClearEnabled,
                    PaidSickLeaveClearMonthDay,
                    EmployeeMpfMinimumSalary,
                    Remarks,
                    ArchivePath,
                    PrintHeaderContent,
                    PrintFooterContent,
                    CreatedAt,
                    UpdatedAt,
                    CreatedBy,
                    UpdatedBy,
                    IsDeleted
                )
            OUTPUT INSERTED.Id
            VALUES
                (
                    @OrganizationCode,
                    @OrganizationName,
                    @CompanyNatureId,
                    @StatusId,
                    @EnterpriseTypeId,
                    @BusinessRegistrationNumber,
                    @BusinessRegistrationExpiryDate,
                    @RegionId,
                    @CityId,
                    @CountyId,
                    @Address,
                    @Principal,
                    @Phone,
                    @Fax,
                    @Email,
                    @Website,
                    @WeeklyWorkDays,
                    @LeaveCountBasisType,
                    @HolidayType,
                    @AnnualLeaveCalculationMonthDay,
                    @AnnualLeaveGrantRule,
                    @AnnualLeaveGrantMonthDay,
                    @IsAnnualLeaveClearEnabled,
                    @AnnualLeaveClearMonthDay,
                    @IsCarryForwardAnnualLeaveAllowed,
                    @BaseAnnualLeaveDays,
                    @AnnualLeaveIncrementStartYears,
                    @AnnualLeaveIncrementPerYearDays,
                    @AnnualLeaveCapDays,
                    @AnnualLeaveMaxAccumulatedDays,
                    @PaidSickLeaveDaysPerYear,
                    @PaidSickLeaveSalaryRatio,
                    @PaidSickLeaveCalculationMonthDay,
                    @IsPaidSickLeaveClearEnabled,
                    @PaidSickLeaveClearMonthDay,
                    @EmployeeMpfMinimumSalary,
                    @Remarks,
                    @ArchivePath,
                    @PrintHeaderContent,
                    @PrintFooterContent,
                    @CreatedAt,
                    @UpdatedAt,
                    @CreatedBy,
                    @UpdatedBy,
                    @IsDeleted
                );
            """;

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        BindCompanyOrganizationParams(cmd, organization);
        cmd.Parameters.Add(new SqlParameter("@CreatedAt", DateTime.Now));
        cmd.Parameters.Add(new SqlParameter("@UpdatedAt", DBNull.Value));
        cmd.Parameters.Add(new SqlParameter("@CreatedBy", ToDbValue(organization.CreatedBy)));
        cmd.Parameters.Add(new SqlParameter("@UpdatedBy", DBNull.Value));
        cmd.Parameters.Add(new SqlParameter("@IsDeleted", false));

        var insertedId = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(insertedId);
    }

    /// <summary>
    /// 更新公司组织资料。
    /// </summary>
    public async Task<bool> UpdateCompanyOrganizationAsync(CompanyOrganization organization)
    {
        var sql = $"""
            UPDATE dbo.{CompanyOrganizationTableName}
            SET
                OrganizationCode = @OrganizationCode,
                OrganizationName = @OrganizationName,
                CompanyNatureId = @CompanyNatureId,
                StatusId = @StatusId,
                EnterpriseTypeId = @EnterpriseTypeId,
                BusinessRegistrationNumber = @BusinessRegistrationNumber,
                BusinessRegistrationExpiryDate = @BusinessRegistrationExpiryDate,
                RegionId = @RegionId,
                CityId = @CityId,
                CountyId = @CountyId,
                Address = @Address,
                Principal = @Principal,
                Phone = @Phone,
                Fax = @Fax,
                Email = @Email,
                Website = @Website,
                WeeklyWorkDays = @WeeklyWorkDays,
                LeaveCountBasisType = @LeaveCountBasisType,
                HolidayType = @HolidayType,
                AnnualLeaveCalculationMonthDay = @AnnualLeaveCalculationMonthDay,
                AnnualLeaveGrantRule = @AnnualLeaveGrantRule,
                AnnualLeaveGrantMonthDay = @AnnualLeaveGrantMonthDay,
                IsAnnualLeaveClearEnabled = @IsAnnualLeaveClearEnabled,
                AnnualLeaveClearMonthDay = @AnnualLeaveClearMonthDay,
                IsCarryForwardAnnualLeaveAllowed = @IsCarryForwardAnnualLeaveAllowed,
                BaseAnnualLeaveDays = @BaseAnnualLeaveDays,
                AnnualLeaveIncrementStartYears = @AnnualLeaveIncrementStartYears,
                AnnualLeaveIncrementPerYearDays = @AnnualLeaveIncrementPerYearDays,
                AnnualLeaveCapDays = @AnnualLeaveCapDays,
                AnnualLeaveMaxAccumulatedDays = @AnnualLeaveMaxAccumulatedDays,
                PaidSickLeaveDaysPerYear = @PaidSickLeaveDaysPerYear,
                PaidSickLeaveSalaryRatio = @PaidSickLeaveSalaryRatio,
                PaidSickLeaveCalculationMonthDay = @PaidSickLeaveCalculationMonthDay,
                IsPaidSickLeaveClearEnabled = @IsPaidSickLeaveClearEnabled,
                PaidSickLeaveClearMonthDay = @PaidSickLeaveClearMonthDay,
                EmployeeMpfMinimumSalary = @EmployeeMpfMinimumSalary,
                Remarks = @Remarks,
                ArchivePath = @ArchivePath,
                PrintHeaderContent = @PrintHeaderContent,
                PrintFooterContent = @PrintFooterContent,
                UpdatedAt = @UpdatedAt,
                UpdatedBy = @UpdatedBy
            WHERE Id = @Id AND IsDeleted = 0;
            """;

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@Id", organization.Id));
        BindCompanyOrganizationParams(cmd, organization);
        cmd.Parameters.Add(new SqlParameter("@UpdatedAt", DateTime.Now));
        cmd.Parameters.Add(new SqlParameter("@UpdatedBy", ToDbValue(organization.UpdatedBy)));

        var affected = await cmd.ExecuteNonQueryAsync();
        return affected > 0;
    }

    /// <summary>
    /// 删除公司组织资料。
    /// </summary>
    public async Task<bool> DeleteCompanyOrganizationAsync(int id)
    {
        var sql = $"""
            UPDATE dbo.{CompanyOrganizationTableName}
            SET
                IsDeleted = 1,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id AND IsDeleted = 0;
            """;

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@Id", id));
        cmd.Parameters.Add(new SqlParameter("@UpdatedAt", DateTime.Now));

        var affected = await cmd.ExecuteNonQueryAsync();
        return affected > 0;
    }

    public async Task<CompanyOrganizationBankAccountPageResult> GetCompanyOrganizationBankAccountsAsync(
        int companyOrganizationId,
        string? keyword,
        string? statusCode,
        int pageNumber,
        int pageSize)
    {
        var normalizedKeyword = keyword?.Trim();
        var normalizedStatusCode = string.IsNullOrWhiteSpace(statusCode) ? null : statusCode.Trim().ToUpperInvariant();
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize <= 0 ? 10 : pageSize;
        var offset = (safePageNumber - 1) * safePageSize;

        var countSql = $"""
            SELECT COUNT(1)
            FROM dbo.{CompanyOrganizationBankAccountTableName} ba
            LEFT JOIN dbo.BD_BasicDataItem bankItem ON ba.BankId = bankItem.Id AND bankItem.IsDeleted = 0
            WHERE ba.CompanyOrganizationId = @CompanyOrganizationId
              AND ba.IsDeleted = 0
              AND (@StatusCode IS NULL OR ba.StatusCode = @StatusCode)
              AND (
                    @Keyword IS NULL
                    OR ba.AccountNumber LIKE '%' + @Keyword + '%'
                    OR bankItem.ItemName LIKE '%' + @Keyword + '%'
                    OR ba.BranchName LIKE '%' + @Keyword + '%'
                    OR ba.SubjectCode LIKE '%' + @Keyword + '%'
                    OR ba.SubjectName LIKE '%' + @Keyword + '%'
                    OR ba.Remarks LIKE '%' + @Keyword + '%'
                  );
            """;

        var dataSql = $"""
            SELECT
                ba.Id,
                ba.CompanyOrganizationId,
                ba.AccountNumber,
                ba.BankId,
                bankItem.ItemName AS BankName,
                ba.BranchName,
                ba.BranchAddress,
                ba.CurrencyCode,
                ba.StatusCode,
                ba.SubjectCode,
                ba.SubjectName,
                ba.Remarks,
                ba.IsDefault,
                ba.CreatedAt,
                ba.UpdatedAt,
                ba.CreatedBy,
                ba.UpdatedBy,
                ba.IsDeleted
            FROM dbo.{CompanyOrganizationBankAccountTableName} ba
            LEFT JOIN dbo.BD_BasicDataItem bankItem ON ba.BankId = bankItem.Id AND bankItem.IsDeleted = 0
            WHERE ba.CompanyOrganizationId = @CompanyOrganizationId
              AND ba.IsDeleted = 0
              AND (@StatusCode IS NULL OR ba.StatusCode = @StatusCode)
              AND (
                    @Keyword IS NULL
                    OR ba.AccountNumber LIKE '%' + @Keyword + '%'
                    OR bankItem.ItemName LIKE '%' + @Keyword + '%'
                    OR ba.BranchName LIKE '%' + @Keyword + '%'
                    OR ba.SubjectCode LIKE '%' + @Keyword + '%'
                    OR ba.SubjectName LIKE '%' + @Keyword + '%'
                    OR ba.Remarks LIKE '%' + @Keyword + '%'
                  )
            ORDER BY ba.IsDefault DESC, ISNULL(ba.UpdatedAt, ba.CreatedAt) DESC, ba.Id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        await using var conn = await OpenConnectionAsync();

        await using var countCmd = new SqlCommand(countSql, conn);
        BindCompanyOrganizationBankAccountQueryParams(countCmd, companyOrganizationId, normalizedKeyword, normalizedStatusCode);
        var totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        var items = new List<CompanyOrganizationBankAccount>();
        await using var dataCmd = new SqlCommand(dataSql, conn);
        BindCompanyOrganizationBankAccountQueryParams(dataCmd, companyOrganizationId, normalizedKeyword, normalizedStatusCode);
        dataCmd.Parameters.AddWithValue("@Offset", offset);
        dataCmd.Parameters.AddWithValue("@PageSize", safePageSize);

        await using var reader = await dataCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(MapCompanyOrganizationBankAccount(reader));
        }

        return new CompanyOrganizationBankAccountPageResult
        {
            PageNumber = safePageNumber,
            PageSize = safePageSize,
            TotalCount = totalCount,
            Items = items
        };
    }

    public async Task<int> SaveCompanyOrganizationBankAccountAsync(CompanyOrganizationBankAccount bankAccount)
    {
        await using var conn = await OpenConnectionAsync();
        await using var tx = await conn.BeginTransactionAsync();

        try
        {
            if (bankAccount.IsDefault)
            {
                await using var resetCmd = new SqlCommand(
                    $"""
                    UPDATE dbo.{CompanyOrganizationBankAccountTableName}
                    SET IsDefault = 0, UpdatedAt = @UpdatedAt, UpdatedBy = @UpdatedBy
                    WHERE CompanyOrganizationId = @CompanyOrganizationId
                      AND IsDeleted = 0
                      AND Id <> @Id;
                    """,
                    conn,
                    (SqlTransaction)tx);

                resetCmd.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
                resetCmd.Parameters.AddWithValue("@UpdatedBy", ToDbValue(bankAccount.UpdatedBy ?? bankAccount.CreatedBy));
                resetCmd.Parameters.AddWithValue("@CompanyOrganizationId", bankAccount.CompanyOrganizationId);
                resetCmd.Parameters.AddWithValue("@Id", bankAccount.Id);
                await resetCmd.ExecuteNonQueryAsync();
            }

            if (bankAccount.Id > 0)
            {
                var updateSql = $"""
                    UPDATE dbo.{CompanyOrganizationBankAccountTableName}
                    SET
                        AccountNumber = @AccountNumber,
                        BankId = @BankId,
                        BranchName = @BranchName,
                        BranchAddress = @BranchAddress,
                        CurrencyCode = @CurrencyCode,
                        StatusCode = @StatusCode,
                        SubjectCode = @SubjectCode,
                        SubjectName = @SubjectName,
                        Remarks = @Remarks,
                        IsDefault = @IsDefault,
                        UpdatedAt = @UpdatedAt,
                        UpdatedBy = @UpdatedBy
                    WHERE Id = @Id
                      AND CompanyOrganizationId = @CompanyOrganizationId
                      AND IsDeleted = 0;
                    """;

                await using var updateCmd = new SqlCommand(updateSql, conn, (SqlTransaction)tx);
                updateCmd.Parameters.AddWithValue("@Id", bankAccount.Id);
                BindCompanyOrganizationBankAccountParams(updateCmd, bankAccount);
                updateCmd.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
                updateCmd.Parameters.AddWithValue("@UpdatedBy", ToDbValue(bankAccount.UpdatedBy));
                var affected = await updateCmd.ExecuteNonQueryAsync();
                if (affected == 0)
                {
                    await tx.RollbackAsync();
                    return 0;
                }
                await tx.CommitAsync();
                return bankAccount.Id;
            }

            var insertSql = $"""
                INSERT INTO dbo.{CompanyOrganizationBankAccountTableName}
                (
                    CompanyOrganizationId,
                    AccountNumber,
                    BankId,
                    BranchName,
                    BranchAddress,
                    CurrencyCode,
                    StatusCode,
                    SubjectCode,
                    SubjectName,
                    Remarks,
                    IsDefault,
                    CreatedAt,
                    UpdatedAt,
                    CreatedBy,
                    UpdatedBy,
                    IsDeleted
                )
                OUTPUT INSERTED.Id
                VALUES
                (
                    @CompanyOrganizationId,
                    @AccountNumber,
                    @BankId,
                    @BranchName,
                    @BranchAddress,
                    @CurrencyCode,
                    @StatusCode,
                    @SubjectCode,
                    @SubjectName,
                    @Remarks,
                    @IsDefault,
                    @CreatedAt,
                    @UpdatedAt,
                    @CreatedBy,
                    @UpdatedBy,
                    @IsDeleted
                );
                """;

            await using var insertCmd = new SqlCommand(insertSql, conn, (SqlTransaction)tx);
            BindCompanyOrganizationBankAccountParams(insertCmd, bankAccount);
            insertCmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
            insertCmd.Parameters.AddWithValue("@UpdatedAt", DBNull.Value);
            insertCmd.Parameters.AddWithValue("@CreatedBy", ToDbValue(bankAccount.CreatedBy));
            insertCmd.Parameters.AddWithValue("@UpdatedBy", DBNull.Value);
            insertCmd.Parameters.AddWithValue("@IsDeleted", false);

            var insertedId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());
            await tx.CommitAsync();
            return insertedId;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<int> DeleteCompanyOrganizationBankAccountsAsync(int companyOrganizationId, List<int> ids)
    {
        if (ids.Count == 0)
        {
            return 0;
        }

        var parameterNames = ids.Select((_, index) => $"@Id{index}").ToList();
        var sql = $"""
            UPDATE dbo.{CompanyOrganizationBankAccountTableName}
            SET
                IsDeleted = 1,
                UpdatedAt = @UpdatedAt
            WHERE CompanyOrganizationId = @CompanyOrganizationId
              AND IsDeleted = 0
              AND Id IN ({string.Join(", ", parameterNames)});
            """;

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@CompanyOrganizationId", companyOrganizationId));
        cmd.Parameters.Add(new SqlParameter("@UpdatedAt", DateTime.Now));

        for (var index = 0; index < ids.Count; index++)
        {
            cmd.Parameters.Add(new SqlParameter(parameterNames[index], ids[index]));
        }

        return await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// 查询公司组织单号规则列表（返回当前组织已保存的规则记录）。
    /// </summary>
    public async Task<List<CompanyOrganizationDocumentNumberRule>> GetCompanyOrganizationDocumentNumberRulesAsync(int companyOrganizationId)
    {
        var sql = $"""
            SELECT
                Id,
                CompanyOrganizationId,
                DocumentTypeCode,
                Prefix,
                DateFormatCode,
                SequenceLength,
                LastDateSegment,
                LastSequenceValue,
                LastGeneratedNumber,
                CreatedAt,
                UpdatedAt,
                CreatedBy,
                UpdatedBy,
                IsDeleted
            FROM dbo.{CompanyOrganizationDocumentNumberRuleTableName}
            WHERE CompanyOrganizationId = @CompanyOrganizationId
              AND IsDeleted = 0
            ORDER BY DocumentTypeCode;
            """;

        var results = new List<CompanyOrganizationDocumentNumberRule>();
        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@CompanyOrganizationId", companyOrganizationId));
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            results.Add(MapCompanyOrganizationDocumentNumberRule(reader));
        }

        return results;
    }

    /// <summary>
    /// 保存公司组织单号规则列表（按单据功能逐条新增或更新）。
    /// </summary>
    public async Task<int> SaveCompanyOrganizationDocumentNumberRulesAsync(
        int companyOrganizationId,
        IReadOnlyCollection<CompanyOrganizationDocumentNumberRule> rules)
    {
        if (companyOrganizationId <= 0 || rules.Count == 0)
        {
            return 0;
        }

        await using var conn = await OpenConnectionAsync();
        await using var tx = await conn.BeginTransactionAsync();
        var now = DateTime.Now;
        var affected = 0;

        try
        {
            foreach (var rule in rules)
            {
                var normalizedRule = NormalizeDocumentNumberRule(companyOrganizationId, rule);
                var updateSql = $"""
                    UPDATE dbo.{CompanyOrganizationDocumentNumberRuleTableName}
                    SET
                        Prefix = @Prefix,
                        DateFormatCode = @DateFormatCode,
                        SequenceLength = @SequenceLength,
                        IsDeleted = 0,
                        UpdatedAt = @UpdatedAt,
                        UpdatedBy = @UpdatedBy
                    WHERE CompanyOrganizationId = @CompanyOrganizationId
                      AND DocumentTypeCode = @DocumentTypeCode;
                    """;

                await using var updateCmd = new SqlCommand(updateSql, conn, (SqlTransaction)tx);
                BindCompanyOrganizationDocumentNumberRuleParams(updateCmd, normalizedRule);
                updateCmd.Parameters.AddWithValue("@UpdatedAt", now);
                updateCmd.Parameters.AddWithValue("@UpdatedBy", ToDbValue(normalizedRule.UpdatedBy ?? normalizedRule.CreatedBy));
                var updateCount = await updateCmd.ExecuteNonQueryAsync();
                if (updateCount > 0)
                {
                    affected += updateCount;
                    continue;
                }

                var insertSql = $"""
                    INSERT INTO dbo.{CompanyOrganizationDocumentNumberRuleTableName}
                    (
                        CompanyOrganizationId,
                        DocumentTypeCode,
                        Prefix,
                        DateFormatCode,
                        SequenceLength,
                        LastDateSegment,
                        LastSequenceValue,
                        LastGeneratedNumber,
                        CreatedAt,
                        UpdatedAt,
                        CreatedBy,
                        UpdatedBy,
                        IsDeleted
                    )
                    VALUES
                    (
                        @CompanyOrganizationId,
                        @DocumentTypeCode,
                        @Prefix,
                        @DateFormatCode,
                        @SequenceLength,
                        @LastDateSegment,
                        @LastSequenceValue,
                        @LastGeneratedNumber,
                        @CreatedAt,
                        @UpdatedAt,
                        @CreatedBy,
                        @UpdatedBy,
                        @IsDeleted
                    );
                    """;

                await using var insertCmd = new SqlCommand(insertSql, conn, (SqlTransaction)tx);
                BindCompanyOrganizationDocumentNumberRuleParams(insertCmd, normalizedRule);
                insertCmd.Parameters.AddWithValue("@CreatedAt", now);
                insertCmd.Parameters.AddWithValue("@UpdatedAt", DBNull.Value);
                insertCmd.Parameters.AddWithValue("@CreatedBy", ToDbValue(normalizedRule.CreatedBy));
                insertCmd.Parameters.AddWithValue("@UpdatedBy", DBNull.Value);
                insertCmd.Parameters.AddWithValue("@IsDeleted", false);
                affected += await insertCmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
            return affected;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// 按公司组织与单据功能生成下一个业务单号（自动处理日期变更后的流水重置）。
    /// </summary>
    public async Task<string> GenerateDocumentNumberAsync(int companyOrganizationId, string documentTypeCode, DateTime? businessDate = null)
    {
        if (companyOrganizationId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(companyOrganizationId), "Company organization id must be greater than zero.");
        }

        var normalizedDocumentTypeCode = NormalizeDocumentTypeCode(documentTypeCode);
        var targetDate = (businessDate ?? DateTime.Today).Date;

        await using var conn = await OpenConnectionAsync();
        await using var tx = await conn.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var ensureRule = BuildDefaultCompanyOrganizationDocumentNumberRule(companyOrganizationId, normalizedDocumentTypeCode);
            var ensureSql = $"""
                IF NOT EXISTS
                (
                    SELECT 1
                    FROM dbo.{CompanyOrganizationDocumentNumberRuleTableName}
                    WHERE CompanyOrganizationId = @CompanyOrganizationId
                      AND DocumentTypeCode = @DocumentTypeCode
                )
                BEGIN
                    INSERT INTO dbo.{CompanyOrganizationDocumentNumberRuleTableName}
                    (
                        CompanyOrganizationId,
                        DocumentTypeCode,
                        Prefix,
                        DateFormatCode,
                        SequenceLength,
                        LastDateSegment,
                        LastSequenceValue,
                        LastGeneratedNumber,
                        CreatedAt,
                        UpdatedAt,
                        CreatedBy,
                        UpdatedBy,
                        IsDeleted
                    )
                    VALUES
                    (
                        @CompanyOrganizationId,
                        @DocumentTypeCode,
                        @Prefix,
                        @DateFormatCode,
                        @SequenceLength,
                        NULL,
                        0,
                        NULL,
                        @CreatedAt,
                        NULL,
                        @CreatedBy,
                        NULL,
                        0
                    );
                END;
                """;

            await using (var ensureCmd = new SqlCommand(ensureSql, conn, (SqlTransaction)tx))
            {
                BindCompanyOrganizationDocumentNumberRuleParams(ensureCmd, ensureRule);
                ensureCmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                ensureCmd.Parameters.AddWithValue("@CreatedBy", ToDbValue("system"));
                await ensureCmd.ExecuteNonQueryAsync();
            }

            var selectSql = $"""
                SELECT
                    Id,
                    CompanyOrganizationId,
                    DocumentTypeCode,
                    Prefix,
                    DateFormatCode,
                    SequenceLength,
                    LastDateSegment,
                    LastSequenceValue,
                    LastGeneratedNumber,
                    CreatedAt,
                    UpdatedAt,
                    CreatedBy,
                    UpdatedBy,
                    IsDeleted
                FROM dbo.{CompanyOrganizationDocumentNumberRuleTableName} WITH (UPDLOCK, HOLDLOCK)
                WHERE CompanyOrganizationId = @CompanyOrganizationId
                  AND DocumentTypeCode = @DocumentTypeCode
                  AND IsDeleted = 0;
                """;

            CompanyOrganizationDocumentNumberRule rule;
            await using (var selectCmd = new SqlCommand(selectSql, conn, (SqlTransaction)tx))
            {
                selectCmd.Parameters.AddWithValue("@CompanyOrganizationId", companyOrganizationId);
                selectCmd.Parameters.AddWithValue("@DocumentTypeCode", normalizedDocumentTypeCode);
                await using var reader = await selectCmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    throw new InvalidOperationException($"Document number rule for {normalizedDocumentTypeCode} was not found.");
                }

                rule = MapCompanyOrganizationDocumentNumberRule(reader);
            }

            var dateSegment = FormatDocumentNumberDateSegment(rule.DateFormatCode, targetDate);
            var nextSequenceValue = string.Equals(rule.LastDateSegment, dateSegment, StringComparison.Ordinal)
                ? rule.LastSequenceValue + 1
                : 1;
            var generatedNumber = BuildDocumentNumber(rule.Prefix, dateSegment, nextSequenceValue, rule.SequenceLength);

            var updateSql = $"""
                UPDATE dbo.{CompanyOrganizationDocumentNumberRuleTableName}
                SET
                    LastDateSegment = @LastDateSegment,
                    LastSequenceValue = @LastSequenceValue,
                    LastGeneratedNumber = @LastGeneratedNumber,
                    UpdatedAt = @UpdatedAt,
                    UpdatedBy = @UpdatedBy
                WHERE Id = @Id;
                """;

            await using (var updateCmd = new SqlCommand(updateSql, conn, (SqlTransaction)tx))
            {
                updateCmd.Parameters.AddWithValue("@Id", rule.Id);
                updateCmd.Parameters.AddWithValue("@LastDateSegment", ToDbValue(dateSegment));
                updateCmd.Parameters.AddWithValue("@LastSequenceValue", nextSequenceValue);
                updateCmd.Parameters.AddWithValue("@LastGeneratedNumber", generatedNumber);
                updateCmd.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
                updateCmd.Parameters.AddWithValue("@UpdatedBy", ToDbValue("system"));
                await updateCmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
            return generatedNumber;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<List<Department>> GetDepartmentsAsync()
    {
        var sql = $"""
            SELECT Id, Name, Description, CreatedAt, UpdatedAt, IsDeleted
            FROM dbo.{DepartmentTableName}
            WHERE IsDeleted = 0
            ORDER BY Id DESC;
            """;

        var results = new List<Department>();
        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            results.Add(new Department
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = ReadNullableString(reader, 2),
                CreatedAt = reader.GetDateTime(3),
                UpdatedAt = ReadNullableDateTime(reader, 4),
                IsDeleted = reader.GetBoolean(5)
            });
        }

        return results;
    }

    /// <summary>
    /// 获取职位列表（来自 HR 职位主数据表）。
    /// </summary>
    public async Task<List<Position>> GetPositionsAsync()
    {
        var sql = $"""
            SELECT Id, Name, Description, CreatedAt, UpdatedAt, IsDeleted
            FROM dbo.{PositionTableName}
            WHERE IsDeleted = 0
            ORDER BY Name;
            """;

        var results = new List<Position>();
        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            results.Add(new Position
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = ReadNullableString(reader, 2),
                CreatedAt = reader.GetDateTime(3),
                UpdatedAt = ReadNullableDateTime(reader, 4),
                IsDeleted = reader.GetBoolean(5)
            });
        }

        return results;
    }

    /// <summary>
    /// 按职位ID获取职位数据（对应 HR_Position 主数据记录）。
    /// </summary>
    public async Task<Position?> GetPositionByIdAsync(int id)
    {
        var sql = $"""
            SELECT Id, Name, Description, CreatedAt, UpdatedAt, IsDeleted
            FROM dbo.{PositionTableName}
            WHERE Id = @Id AND IsDeleted = 0;
            """;

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@Id", id));
        await using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new Position
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            Description = ReadNullableString(reader, 2),
            CreatedAt = reader.GetDateTime(3),
            UpdatedAt = ReadNullableDateTime(reader, 4),
            IsDeleted = reader.GetBoolean(5)
        };
    }

    /// <summary>
    /// 新增职位数据（写入 HR_Position 表并返回新主键）。
    /// </summary>
    public async Task<int> CreatePositionAsync(Position position)
    {
        var sql = $"""
            INSERT INTO dbo.{PositionTableName}
            (
                Name, Description, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, IsDeleted
            )
            VALUES
            (
                @Name, @Description, @CreatedAt, @UpdatedAt, @CreatedBy, @UpdatedBy, @IsDeleted
            );

            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@Name", position.Name.Trim()));
        cmd.Parameters.Add(new SqlParameter("@Description", ToDbValue(position.Description)));
        cmd.Parameters.Add(new SqlParameter("@CreatedAt", DateTime.Now));
        cmd.Parameters.Add(new SqlParameter("@UpdatedAt", DBNull.Value));
        cmd.Parameters.Add(new SqlParameter("@CreatedBy", ToDbValue(position.CreatedBy)));
        cmd.Parameters.Add(new SqlParameter("@UpdatedBy", DBNull.Value));
        cmd.Parameters.Add(new SqlParameter("@IsDeleted", false));

        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    /// <summary>
    /// 更新职位数据（更新职位名称与说明）。
    /// </summary>
    public async Task<bool> UpdatePositionAsync(Position position)
    {
        var sql = $"""
            UPDATE dbo.{PositionTableName}
            SET
                Name = @Name,
                Description = @Description,
                UpdatedAt = @UpdatedAt,
                UpdatedBy = @UpdatedBy
            WHERE Id = @Id AND IsDeleted = 0;
            """;

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@Id", position.Id));
        cmd.Parameters.Add(new SqlParameter("@Name", position.Name.Trim()));
        cmd.Parameters.Add(new SqlParameter("@Description", ToDbValue(position.Description)));
        cmd.Parameters.Add(new SqlParameter("@UpdatedAt", DateTime.Now));
        cmd.Parameters.Add(new SqlParameter("@UpdatedBy", ToDbValue(position.UpdatedBy)));

        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    /// <summary>
    /// 删除职位数据（逻辑删除职位记录）。
    /// </summary>
    public async Task<bool> DeletePositionAsync(int id)
    {
        var sql = $"""
            UPDATE dbo.{PositionTableName}
            SET
                IsDeleted = 1,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id AND IsDeleted = 0;
            """;

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@Id", id));
        cmd.Parameters.Add(new SqlParameter("@UpdatedAt", DateTime.Now));

        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task CreateDepartmentAsync(Department department)
    {
        var sql = $"""
            INSERT INTO dbo.{DepartmentTableName} (Name, Description, CreatedAt, UpdatedAt, IsDeleted)
            VALUES (@Name, @Description, @CreatedAt, @UpdatedAt, @IsDeleted);
            """;

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@Name", department.Name));
        cmd.Parameters.Add(new SqlParameter("@Description", ToDbValue(department.Description)));
        cmd.Parameters.Add(new SqlParameter("@CreatedAt", DateTime.Now));
        cmd.Parameters.Add(new SqlParameter("@UpdatedAt", DBNull.Value));
        cmd.Parameters.Add(new SqlParameter("@IsDeleted", false));

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<Employee>> GetEmployeesAsync()
    {
        var sql = $"""
            {BuildEmployeeSelectSql(includeNav: true)}
            WHERE e.IsDeleted = 0
            ORDER BY e.Id DESC;
            """;

        var results = new List<Employee>();
        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            results.Add(MapEmployee(reader, includeNav: true));
        }

        return results;
    }

    public async Task<Employee?> GetEmployeeByIdAsync(int id)
    {
        var sql = $"""
            {BuildEmployeeSelectSql(includeNav: false)}
            WHERE e.Id = @Id AND e.IsDeleted = 0;
            """;

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@Id", id));
        await using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return MapEmployee(reader, includeNav: false);
    }

    public async Task<Employee?> GetEmployeeDetailsAsync(int id)
    {
        var sql = $"""
            {BuildEmployeeSelectSql(includeNav: true)}
            WHERE e.Id = @Id AND e.IsDeleted = 0;
            """;

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@Id", id));
        await using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return MapEmployee(reader, includeNav: true);
    }

    public async Task<int> CreateEmployeeAsync(Employee employee)
    {
        var sql = $"""
            INSERT INTO dbo.{EmployeeTableName}
                (
                    EmployeeCode,
                    FirstName,
                    LastName,
                    OrganizationId,
                    Email,
                    PhoneNumber,
                    PhoneNumber2,
                    PhoneNumber3,
                    JobTitle,
                    DepartmentId,
                    PositionId,
                    GroupId,
                    GenderId,
                    AliasName,
                    BirthDate,
                    IdCardNumber,
                    CardNumber,
                    EthnicityId,
                    MaritalStatusId,
                    EducationLevelId,
                    EducationCertificateNumber,
                    ProfessionalTitleId,
                    CountryRegionId,
                    CityId,
                    CountyId,
                    Address,
                    EmergencyContact,
                    EmergencyContactPhone,
                    Referrer,
                    ArchivePath,
                    PhotoPath,
                    Remarks,
                    HireDate,
                    LeaveDate,
                    LoginAccount,
                    LoginPassword,
                    EmploymentTypeId,
                    AllowancePackageId,
                    ProbationEndDate,
                    AnnualLeaveCalculationMethodId,
                    IsAttendanceRequired,
                    CurrentYearAnnualLeaveDays,
                    AnnualLeaveMaxAccumulatedDays,
                    AnnualLeaveRemainingDays,
                    AnnualLeaveIncrementStartYears,
                    AnnualLeaveIncrementPerYearDays,
                    AnnualLeaveCapDays,
                    DefaultShiftId,
                    SchedulingGroupId,
                    IsAutoSchedulingEnabled,
                    SalaryGradeId,
                    PayrollCompanyId,
                    BankAccountNumber,
                    BankAccountName,
                    BankId,
                    CreatedAt,
                    UpdatedAt,
                    CreatedBy,
                    UpdatedBy,
                    IsDeleted
                )
            OUTPUT INSERTED.Id
            VALUES
                (
                    @EmployeeCode,
                    @FirstName,
                    @LastName,
                    @OrganizationId,
                    @Email,
                    @PhoneNumber,
                    @PhoneNumber2,
                    @PhoneNumber3,
                    @JobTitle,
                    @DepartmentId,
                    @PositionId,
                    @GroupId,
                    @GenderId,
                    @AliasName,
                    @BirthDate,
                    @IdCardNumber,
                    @CardNumber,
                    @EthnicityId,
                    @MaritalStatusId,
                    @EducationLevelId,
                    @EducationCertificateNumber,
                    @ProfessionalTitleId,
                    @CountryRegionId,
                    @CityId,
                    @CountyId,
                    @Address,
                    @EmergencyContact,
                    @EmergencyContactPhone,
                    @Referrer,
                    @ArchivePath,
                    @PhotoPath,
                    @Remarks,
                    @HireDate,
                    @LeaveDate,
                    @LoginAccount,
                    @LoginPassword,
                    @EmploymentTypeId,
                    @AllowancePackageId,
                    @ProbationEndDate,
                    @AnnualLeaveCalculationMethodId,
                    @IsAttendanceRequired,
                    @CurrentYearAnnualLeaveDays,
                    @AnnualLeaveMaxAccumulatedDays,
                    @AnnualLeaveRemainingDays,
                    @AnnualLeaveIncrementStartYears,
                    @AnnualLeaveIncrementPerYearDays,
                    @AnnualLeaveCapDays,
                    @DefaultShiftId,
                    @SchedulingGroupId,
                    @IsAutoSchedulingEnabled,
                    @SalaryGradeId,
                    @PayrollCompanyId,
                    @BankAccountNumber,
                    @BankAccountName,
                    @BankId,
                    @CreatedAt,
                    @UpdatedAt,
                    @CreatedBy,
                    @UpdatedBy,
                    @IsDeleted
                );
            """;

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        BindEmployeeParams(cmd, employee);
        cmd.Parameters.Add(new SqlParameter("@CreatedAt", DateTime.Now));
        cmd.Parameters.Add(new SqlParameter("@UpdatedAt", DBNull.Value));
        cmd.Parameters.Add(new SqlParameter("@CreatedBy", ToDbValue(employee.CreatedBy)));
        cmd.Parameters.Add(new SqlParameter("@UpdatedBy", DBNull.Value));
        cmd.Parameters.Add(new SqlParameter("@IsDeleted", false));

        var insertedId = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(insertedId);
    }

    public async Task<bool> UpdateEmployeeAsync(Employee employee)
    {
        var sql = $"""
            UPDATE dbo.{EmployeeTableName}
            SET
                EmployeeCode = @EmployeeCode,
                FirstName = @FirstName,
                LastName = @LastName,
                OrganizationId = @OrganizationId,
                Email = @Email,
                PhoneNumber = @PhoneNumber,
                PhoneNumber2 = @PhoneNumber2,
                PhoneNumber3 = @PhoneNumber3,
                JobTitle = @JobTitle,
                DepartmentId = @DepartmentId,
                PositionId = @PositionId,
                GroupId = @GroupId,
                GenderId = @GenderId,
                AliasName = @AliasName,
                BirthDate = @BirthDate,
                IdCardNumber = @IdCardNumber,
                CardNumber = @CardNumber,
                EthnicityId = @EthnicityId,
                MaritalStatusId = @MaritalStatusId,
                EducationLevelId = @EducationLevelId,
                EducationCertificateNumber = @EducationCertificateNumber,
                ProfessionalTitleId = @ProfessionalTitleId,
                CountryRegionId = @CountryRegionId,
                CityId = @CityId,
                CountyId = @CountyId,
                Address = @Address,
                EmergencyContact = @EmergencyContact,
                EmergencyContactPhone = @EmergencyContactPhone,
                Referrer = @Referrer,
                ArchivePath = @ArchivePath,
                PhotoPath = @PhotoPath,
                Remarks = @Remarks,
                HireDate = @HireDate,
                LeaveDate = @LeaveDate,
                LoginAccount = @LoginAccount,
                LoginPassword = @LoginPassword,
                EmploymentTypeId = @EmploymentTypeId,
                AllowancePackageId = @AllowancePackageId,
                ProbationEndDate = @ProbationEndDate,
                AnnualLeaveCalculationMethodId = @AnnualLeaveCalculationMethodId,
                IsAttendanceRequired = @IsAttendanceRequired,
                CurrentYearAnnualLeaveDays = @CurrentYearAnnualLeaveDays,
                AnnualLeaveMaxAccumulatedDays = @AnnualLeaveMaxAccumulatedDays,
                AnnualLeaveRemainingDays = @AnnualLeaveRemainingDays,
                AnnualLeaveIncrementStartYears = @AnnualLeaveIncrementStartYears,
                AnnualLeaveIncrementPerYearDays = @AnnualLeaveIncrementPerYearDays,
                AnnualLeaveCapDays = @AnnualLeaveCapDays,
                DefaultShiftId = @DefaultShiftId,
                SchedulingGroupId = @SchedulingGroupId,
                IsAutoSchedulingEnabled = @IsAutoSchedulingEnabled,
                SalaryGradeId = @SalaryGradeId,
                PayrollCompanyId = @PayrollCompanyId,
                BankAccountNumber = @BankAccountNumber,
                BankAccountName = @BankAccountName,
                BankId = @BankId,
                UpdatedAt = @UpdatedAt,
                UpdatedBy = @UpdatedBy
            WHERE Id = @Id AND IsDeleted = 0;
            """;

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@Id", employee.Id));
        BindEmployeeParams(cmd, employee);
        cmd.Parameters.Add(new SqlParameter("@UpdatedAt", DateTime.Now));
        cmd.Parameters.Add(new SqlParameter("@UpdatedBy", ToDbValue(employee.UpdatedBy)));

        var affected = await cmd.ExecuteNonQueryAsync();
        return affected > 0;
    }

    /// <summary>
    /// 更新员工主图路径（用于图片管理页面选择员工主图）。
    /// </summary>
    public async Task<bool> UpdateEmployeePhotoPathAsync(int employeeId, string? photoPath, string? updatedBy)
    {
        var sql = $"""
            UPDATE dbo.{EmployeeTableName}
            SET
                PhotoPath = @PhotoPath,
                UpdatedAt = @UpdatedAt,
                UpdatedBy = @UpdatedBy
            WHERE Id = @Id AND IsDeleted = 0;
            """;

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@Id", employeeId));
        cmd.Parameters.Add(new SqlParameter("@PhotoPath", ToDbValue(photoPath)));
        cmd.Parameters.Add(new SqlParameter("@UpdatedAt", DateTime.Now));
        cmd.Parameters.Add(new SqlParameter("@UpdatedBy", ToDbValue(updatedBy)));

        var affected = await cmd.ExecuteNonQueryAsync();
        return affected > 0;
    }

    public async Task<bool> DeleteEmployeeAsync(int id)
    {
        var sql = $"""
            UPDATE dbo.{EmployeeTableName}
            SET
                IsDeleted = 1,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id AND IsDeleted = 0;
            """;

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@Id", id));
        cmd.Parameters.Add(new SqlParameter("@UpdatedAt", DateTime.Now));

        var affected = await cmd.ExecuteNonQueryAsync();
        return affected > 0;
    }

    public async Task<bool> ExistsActiveEmployeeCardNumberAsync(string cardNumber, int? excludedEmployeeId = null)
    {
        var normalizedCardNumber = cardNumber.Trim();
        var sql = $"""
            SELECT TOP (1) 1
            FROM dbo.{EmployeeTableName}
            WHERE IsDeleted = 0
              AND LeaveDate IS NULL
              AND LTRIM(RTRIM(CardNumber)) = @CardNumber
              AND (@ExcludedEmployeeId IS NULL OR Id <> @ExcludedEmployeeId);
            """;

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@CardNumber", normalizedCardNumber));
        cmd.Parameters.Add(new SqlParameter("@ExcludedEmployeeId", ToDbValue(excludedEmployeeId)));

        var result = await cmd.ExecuteScalarAsync();
        return result != null;
    }

    /// <summary>
    /// 构建公司组织表初始化 SQL。
    /// </summary>
    private static string BuildCompanyOrganizationInitializeSql()
        => $"""
            IF OBJECT_ID('dbo.{CompanyOrganizationTableName}', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.{CompanyOrganizationTableName}
                (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    OrganizationCode NVARCHAR(10) NOT NULL,
                    OrganizationName NVARCHAR(100) NOT NULL,
                    CompanyNatureId INT NULL,
                    StatusId INT NULL,
                    EnterpriseTypeId INT NULL,
                    BusinessRegistrationNumber NVARCHAR(50) NULL,
                    BusinessRegistrationExpiryDate DATETIME2 NULL,
                    RegionId INT NULL,
                    CityId INT NULL,
                    CountyId INT NULL,
                    Address NVARCHAR(300) NULL,
                    Principal NVARCHAR(50) NULL,
                    Phone NVARCHAR(50) NULL,
                    Fax NVARCHAR(50) NULL,
                    Email NVARCHAR(150) NULL,
                    Website NVARCHAR(200) NULL,
                    WeeklyWorkDays DECIMAL(18,2) NULL,
                    LeaveCountBasisType INT NULL,
                    HolidayType INT NULL,
                    AnnualLeaveCalculationMonthDay NVARCHAR(5) NULL,
                    AnnualLeaveGrantRule INT NULL,
                    AnnualLeaveGrantMonthDay NVARCHAR(5) NULL,
                    IsAnnualLeaveClearEnabled BIT NOT NULL CONSTRAINT DF_HR_CompanyOrganization_IsAnnualLeaveClearEnabled DEFAULT(0),
                    AnnualLeaveClearMonthDay NVARCHAR(5) NULL,
                    IsCarryForwardAnnualLeaveAllowed BIT NOT NULL CONSTRAINT DF_HR_CompanyOrganization_IsCarryForwardAnnualLeaveAllowed DEFAULT(0),
                    BaseAnnualLeaveDays DECIMAL(18,2) NULL,
                    AnnualLeaveIncrementStartYears INT NULL,
                    AnnualLeaveIncrementPerYearDays DECIMAL(18,2) NULL,
                    AnnualLeaveCapDays DECIMAL(18,2) NULL,
                    AnnualLeaveMaxAccumulatedDays DECIMAL(18,2) NULL,
                    PaidSickLeaveDaysPerYear DECIMAL(18,2) NULL,
                    PaidSickLeaveSalaryRatio DECIMAL(18,2) NULL,
                    PaidSickLeaveCalculationMonthDay NVARCHAR(5) NULL,
                    IsPaidSickLeaveClearEnabled BIT NOT NULL CONSTRAINT DF_HR_CompanyOrganization_IsPaidSickLeaveClearEnabled DEFAULT(0),
                    PaidSickLeaveClearMonthDay NVARCHAR(5) NULL,
                    EmployeeMpfMinimumSalary DECIMAL(18,2) NULL,
                    Remarks NVARCHAR(1000) NULL,
                    ArchivePath NVARCHAR(500) NULL,
                    PrintHeaderContent NVARCHAR(4000) NULL,
                    PrintFooterContent NVARCHAR(4000) NULL,
                    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_HR_CompanyOrganization_CreatedAt DEFAULT(SYSDATETIME()),
                    UpdatedAt DATETIME2 NULL,
                    CreatedBy NVARCHAR(50) NULL,
                    UpdatedBy NVARCHAR(50) NULL,
                    IsDeleted BIT NOT NULL CONSTRAINT DF_HR_CompanyOrganization_IsDeleted DEFAULT(0)
                );
            END;

            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'OrganizationCode') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD OrganizationCode NVARCHAR(10) NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'OrganizationName') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD OrganizationName NVARCHAR(100) NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'CompanyNatureId') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD CompanyNatureId INT NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'StatusId') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD StatusId INT NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'EnterpriseTypeId') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD EnterpriseTypeId INT NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'BusinessRegistrationNumber') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD BusinessRegistrationNumber NVARCHAR(50) NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'BusinessRegistrationExpiryDate') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD BusinessRegistrationExpiryDate DATETIME2 NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'RegionId') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD RegionId INT NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'CityId') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD CityId INT NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'CountyId') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD CountyId INT NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'Address') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD Address NVARCHAR(300) NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'Principal') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD Principal NVARCHAR(50) NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'Phone') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD Phone NVARCHAR(50) NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'Fax') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD Fax NVARCHAR(50) NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'Email') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD Email NVARCHAR(150) NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'Website') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD Website NVARCHAR(200) NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'WeeklyWorkDays') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD WeeklyWorkDays DECIMAL(18,2) NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'LeaveCountBasisType') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD LeaveCountBasisType INT NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'HolidayType') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD HolidayType INT NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'AnnualLeaveCalculationMonthDay') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD AnnualLeaveCalculationMonthDay NVARCHAR(5) NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'AnnualLeaveGrantRule') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD AnnualLeaveGrantRule INT NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'AnnualLeaveGrantMonthDay') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD AnnualLeaveGrantMonthDay NVARCHAR(5) NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'IsAnnualLeaveClearEnabled') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD IsAnnualLeaveClearEnabled BIT NOT NULL CONSTRAINT DF_HR_CompanyOrganization_IsAnnualLeaveClearEnabled_Alter DEFAULT(0);
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'AnnualLeaveClearMonthDay') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD AnnualLeaveClearMonthDay NVARCHAR(5) NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'IsCarryForwardAnnualLeaveAllowed') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD IsCarryForwardAnnualLeaveAllowed BIT NOT NULL CONSTRAINT DF_HR_CompanyOrganization_IsCarryForwardAnnualLeaveAllowed_Alter DEFAULT(0);
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'BaseAnnualLeaveDays') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD BaseAnnualLeaveDays DECIMAL(18,2) NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'AnnualLeaveIncrementStartYears') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD AnnualLeaveIncrementStartYears INT NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'AnnualLeaveIncrementPerYearDays') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD AnnualLeaveIncrementPerYearDays DECIMAL(18,2) NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'AnnualLeaveCapDays') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD AnnualLeaveCapDays DECIMAL(18,2) NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'AnnualLeaveMaxAccumulatedDays') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD AnnualLeaveMaxAccumulatedDays DECIMAL(18,2) NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'PaidSickLeaveDaysPerYear') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD PaidSickLeaveDaysPerYear DECIMAL(18,2) NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'PaidSickLeaveSalaryRatio') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD PaidSickLeaveSalaryRatio DECIMAL(18,2) NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'PaidSickLeaveCalculationMonthDay') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD PaidSickLeaveCalculationMonthDay NVARCHAR(5) NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'IsPaidSickLeaveClearEnabled') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD IsPaidSickLeaveClearEnabled BIT NOT NULL CONSTRAINT DF_HR_CompanyOrganization_IsPaidSickLeaveClearEnabled_Alter DEFAULT(0);
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'PaidSickLeaveClearMonthDay') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD PaidSickLeaveClearMonthDay NVARCHAR(5) NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'EmployeeMpfMinimumSalary') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD EmployeeMpfMinimumSalary DECIMAL(18,2) NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'Remarks') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD Remarks NVARCHAR(1000) NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'ArchivePath') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD ArchivePath NVARCHAR(500) NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'PrintHeaderContent') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD PrintHeaderContent NVARCHAR(4000) NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'PrintFooterContent') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD PrintFooterContent NVARCHAR(4000) NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'CreatedAt') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_HR_CompanyOrganization_CreatedAt_Alter DEFAULT(SYSDATETIME());
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'UpdatedAt') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD UpdatedAt DATETIME2 NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'CreatedBy') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD CreatedBy NVARCHAR(50) NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'UpdatedBy') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD UpdatedBy NVARCHAR(50) NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationTableName}', 'IsDeleted') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationTableName} ADD IsDeleted BIT NOT NULL CONSTRAINT DF_HR_CompanyOrganization_IsDeleted_Alter DEFAULT(0);

            IF NOT EXISTS
            (
                SELECT 1
                FROM sys.indexes
                WHERE name = 'UX_HR_CompanyOrganization_OrganizationCode'
                  AND object_id = OBJECT_ID('dbo.{CompanyOrganizationTableName}')
            )
            BEGIN
                CREATE UNIQUE INDEX UX_HR_CompanyOrganization_OrganizationCode
                    ON dbo.{CompanyOrganizationTableName}(OrganizationCode)
                    WHERE IsDeleted = 0 AND OrganizationCode IS NOT NULL;
            END;
            """;

    /// <summary>
    /// 为现有员工补齐默认登录账号与默认登录密码（账号默认使用员工编号，密码默认使用 123456）。
    /// </summary>
    private static string BuildEmployeeCredentialDefaultSql()
        => $"""
            IF OBJECT_ID('dbo.{EmployeeTableName}', 'U') IS NULL
            BEGIN
                RETURN;
            END;

            IF COL_LENGTH('dbo.{EmployeeTableName}', 'LoginAccount') IS NULL
               OR COL_LENGTH('dbo.{EmployeeTableName}', 'LoginPassword') IS NULL
               OR COL_LENGTH('dbo.{EmployeeTableName}', 'EmployeeCode') IS NULL
            BEGIN
                RETURN;
            END;

            UPDATE dbo.{EmployeeTableName}
            SET LoginAccount = EmployeeCode
            WHERE ISNULL(IsDeleted, 0) = 0
              AND NULLIF(LTRIM(RTRIM(ISNULL(LoginAccount, N''))), N'') IS NULL
              AND NULLIF(LTRIM(RTRIM(ISNULL(EmployeeCode, N''))), N'') IS NOT NULL;

            UPDATE dbo.{EmployeeTableName}
            SET LoginPassword = N'123456'
            WHERE ISNULL(IsDeleted, 0) = 0
              AND NULLIF(LTRIM(RTRIM(ISNULL(LoginPassword, N''))), N'') IS NULL;
            """;

    private static string BuildCompanyOrganizationBankAccountInitializeSql()
        => $"""
            IF OBJECT_ID('dbo.{CompanyOrganizationBankAccountTableName}', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.{CompanyOrganizationBankAccountTableName}
                (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    CompanyOrganizationId INT NOT NULL,
                    AccountNumber NVARCHAR(80) NOT NULL,
                    BankId INT NULL,
                    BranchName NVARCHAR(200) NOT NULL,
                    BranchAddress NVARCHAR(300) NULL,
                    CurrencyCode NVARCHAR(10) NOT NULL CONSTRAINT DF_HR_CompanyOrganizationBankAccount_CurrencyCode DEFAULT(N'RMB'),
                    StatusCode NVARCHAR(20) NOT NULL CONSTRAINT DF_HR_CompanyOrganizationBankAccount_StatusCode DEFAULT(N'NORMAL'),
                    SubjectCode NVARCHAR(50) NULL,
                    SubjectName NVARCHAR(100) NULL,
                    Remarks NVARCHAR(500) NULL,
                    IsDefault BIT NOT NULL CONSTRAINT DF_HR_CompanyOrganizationBankAccount_IsDefault DEFAULT(0),
                    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_HR_CompanyOrganizationBankAccount_CreatedAt DEFAULT(SYSDATETIME()),
                    UpdatedAt DATETIME2 NULL,
                    CreatedBy NVARCHAR(50) NULL,
                    UpdatedBy NVARCHAR(50) NULL,
                    IsDeleted BIT NOT NULL CONSTRAINT DF_HR_CompanyOrganizationBankAccount_IsDeleted DEFAULT(0)
                );
            END;

            IF COL_LENGTH('dbo.{CompanyOrganizationBankAccountTableName}', 'CompanyOrganizationId') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationBankAccountTableName} ADD CompanyOrganizationId INT NOT NULL CONSTRAINT DF_HR_CompanyOrganizationBankAccount_CompanyOrganizationId DEFAULT(0);
            IF COL_LENGTH('dbo.{CompanyOrganizationBankAccountTableName}', 'AccountNumber') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationBankAccountTableName} ADD AccountNumber NVARCHAR(80) NOT NULL CONSTRAINT DF_HR_CompanyOrganizationBankAccount_AccountNumber DEFAULT(N'');
            IF COL_LENGTH('dbo.{CompanyOrganizationBankAccountTableName}', 'BankId') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationBankAccountTableName} ADD BankId INT NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationBankAccountTableName}', 'BranchName') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationBankAccountTableName} ADD BranchName NVARCHAR(200) NOT NULL CONSTRAINT DF_HR_CompanyOrganizationBankAccount_BranchName DEFAULT(N'');
            IF COL_LENGTH('dbo.{CompanyOrganizationBankAccountTableName}', 'BranchAddress') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationBankAccountTableName} ADD BranchAddress NVARCHAR(300) NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationBankAccountTableName}', 'CurrencyCode') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationBankAccountTableName} ADD CurrencyCode NVARCHAR(10) NOT NULL CONSTRAINT DF_HR_CompanyOrganizationBankAccount_CurrencyCode_Alter DEFAULT(N'RMB');
            IF COL_LENGTH('dbo.{CompanyOrganizationBankAccountTableName}', 'StatusCode') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationBankAccountTableName} ADD StatusCode NVARCHAR(20) NOT NULL CONSTRAINT DF_HR_CompanyOrganizationBankAccount_StatusCode_Alter DEFAULT(N'NORMAL');
            IF COL_LENGTH('dbo.{CompanyOrganizationBankAccountTableName}', 'SubjectCode') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationBankAccountTableName} ADD SubjectCode NVARCHAR(50) NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationBankAccountTableName}', 'SubjectName') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationBankAccountTableName} ADD SubjectName NVARCHAR(100) NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationBankAccountTableName}', 'Remarks') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationBankAccountTableName} ADD Remarks NVARCHAR(500) NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationBankAccountTableName}', 'IsDefault') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationBankAccountTableName} ADD IsDefault BIT NOT NULL CONSTRAINT DF_HR_CompanyOrganizationBankAccount_IsDefault_Alter DEFAULT(0);
            IF COL_LENGTH('dbo.{CompanyOrganizationBankAccountTableName}', 'CreatedAt') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationBankAccountTableName} ADD CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_HR_CompanyOrganizationBankAccount_CreatedAt_Alter DEFAULT(SYSDATETIME());
            IF COL_LENGTH('dbo.{CompanyOrganizationBankAccountTableName}', 'UpdatedAt') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationBankAccountTableName} ADD UpdatedAt DATETIME2 NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationBankAccountTableName}', 'CreatedBy') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationBankAccountTableName} ADD CreatedBy NVARCHAR(50) NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationBankAccountTableName}', 'UpdatedBy') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationBankAccountTableName} ADD UpdatedBy NVARCHAR(50) NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationBankAccountTableName}', 'IsDeleted') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationBankAccountTableName} ADD IsDeleted BIT NOT NULL CONSTRAINT DF_HR_CompanyOrganizationBankAccount_IsDeleted_Alter DEFAULT(0);

            IF NOT EXISTS (
                SELECT 1
                FROM sys.indexes
                WHERE name = 'IX_HR_CompanyOrganizationBankAccount_Org'
                  AND object_id = OBJECT_ID('dbo.{CompanyOrganizationBankAccountTableName}')
            )
            BEGIN
                CREATE INDEX IX_HR_CompanyOrganizationBankAccount_Org
                    ON dbo.{CompanyOrganizationBankAccountTableName}(CompanyOrganizationId, IsDeleted, StatusCode);
            END;
            """;

    /// <summary>
    /// 构建公司组织单号规则表初始化 SQL。
    /// </summary>
    private static string BuildCompanyOrganizationDocumentNumberRuleInitializeSql()
        => $"""
            IF OBJECT_ID('dbo.{CompanyOrganizationDocumentNumberRuleTableName}', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.{CompanyOrganizationDocumentNumberRuleTableName}
                (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    CompanyOrganizationId INT NOT NULL,
                    DocumentTypeCode NVARCHAR(50) NOT NULL,
                    Prefix NVARCHAR(20) NOT NULL CONSTRAINT DF_HR_CompanyOrganizationDocumentNumberRule_Prefix DEFAULT(N''),
                    DateFormatCode NVARCHAR(20) NOT NULL CONSTRAINT DF_HR_CompanyOrganizationDocumentNumberRule_DateFormatCode DEFAULT(N'yyMM'),
                    SequenceLength INT NOT NULL CONSTRAINT DF_HR_CompanyOrganizationDocumentNumberRule_SequenceLength DEFAULT(5),
                    LastDateSegment NVARCHAR(20) NULL,
                    LastSequenceValue INT NOT NULL CONSTRAINT DF_HR_CompanyOrganizationDocumentNumberRule_LastSequenceValue DEFAULT(0),
                    LastGeneratedNumber NVARCHAR(80) NULL,
                    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_HR_CompanyOrganizationDocumentNumberRule_CreatedAt DEFAULT(SYSDATETIME()),
                    UpdatedAt DATETIME2 NULL,
                    CreatedBy NVARCHAR(50) NULL,
                    UpdatedBy NVARCHAR(50) NULL,
                    IsDeleted BIT NOT NULL CONSTRAINT DF_HR_CompanyOrganizationDocumentNumberRule_IsDeleted DEFAULT(0)
                );
            END;

            IF COL_LENGTH('dbo.{CompanyOrganizationDocumentNumberRuleTableName}', 'CompanyOrganizationId') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationDocumentNumberRuleTableName} ADD CompanyOrganizationId INT NOT NULL CONSTRAINT DF_HR_CompanyOrganizationDocumentNumberRule_CompanyOrganizationId DEFAULT(0);
            IF COL_LENGTH('dbo.{CompanyOrganizationDocumentNumberRuleTableName}', 'DocumentTypeCode') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationDocumentNumberRuleTableName} ADD DocumentTypeCode NVARCHAR(50) NOT NULL CONSTRAINT DF_HR_CompanyOrganizationDocumentNumberRule_DocumentTypeCode DEFAULT(N'');
            IF COL_LENGTH('dbo.{CompanyOrganizationDocumentNumberRuleTableName}', 'Prefix') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationDocumentNumberRuleTableName} ADD Prefix NVARCHAR(20) NOT NULL CONSTRAINT DF_HR_CompanyOrganizationDocumentNumberRule_Prefix_Alter DEFAULT(N'');
            IF COL_LENGTH('dbo.{CompanyOrganizationDocumentNumberRuleTableName}', 'DateFormatCode') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationDocumentNumberRuleTableName} ADD DateFormatCode NVARCHAR(20) NOT NULL CONSTRAINT DF_HR_CompanyOrganizationDocumentNumberRule_DateFormatCode_Alter DEFAULT(N'yyMM');
            IF COL_LENGTH('dbo.{CompanyOrganizationDocumentNumberRuleTableName}', 'SequenceLength') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationDocumentNumberRuleTableName} ADD SequenceLength INT NOT NULL CONSTRAINT DF_HR_CompanyOrganizationDocumentNumberRule_SequenceLength_Alter DEFAULT(5);
            IF COL_LENGTH('dbo.{CompanyOrganizationDocumentNumberRuleTableName}', 'LastDateSegment') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationDocumentNumberRuleTableName} ADD LastDateSegment NVARCHAR(20) NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationDocumentNumberRuleTableName}', 'LastSequenceValue') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationDocumentNumberRuleTableName} ADD LastSequenceValue INT NOT NULL CONSTRAINT DF_HR_CompanyOrganizationDocumentNumberRule_LastSequenceValue_Alter DEFAULT(0);
            IF COL_LENGTH('dbo.{CompanyOrganizationDocumentNumberRuleTableName}', 'LastGeneratedNumber') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationDocumentNumberRuleTableName} ADD LastGeneratedNumber NVARCHAR(80) NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationDocumentNumberRuleTableName}', 'CreatedAt') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationDocumentNumberRuleTableName} ADD CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_HR_CompanyOrganizationDocumentNumberRule_CreatedAt_Alter DEFAULT(SYSDATETIME());
            IF COL_LENGTH('dbo.{CompanyOrganizationDocumentNumberRuleTableName}', 'UpdatedAt') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationDocumentNumberRuleTableName} ADD UpdatedAt DATETIME2 NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationDocumentNumberRuleTableName}', 'CreatedBy') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationDocumentNumberRuleTableName} ADD CreatedBy NVARCHAR(50) NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationDocumentNumberRuleTableName}', 'UpdatedBy') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationDocumentNumberRuleTableName} ADD UpdatedBy NVARCHAR(50) NULL;
            IF COL_LENGTH('dbo.{CompanyOrganizationDocumentNumberRuleTableName}', 'IsDeleted') IS NULL
                ALTER TABLE dbo.{CompanyOrganizationDocumentNumberRuleTableName} ADD IsDeleted BIT NOT NULL CONSTRAINT DF_HR_CompanyOrganizationDocumentNumberRule_IsDeleted_Alter DEFAULT(0);

            IF NOT EXISTS
            (
                SELECT 1
                FROM sys.indexes
                WHERE name = 'UX_HR_CompanyOrganizationDocumentNumberRule_OrgDocType'
                  AND object_id = OBJECT_ID('dbo.{CompanyOrganizationDocumentNumberRuleTableName}')
            )
            BEGIN
                CREATE UNIQUE INDEX UX_HR_CompanyOrganizationDocumentNumberRule_OrgDocType
                    ON dbo.{CompanyOrganizationDocumentNumberRuleTableName}(CompanyOrganizationId, DocumentTypeCode);
            END;
            """;

    /// <summary>
    /// 构建部门资料表初始化 SQL。
    /// </summary>
    private static string BuildDepartmentInitializeSql()
        => $"""
            IF OBJECT_ID('dbo.{DepartmentTableName}', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.{DepartmentTableName}
                (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    Name NVARCHAR(100) NOT NULL,
                    Description NVARCHAR(MAX) NULL,
                    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_HR_Department_CreatedAt DEFAULT(SYSDATETIME()),
                    UpdatedAt DATETIME2 NULL,
                    CreatedBy NVARCHAR(50) NULL,
                    UpdatedBy NVARCHAR(50) NULL,
                    IsDeleted BIT NOT NULL CONSTRAINT DF_HR_Department_IsDeleted DEFAULT(0)
                );
            END;

            IF COL_LENGTH('dbo.{DepartmentTableName}', 'Name') IS NULL
                ALTER TABLE dbo.{DepartmentTableName} ADD Name NVARCHAR(100) NULL;
            IF COL_LENGTH('dbo.{DepartmentTableName}', 'Description') IS NULL
                ALTER TABLE dbo.{DepartmentTableName} ADD Description NVARCHAR(MAX) NULL;
            IF COL_LENGTH('dbo.{DepartmentTableName}', 'CreatedAt') IS NULL
                ALTER TABLE dbo.{DepartmentTableName} ADD CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_HR_Department_CreatedAt_Alter DEFAULT(SYSDATETIME());
            IF COL_LENGTH('dbo.{DepartmentTableName}', 'UpdatedAt') IS NULL
                ALTER TABLE dbo.{DepartmentTableName} ADD UpdatedAt DATETIME2 NULL;
            IF COL_LENGTH('dbo.{DepartmentTableName}', 'CreatedBy') IS NULL
                ALTER TABLE dbo.{DepartmentTableName} ADD CreatedBy NVARCHAR(50) NULL;
            IF COL_LENGTH('dbo.{DepartmentTableName}', 'UpdatedBy') IS NULL
                ALTER TABLE dbo.{DepartmentTableName} ADD UpdatedBy NVARCHAR(50) NULL;
            IF COL_LENGTH('dbo.{DepartmentTableName}', 'IsDeleted') IS NULL
                ALTER TABLE dbo.{DepartmentTableName} ADD IsDeleted BIT NOT NULL CONSTRAINT DF_HR_Department_IsDeleted_Alter DEFAULT(0);

            IF NOT EXISTS
            (
                SELECT 1
                FROM sys.indexes
                WHERE name = 'IX_HR_Department_Name'
                  AND object_id = OBJECT_ID('dbo.{DepartmentTableName}')
            )
            BEGIN
                CREATE INDEX IX_HR_Department_Name
                    ON dbo.{DepartmentTableName}(Name);
            END;
            """;

    /// <summary>
    /// 构建员工资料表初始化 SQL。
    /// </summary>
    /// <summary>
    /// 构建职位资料表初始化 SQL。
    /// </summary>
    private static string BuildPositionInitializeSql()
        => $"""
            IF OBJECT_ID('dbo.{PositionTableName}', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.{PositionTableName}
                (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    Name NVARCHAR(100) NOT NULL,
                    Description NVARCHAR(200) NULL,
                    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_HR_Position_CreatedAt DEFAULT(SYSDATETIME()),
                    UpdatedAt DATETIME2 NULL,
                    CreatedBy NVARCHAR(50) NULL,
                    UpdatedBy NVARCHAR(50) NULL,
                    IsDeleted BIT NOT NULL CONSTRAINT DF_HR_Position_IsDeleted DEFAULT(0)
                );
            END;

            IF COL_LENGTH('dbo.{PositionTableName}', 'Name') IS NULL
                ALTER TABLE dbo.{PositionTableName} ADD Name NVARCHAR(100) NULL;
            IF COL_LENGTH('dbo.{PositionTableName}', 'Description') IS NULL
                ALTER TABLE dbo.{PositionTableName} ADD Description NVARCHAR(200) NULL;
            IF COL_LENGTH('dbo.{PositionTableName}', 'CreatedAt') IS NULL
                ALTER TABLE dbo.{PositionTableName} ADD CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_HR_Position_CreatedAt_Alter DEFAULT(SYSDATETIME());
            IF COL_LENGTH('dbo.{PositionTableName}', 'UpdatedAt') IS NULL
                ALTER TABLE dbo.{PositionTableName} ADD UpdatedAt DATETIME2 NULL;
            IF COL_LENGTH('dbo.{PositionTableName}', 'CreatedBy') IS NULL
                ALTER TABLE dbo.{PositionTableName} ADD CreatedBy NVARCHAR(50) NULL;
            IF COL_LENGTH('dbo.{PositionTableName}', 'UpdatedBy') IS NULL
                ALTER TABLE dbo.{PositionTableName} ADD UpdatedBy NVARCHAR(50) NULL;
            IF COL_LENGTH('dbo.{PositionTableName}', 'IsDeleted') IS NULL
                ALTER TABLE dbo.{PositionTableName} ADD IsDeleted BIT NOT NULL CONSTRAINT DF_HR_Position_IsDeleted_Alter DEFAULT(0);

            IF NOT EXISTS
            (
                SELECT 1
                FROM sys.indexes
                WHERE name = 'IX_HR_Position_Name'
                  AND object_id = OBJECT_ID('dbo.{PositionTableName}')
            )
            BEGIN
                CREATE INDEX IX_HR_Position_Name
                    ON dbo.{PositionTableName}(Name);
            END;
            """;

    private static string BuildEmployeeInitializeSql()
        => $"""
            IF OBJECT_ID('dbo.{EmployeeTableName}', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.{EmployeeTableName}
                (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    EmployeeCode NVARCHAR(30) NULL,
                    FirstName NVARCHAR(50) NOT NULL,
                    LastName NVARCHAR(50) NOT NULL,
                    OrganizationId INT NULL,
                    Email NVARCHAR(150) NOT NULL,
                    PhoneNumber NVARCHAR(50) NULL,
                    PhoneNumber2 NVARCHAR(50) NULL,
                    PhoneNumber3 NVARCHAR(50) NULL,
                    JobTitle NVARCHAR(100) NULL,
                    DepartmentId INT NULL,
                    PositionId INT NULL,
                    GroupId INT NULL,
                    EmploymentStatusId INT NULL,
                    SalaryGradeId INT NULL,
                    PayrollCompanyId INT NULL,
                    BankAccountNumber NVARCHAR(80) NULL,
                    BankAccountName NVARCHAR(100) NULL,
                    BankId INT NULL,
                    AliasName NVARCHAR(100) NULL,
                    GenderId INT NULL,
                    BirthDate DATETIME2 NULL,
                    IdCardNumber NVARCHAR(30) NULL,
                    CardNumber NVARCHAR(50) NULL,
                    EthnicityId INT NULL,
                    MaritalStatusId INT NULL,
                    EducationLevelId INT NULL,
                    EducationCertificateNumber NVARCHAR(100) NULL,
                    ProfessionalTitleId INT NULL,
                    CountryRegionId INT NULL,
                    CityId INT NULL,
                    CountyId INT NULL,
                    Address NVARCHAR(300) NULL,
                    Remarks NVARCHAR(1000) NULL,
                    EmergencyContact NVARCHAR(50) NULL,
                    EmergencyContactPhone NVARCHAR(50) NULL,
                    Referrer NVARCHAR(50) NULL,
                    ArchivePath NVARCHAR(500) NULL,
                    PhotoPath NVARCHAR(500) NULL,
                    LoginAccount NVARCHAR(100) NULL,
                    LoginPassword NVARCHAR(200) NULL,
                    ForceViewRecordDays INT NULL,
                    RoleId INT NULL,
                    AccountValidUntil DATETIME2 NULL,
                    IsAccountFrozen BIT NOT NULL CONSTRAINT DF_HR_Employee_IsAccountFrozen DEFAULT(0),
                    HireDate DATETIME2 NULL,
                    LeaveDate DATETIME2 NULL,
                    EmploymentTypeId INT NULL,
                    AllowancePackageId INT NULL,
                    ProbationEndDate DATETIME2 NULL,
                    AnnualLeaveCalculationMethodId INT NULL,
                    IsAttendanceRequired BIT NOT NULL CONSTRAINT DF_HR_Employee_IsAttendanceRequired DEFAULT(1),
                    CurrentYearAnnualLeaveDays DECIMAL(18,2) NULL,
                    AnnualLeaveMaxAccumulatedDays DECIMAL(18,2) NULL,
                    AnnualLeaveRemainingDays DECIMAL(18,2) NULL,
                    AnnualLeaveIncrementStartYears INT NULL,
                    AnnualLeaveIncrementPerYearDays DECIMAL(18,2) NULL,
                    AnnualLeaveCapDays DECIMAL(18,2) NULL,
                    DefaultShiftId INT NULL,
                    SchedulingGroupId INT NULL,
                    IsAutoSchedulingEnabled BIT NOT NULL CONSTRAINT DF_HR_Employee_IsAutoSchedulingEnabled DEFAULT(0),
                    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_HR_Employee_CreatedAt DEFAULT(SYSDATETIME()),
                    UpdatedAt DATETIME2 NULL,
                    CreatedBy NVARCHAR(50) NULL,
                    UpdatedBy NVARCHAR(50) NULL,
                    IsDeleted BIT NOT NULL CONSTRAINT DF_HR_Employee_IsDeleted DEFAULT(0)
                );
            END;

            IF COL_LENGTH('dbo.{EmployeeTableName}', 'EmployeeCode') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD EmployeeCode NVARCHAR(30) NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'FirstName') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD FirstName NVARCHAR(50) NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'LastName') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD LastName NVARCHAR(50) NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'OrganizationId') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD OrganizationId INT NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'Email') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD Email NVARCHAR(150) NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'PhoneNumber') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD PhoneNumber NVARCHAR(50) NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'PhoneNumber2') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD PhoneNumber2 NVARCHAR(50) NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'PhoneNumber3') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD PhoneNumber3 NVARCHAR(50) NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'JobTitle') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD JobTitle NVARCHAR(100) NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'DepartmentId') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD DepartmentId INT NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'PositionId') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD PositionId INT NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'GroupId') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD GroupId INT NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'EmploymentStatusId') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD EmploymentStatusId INT NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'SalaryGradeId') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD SalaryGradeId INT NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'PayrollCompanyId') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD PayrollCompanyId INT NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'BankAccountNumber') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD BankAccountNumber NVARCHAR(80) NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'BankAccountName') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD BankAccountName NVARCHAR(100) NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'BankId') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD BankId INT NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'AliasName') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD AliasName NVARCHAR(100) NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'GenderId') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD GenderId INT NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'BirthDate') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD BirthDate DATETIME2 NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'IdCardNumber') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD IdCardNumber NVARCHAR(30) NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'CardNumber') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD CardNumber NVARCHAR(50) NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'EthnicityId') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD EthnicityId INT NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'MaritalStatusId') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD MaritalStatusId INT NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'EducationLevelId') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD EducationLevelId INT NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'EducationCertificateNumber') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD EducationCertificateNumber NVARCHAR(100) NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'ProfessionalTitleId') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD ProfessionalTitleId INT NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'CountryRegionId') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD CountryRegionId INT NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'CityId') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD CityId INT NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'CountyId') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD CountyId INT NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'Address') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD Address NVARCHAR(300) NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'Remarks') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD Remarks NVARCHAR(1000) NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'EmergencyContact') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD EmergencyContact NVARCHAR(50) NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'EmergencyContactPhone') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD EmergencyContactPhone NVARCHAR(50) NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'Referrer') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD Referrer NVARCHAR(50) NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'ArchivePath') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD ArchivePath NVARCHAR(500) NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'PhotoPath') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD PhotoPath NVARCHAR(500) NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'LoginAccount') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD LoginAccount NVARCHAR(100) NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'LoginPassword') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD LoginPassword NVARCHAR(200) NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'ForceViewRecordDays') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD ForceViewRecordDays INT NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'RoleId') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD RoleId INT NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'AccountValidUntil') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD AccountValidUntil DATETIME2 NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'IsAccountFrozen') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD IsAccountFrozen BIT NOT NULL CONSTRAINT DF_HR_Employee_IsAccountFrozen_Alter DEFAULT(0);
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'HireDate') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD HireDate DATETIME2 NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'LeaveDate') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD LeaveDate DATETIME2 NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'EmploymentTypeId') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD EmploymentTypeId INT NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'AllowancePackageId') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD AllowancePackageId INT NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'ProbationEndDate') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD ProbationEndDate DATETIME2 NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'AnnualLeaveCalculationMethodId') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD AnnualLeaveCalculationMethodId INT NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'IsAttendanceRequired') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD IsAttendanceRequired BIT NOT NULL CONSTRAINT DF_HR_Employee_IsAttendanceRequired_Alter DEFAULT(1);
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'CurrentYearAnnualLeaveDays') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD CurrentYearAnnualLeaveDays DECIMAL(18,2) NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'AnnualLeaveMaxAccumulatedDays') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD AnnualLeaveMaxAccumulatedDays DECIMAL(18,2) NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'AnnualLeaveRemainingDays') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD AnnualLeaveRemainingDays DECIMAL(18,2) NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'AnnualLeaveIncrementStartYears') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD AnnualLeaveIncrementStartYears INT NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'AnnualLeaveIncrementPerYearDays') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD AnnualLeaveIncrementPerYearDays DECIMAL(18,2) NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'AnnualLeaveCapDays') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD AnnualLeaveCapDays DECIMAL(18,2) NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'DefaultShiftId') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD DefaultShiftId INT NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'SchedulingGroupId') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD SchedulingGroupId INT NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'IsAutoSchedulingEnabled') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD IsAutoSchedulingEnabled BIT NOT NULL CONSTRAINT DF_HR_Employee_IsAutoSchedulingEnabled_Alter DEFAULT(0);
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'CreatedAt') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_HR_Employee_CreatedAt_Alter DEFAULT(SYSDATETIME());
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'UpdatedAt') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD UpdatedAt DATETIME2 NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'CreatedBy') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD CreatedBy NVARCHAR(50) NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'UpdatedBy') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD UpdatedBy NVARCHAR(50) NULL;
            IF COL_LENGTH('dbo.{EmployeeTableName}', 'IsDeleted') IS NULL ALTER TABLE dbo.{EmployeeTableName} ADD IsDeleted BIT NOT NULL CONSTRAINT DF_HR_Employee_IsDeleted_Alter DEFAULT(0);

            IF NOT EXISTS
            (
                SELECT 1 FROM sys.indexes
                WHERE name = 'UX_HR_Employee_EmployeeCode'
                  AND object_id = OBJECT_ID('dbo.{EmployeeTableName}')
            )
            BEGIN
                CREATE UNIQUE INDEX UX_HR_Employee_EmployeeCode
                    ON dbo.{EmployeeTableName}(EmployeeCode)
                    WHERE IsDeleted = 0 AND EmployeeCode IS NOT NULL;
            END;

            IF NOT EXISTS
            (
                SELECT 1 FROM sys.indexes
                WHERE name = 'UX_HR_Employee_LoginAccount'
                  AND object_id = OBJECT_ID('dbo.{EmployeeTableName}')
            )
            BEGIN
                CREATE UNIQUE INDEX UX_HR_Employee_LoginAccount
                    ON dbo.{EmployeeTableName}(LoginAccount)
                    WHERE IsDeleted = 0 AND LoginAccount IS NOT NULL;
            END;
            """;

    /// <summary>
    /// 构建旧版员工表迁移 SQL。
    /// </summary>
    private static string BuildEmployeeMigrationSql()
        => $"""
            IF OBJECT_ID('dbo.{LegacyEmployeeTableName}', 'U') IS NOT NULL
               AND NOT EXISTS (SELECT 1 FROM dbo.{EmployeeTableName})
            BEGIN
                SET IDENTITY_INSERT dbo.{EmployeeTableName} ON;

                DECLARE @LegacyEmployeeMigrationSql NVARCHAR(MAX) = N'
                    INSERT INTO dbo.{EmployeeTableName}
                    (
                        Id, EmployeeCode, FirstName, LastName, OrganizationId, Email, PhoneNumber, PhoneNumber2,
                        JobTitle, DepartmentId, PositionId, GroupId, GenderId, ArchivePath, Remarks, LeaveDate,
                        LoginAccount, LoginPassword, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, IsDeleted
                    )
                    SELECT
                        Id,
                        ' + CASE WHEN COL_LENGTH('dbo.{LegacyEmployeeTableName}', 'EmployeeCode') IS NOT NULL THEN N'NULLIF(EmployeeCode, '''')' ELSE N'NULL' END + N',
                        ISNULL(FirstName, N''''),
                        ISNULL(LastName, N''''),
                        ' + CASE WHEN COL_LENGTH('dbo.{LegacyEmployeeTableName}', 'OrganizationId') IS NOT NULL THEN N'OrganizationId' ELSE N'NULL' END + N',
                        ISNULL(Email, N''''),
                        ' + CASE WHEN COL_LENGTH('dbo.{LegacyEmployeeTableName}', 'PhoneNumber') IS NOT NULL THEN N'PhoneNumber' ELSE N'NULL' END + N',
                        ' + CASE WHEN COL_LENGTH('dbo.{LegacyEmployeeTableName}', 'PhoneNumber2') IS NOT NULL THEN N'PhoneNumber2' ELSE N'NULL' END + N',
                        ' + CASE WHEN COL_LENGTH('dbo.{LegacyEmployeeTableName}', 'JobTitle') IS NOT NULL THEN N'JobTitle' ELSE N'NULL' END + N',
                        ' + CASE WHEN COL_LENGTH('dbo.{LegacyEmployeeTableName}', 'DepartmentId') IS NOT NULL THEN N'DepartmentId' ELSE N'NULL' END + N',
                        ' + CASE WHEN COL_LENGTH('dbo.{LegacyEmployeeTableName}', 'PositionId') IS NOT NULL THEN N'PositionId' ELSE N'NULL' END + N',
                        ' + CASE WHEN COL_LENGTH('dbo.{LegacyEmployeeTableName}', 'GroupId') IS NOT NULL THEN N'GroupId' ELSE N'NULL' END + N',
                        ' + CASE WHEN COL_LENGTH('dbo.{LegacyEmployeeTableName}', 'GenderId') IS NOT NULL THEN N'GenderId' ELSE N'NULL' END + N',
                        ' + CASE WHEN COL_LENGTH('dbo.{LegacyEmployeeTableName}', 'ArchivePath') IS NOT NULL THEN N'ArchivePath' ELSE N'NULL' END + N',
                        ' + CASE WHEN COL_LENGTH('dbo.{LegacyEmployeeTableName}', 'Remarks') IS NOT NULL THEN N'Remarks' ELSE N'NULL' END + N',
                        ' + CASE WHEN COL_LENGTH('dbo.{LegacyEmployeeTableName}', 'LeaveDate') IS NOT NULL THEN N'LeaveDate' ELSE N'NULL' END + N',
                        ' + CASE WHEN COL_LENGTH('dbo.{LegacyEmployeeTableName}', 'LoginAccount') IS NOT NULL THEN N'LoginAccount' ELSE N'NULL' END + N',
                        ' + CASE WHEN COL_LENGTH('dbo.{LegacyEmployeeTableName}', 'LoginPassword') IS NOT NULL THEN N'LoginPassword' ELSE N'NULL' END + N',
                        ' + CASE WHEN COL_LENGTH('dbo.{LegacyEmployeeTableName}', 'CreatedAt') IS NOT NULL THEN N'CreatedAt' ELSE N'SYSDATETIME()' END + N',
                        ' + CASE WHEN COL_LENGTH('dbo.{LegacyEmployeeTableName}', 'UpdatedAt') IS NOT NULL THEN N'UpdatedAt' ELSE N'NULL' END + N',
                        ' + CASE WHEN COL_LENGTH('dbo.{LegacyEmployeeTableName}', 'CreatedBy') IS NOT NULL THEN N'CreatedBy' ELSE N'NULL' END + N',
                        ' + CASE WHEN COL_LENGTH('dbo.{LegacyEmployeeTableName}', 'UpdatedBy') IS NOT NULL THEN N'UpdatedBy' ELSE N'NULL' END + N',
                        ' + CASE WHEN COL_LENGTH('dbo.{LegacyEmployeeTableName}', 'IsDeleted') IS NOT NULL THEN N'IsDeleted' ELSE N'0' END + N'
                    FROM dbo.{LegacyEmployeeTableName};';

                EXEC sp_executesql @LegacyEmployeeMigrationSql;
                SET IDENTITY_INSERT dbo.{EmployeeTableName} OFF;
            END;
            """;

    /// <summary>
    /// 构建旧版部门表迁移 SQL。
    /// </summary>
    private static string BuildDepartmentMigrationSql()
        => $"""
            IF OBJECT_ID('dbo.{LegacyDepartmentTableName}', 'U') IS NOT NULL
               AND NOT EXISTS (SELECT 1 FROM dbo.{DepartmentTableName})
            BEGIN
                SET IDENTITY_INSERT dbo.{DepartmentTableName} ON;

                DECLARE @LegacyDepartmentMigrationSql NVARCHAR(MAX) = N'
                    INSERT INTO dbo.{DepartmentTableName}
                    (
                        Id,
                        Name,
                        Description,
                        CreatedAt,
                        UpdatedAt,
                        CreatedBy,
                        UpdatedBy,
                        IsDeleted
                    )
                    SELECT
                        Id,
                        ISNULL(Name, N''''),
                        ' + CASE WHEN COL_LENGTH('dbo.{LegacyDepartmentTableName}', 'Description') IS NOT NULL THEN N'Description' ELSE N'NULL' END + N',
                        ' + CASE WHEN COL_LENGTH('dbo.{LegacyDepartmentTableName}', 'CreatedAt') IS NOT NULL THEN N'CreatedAt' ELSE N'SYSDATETIME()' END + N',
                        ' + CASE WHEN COL_LENGTH('dbo.{LegacyDepartmentTableName}', 'UpdatedAt') IS NOT NULL THEN N'UpdatedAt' ELSE N'NULL' END + N',
                        ' + CASE WHEN COL_LENGTH('dbo.{LegacyDepartmentTableName}', 'CreatedBy') IS NOT NULL THEN N'CreatedBy' ELSE N'NULL' END + N',
                        ' + CASE WHEN COL_LENGTH('dbo.{LegacyDepartmentTableName}', 'UpdatedBy') IS NOT NULL THEN N'UpdatedBy' ELSE N'NULL' END + N',
                        ' + CASE WHEN COL_LENGTH('dbo.{LegacyDepartmentTableName}', 'IsDeleted') IS NOT NULL THEN N'IsDeleted' ELSE N'0' END + N'
                    FROM dbo.{LegacyDepartmentTableName};';

                EXEC sp_executesql @LegacyDepartmentMigrationSql;
                SET IDENTITY_INSERT dbo.{DepartmentTableName} OFF;
            END;
            """;

    /// <summary>
    /// 构建员工查询 SQL 主体。
    /// </summary>
    /// <summary>
    /// 构建旧版职位表迁移 SQL。
    /// </summary>
    private static string BuildPositionMigrationSql()
        => $"""
            IF OBJECT_ID('dbo.{LegacyPositionTableName}', 'U') IS NOT NULL
               AND NOT EXISTS (SELECT 1 FROM dbo.{PositionTableName})
            BEGIN
                SET IDENTITY_INSERT dbo.{PositionTableName} ON;

                DECLARE @LegacyPositionMigrationSql NVARCHAR(MAX) = N'
                    INSERT INTO dbo.{PositionTableName}
                    (
                        Id,
                        Name,
                        Description,
                        CreatedAt,
                        UpdatedAt,
                        CreatedBy,
                        UpdatedBy,
                        IsDeleted
                    )
                    SELECT
                        Id,
                        ISNULL(Name, N''''),
                        ' + CASE WHEN COL_LENGTH('dbo.{LegacyPositionTableName}', 'Description') IS NOT NULL THEN N'Description' ELSE N'NULL' END + N',
                        ' + CASE WHEN COL_LENGTH('dbo.{LegacyPositionTableName}', 'CreatedAt') IS NOT NULL THEN N'CreatedAt' ELSE N'SYSDATETIME()' END + N',
                        ' + CASE WHEN COL_LENGTH('dbo.{LegacyPositionTableName}', 'UpdatedAt') IS NOT NULL THEN N'UpdatedAt' ELSE N'NULL' END + N',
                        ' + CASE WHEN COL_LENGTH('dbo.{LegacyPositionTableName}', 'CreatedBy') IS NOT NULL THEN N'CreatedBy' ELSE N'NULL' END + N',
                        ' + CASE WHEN COL_LENGTH('dbo.{LegacyPositionTableName}', 'UpdatedBy') IS NOT NULL THEN N'UpdatedBy' ELSE N'NULL' END + N',
                        ' + CASE WHEN COL_LENGTH('dbo.{LegacyPositionTableName}', 'IsDeleted') IS NOT NULL THEN N'IsDeleted' ELSE N'0' END + N'
                    FROM dbo.{LegacyPositionTableName};';

                EXEC sp_executesql @LegacyPositionMigrationSql;
                SET IDENTITY_INSERT dbo.{PositionTableName} OFF;
            END;
            """;
    private static string BuildCompanyOrganizationSelectSql()
        => $"""
            SELECT
                Id,
                OrganizationCode,
                OrganizationName,
                CompanyNatureId,
                StatusId,
                EnterpriseTypeId,
                BusinessRegistrationNumber,
                BusinessRegistrationExpiryDate,
                RegionId,
                CityId,
                CountyId,
                Address,
                Principal,
                Phone,
                Fax,
                Email,
                Website,
                WeeklyWorkDays,
                LeaveCountBasisType,
                HolidayType,
                AnnualLeaveCalculationMonthDay,
                AnnualLeaveGrantRule,
                AnnualLeaveGrantMonthDay,
                IsAnnualLeaveClearEnabled,
                AnnualLeaveClearMonthDay,
                IsCarryForwardAnnualLeaveAllowed,
                BaseAnnualLeaveDays,
                AnnualLeaveIncrementStartYears,
                AnnualLeaveIncrementPerYearDays,
                AnnualLeaveCapDays,
                AnnualLeaveMaxAccumulatedDays,
                PaidSickLeaveDaysPerYear,
                PaidSickLeaveSalaryRatio,
                PaidSickLeaveCalculationMonthDay,
                IsPaidSickLeaveClearEnabled,
                PaidSickLeaveClearMonthDay,
                EmployeeMpfMinimumSalary,
                Remarks,
                ArchivePath,
                PrintHeaderContent,
                PrintFooterContent,
                CreatedAt,
                UpdatedAt,
                CreatedBy,
                UpdatedBy,
                IsDeleted
            FROM dbo.{CompanyOrganizationTableName}
            """;

    private static string BuildEmployeeSelectSql(bool includeNav)
        => includeNav
            ? $"""
                SELECT
                    e.Id, e.EmployeeCode, e.FirstName, e.LastName, e.OrganizationId, e.Email, e.PhoneNumber,
                    e.PhoneNumber2, e.PhoneNumber3, e.JobTitle, e.DepartmentId, e.PositionId, e.GroupId, e.GenderId,
                    e.AliasName, e.BirthDate, e.IdCardNumber, e.EthnicityId, e.MaritalStatusId, e.EducationLevelId,
                    e.EducationCertificateNumber, e.ProfessionalTitleId, e.CountryRegionId, e.CityId, e.CountyId,
                    e.Address, e.Remarks, e.EmergencyContact, e.EmergencyContactPhone, e.Referrer, e.ArchivePath,
                    e.PhotoPath, e.HireDate, e.LeaveDate, e.LoginAccount, e.LoginPassword, e.ForceViewRecordDays,
                    e.RoleId, e.AccountValidUntil, e.IsAccountFrozen, e.EmploymentTypeId, e.AllowancePackageId,
                    e.ProbationEndDate, e.AnnualLeaveCalculationMethodId, e.IsAttendanceRequired,
                    e.CurrentYearAnnualLeaveDays, e.AnnualLeaveMaxAccumulatedDays, e.AnnualLeaveRemainingDays,
                    e.AnnualLeaveIncrementStartYears, e.AnnualLeaveIncrementPerYearDays, e.AnnualLeaveCapDays,
                    e.DefaultShiftId, e.SchedulingGroupId, e.IsAutoSchedulingEnabled, e.SalaryGradeId,
                    e.PayrollCompanyId, e.BankAccountNumber, e.BankAccountName, e.BankId, e.CardNumber, e.CreatedAt, e.UpdatedAt,
                    e.CreatedBy, e.UpdatedBy, e.IsDeleted, d.Name AS DepartmentName,
                    CASE WHEN positionType.Id IS NOT NULL THEN positionItem.ItemName ELSE p.Name END AS PositionName,
                    o.OrganizationName
                FROM dbo.{EmployeeTableName} e
                LEFT JOIN dbo.{DepartmentTableName} d ON e.DepartmentId = d.Id
                LEFT JOIN dbo.BD_BasicDataItem positionItem ON e.PositionId = positionItem.Id AND positionItem.IsDeleted = 0
                LEFT JOIN dbo.BD_BasicDataType positionType ON positionItem.TypeId = positionType.Id AND positionType.TypeCode = 'POSITION' AND positionType.IsDeleted = 0
                LEFT JOIN dbo.{PositionTableName} p ON e.PositionId = p.Id
                LEFT JOIN dbo.{CompanyOrganizationTableName} o ON e.OrganizationId = o.Id
                """
            : $"""
                SELECT
                    e.Id, e.EmployeeCode, e.FirstName, e.LastName, e.OrganizationId, e.Email, e.PhoneNumber,
                    e.PhoneNumber2, e.PhoneNumber3, e.JobTitle, e.DepartmentId, e.PositionId, e.GroupId, e.GenderId,
                    e.AliasName, e.BirthDate, e.IdCardNumber, e.EthnicityId, e.MaritalStatusId, e.EducationLevelId,
                    e.EducationCertificateNumber, e.ProfessionalTitleId, e.CountryRegionId, e.CityId, e.CountyId,
                    e.Address, e.Remarks, e.EmergencyContact, e.EmergencyContactPhone, e.Referrer, e.ArchivePath,
                    e.PhotoPath, e.HireDate, e.LeaveDate, e.LoginAccount, e.LoginPassword, e.ForceViewRecordDays,
                    e.RoleId, e.AccountValidUntil, e.IsAccountFrozen, e.EmploymentTypeId, e.AllowancePackageId,
                    e.ProbationEndDate, e.AnnualLeaveCalculationMethodId, e.IsAttendanceRequired,
                    e.CurrentYearAnnualLeaveDays, e.AnnualLeaveMaxAccumulatedDays, e.AnnualLeaveRemainingDays,
                    e.AnnualLeaveIncrementStartYears, e.AnnualLeaveIncrementPerYearDays, e.AnnualLeaveCapDays,
                    e.DefaultShiftId, e.SchedulingGroupId, e.IsAutoSchedulingEnabled, e.SalaryGradeId,
                    e.PayrollCompanyId, e.BankAccountNumber, e.BankAccountName, e.BankId, e.CardNumber, e.CreatedAt, e.UpdatedAt,
                    e.CreatedBy, e.UpdatedBy, e.IsDeleted
                FROM dbo.{EmployeeTableName} e
                """;

    /// <summary>
    /// 将员工查询结果映射为实体对象。
    /// </summary>
    private static Employee MapEmployee(SqlDataReader reader, bool includeNav)
    {
        var employee = new Employee
        {
            Id = reader.GetInt32(0),
            EmployeeCode = ReadNullableString(reader, 1),
            FirstName = ReadNullableString(reader, 2) ?? string.Empty,
            LastName = ReadNullableString(reader, 3) ?? string.Empty,
            OrganizationId = ReadNullableInt(reader, 4),
            Email = ReadNullableString(reader, 5) ?? string.Empty,
            PhoneNumber = ReadNullableString(reader, 6),
            PhoneNumber2 = ReadNullableString(reader, 7),
            PhoneNumber3 = ReadNullableString(reader, 8),
            JobTitle = ReadNullableString(reader, 9),
            DepartmentId = ReadNullableInt(reader, 10),
            PositionId = ReadNullableInt(reader, 11),
            GroupId = ReadNullableInt(reader, 12),
            GenderId = ReadNullableInt(reader, 13),
            AliasName = ReadNullableString(reader, 14),
            BirthDate = ReadNullableDateTime(reader, 15),
            IdCardNumber = ReadNullableString(reader, 16),
            EthnicityId = ReadNullableInt(reader, 17),
            MaritalStatusId = ReadNullableInt(reader, 18),
            EducationLevelId = ReadNullableInt(reader, 19),
            EducationCertificateNumber = ReadNullableString(reader, 20),
            ProfessionalTitleId = ReadNullableInt(reader, 21),
            CountryRegionId = ReadNullableInt(reader, 22),
            CityId = ReadNullableInt(reader, 23),
            CountyId = ReadNullableInt(reader, 24),
            Address = ReadNullableString(reader, 25),
            Remarks = ReadNullableString(reader, 26),
            EmergencyContact = ReadNullableString(reader, 27),
            EmergencyContactPhone = ReadNullableString(reader, 28),
            Referrer = ReadNullableString(reader, 29),
            ArchivePath = ReadNullableString(reader, 30),
            PhotoPath = ReadNullableString(reader, 31),
            HireDate = ReadNullableDateTime(reader, 32),
            LeaveDate = ReadNullableDateTime(reader, 33),
            LoginAccount = ReadNullableString(reader, 34),
            LoginPassword = ReadNullableString(reader, 35),
            ForceViewRecordDays = ReadNullableInt(reader, 36),
            RoleId = ReadNullableInt(reader, 37),
            AccountValidUntil = ReadNullableDateTime(reader, 38),
            IsAccountFrozen = !reader.IsDBNull(39) && reader.GetBoolean(39),
            EmploymentTypeId = ReadNullableInt(reader, 40),
            AllowancePackageId = ReadNullableInt(reader, 41),
            ProbationEndDate = ReadNullableDateTime(reader, 42),
            AnnualLeaveCalculationMethodId = ReadNullableInt(reader, 43),
            IsAttendanceRequired = !reader.IsDBNull(44) && reader.GetBoolean(44),
            CurrentYearAnnualLeaveDays = ReadNullableDecimal(reader, 45),
            AnnualLeaveMaxAccumulatedDays = ReadNullableDecimal(reader, 46),
            AnnualLeaveRemainingDays = ReadNullableDecimal(reader, 47),
            AnnualLeaveIncrementStartYears = ReadNullableInt(reader, 48),
            AnnualLeaveIncrementPerYearDays = ReadNullableDecimal(reader, 49),
            AnnualLeaveCapDays = ReadNullableDecimal(reader, 50),
            DefaultShiftId = ReadNullableInt(reader, 51),
            SchedulingGroupId = ReadNullableInt(reader, 52),
            IsAutoSchedulingEnabled = !reader.IsDBNull(53) && reader.GetBoolean(53),
            SalaryGradeId = ReadNullableInt(reader, 54),
            PayrollCompanyId = ReadNullableInt(reader, 55),
            BankAccountNumber = ReadNullableString(reader, 56),
            BankAccountName = ReadNullableString(reader, 57),
            BankId = ReadNullableInt(reader, 58),
            CardNumber = ReadNullableString(reader, 59),
            CreatedAt = reader.GetDateTime(60),
            UpdatedAt = ReadNullableDateTime(reader, 61),
            CreatedBy = ReadNullableString(reader, 62),
            UpdatedBy = ReadNullableString(reader, 63),
            IsDeleted = reader.GetBoolean(64)
        };

        if (includeNav)
        {
            var departmentName = ReadNullableString(reader, 65);
            var positionName = ReadNullableString(reader, 66);
            var organizationName = ReadNullableString(reader, 67);

            if (!string.IsNullOrWhiteSpace(departmentName))
            {
                employee.Department = new Department
                {
                    Id = employee.DepartmentId ?? 0,
                    Name = departmentName
                };
            }

            if (!string.IsNullOrWhiteSpace(positionName))
            {
                employee.Position = new Position
                {
                    Id = employee.PositionId ?? 0,
                    Name = positionName
                };
            }

            if (!string.IsNullOrWhiteSpace(organizationName))
            {
                employee.Organization = new CompanyOrganization
                {
                    Id = employee.OrganizationId ?? 0,
                    OrganizationName = organizationName
                };
            }
        }

        return employee;
    }

    /// <summary>
    /// 将公司组织查询结果映射为实体对象。
    /// </summary>
    private static CompanyOrganization MapCompanyOrganization(SqlDataReader reader)
    {
        return new CompanyOrganization
        {
            Id = reader.GetInt32(0),
            OrganizationCode = reader.GetString(1),
            OrganizationName = reader.GetString(2),
            CompanyNatureId = ReadNullableInt(reader, 3),
            StatusId = ReadNullableInt(reader, 4),
            EnterpriseTypeId = ReadNullableInt(reader, 5),
            BusinessRegistrationNumber = ReadNullableString(reader, 6),
            BusinessRegistrationExpiryDate = ReadNullableDateTime(reader, 7),
            RegionId = ReadNullableInt(reader, 8),
            CityId = ReadNullableInt(reader, 9),
            CountyId = ReadNullableInt(reader, 10),
            Address = ReadNullableString(reader, 11),
            Principal = ReadNullableString(reader, 12),
            Phone = ReadNullableString(reader, 13),
            Fax = ReadNullableString(reader, 14),
            Email = ReadNullableString(reader, 15),
            Website = ReadNullableString(reader, 16),
            WeeklyWorkDays = ReadNullableDecimal(reader, 17),
            LeaveCountBasisType = ReadNullableInt(reader, 18),
            HolidayType = ReadNullableInt(reader, 19),
            AnnualLeaveCalculationMonthDay = ReadNullableString(reader, 20),
            AnnualLeaveGrantRule = ReadNullableInt(reader, 21),
            AnnualLeaveGrantMonthDay = ReadNullableString(reader, 22),
            IsAnnualLeaveClearEnabled = reader.GetBoolean(23),
            AnnualLeaveClearMonthDay = ReadNullableString(reader, 24),
            IsCarryForwardAnnualLeaveAllowed = reader.GetBoolean(25),
            BaseAnnualLeaveDays = ReadNullableDecimal(reader, 26),
            AnnualLeaveIncrementStartYears = ReadNullableInt(reader, 27),
            AnnualLeaveIncrementPerYearDays = ReadNullableDecimal(reader, 28),
            AnnualLeaveCapDays = ReadNullableDecimal(reader, 29),
            AnnualLeaveMaxAccumulatedDays = ReadNullableDecimal(reader, 30),
            PaidSickLeaveDaysPerYear = ReadNullableDecimal(reader, 31),
            PaidSickLeaveSalaryRatio = ReadNullableDecimal(reader, 32),
            PaidSickLeaveCalculationMonthDay = ReadNullableString(reader, 33),
            IsPaidSickLeaveClearEnabled = reader.GetBoolean(34),
            PaidSickLeaveClearMonthDay = ReadNullableString(reader, 35),
            EmployeeMpfMinimumSalary = ReadNullableDecimal(reader, 36),
            Remarks = ReadNullableString(reader, 37),
            ArchivePath = ReadNullableString(reader, 38),
            PrintHeaderContent = ReadNullableString(reader, 39),
            PrintFooterContent = ReadNullableString(reader, 40),
            CreatedAt = reader.GetDateTime(41),
            UpdatedAt = ReadNullableDateTime(reader, 42),
            CreatedBy = ReadNullableString(reader, 43),
            UpdatedBy = ReadNullableString(reader, 44),
            IsDeleted = reader.GetBoolean(45)
        };
    }

    /// <summary>
    /// 映射公司组织银行账号记录（将查询结果转换为银行账号实体）。
    /// </summary>
    private static CompanyOrganizationBankAccount MapCompanyOrganizationBankAccount(SqlDataReader reader)
        => new()
        {
            Id = reader.GetInt32(0),
            CompanyOrganizationId = reader.GetInt32(1),
            AccountNumber = ReadNullableString(reader, 2) ?? string.Empty,
            BankId = ReadNullableInt(reader, 3),
            BankName = ReadNullableString(reader, 4),
            BranchName = ReadNullableString(reader, 5) ?? string.Empty,
            BranchAddress = ReadNullableString(reader, 6),
            CurrencyCode = ReadNullableString(reader, 7) ?? "RMB",
            StatusCode = ReadNullableString(reader, 8) ?? "NORMAL",
            SubjectCode = ReadNullableString(reader, 9),
            SubjectName = ReadNullableString(reader, 10),
            Remarks = ReadNullableString(reader, 11),
            IsDefault = !reader.IsDBNull(12) && reader.GetBoolean(12),
            CreatedAt = reader.GetDateTime(13),
            UpdatedAt = ReadNullableDateTime(reader, 14),
            CreatedBy = ReadNullableString(reader, 15),
            UpdatedBy = ReadNullableString(reader, 16),
            IsDeleted = reader.GetBoolean(17)
        };

    /// <summary>
    /// 映射公司组织单号规则记录（将查询结果转换为单号规则实体）。
    /// </summary>
    private static CompanyOrganizationDocumentNumberRule MapCompanyOrganizationDocumentNumberRule(SqlDataReader reader)
        => new()
        {
            Id = reader.GetInt32(0),
            CompanyOrganizationId = reader.GetInt32(1),
            DocumentTypeCode = ReadNullableString(reader, 2) ?? string.Empty,
            Prefix = ReadNullableString(reader, 3) ?? string.Empty,
            DateFormatCode = ReadNullableString(reader, 4) ?? "yyMM",
            SequenceLength = reader.IsDBNull(5) ? 5 : reader.GetInt32(5),
            LastDateSegment = ReadNullableString(reader, 6),
            LastSequenceValue = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
            LastGeneratedNumber = ReadNullableString(reader, 8),
            CreatedAt = reader.GetDateTime(9),
            UpdatedAt = ReadNullableDateTime(reader, 10),
            CreatedBy = ReadNullableString(reader, 11),
            UpdatedBy = ReadNullableString(reader, 12),
            IsDeleted = reader.GetBoolean(13)
        };

    /// <summary>
    /// 初始化公司组织演示数据（仅在空表时写入）。
    /// </summary>
    private static async Task SeedCompanyOrganizationsAsync(SqlConnection conn)
    {
        var sql = $"""
            IF EXISTS (SELECT 1 FROM dbo.{CompanyOrganizationTableName} WHERE IsDeleted = 0)
                RETURN;

            DECLARE @PrivateNatureId INT =
            (
                SELECT TOP (1) item.Id
                FROM dbo.BD_BasicDataItem item
                INNER JOIN dbo.BD_BasicDataType type ON type.Id = item.TypeId
                WHERE type.TypeCode = 'COMPANY_NATURE' AND item.ItemCode = 'PRIVATE' AND item.IsDeleted = 0
            );

            DECLARE @ActiveStatusId INT =
            (
                SELECT TOP (1) item.Id
                FROM dbo.BD_BasicDataItem item
                INNER JOIN dbo.BD_BasicDataType type ON type.Id = item.TypeId
                WHERE type.TypeCode = 'ORG_STATUS' AND item.ItemCode = 'ACTIVE' AND item.IsDeleted = 0
            );

            DECLARE @LlcTypeId INT =
            (
                SELECT TOP (1) item.Id
                FROM dbo.BD_BasicDataItem item
                INNER JOIN dbo.BD_BasicDataType type ON type.Id = item.TypeId
                WHERE type.TypeCode = 'ENTERPRISE_TYPE' AND item.ItemCode = 'LLC' AND item.IsDeleted = 0
            );

            DECLARE @ChinaCountryRegionId INT =
            (
                SELECT TOP (1) item.Id
                FROM dbo.BD_BasicDataItem item
                INNER JOIN dbo.BD_BasicDataType type ON type.Id = item.TypeId
                WHERE type.TypeCode = 'COUNTRY_REGION' AND item.ItemCode = 'CN' AND item.IsDeleted = 0
            );

            DECLARE @GuangzhouCityId INT =
            (
                SELECT TOP (1) item.Id
                FROM dbo.BD_BasicDataItem item
                INNER JOIN dbo.BD_BasicDataType type ON type.Id = item.TypeId
                WHERE type.TypeCode = 'CITY' AND item.ItemCode = 'GUANGZHOU' AND item.IsDeleted = 0
            );

            DECLARE @TianheCountyId INT =
            (
                SELECT TOP (1) item.Id
                FROM dbo.BD_BasicDataItem item
                INNER JOIN dbo.BD_BasicDataType type ON type.Id = item.TypeId
                WHERE type.TypeCode = 'COUNTY' AND item.ItemCode = 'TIANHE' AND item.IsDeleted = 0
            );

            INSERT INTO dbo.{CompanyOrganizationTableName}
            (
                OrganizationCode, OrganizationName, CompanyNatureId, StatusId, EnterpriseTypeId,
                RegionId, CityId, CountyId, Address, Principal, Phone, Fax, Email, Website,
                WeeklyWorkDays, Remarks, ArchivePath, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, IsDeleted
            )
            VALUES
            (
                N'HYT', N'宏易科技有限公司', @PrivateNatureId, @ActiveStatusId, @LlcTypeId,
                @ChinaCountryRegionId, @GuangzhouCityId, @TianheCountyId,
                N'广州市天河区软件路 18 号', N'黄总', N'(020) 6666 8888', N'(020) 6666 8889',
                N'jeky@honeyi.com', N'https://www.honeyi.com', 5, N'示例公司组织资料', N'/archive/company/hyt',
                DATEADD(DAY, -18, SYSDATETIME()), DATEADD(DAY, -2, SYSDATETIME()), N'Admin', N'Admin', 0
            ),
            (
                N'HYC', N'鸿易信息技术有限公司', @PrivateNatureId, @ActiveStatusId, @LlcTypeId,
                @ChinaCountryRegionId, @GuangzhouCityId, @TianheCountyId,
                N'广州市天河区科韵路 28 号', N'黄总', N'(020) 6666 8890', N'(020) 6666 8891',
                N'contact@hongyiit.com', N'https://www.hongyiit.com', 5, N'用于演示分子公司组织资料', N'/archive/company/hyc',
                DATEADD(DAY, -12, SYSDATETIME()), DATEADD(DAY, -1, SYSDATETIME()), N'Admin', N'Admin', 0
            ),
            (
                N'GMT', N'广州元科技有限公司', @PrivateNatureId, @ActiveStatusId, @LlcTypeId,
                @ChinaCountryRegionId, @GuangzhouCityId, @TianheCountyId,
                N'广州市天河区智慧园 8 栋', N'陈太文', N'(020) 6666 8892', N'(020) 6666 8893',
                N'hello@gmt-tech.com', N'https://www.gmt-tech.com', 5, N'可继续扩展为多组织架构', N'/archive/company/gmt',
                DATEADD(DAY, -8, SYSDATETIME()), NULL, N'Admin', NULL, 0
            );
            """;

        await ExecuteNonQueryAsync(conn, sql);
    }

    /// <summary>
    /// 绑定员工资料写入参数（用于新增与编辑）。
    /// </summary>
    /// <summary>
    /// 初始化员工演示数据（仅在员工表为空时写入三条员工记录）。
    /// </summary>
    private static async Task SeedEmployeesAsync(SqlConnection conn)
    {
        var sql = $"""
            IF EXISTS (SELECT 1 FROM dbo.{EmployeeTableName} WHERE IsDeleted = 0)
            BEGIN
                RETURN;
            END;

            INSERT INTO dbo.{EmployeeTableName}
            (
                EmployeeCode, FirstName, LastName, OrganizationId, Email, PhoneNumber, PhoneNumber2, JobTitle,
                DepartmentId, PositionId, GroupId, GenderId, ArchivePath, Remarks, LoginAccount, LoginPassword,
                CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, IsDeleted
            )
            VALUES
            (
                N'A001', N'文', N'陈',
                (SELECT TOP (1) Id FROM dbo.{CompanyOrganizationTableName} WHERE OrganizationCode = N'HYT' AND IsDeleted = 0 ORDER BY Id),
                N'jeky@honeyi.com', N'(020) 6666 8888', N'(020) 6666 8889', N'总经理',
                (SELECT TOP (1) Id FROM dbo.{DepartmentTableName} WHERE Name = N'IT' AND IsDeleted = 0 ORDER BY Id),
                (
                    SELECT TOP (1) item.Id
                    FROM dbo.BD_BasicDataItem item
                    INNER JOIN dbo.BD_BasicDataType type ON type.Id = item.TypeId
                    WHERE type.TypeCode = 'POSITION' AND item.ItemCode = 'GENERAL_MANAGER' AND item.IsDeleted = 0
                ),
                1, 1, N'/archive/employee/a001', N'示例员工资料', N'A001', N'123456',
                DATEADD(DAY, -10, SYSDATETIME()), DATEADD(DAY, -2, SYSDATETIME()), N'Admin', N'Admin', 0
            ),
            (
                N'A002', N'红', N'林',
                (SELECT TOP (1) Id FROM dbo.{CompanyOrganizationTableName} WHERE OrganizationCode = N'HYC' AND IsDeleted = 0 ORDER BY Id),
                N'linhong@hongyiit.com', N'(020) 6666 8888', N'(020) 6666 8889', N'会计',
                (SELECT TOP (1) Id FROM dbo.{DepartmentTableName} WHERE Name = N'IT' AND IsDeleted = 0 ORDER BY Id),
                (
                    SELECT TOP (1) item.Id
                    FROM dbo.BD_BasicDataItem item
                    INNER JOIN dbo.BD_BasicDataType type ON type.Id = item.TypeId
                    WHERE type.TypeCode = 'POSITION' AND item.ItemCode = 'ACCOUNTANT' AND item.IsDeleted = 0
                ),
                1, 2, N'/archive/employee/a002', N'示例员工资料', N'A002', N'123456',
                DATEADD(DAY, -8, SYSDATETIME()), DATEADD(DAY, -1, SYSDATETIME()), N'Admin', N'Admin', 0
            ),
            (
                N'A003', N'英', N'黄',
                (SELECT TOP (1) Id FROM dbo.{CompanyOrganizationTableName} WHERE OrganizationCode = N'GMT' AND IsDeleted = 0 ORDER BY Id),
                N'hello@gmt-tech.com', N'(020) 6666 8888', N'(020) 6666 8889', N'人事专员',
                (SELECT TOP (1) Id FROM dbo.{DepartmentTableName} WHERE Name = N'IT' AND IsDeleted = 0 ORDER BY Id),
                (
                    SELECT TOP (1) item.Id
                    FROM dbo.BD_BasicDataItem item
                    INNER JOIN dbo.BD_BasicDataType type ON type.Id = item.TypeId
                    WHERE type.TypeCode = 'POSITION' AND item.ItemCode = 'HR_SPECIALIST' AND item.IsDeleted = 0
                ),
                1, 2, N'/archive/employee/a003', N'示例员工资料', N'A003', N'123456',
                DATEADD(DAY, -6, SYSDATETIME()), NULL, N'Admin', NULL, 0
            );
            """;

        await ExecuteNonQueryAsync(conn, sql);
    }

    /// <summary>
    /// 构建角色表初始化 SQL。
    /// </summary>
    private static string BuildRoleInitializeSql()
        => $"""
            IF OBJECT_ID('dbo.{RoleTableName}', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.{RoleTableName}
                (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    RoleCode NVARCHAR(50) NOT NULL,
                    RoleName NVARCHAR(100) NOT NULL,
                    Description NVARCHAR(500) NULL,
                    SortOrder INT NOT NULL CONSTRAINT DF_HR_Role_SortOrder DEFAULT(0),
                    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_HR_Role_CreatedAt DEFAULT(SYSDATETIME()),
                    UpdatedAt DATETIME2 NULL,
                    CreatedBy NVARCHAR(50) NULL,
                    UpdatedBy NVARCHAR(50) NULL,
                    IsDeleted BIT NOT NULL CONSTRAINT DF_HR_Role_IsDeleted DEFAULT(0)
                );
            END;
            """;

    /// <summary>
    /// 构建功能权限表初始化 SQL。
    /// </summary>
    private static string BuildPermissionInitializeSql()
        => $"""
            IF OBJECT_ID('dbo.{PermissionTableName}', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.{PermissionTableName}
                (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    PermissionCode NVARCHAR(100) NOT NULL,
                    PermissionName NVARCHAR(200) NOT NULL,
                    Category NVARCHAR(100) NULL,
                    SortOrder INT NOT NULL CONSTRAINT DF_HR_Permission_SortOrder DEFAULT(0),
                    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_HR_Permission_CreatedAt DEFAULT(SYSDATETIME()),
                    UpdatedAt DATETIME2 NULL,
                    CreatedBy NVARCHAR(50) NULL,
                    UpdatedBy NVARCHAR(50) NULL,
                    IsDeleted BIT NOT NULL CONSTRAINT DF_HR_Permission_IsDeleted DEFAULT(0)
                );
            END;
            """;

    /// <summary>
    /// 构建角色权限关联表初始化 SQL。
    /// </summary>
    private static string BuildRolePermissionInitializeSql()
        => $"""
            IF OBJECT_ID('dbo.{RolePermissionTableName}', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.{RolePermissionTableName}
                (
                    RoleId INT NOT NULL,
                    PermissionId INT NOT NULL,
                    CONSTRAINT PK_{RolePermissionTableName} PRIMARY KEY (RoleId, PermissionId)
                );
            END;
            """;

    /// <summary>
    /// 构建用户权限关联表初始化 SQL。
    /// </summary>
    private static string BuildUserPermissionInitializeSql()
        => $"""
            IF OBJECT_ID('dbo.{UserPermissionTableName}', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.{UserPermissionTableName}
                (
                    EmployeeId INT NOT NULL,
                    PermissionId INT NOT NULL,
                    CONSTRAINT PK_{UserPermissionTableName} PRIMARY KEY (EmployeeId, PermissionId)
                );
            END;
            """;

    /// <summary>
    /// 构建用户管理公司关联表初始化 SQL。
    /// </summary>
    private static string BuildUserCompanyInitializeSql()
        => $"""
            IF OBJECT_ID('dbo.{UserCompanyTableName}', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.{UserCompanyTableName}
                (
                    EmployeeId INT NOT NULL,
                    CompanyOrganizationId INT NOT NULL,
                    CONSTRAINT PK_{UserCompanyTableName} PRIMARY KEY (EmployeeId, CompanyOrganizationId)
                );
            END;
            """;

    /// <summary>
    /// 构建通用任务跟进表初始化 SQL。
    /// </summary>
    private static string BuildTaskFollowUpInitializeSql()
        => $"""
            IF OBJECT_ID('dbo.{TaskFollowUpTableName}', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.{TaskFollowUpTableName}
                (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    FeatureCode NVARCHAR(50) NOT NULL,
                    EntityId INT NOT NULL,
                    TaskCode NVARCHAR(30) NOT NULL,
                    ArchivePath NVARCHAR(500) NULL,
                    StatusCode NVARCHAR(30) NOT NULL,
                    PlannedDate DATETIME2 NULL,
                    TaskTypeCode NVARCHAR(30) NOT NULL,
                    ExecutorName NVARCHAR(100) NULL,
                    Description NVARCHAR(1000) NULL,
                    PriorityCode NVARCHAR(30) NOT NULL,
                    ProgressPercent INT NOT NULL CONSTRAINT DF_SYS_TaskFollowUp_ProgressPercent DEFAULT(0),
                    CompletedDate DATETIME2 NULL,
                    ProjectCode NVARCHAR(50) NULL,
                    InitiatorName NVARCHAR(100) NULL,
                    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_SYS_TaskFollowUp_CreatedAt DEFAULT(SYSDATETIME()),
                    UpdatedAt DATETIME2 NULL,
                    CreatedBy NVARCHAR(50) NULL,
                    UpdatedBy NVARCHAR(50) NULL,
                    IsDeleted BIT NOT NULL CONSTRAINT DF_SYS_TaskFollowUp_IsDeleted DEFAULT(0)
                );

                CREATE INDEX IX_SYS_TaskFollowUp_FeatureEntity
                    ON dbo.{TaskFollowUpTableName}(FeatureCode, EntityId, IsDeleted, CreatedAt DESC);
            END;
            """;

    /// <summary>
    /// 构建员工培训历程表初始化 SQL。
    /// </summary>
    private static string BuildEmployeeTrainingExperienceInitializeSql()
        => $"""
            IF OBJECT_ID('dbo.{EmployeeTrainingExperienceTableName}', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.{EmployeeTrainingExperienceTableName}
                (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    EmployeeId INT NOT NULL,
                    ExperienceTypeCode NVARCHAR(30) NOT NULL,
                    StartDate DATETIME2 NOT NULL,
                    EndDate DATETIME2 NULL,
                    Description NVARCHAR(1000) NOT NULL,
                    CertificateName NVARCHAR(200) NULL,
                    OrganizationName NVARCHAR(200) NULL,
                    ArchivePath NVARCHAR(500) NULL,
                    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_HR_EmployeeTrainingExperience_CreatedAt DEFAULT(SYSDATETIME()),
                    UpdatedAt DATETIME2 NULL,
                    CreatedBy NVARCHAR(50) NULL,
                    UpdatedBy NVARCHAR(50) NULL,
                    IsDeleted BIT NOT NULL CONSTRAINT DF_HR_EmployeeTrainingExperience_IsDeleted DEFAULT(0)
                );

                CREATE INDEX IX_HR_EmployeeTrainingExperience_Employee
                    ON dbo.{EmployeeTrainingExperienceTableName}(EmployeeId, IsDeleted, StartDate DESC, CreatedAt DESC);
            END;
            """;

    /// <summary>
    /// 初始化角色演示数据（系统管理员、普通用户）。
    /// </summary>
    private static async Task SeedRolesAsync(SqlConnection conn)
    {
        var sql = $"""
            IF EXISTS (SELECT 1 FROM dbo.{RoleTableName} WHERE IsDeleted = 0)
                RETURN;

            INSERT INTO dbo.{RoleTableName} (RoleCode, RoleName, Description, SortOrder, CreatedAt, IsDeleted)
            VALUES
                (N'ADMIN', N'系统管理员', N'拥有系统全部功能的完整操作权限', 1, SYSDATETIME(), 0),
                (N'USER', N'普通用户', N'拥有基础功能的查看和操作权限', 2, SYSDATETIME(), 0),
                (N'VIEWER', N'只读用户', N'仅有查看权限，不可编辑和操作', 3, SYSDATETIME(), 0);
            """;

        await ExecuteNonQueryAsync(conn, sql);
    }

    /// <summary>
    /// 初始化功能权限数据（基于系统导航菜单定义）。
    /// </summary>
    private static async Task SeedPermissionsAsync(SqlConnection conn)
    {
        foreach (var definition in GetPermissionSeedDefinitions())
        {
            var sql = $"""
                IF EXISTS (SELECT 1 FROM dbo.{PermissionTableName} WHERE PermissionCode = @PermissionCode)
                BEGIN
                    UPDATE dbo.{PermissionTableName}
                    SET PermissionName = @PermissionName,
                        Category = @Category,
                        SortOrder = @SortOrder,
                        IsDeleted = 0
                    WHERE PermissionCode = @PermissionCode;
                END
                ELSE
                BEGIN
                    INSERT INTO dbo.{PermissionTableName}
                    (
                        PermissionCode,
                        PermissionName,
                        Category,
                        SortOrder,
                        CreatedAt,
                        IsDeleted
                    )
                    VALUES
                    (
                        @PermissionCode,
                        @PermissionName,
                        @Category,
                        @SortOrder,
                        SYSDATETIME(),
                        0
                    );
                END;
                """;

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@PermissionCode", definition.PermissionCode));
            cmd.Parameters.Add(new SqlParameter("@PermissionName", definition.PermissionName));
            cmd.Parameters.Add(new SqlParameter("@Category", definition.Category));
            cmd.Parameters.Add(new SqlParameter("@SortOrder", definition.SortOrder));
            await cmd.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// 初始化角色权限关联数据（系统管理员拥有所有权限）。
    /// </summary>
    private static async Task SeedRolePermissionsAsync(SqlConnection conn)
    {
        var adminPermissionSql = $"""
            INSERT INTO dbo.{RolePermissionTableName} (RoleId, PermissionId)
            SELECT r.Id, p.Id
            FROM dbo.{RoleTableName} r
            CROSS JOIN dbo.{PermissionTableName} p
            WHERE r.RoleCode = N'ADMIN'
              AND r.IsDeleted = 0
              AND p.IsDeleted = 0
              AND NOT EXISTS
              (
                  SELECT 1
                  FROM dbo.{RolePermissionTableName} rp
                  WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id
              );
            """;

        await ExecuteNonQueryAsync(conn, adminPermissionSql);

        foreach (var permissionCode in GetCommonUserDefaultPermissionCodes())
        {
            var userPermissionSql = $"""
                INSERT INTO dbo.{RolePermissionTableName} (RoleId, PermissionId)
                SELECT r.Id, p.Id
                FROM dbo.{RoleTableName} r
                INNER JOIN dbo.{PermissionTableName} p ON p.PermissionCode = @PermissionCode AND p.IsDeleted = 0
                WHERE r.RoleCode = N'USER'
                  AND r.IsDeleted = 0
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM dbo.{RolePermissionTableName} rp
                      WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id
                  );
                """;

            await using var cmd = new SqlCommand(userPermissionSql, conn);
            cmd.Parameters.Add(new SqlParameter("@PermissionCode", permissionCode));
            await cmd.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// 系统导航权限种子定义（对应左侧导航与已落地的业务页面）。
    /// </summary>
    private static IReadOnlyList<(string PermissionCode, string PermissionName, string Category, int SortOrder)> GetPermissionSeedDefinitions()
    {
        return
        [
            ("DASHBOARD", "首页", "首页", 10),
            ("PURCHASING", "供应链管理", "供应链管理", 20),
            ("PURCHASING_SUPPLIERS", "供应商/厂商资料", "供应链管理", 21),
            ("PURCHASING_CONTRACTS", "合约管理", "供应链管理", 22),
            ("PURCHASING_INQUIRIES", "采购询价", "供应链管理", 23),
            ("PURCHASING_PURCHASE_ORDERS", "采购订单", "供应链管理", 24),
            ("PURCHASING_INVOICES", "采购发票", "供应链管理", 25),
            ("PURCHASING_PAYMENTS", "采购付款", "供应链管理", 26),
            ("PURCHASING_PREPAYMENTS", "预付款", "供应链管理", 27),
            ("PURCHASING_RETURNS", "采购退换", "供应链管理", 28),
            ("SALES", "销售管理", "销售管理", 30),
            ("SALES_QUOTATIONS", "销售报价", "销售管理", 31),
            ("SALES_SALES_ORDERS", "销售订单", "销售管理", 32),
            ("SALES_DELIVERIES", "销售送货", "销售管理", 33),
            ("SALES_INVOICES", "销售发票", "销售管理", 34),
            ("SALES_RECEIPTS", "销售收款", "销售管理", 35),
            ("SALES_ADVANCE_RECEIPTS", "预收款", "销售管理", 36),
            ("SALES_RETURNS", "销售退换", "销售管理", 37),
            ("SALES_RECURRING_INVOICES", "定期发票", "销售管理", 38),
            ("SALES_CUSTOMERS", "客户资料", "销售管理", 39),
            ("LOGISTICS", "仓务管理", "仓务管理", 40),
            ("LOGISTICS_WORKSHOP_WAREHOUSES", "车间仓库资料", "仓务管理", 41),
            ("LOGISTICS_CARRIERS", "承运商资料", "仓务管理", 42),
            ("LOGISTICS_SHIPMENTS", "出货管理", "仓务管理", 43),
            ("LOGISTICS_MATERIAL_MANAGEMENT", "物料管理", "仓务管理", 44),
            ("LOGISTICS_PRODUCTS", "产品", "仓务管理", 45),
            ("LOGISTICS_MATERIALS", "材料", "仓务管理", 46),
            ("LOGISTICS_AUXILIARY_MATERIALS", "辅料", "仓务管理", 47),
            ("LOGISTICS_INVENTORY_QUERY", "库存查询", "仓务管理", 48),
            ("LOGISTICS_PRODUCT_INVENTORY", "产品库存", "仓务管理", 481),
            ("LOGISTICS_MATERIAL_INVENTORY", "材料库存", "仓务管理", 482),
            ("LOGISTICS_AUXILIARY_MATERIAL_INVENTORY", "辅料库存", "仓务管理", 483),
            ("LOGISTICS_INBOUND_MANAGEMENT", "入库管理", "仓务管理", 49),
            ("LOGISTICS_RECEIVING_NOTICES", "收货通知", "仓务管理", 491),
            ("LOGISTICS_INBOUND_VERIFICATIONS", "入库核实", "仓务管理", 492),
            ("LOGISTICS_INBOUND_IQC", "入库IQC", "仓务管理", 493),
            ("LOGISTICS_INBOUND_ORDERS", "入库单", "仓务管理", 494),
            ("LOGISTICS_INBOUND_RECORDS", "入库记录", "仓务管理", 495),
            ("LOGISTICS_OUTBOUND_MANAGEMENT", "出库管理", "仓务管理", 50),
            ("LOGISTICS_SHIPPING_NOTICES", "出货通知", "仓务管理", 501),
            ("LOGISTICS_OUTBOUND_VERIFICATIONS", "出库核实", "仓务管理", 502),
            ("LOGISTICS_OUTBOUND_ORDERS", "出库单", "仓务管理", 503),
            ("LOGISTICS_OUTBOUND_RECORDS", "出库记录", "仓务管理", 504),
            ("LOGISTICS_MATERIAL_REQUISITIONS", "领料单", "仓务管理", 51),
            ("LOGISTICS_RETURN_ORDERS", "退货单", "仓务管理", 52),
            ("LOGISTICS_TRANSFER_ORDERS", "调拨单", "仓务管理", 53),
            ("LOGISTICS_REPLENISHMENT_ORDERS", "补货单", "仓务管理", 54),
            ("LOGISTICS_RELOCATION_ORDERS", "转位单", "仓务管理", 55),
            ("LOGISTICS_ADJUSTMENT_ORDERS", "调整单", "仓务管理", 56),
            ("LOGISTICS_STOCKTAKES", "盘点管理", "仓务管理", 57),
            ("LOGISTICS_STOCKTAKE_ORDERS", "盘点单", "仓务管理", 571),
            ("LOGISTICS_CYCLE_COUNT_PLANS", "周期盘点计划", "仓务管理", 572),
            ("LOGISTICS_BATCHES", "批次管理", "仓务管理", 58),
            ("LOGISTICS_RFID_RECORDS", "RFID记录", "仓务管理", 59),
            ("PRODUCTION", "生产管理", "生产管理", 50),
            ("PRODUCTION_EQUIPMENT", "生产设备", "生产管理", 501),
            ("PRODUCTION_PROCESSES", "工序管理", "生产管理", 502),
            ("PRODUCTION_SAMPLE_WORK_ORDERS", "样板工单", "生产管理", 503),
            ("PRODUCTION_ORDERS", "生产工单", "生产管理", 504),
            ("PRODUCTION_SCHEDULING_PLANS", "排产计划", "生产管理", 505),
            ("PRODUCTION_WORKPIECE_ENTRIES", "工件录入", "生产管理", 506),
            ("PRODUCTION_PROGRESS", "生产进度", "生产管理", 507),
            ("PRODUCTION_FINISHED_PRODUCT_DISASSEMBLY", "成品拆解", "生产管理", 508),
            ("PRODUCTION_FINISHED_PRODUCT_ASSEMBLY", "成品组装", "生产管理", 509),
            ("PRODUCTION_OUTSOURCED_PROCESSING", "外发加工", "生产管理", 510),
            ("PRODUCTION_WORK_CENTERS", "工作中心", "生产管理", 52),
            ("FINANCE", "财务管理", "财务管理", 60),
            ("FINANCE_ACCOUNTS", "科目资料", "财务管理", 601),
            ("FINANCE_RECEIPTS", "收款管理", "财务管理", 602),
            ("FINANCE_ACCOUNTS_RECEIVABLE", "应收账", "财务管理", 6021),
            ("FINANCE_DEBIT_NOTES", "借记单", "财务管理", 6022),
            ("FINANCE_ADVANCE_RECEIPTS", "预收款", "财务管理", 6023),
            ("FINANCE_RECEIPT_ORDERS", "收款单", "财务管理", 6024),
            ("FINANCE_OTHER_RECEIPTS", "其他收款", "财务管理", 6025),
            ("FINANCE_PAYMENTS", "付款管理", "财务管理", 603),
            ("FINANCE_ACCOUNTS_PAYABLE", "应付账", "财务管理", 6031),
            ("FINANCE_CREDIT_NOTES", "贷记单", "财务管理", 6032),
            ("FINANCE_ADVANCE_PAYMENTS", "预付款", "财务管理", 6033),
            ("FINANCE_PAYMENT_ORDERS", "付款单", "财务管理", 6034),
            ("FINANCE_OTHER_PAYMENTS", "其他付款", "财务管理", 6035),
            ("FINANCE_EXPENSE_REIMBURSEMENTS", "费用报销", "财务管理", 604),
            ("FINANCE_PAYROLL_RECORDS", "工资记录", "财务管理", 605),
            ("FINANCE_ACCOUNTING_VOUCHERS", "记账凭证", "财务管理", 606),
            ("FINANCE_BATCH_POSTING", "批量记账", "财务管理", 607),
            ("FINANCE_VOUCHER_REVIEWS", "凭证审核", "财务管理", 608),
            ("FINANCE_CHECK_CASHINGS", "支票兑现", "财务管理", 609),
            ("FINANCE_BANK_RECONCILIATIONS", "银行对账", "财务管理", 610),
            ("FINANCE_COST_ADJUSTMENTS", "成本调整", "财务管理", 611),
            ("FINANCE_ACCRUED_RECEIPTS", "暂估入库", "财务管理", 612),
            ("FINANCE_COST_TRANSFERS", "成本转移", "财务管理", 613),
            ("FINANCE_INVENTORY_MONTHLY_CLOSING", "库存月结", "财务管理", 614),
            ("FINANCE_MONTHLY_POSTING", "月结过账", "财务管理", 615),
            ("FINANCE_YEARLY_POSTING", "年结过账", "财务管理", 616),
            ("FINANCE_YEAR_OPENING_BALANCES", "年初始账", "财务管理", 617),
            ("FINANCE_STATISTICS", "财务统计", "财务管理", 618),
            ("FINANCE_ACCOUNT_BALANCE_REPORTS", "科目余额表", "财务管理", 6181),
            ("FINANCE_DETAIL_LEDGERS", "明细账", "财务管理", 6182),
            ("FINANCE_INCOME_STATEMENTS", "利润表", "财务管理", 6183),
            ("FINANCE_CASH_FLOW_STATEMENTS", "现金流量表", "财务管理", 6184),
            ("FINANCE_BALANCE_SHEETS", "资产负债表", "财务管理", 6185),
            ("FINANCE_TRANSACTIONS", "交易流水", "财务管理", 62),
            ("HR", "人事管理", "人事管理", 70),
            ("HR_EMPLOYEES", "员工资料", "人事管理", 71),
            ("HR_COMPANY_ORGANIZATIONS", "公司组织", "人事管理", 72),
            ("HR_DEPARTMENTS", "部门资料", "人事管理", 73),
            ("HR_EMPLOYEE_TRAINING", "员工培训", "人事管理", 74),
            ("HR_PERSONNEL_CHANGES", "人事变更", "人事管理", 75),
            ("HR_HOLIDAY_MANAGEMENT", "节日管理", "人事管理", 76),
            ("HR_ATTENDANCE_MANAGEMENT", "考勤管理", "人事管理", 77),
            ("HR_ATTENDANCE_SHIFT_SETTINGS", "班次设置", "人事管理", 771),
            ("HR_ATTENDANCE_SCHEDULING", "考勤排班", "人事管理", 772),
            ("HR_ATTENDANCE_RECORDS", "出勤记录", "人事管理", 773),
            ("HR_LEAVE_REGISTRATIONS", "请假登记", "人事管理", 774),
            ("HR_LEAVE_APPROVALS", "请假审批", "人事管理", 775),
            ("HR_FIELD_WORK_REGISTRATIONS", "外勤登记", "人事管理", 776),
            ("HR_ATTENDANCE_DEVICES", "考勤机管理", "人事管理", 777),
            ("HR_ATTENDANCE_ADJUSTMENTS", "应补应扣", "人事管理", 778),
            ("HR_ATTENDANCE_STATISTICS", "考勤统计", "人事管理", 779),
            ("CRM", "CRM管理", "CRM管理", 80),
            ("CRM_CUSTOMERS", "客户资料", "CRM管理", 811),
            ("CRM_BUSINESS_PARTNERS", "友商资料", "CRM管理", 812),
            ("CRM_CONTRACTS", "合约管理", "CRM管理", 813),
            ("CRM_COMMISSION_RULES", "佣金规则", "CRM管理", 814),
            ("CRM_CUSTOMER_VISITS", "客户拜访", "CRM管理", 815),
            ("CRM_CUSTOMER_TRAINING", "客户培训", "CRM管理", 816),
            ("CRM_TECHNICAL_SEMINARS", "技术讲座", "CRM管理", 817),
            ("CRM_MASS_PROMOTIONS", "宣传群发", "CRM管理", 818),
            ("CRM_LEADS", "线索资料", "CRM管理", 819),
            ("CRM_OPPORTUNITIES", "商机资料", "CRM管理", 820),
            ("SERVICE", "服务管理", "服务管理", 90),
            ("SERVICE_CONTRACTS", "服务合同", "服务管理", 91),
            ("SERVICE_REQUESTS", "服务请求", "服务管理", 92),
            ("SERVICE_LENDING_ORDERS", "借出单", "服务管理", 93),
            ("SERVICE_RETURN_ORDERS", "归还单", "服务管理", 94),
            ("SERVICE_MAINTENANCE", "维修保养", "服务管理", 95),
            ("SERVICE_FIELD_SERVICE", "外出服务", "服务管理", 96),
            ("SERVICE_MAINTENANCE_CONTRACTS", "保养合约", "服务管理", 97),
            ("SERVICE_MAINTENANCE_QUERIES", "保养查询", "服务管理", 98),
            ("TRANSPORT", "运输管理", "运输管理", 100),
            ("TRANSPORT_VEHICLES", "车辆资料", "运输管理", 101),
            ("TRANSPORT_REQUESTS", "运输申请", "运输管理", 102),
            ("TRANSPORT_DRIVERS", "司机资料", "运输管理", 103),
            ("TRANSPORT_DISPATCHES", "运输排车", "运输管理", 104),
            ("TRANSPORT_COSTS", "运输费用", "运输管理", 105),
            ("ASSET", "资产管理", "资产管理", 110),
            ("ASSET_ASSETS", "资产资料", "资产管理", 111),
            ("ASSET_PRE_BORROW_ORDERS", "预借单", "资产管理", 112),
            ("ASSET_BORROW_ORDERS", "借用单", "资产管理", 113),
            ("ASSET_RETURN_ORDERS", "退还单", "资产管理", 114),
            ("ASSET_MAINTENANCE_RECORDS", "资产维护", "资产管理", 115),
            ("ASSET_STOCKTAKES", "资产盘点", "资产管理", 116),
            ("ASSET_DEPRECIATIONS", "资产折旧", "资产管理", 117),
            ("ASSET_LOSS_REPORTS", "资产报损", "资产管理", 118),
            ("OFFICE", "办公管理", "办公管理", 120),
            ("OFFICE_MEETINGS", "会议管理", "办公管理", 121),
            ("OFFICE_TASKS", "任务管理", "办公管理", 122),
            ("OFFICE_SUPPLIES", "办公用品", "办公管理", 123),
            ("OFFICE_APPLY_ORDERS", "申请单", "办公管理", 124),
            ("OFFICE_ISSUE_ORDERS", "领用单", "办公管理", 125),
            ("OFFICE_RETURN_ORDERS", "退回单", "办公管理", 126),
            ("OFFICE_LOSS_ORDERS", "报损单", "办公管理", 127),
            ("BASIC_DATA", "基础资料", "基本资料", 130),
            ("BASIC_DATA_TYPES", "数据类型", "基础资料", 131),
            ("BASIC_DATA_ITEMS", "数据项目", "基础资料", 132),
            ("SYSTEM_SETTINGS", "系统管理", "系统管理", 140),
            ("SYSTEM_USERS", "用户资料", "系统管理", 141),
            ("SYSTEM_ROLES", "角色管理", "系统管理", 142),
            ("SYSTEM_CONFIGURATIONS", "系统设置", "系统管理", 143),
            ("SYSTEM_ONLINE_USERS", "在线用户", "系统管理", 144),
            ("SYSTEM_LOGS", "查看日志", "系统管理", 145),
            ("SYSTEM_LOGIN_LOGS", "登入日志", "系统管理", 146),
            ("SYSTEM_OPERATION_LOGS", "操作日志", "系统管理", 147),
            ("SYSTEM_SYSTEM_LOGS", "系统日志", "系统管理", 148),
            ("REPORT_CENTER", "报表中心", "报表中心", 150),
            ("REPORT_HR", "人事报表", "报表中心", 151),
            ("REPORT_FINANCE", "财务报表", "报表中心", 152),
            ("REPORT_PURCHASING", "采购报表", "报表中心", 153),
            ("REPORT_SALES", "销售报表", "报表中心", 154),
            ("REPORT_WAREHOUSE", "仓务报表", "报表中心", 155),
            ("REPORT_PRODUCTION", "生产报表", "报表中心", 156),
            ("REPORT_SERVICE", "服务报表", "报表中心", 157),
            ("REPORT_ATTENDANCE", "考勤报表", "报表中心", 158),
            ("REPORT_ASSET", "资产报表", "报表中心", 159),
            ("REPORT_OFFICE", "办公报表", "报表中心", 160),
            ("REPORT_TRANSPORT", "运输报表", "报表中心", 161),
            ("REPORT_CRM", "CRM报表", "报表中心", 162),
            ("BASIC_INFORMATION", "基本资料", "基本资料", 165),
            ("BASIC_INFO_CUSTOMERS", "客户/友商资料", "基本资料", 166),
            ("BASIC_INFO_SUPPLIERS", "供应商/厂商资料", "基本资料", 167),
            ("BASIC_INFO_WAREHOUSES", "仓库/车间资料", "基本资料", 168),
            ("BASIC_INFO_PROJECTS", "工程项目资料", "基本资料", 169),
            ("TASK_CENTER", "任务中心", "任务中心", 170),
            ("TASK_MY_APPROVALS", "我的审批", "任务中心", 171),
            ("TASK_DAILY_TASKS", "日常任务", "任务中心", 172),
            ("TASK_FOLLOW_UPS", "跟单管理", "任务中心", 173),
            ("TASK_CONTENT_MESSAGES", "内容消息", "任务中心", 174),
            ("TASK_EMAILS", "收发邮件", "任务中心", 175),
            ("TASK_ANNOUNCEMENTS", "系统公告", "任务中心", 176),
            ("SYSTEM_DEVICES", "系统设备", "系统设备", 180),
            ("SYSTEM_DEVICE_RFID_READERS", "RFID Reader", "系统设备", 181),
            ("SYSTEM_DEVICE_CAMERAS", "镜头设备", "系统设备", 182),
            ("SYSTEM_DEVICE_GPIO_DEVICES", "GPIO设备", "系统设备", 183),
            ("TECHNICAL_SUPPORT", "技术支持", "技术支持", 190),
            ("TECH_SUPPORT_ONLINE_HELP", "在线帮助", "技术支持", 191),
            ("TECH_SUPPORT_DOWNLOAD_CENTER", "下载中心", "技术支持", 192),
            ("TECH_SUPPORT_ONLINE_SUPPORT", "在线支持", "技术支持", 193),
            ("TECH_SUPPORT_CONTACT_US", "联络我们", "技术支持", 194),
            ("TECH_SUPPORT_SYSTEM_VERSION", "系统版本", "技术支持", 195),
            ("TECH_SUPPORT_ONLINE_UPDATES", "在线更新", "技术支持", 196),
            ("TECH_SUPPORT_UPDATE_LOGS", "更新日志", "技术支持", 197),
            ("TECH_SUPPORT_API_MANAGEMENT", "API管理", "技术支持", 198)
        ];
    }

    /// <summary>
    /// 普通用户默认权限编码（用于初始化常规账号的基础可用菜单）。
    /// </summary>
    private static IReadOnlyList<string> GetCommonUserDefaultPermissionCodes()
    {
        return
        [
            "DASHBOARD",
            "HR",
            "HR_EMPLOYEES",
            "HR_COMPANY_ORGANIZATIONS",
            "HR_DEPARTMENTS",
            "HR_EMPLOYEE_TRAINING",
            "HR_PERSONNEL_CHANGES",
            "HR_HOLIDAY_MANAGEMENT",
            "HR_ATTENDANCE_MANAGEMENT",
            "HR_ATTENDANCE_SHIFT_SETTINGS",
            "HR_ATTENDANCE_SCHEDULING",
            "HR_ATTENDANCE_RECORDS",
            "HR_LEAVE_REGISTRATIONS",
            "HR_LEAVE_APPROVALS",
            "HR_FIELD_WORK_REGISTRATIONS",
            "HR_ATTENDANCE_DEVICES",
            "HR_ATTENDANCE_ADJUSTMENTS",
            "HR_ATTENDANCE_STATISTICS",
            "SYSTEM_SETTINGS",
            "SYSTEM_USERS",
            "SYSTEM_ROLES",
            "SYSTEM_CONFIGURATIONS",
            "SYSTEM_ONLINE_USERS",
            "SYSTEM_LOGS",
            "SYSTEM_LOGIN_LOGS",
            "SYSTEM_OPERATION_LOGS",
            "SYSTEM_SYSTEM_LOGS",
            "REPORT_CENTER",
            "REPORT_HR",
            "REPORT_FINANCE",
            "REPORT_PURCHASING",
            "REPORT_SALES",
            "REPORT_WAREHOUSE",
            "REPORT_PRODUCTION",
            "REPORT_SERVICE",
            "REPORT_ATTENDANCE",
            "REPORT_ASSET",
            "REPORT_OFFICE",
            "REPORT_TRANSPORT",
            "REPORT_CRM",
            "BASIC_INFORMATION",
            "BASIC_INFO_CUSTOMERS",
            "BASIC_INFO_SUPPLIERS",
            "BASIC_INFO_WAREHOUSES",
            "BASIC_INFO_PROJECTS",
            "BASIC_DATA",
            "BASIC_DATA_TYPES",
            "BASIC_DATA_ITEMS",
            "TASK_CENTER",
            "TASK_MY_APPROVALS",
            "TASK_DAILY_TASKS",
            "TASK_FOLLOW_UPS",
            "TASK_CONTENT_MESSAGES",
            "TASK_EMAILS",
            "TASK_ANNOUNCEMENTS",
            "SYSTEM_DEVICES",
            "SYSTEM_DEVICE_RFID_READERS",
            "SYSTEM_DEVICE_CAMERAS",
            "SYSTEM_DEVICE_GPIO_DEVICES",
            "TECHNICAL_SUPPORT",
            "TECH_SUPPORT_ONLINE_HELP",
            "TECH_SUPPORT_DOWNLOAD_CENTER",
            "TECH_SUPPORT_ONLINE_SUPPORT",
            "TECH_SUPPORT_CONTACT_US",
            "TECH_SUPPORT_SYSTEM_VERSION",
            "TECH_SUPPORT_ONLINE_UPDATES",
            "TECH_SUPPORT_UPDATE_LOGS",
            "TECH_SUPPORT_API_MANAGEMENT"
        ];
    }

    /// <summary>
    /// 兼容旧版地址字段取值（将旧静态地区/地域ID迁移到基础数据字典ID）。
    /// </summary>
    private static async Task EnsureLocationDictionaryCompatibilityAsync(SqlConnection conn)
    {
        var sql = $"""
            DECLARE @ChinaCountryRegionId INT =
            (
                SELECT TOP (1) item.Id
                FROM dbo.BD_BasicDataItem item
                INNER JOIN dbo.BD_BasicDataType type ON type.Id = item.TypeId
                WHERE type.TypeCode = 'COUNTRY_REGION' AND item.ItemCode = 'CN' AND item.IsDeleted = 0
            );

            DECLARE @GuangzhouCityId INT =
            (
                SELECT TOP (1) item.Id
                FROM dbo.BD_BasicDataItem item
                INNER JOIN dbo.BD_BasicDataType type ON type.Id = item.TypeId
                WHERE type.TypeCode = 'CITY' AND item.ItemCode = 'GUANGZHOU' AND item.IsDeleted = 0
            );

            DECLARE @ShenzhenCityId INT =
            (
                SELECT TOP (1) item.Id
                FROM dbo.BD_BasicDataItem item
                INNER JOIN dbo.BD_BasicDataType type ON type.Id = item.TypeId
                WHERE type.TypeCode = 'CITY' AND item.ItemCode = 'SHENZHEN' AND item.IsDeleted = 0
            );

            DECLARE @FoshanCityId INT =
            (
                SELECT TOP (1) item.Id
                FROM dbo.BD_BasicDataItem item
                INNER JOIN dbo.BD_BasicDataType type ON type.Id = item.TypeId
                WHERE type.TypeCode = 'CITY' AND item.ItemCode = 'FOSHAN' AND item.IsDeleted = 0
            );

            DECLARE @TianheCountyId INT =
            (
                SELECT TOP (1) item.Id
                FROM dbo.BD_BasicDataItem item
                INNER JOIN dbo.BD_BasicDataType type ON type.Id = item.TypeId
                WHERE type.TypeCode = 'COUNTY' AND item.ItemCode = 'TIANHE' AND item.IsDeleted = 0
            );

            DECLARE @YuexiuCountyId INT =
            (
                SELECT TOP (1) item.Id
                FROM dbo.BD_BasicDataItem item
                INNER JOIN dbo.BD_BasicDataType type ON type.Id = item.TypeId
                WHERE type.TypeCode = 'COUNTY' AND item.ItemCode = 'YUEXIU' AND item.IsDeleted = 0
            );

            DECLARE @NanshanCountyId INT =
            (
                SELECT TOP (1) item.Id
                FROM dbo.BD_BasicDataItem item
                INNER JOIN dbo.BD_BasicDataType type ON type.Id = item.TypeId
                WHERE type.TypeCode = 'COUNTY' AND item.ItemCode = 'NANSHAN' AND item.IsDeleted = 0
            );

            UPDATE org
            SET RegionId = @ChinaCountryRegionId
            FROM dbo.{CompanyOrganizationTableName} org
            LEFT JOIN dbo.BD_BasicDataItem regionItem ON regionItem.Id = org.RegionId AND regionItem.IsDeleted = 0
            LEFT JOIN dbo.BD_BasicDataType regionType ON regionType.Id = regionItem.TypeId
            WHERE org.IsDeleted = 0
              AND org.RegionId IS NOT NULL
              AND @ChinaCountryRegionId IS NOT NULL
              AND
              (
                  regionType.TypeCode = 'REGION'
                  OR (org.RegionId IN (1, 2, 3) AND regionItem.Id IS NULL)
              );

            UPDATE emp
            SET CountryRegionId = @ChinaCountryRegionId
            FROM dbo.{EmployeeTableName} emp
            LEFT JOIN dbo.BD_BasicDataItem countryItem ON countryItem.Id = emp.CountryRegionId AND countryItem.IsDeleted = 0
            LEFT JOIN dbo.BD_BasicDataType countryType ON countryType.Id = countryItem.TypeId
            WHERE emp.IsDeleted = 0
              AND emp.CountryRegionId IS NOT NULL
              AND @ChinaCountryRegionId IS NOT NULL
              AND
              (
                  countryType.TypeCode = 'REGION'
                  OR (emp.CountryRegionId IN (1, 2, 3) AND countryItem.Id IS NULL)
              );

            UPDATE emp
            SET CityId = @GuangzhouCityId
            FROM dbo.{EmployeeTableName} emp
            LEFT JOIN dbo.BD_BasicDataItem cityItem ON cityItem.Id = emp.CityId AND cityItem.IsDeleted = 0
            LEFT JOIN dbo.BD_BasicDataType cityType ON cityType.Id = cityItem.TypeId
            WHERE emp.IsDeleted = 0
              AND emp.CityId = 1
              AND @GuangzhouCityId IS NOT NULL
              AND (cityItem.Id IS NULL OR cityType.TypeCode <> 'CITY');

            UPDATE emp
            SET CityId = @ShenzhenCityId
            FROM dbo.{EmployeeTableName} emp
            LEFT JOIN dbo.BD_BasicDataItem cityItem ON cityItem.Id = emp.CityId AND cityItem.IsDeleted = 0
            LEFT JOIN dbo.BD_BasicDataType cityType ON cityType.Id = cityItem.TypeId
            WHERE emp.IsDeleted = 0
              AND emp.CityId = 2
              AND @ShenzhenCityId IS NOT NULL
              AND (cityItem.Id IS NULL OR cityType.TypeCode <> 'CITY');

            UPDATE emp
            SET CityId = @FoshanCityId
            FROM dbo.{EmployeeTableName} emp
            LEFT JOIN dbo.BD_BasicDataItem cityItem ON cityItem.Id = emp.CityId AND cityItem.IsDeleted = 0
            LEFT JOIN dbo.BD_BasicDataType cityType ON cityType.Id = cityItem.TypeId
            WHERE emp.IsDeleted = 0
              AND emp.CityId = 3
              AND @FoshanCityId IS NOT NULL
              AND (cityItem.Id IS NULL OR cityType.TypeCode <> 'CITY');

            UPDATE emp
            SET CountyId = @TianheCountyId
            FROM dbo.{EmployeeTableName} emp
            LEFT JOIN dbo.BD_BasicDataItem countyItem ON countyItem.Id = emp.CountyId AND countyItem.IsDeleted = 0
            LEFT JOIN dbo.BD_BasicDataType countyType ON countyType.Id = countyItem.TypeId
            WHERE emp.IsDeleted = 0
              AND emp.CountyId = 1
              AND @TianheCountyId IS NOT NULL
              AND (countyItem.Id IS NULL OR countyType.TypeCode <> 'COUNTY');

            UPDATE emp
            SET CountyId = @YuexiuCountyId
            FROM dbo.{EmployeeTableName} emp
            LEFT JOIN dbo.BD_BasicDataItem countyItem ON countyItem.Id = emp.CountyId AND countyItem.IsDeleted = 0
            LEFT JOIN dbo.BD_BasicDataType countyType ON countyType.Id = countyItem.TypeId
            WHERE emp.IsDeleted = 0
              AND emp.CountyId = 2
              AND @YuexiuCountyId IS NOT NULL
              AND (countyItem.Id IS NULL OR countyType.TypeCode <> 'COUNTY');

            UPDATE emp
            SET CountyId = @NanshanCountyId
            FROM dbo.{EmployeeTableName} emp
            LEFT JOIN dbo.BD_BasicDataItem countyItem ON countyItem.Id = emp.CountyId AND countyItem.IsDeleted = 0
            LEFT JOIN dbo.BD_BasicDataType countyType ON countyType.Id = countyItem.TypeId
            WHERE emp.IsDeleted = 0
              AND emp.CountyId = 3
              AND @NanshanCountyId IS NOT NULL
              AND (countyItem.Id IS NULL OR countyType.TypeCode <> 'COUNTY');
            """;

        await ExecuteNonQueryAsync(conn, sql);
    }

    public async Task<List<Role>> GetRolesAsync()
    {
        var sql = $"""
            SELECT Id, RoleCode, RoleName, Description, SortOrder, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, IsDeleted
            FROM dbo.{RoleTableName}
            WHERE IsDeleted = 0
            ORDER BY SortOrder, Id;
            """;

        var results = new List<Role>();
        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            results.Add(new Role
            {
                Id = reader.GetInt32(0),
                RoleCode = ReadNullableString(reader, 1) ?? string.Empty,
                RoleName = ReadNullableString(reader, 2) ?? string.Empty,
                Description = ReadNullableString(reader, 3),
                SortOrder = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                CreatedAt = reader.GetDateTime(5),
                UpdatedAt = ReadNullableDateTime(reader, 6),
                CreatedBy = ReadNullableString(reader, 7),
                UpdatedBy = ReadNullableString(reader, 8),
                IsDeleted = reader.GetBoolean(9)
            });
        }

        return results;
    }

    public async Task<List<Permission>> GetPermissionsAsync()
    {
        var sql = $"""
            SELECT Id, PermissionCode, PermissionName, Category, SortOrder, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, IsDeleted
            FROM dbo.{PermissionTableName}
            WHERE IsDeleted = 0
            ORDER BY SortOrder, Id;
            """;

        var results = new List<Permission>();
        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            results.Add(new Permission
            {
                Id = reader.GetInt32(0),
                PermissionCode = ReadNullableString(reader, 1) ?? string.Empty,
                PermissionName = ReadNullableString(reader, 2) ?? string.Empty,
                Category = ReadNullableString(reader, 3),
                SortOrder = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                CreatedAt = reader.GetDateTime(5),
                UpdatedAt = ReadNullableDateTime(reader, 6),
                CreatedBy = ReadNullableString(reader, 7),
                UpdatedBy = ReadNullableString(reader, 8),
                IsDeleted = reader.GetBoolean(9)
            });
        }

        return results;
    }

    public async Task<List<int>> GetUserPermissionIdsAsync(int employeeId)
    {
        var sql = $"""
            SELECT PermissionId
            FROM dbo.{UserPermissionTableName}
            WHERE EmployeeId = @EmployeeId;
            """;

        var results = new List<int>();
        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@EmployeeId", employeeId));
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            results.Add(reader.GetInt32(0));
        }

        return results;
    }

    /// <summary>
    /// 获取用户生效权限编码列表（角色默认权限与个人权限合并去重，用于菜单与授权判断）。
    /// </summary>
    public async Task<List<string>> GetEffectivePermissionCodesAsync(int employeeId)
    {
        var sql = $"""
            SELECT DISTINCT PermissionCode
            FROM
            (
                SELECT p.PermissionCode
                FROM dbo.{EmployeeTableName} e
                INNER JOIN dbo.{RolePermissionTableName} rp ON e.RoleId = rp.RoleId
                INNER JOIN dbo.{PermissionTableName} p ON rp.PermissionId = p.Id
                WHERE e.Id = @EmployeeId
                  AND e.IsDeleted = 0
                  AND p.IsDeleted = 0

                UNION

                SELECT p.PermissionCode
                FROM dbo.{UserPermissionTableName} up
                INNER JOIN dbo.{PermissionTableName} p ON up.PermissionId = p.Id
                INNER JOIN dbo.{EmployeeTableName} e ON up.EmployeeId = e.Id
                WHERE up.EmployeeId = @EmployeeId
                  AND e.IsDeleted = 0
                  AND p.IsDeleted = 0
            ) AS PermissionSource
            WHERE PermissionCode IS NOT NULL AND LTRIM(RTRIM(PermissionCode)) <> N''
            ORDER BY PermissionCode;
            """;

        var results = new List<string>();
        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@EmployeeId", employeeId));
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var permissionCode = ReadNullableString(reader, 0);
            if (!string.IsNullOrWhiteSpace(permissionCode))
            {
                results.Add(permissionCode);
            }
        }

        return results;
    }

    public async Task<List<int>> GetUserCompanyIdsAsync(int employeeId)
    {
        var sql = $"""
            SELECT CompanyOrganizationId
            FROM dbo.{UserCompanyTableName}
            WHERE EmployeeId = @EmployeeId;
            """;

        var results = new List<int>();
        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@EmployeeId", employeeId));
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            results.Add(reader.GetInt32(0));
        }

        return results;
    }

    /// <summary>
    /// 查询任务跟进分页数据（按功能编码、所属个体与筛选条件返回任务记录）。
    /// </summary>
    public async Task<TaskFollowUpPageResult> GetTaskFollowUpsAsync(string featureCode, int entityId, string? keyword, string? statusCode, int pageNumber, int pageSize)
    {
        if (string.IsNullOrWhiteSpace(featureCode) || entityId <= 0)
        {
            return new TaskFollowUpPageResult();
        }

        var normalizedKeyword = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();
        var normalizedStatusCode = string.IsNullOrWhiteSpace(statusCode) ? null : statusCode.Trim();
        var safePageNumber = pageNumber <= 0 ? 1 : pageNumber;
        var safePageSize = pageSize <= 0 ? 10 : pageSize;
        var offset = (safePageNumber - 1) * safePageSize;

        var countSql = $"""
            SELECT COUNT(1)
            FROM dbo.{TaskFollowUpTableName}
            WHERE FeatureCode = @FeatureCode
              AND EntityId = @EntityId
              AND IsDeleted = 0
              AND (@StatusCode IS NULL OR StatusCode = @StatusCode)
              AND
              (
                  @Keyword IS NULL
                  OR TaskCode LIKE N'%' + @Keyword + N'%'
                  OR ExecutorName LIKE N'%' + @Keyword + N'%'
                  OR Description LIKE N'%' + @Keyword + N'%'
                  OR ProjectCode LIKE N'%' + @Keyword + N'%'
                  OR InitiatorName LIKE N'%' + @Keyword + N'%'
              );
            """;

        var pageSql = $"""
            SELECT
                Id,
                FeatureCode,
                EntityId,
                TaskCode,
                ArchivePath,
                StatusCode,
                PlannedDate,
                TaskTypeCode,
                ExecutorName,
                Description,
                PriorityCode,
                ProgressPercent,
                CompletedDate,
                ProjectCode,
                InitiatorName,
                CreatedAt,
                UpdatedAt,
                CreatedBy,
                UpdatedBy,
                IsDeleted
            FROM dbo.{TaskFollowUpTableName}
            WHERE FeatureCode = @FeatureCode
              AND EntityId = @EntityId
              AND IsDeleted = 0
              AND (@StatusCode IS NULL OR StatusCode = @StatusCode)
              AND
              (
                  @Keyword IS NULL
                  OR TaskCode LIKE N'%' + @Keyword + N'%'
                  OR ExecutorName LIKE N'%' + @Keyword + N'%'
                  OR Description LIKE N'%' + @Keyword + N'%'
                  OR ProjectCode LIKE N'%' + @Keyword + N'%'
                  OR InitiatorName LIKE N'%' + @Keyword + N'%'
              )
            ORDER BY CreatedAt DESC, Id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        await using var conn = await OpenConnectionAsync();
        await using var countCmd = new SqlCommand(countSql, conn);
        BindTaskFollowUpQueryParams(countCmd, featureCode, entityId, normalizedKeyword, normalizedStatusCode);
        var totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync() ?? 0);

        await using var pageCmd = new SqlCommand(pageSql, conn);
        BindTaskFollowUpQueryParams(pageCmd, featureCode, entityId, normalizedKeyword, normalizedStatusCode);
        pageCmd.Parameters.AddWithValue("@Offset", offset);
        pageCmd.Parameters.AddWithValue("@PageSize", safePageSize);

        var records = new List<TaskFollowUp>();
        await using var reader = await pageCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            records.Add(MapTaskFollowUp(reader));
        }

        return new TaskFollowUpPageResult
        {
            Records = records,
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// 保存任务跟进记录（新增时创建新记录，编辑时更新当前功能与所属个体下的既有记录）。
    /// </summary>
    public async Task<int> SaveTaskFollowUpAsync(TaskFollowUp taskFollowUp)
    {
        if (string.IsNullOrWhiteSpace(taskFollowUp.FeatureCode) || taskFollowUp.EntityId <= 0)
        {
            return 0;
        }

        taskFollowUp.FeatureCode = taskFollowUp.FeatureCode.Trim();
        taskFollowUp.TaskCode = string.IsNullOrWhiteSpace(taskFollowUp.TaskCode)
            ? GenerateTaskFollowUpCode()
            : taskFollowUp.TaskCode.Trim();
        taskFollowUp.StatusCode = string.IsNullOrWhiteSpace(taskFollowUp.StatusCode) ? "PENDING" : taskFollowUp.StatusCode.Trim();
        taskFollowUp.TaskTypeCode = string.IsNullOrWhiteSpace(taskFollowUp.TaskTypeCode) ? "TASK" : taskFollowUp.TaskTypeCode.Trim();
        taskFollowUp.PriorityCode = string.IsNullOrWhiteSpace(taskFollowUp.PriorityCode) ? "NORMAL" : taskFollowUp.PriorityCode.Trim();
        taskFollowUp.ProgressPercent = Math.Clamp(taskFollowUp.ProgressPercent, 0, 100);

        await using var conn = await OpenConnectionAsync();

        if (taskFollowUp.Id > 0)
        {
            var updateSql = $"""
                UPDATE dbo.{TaskFollowUpTableName}
                SET
                    TaskCode = @TaskCode,
                    ArchivePath = @ArchivePath,
                    StatusCode = @StatusCode,
                    PlannedDate = @PlannedDate,
                    TaskTypeCode = @TaskTypeCode,
                    ExecutorName = @ExecutorName,
                    Description = @Description,
                    PriorityCode = @PriorityCode,
                    ProgressPercent = @ProgressPercent,
                    CompletedDate = @CompletedDate,
                    ProjectCode = @ProjectCode,
                    InitiatorName = @InitiatorName,
                    UpdatedAt = @UpdatedAt,
                    UpdatedBy = @UpdatedBy
                WHERE Id = @Id
                  AND FeatureCode = @FeatureCode
                  AND EntityId = @EntityId
                  AND IsDeleted = 0;
                """;

            await using var updateCmd = new SqlCommand(updateSql, conn);
            updateCmd.Parameters.AddWithValue("@Id", taskFollowUp.Id);
            BindTaskFollowUpParams(updateCmd, taskFollowUp);
            updateCmd.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
            updateCmd.Parameters.AddWithValue("@UpdatedBy", ToDbValue(taskFollowUp.UpdatedBy));
            var affectedRows = await updateCmd.ExecuteNonQueryAsync();
            return affectedRows > 0 ? taskFollowUp.Id : 0;
        }

        var insertSql = $"""
            INSERT INTO dbo.{TaskFollowUpTableName}
            (
                FeatureCode,
                EntityId,
                TaskCode,
                ArchivePath,
                StatusCode,
                PlannedDate,
                TaskTypeCode,
                ExecutorName,
                Description,
                PriorityCode,
                ProgressPercent,
                CompletedDate,
                ProjectCode,
                InitiatorName,
                CreatedAt,
                UpdatedAt,
                CreatedBy,
                UpdatedBy,
                IsDeleted
            )
            OUTPUT INSERTED.Id
            VALUES
            (
                @FeatureCode,
                @EntityId,
                @TaskCode,
                @ArchivePath,
                @StatusCode,
                @PlannedDate,
                @TaskTypeCode,
                @ExecutorName,
                @Description,
                @PriorityCode,
                @ProgressPercent,
                @CompletedDate,
                @ProjectCode,
                @InitiatorName,
                @CreatedAt,
                NULL,
                @CreatedBy,
                NULL,
                0
            );
            """;

        await using var insertCmd = new SqlCommand(insertSql, conn);
        BindTaskFollowUpParams(insertCmd, taskFollowUp);
        insertCmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
        insertCmd.Parameters.AddWithValue("@CreatedBy", ToDbValue(taskFollowUp.CreatedBy));
        return Convert.ToInt32(await insertCmd.ExecuteScalarAsync() ?? 0);
    }

    /// <summary>
    /// 删除任务跟进记录（仅删除当前功能与所属个体下选中的记录）。
    /// </summary>
    public async Task<int> DeleteTaskFollowUpsAsync(string featureCode, int entityId, List<int> ids)
    {
        if (string.IsNullOrWhiteSpace(featureCode) || entityId <= 0 || ids.Count == 0)
        {
            return 0;
        }

        var safeIds = ids.Where(id => id > 0).Distinct().ToList();
        if (safeIds.Count == 0)
        {
            return 0;
        }

        var idParameterNames = safeIds.Select((_, index) => $"@Id{index}").ToArray();
        var sql = $"""
            UPDATE dbo.{TaskFollowUpTableName}
            SET IsDeleted = 1,
                UpdatedAt = @UpdatedAt
            WHERE FeatureCode = @FeatureCode
              AND EntityId = @EntityId
              AND IsDeleted = 0
              AND Id IN ({string.Join(", ", idParameterNames)});
            """;

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@FeatureCode", featureCode.Trim()));
        cmd.Parameters.Add(new SqlParameter("@EntityId", entityId));
        cmd.Parameters.Add(new SqlParameter("@UpdatedAt", DateTime.Now));
        for (var index = 0; index < safeIds.Count; index++)
        {
            cmd.Parameters.Add(new SqlParameter(idParameterNames[index], safeIds[index]));
        }

        return await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// 查询员工培训历程分页数据（按员工、历程类型与关键字返回工作/培训/教育经历）。
    /// </summary>
    public async Task<EmployeeTrainingExperiencePageResult> GetEmployeeTrainingExperiencesAsync(
        int employeeId,
        string? keyword,
        string? experienceTypeCode,
        int pageNumber,
        int pageSize)
    {
        if (employeeId <= 0)
        {
            return new EmployeeTrainingExperiencePageResult();
        }

        var normalizedKeyword = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();
        var normalizedExperienceTypeCode = string.IsNullOrWhiteSpace(experienceTypeCode) ? null : experienceTypeCode.Trim();
        var safePageNumber = pageNumber <= 0 ? 1 : pageNumber;
        var safePageSize = pageSize <= 0 ? 10 : pageSize;
        var offset = (safePageNumber - 1) * safePageSize;

        var countSql = $"""
            SELECT COUNT(1)
            FROM dbo.{EmployeeTrainingExperienceTableName}
            WHERE EmployeeId = @EmployeeId
              AND IsDeleted = 0
              AND (@ExperienceTypeCode IS NULL OR ExperienceTypeCode = @ExperienceTypeCode)
              AND
              (
                  @Keyword IS NULL
                  OR Description LIKE N'%' + @Keyword + N'%'
                  OR CertificateName LIKE N'%' + @Keyword + N'%'
                  OR OrganizationName LIKE N'%' + @Keyword + N'%'
                  OR ArchivePath LIKE N'%' + @Keyword + N'%'
              );
            """;

        var pageSql = $"""
            SELECT
                Id,
                EmployeeId,
                ExperienceTypeCode,
                StartDate,
                EndDate,
                Description,
                CertificateName,
                OrganizationName,
                ArchivePath,
                CreatedAt,
                UpdatedAt,
                CreatedBy,
                UpdatedBy,
                IsDeleted
            FROM dbo.{EmployeeTrainingExperienceTableName}
            WHERE EmployeeId = @EmployeeId
              AND IsDeleted = 0
              AND (@ExperienceTypeCode IS NULL OR ExperienceTypeCode = @ExperienceTypeCode)
              AND
              (
                  @Keyword IS NULL
                  OR Description LIKE N'%' + @Keyword + N'%'
                  OR CertificateName LIKE N'%' + @Keyword + N'%'
                  OR OrganizationName LIKE N'%' + @Keyword + N'%'
                  OR ArchivePath LIKE N'%' + @Keyword + N'%'
              )
            ORDER BY StartDate DESC, ISNULL(EndDate, StartDate) DESC, CreatedAt DESC, Id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        await using var conn = await OpenConnectionAsync();
        await using var countCmd = new SqlCommand(countSql, conn);
        BindEmployeeTrainingExperienceQueryParams(countCmd, employeeId, normalizedKeyword, normalizedExperienceTypeCode);
        var totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync() ?? 0);

        await using var pageCmd = new SqlCommand(pageSql, conn);
        BindEmployeeTrainingExperienceQueryParams(pageCmd, employeeId, normalizedKeyword, normalizedExperienceTypeCode);
        pageCmd.Parameters.AddWithValue("@Offset", offset);
        pageCmd.Parameters.AddWithValue("@PageSize", safePageSize);

        var records = new List<EmployeeTrainingExperience>();
        await using var reader = await pageCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            records.Add(MapEmployeeTrainingExperience(reader));
        }

        return new EmployeeTrainingExperiencePageResult
        {
            Records = records,
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// 保存员工培训历程记录（新增时创建记录，编辑时更新当前员工名下的既有记录）。
    /// </summary>
    public async Task<int> SaveEmployeeTrainingExperienceAsync(EmployeeTrainingExperience experience)
    {
        if (experience.EmployeeId <= 0 || string.IsNullOrWhiteSpace(experience.Description))
        {
            return 0;
        }

        experience.ExperienceTypeCode = string.IsNullOrWhiteSpace(experience.ExperienceTypeCode)
            ? "TRAINING"
            : experience.ExperienceTypeCode.Trim();
        experience.Description = experience.Description.Trim();

        await using var conn = await OpenConnectionAsync();

        if (experience.Id > 0)
        {
            var updateSql = $"""
                UPDATE dbo.{EmployeeTrainingExperienceTableName}
                SET
                    ExperienceTypeCode = @ExperienceTypeCode,
                    StartDate = @StartDate,
                    EndDate = @EndDate,
                    Description = @Description,
                    CertificateName = @CertificateName,
                    OrganizationName = @OrganizationName,
                    ArchivePath = @ArchivePath,
                    UpdatedAt = @UpdatedAt,
                    UpdatedBy = @UpdatedBy
                WHERE Id = @Id
                  AND EmployeeId = @EmployeeId
                  AND IsDeleted = 0;
                """;

            await using var updateCmd = new SqlCommand(updateSql, conn);
            updateCmd.Parameters.AddWithValue("@Id", experience.Id);
            BindEmployeeTrainingExperienceParams(updateCmd, experience);
            updateCmd.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
            updateCmd.Parameters.AddWithValue("@UpdatedBy", ToDbValue(experience.UpdatedBy));
            var affectedRows = await updateCmd.ExecuteNonQueryAsync();
            return affectedRows > 0 ? experience.Id : 0;
        }

        var insertSql = $"""
            INSERT INTO dbo.{EmployeeTrainingExperienceTableName}
            (
                EmployeeId,
                ExperienceTypeCode,
                StartDate,
                EndDate,
                Description,
                CertificateName,
                OrganizationName,
                ArchivePath,
                CreatedAt,
                UpdatedAt,
                CreatedBy,
                UpdatedBy,
                IsDeleted
            )
            OUTPUT INSERTED.Id
            VALUES
            (
                @EmployeeId,
                @ExperienceTypeCode,
                @StartDate,
                @EndDate,
                @Description,
                @CertificateName,
                @OrganizationName,
                @ArchivePath,
                @CreatedAt,
                NULL,
                @CreatedBy,
                NULL,
                0
            );
            """;

        await using var insertCmd = new SqlCommand(insertSql, conn);
        BindEmployeeTrainingExperienceParams(insertCmd, experience);
        insertCmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
        insertCmd.Parameters.AddWithValue("@CreatedBy", ToDbValue(experience.CreatedBy));
        return Convert.ToInt32(await insertCmd.ExecuteScalarAsync() ?? 0);
    }

    /// <summary>
    /// 删除员工培训历程记录（仅删除当前员工名下选中的记录）。
    /// </summary>
    public async Task<int> DeleteEmployeeTrainingExperiencesAsync(int employeeId, List<int> ids)
    {
        if (employeeId <= 0 || ids.Count == 0)
        {
            return 0;
        }

        var safeIds = ids.Where(id => id > 0).Distinct().ToList();
        if (safeIds.Count == 0)
        {
            return 0;
        }

        var idParameterNames = safeIds.Select((_, index) => $"@EmployeeTrainingExperienceId{index}").ToArray();
        var sql = $"""
            UPDATE dbo.{EmployeeTrainingExperienceTableName}
            SET IsDeleted = 1,
                UpdatedAt = @UpdatedAt
            WHERE EmployeeId = @EmployeeId
              AND IsDeleted = 0
              AND Id IN ({string.Join(", ", idParameterNames)});
            """;

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@EmployeeId", employeeId));
        cmd.Parameters.Add(new SqlParameter("@UpdatedAt", DateTime.Now));
        for (var index = 0; index < safeIds.Count; index++)
        {
            cmd.Parameters.Add(new SqlParameter(idParameterNames[index], safeIds[index]));
        }

        return await cmd.ExecuteNonQueryAsync();
    }

    public async Task<bool> SaveUserAccountAsync(int employeeId, string? loginAccount, string? hashedPassword,
        int? roleId, DateTime? accountValidUntil, bool isAccountFrozen, int? forceViewRecordDays,
        List<int> permissionIds, List<int> companyIds)
    {
        await using var conn = await OpenConnectionAsync();
        await using var tx = await conn.BeginTransactionAsync();

        try
        {
            // 鏇存柊鍛樺伐璐﹀彿瀛楁
            var updateSql = hashedPassword != null
                ? $"""
                    UPDATE dbo.{EmployeeTableName}
                    SET LoginAccount = @LoginAccount,
                        LoginPassword = @LoginPassword,
                        RoleId = @RoleId,
                        AccountValidUntil = @AccountValidUntil,
                        IsAccountFrozen = @IsAccountFrozen,
                        ForceViewRecordDays = @ForceViewRecordDays,
                        UpdatedAt = @UpdatedAt
                    WHERE Id = @Id AND IsDeleted = 0;
                    """
                : $"""
                    UPDATE dbo.{EmployeeTableName}
                    SET LoginAccount = @LoginAccount,
                        RoleId = @RoleId,
                        AccountValidUntil = @AccountValidUntil,
                        IsAccountFrozen = @IsAccountFrozen,
                        ForceViewRecordDays = @ForceViewRecordDays,
                        UpdatedAt = @UpdatedAt
                    WHERE Id = @Id AND IsDeleted = 0;
                    """;

            await using (var cmd = new SqlCommand(updateSql, conn, (SqlTransaction)tx))
            {
                cmd.Parameters.Add(new SqlParameter("@Id", employeeId));
                cmd.Parameters.Add(new SqlParameter("@LoginAccount", ToDbValue(loginAccount)));
                if (hashedPassword != null)
                {
                    cmd.Parameters.Add(new SqlParameter("@LoginPassword", hashedPassword));
                }
                cmd.Parameters.Add(new SqlParameter("@RoleId", ToDbValue(roleId)));
                cmd.Parameters.Add(new SqlParameter("@AccountValidUntil", ToDbValue(accountValidUntil)));
                cmd.Parameters.Add(new SqlParameter("@IsAccountFrozen", isAccountFrozen));
                cmd.Parameters.Add(new SqlParameter("@ForceViewRecordDays", ToDbValue(forceViewRecordDays)));
                cmd.Parameters.Add(new SqlParameter("@UpdatedAt", DateTime.Now));

                if (await cmd.ExecuteNonQueryAsync() == 0)
                {
                    await tx.RollbackAsync();
                    return false;
                }
            }

            // 先删除再插入用户权限。
            await using (var cmd = new SqlCommand($"DELETE FROM dbo.{UserPermissionTableName} WHERE EmployeeId = @EmployeeId;", conn, (SqlTransaction)tx))
            {
                cmd.Parameters.Add(new SqlParameter("@EmployeeId", employeeId));
                await cmd.ExecuteNonQueryAsync();
            }

            foreach (var permId in permissionIds)
            {
                await using var cmd = new SqlCommand(
                    $"INSERT INTO dbo.{UserPermissionTableName} (EmployeeId, PermissionId) VALUES (@EmployeeId, @PermissionId);",
                    conn, (SqlTransaction)tx);
                cmd.Parameters.Add(new SqlParameter("@EmployeeId", employeeId));
                cmd.Parameters.Add(new SqlParameter("@PermissionId", permId));
                await cmd.ExecuteNonQueryAsync();
            }

            // 先删除再插入用户管理公司。
            await using (var cmd = new SqlCommand($"DELETE FROM dbo.{UserCompanyTableName} WHERE EmployeeId = @EmployeeId;", conn, (SqlTransaction)tx))
            {
                cmd.Parameters.Add(new SqlParameter("@EmployeeId", employeeId));
                await cmd.ExecuteNonQueryAsync();
            }

            foreach (var companyId in companyIds)
            {
                await using var cmd = new SqlCommand(
                    $"INSERT INTO dbo.{UserCompanyTableName} (EmployeeId, CompanyOrganizationId) VALUES (@EmployeeId, @CompanyOrganizationId);",
                    conn, (SqlTransaction)tx);
                cmd.Parameters.Add(new SqlParameter("@EmployeeId", employeeId));
                cmd.Parameters.Add(new SqlParameter("@CompanyOrganizationId", companyId));
                await cmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
            return true;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<Employee?> GetEmployeeByLoginAccountAsync(string loginAccount)
    {
        var sql = $"""
            SELECT TOP (1)
                e.Id, e.EmployeeCode, e.FirstName, e.LastName, e.LoginAccount, e.LoginPassword,
                e.RoleId, e.AccountValidUntil, e.IsAccountFrozen, e.IsDeleted
            FROM dbo.{EmployeeTableName} e
            WHERE e.LoginAccount = @LoginAccount;
            """;

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@LoginAccount", loginAccount));
        await using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new Employee
        {
            Id = reader.GetInt32(0),
            EmployeeCode = ReadNullableString(reader, 1),
            FirstName = ReadNullableString(reader, 2) ?? string.Empty,
            LastName = ReadNullableString(reader, 3) ?? string.Empty,
            LoginAccount = ReadNullableString(reader, 4),
            LoginPassword = ReadNullableString(reader, 5),
            RoleId = ReadNullableInt(reader, 6),
            AccountValidUntil = ReadNullableDateTime(reader, 7),
            IsAccountFrozen = reader.GetBoolean(8),
            IsDeleted = reader.GetBoolean(9)
        };
    }

    public async Task<bool> UpdateEmployeePasswordAsync(int employeeId, string hashedPassword)
    {
        var sql = $"""
            UPDATE dbo.{EmployeeTableName}
            SET LoginPassword = @LoginPassword, UpdatedAt = @UpdatedAt
            WHERE Id = @Id AND IsDeleted = 0;
            """;

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@Id", employeeId));
        cmd.Parameters.Add(new SqlParameter("@LoginPassword", hashedPassword));
        cmd.Parameters.Add(new SqlParameter("@UpdatedAt", DateTime.Now));

        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<List<string>> GetUserCompanyCodesAsync(int employeeId)
    {
        var sql = $"""
            SELECT c.OrganizationCode
            FROM dbo.{UserCompanyTableName} uc
            INNER JOIN dbo.{CompanyOrganizationTableName} c ON uc.CompanyOrganizationId = c.Id AND c.IsDeleted = 0
            WHERE uc.EmployeeId = @EmployeeId;
            """;

        var results = new List<string>();
        await using var conn = await OpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@EmployeeId", employeeId));
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var code = ReadNullableString(reader, 0);
            if (code != null) results.Add(code);
        }

        return results;
    }

    /// <summary>
    /// 绑定公司组织银行账号筛选参数（统一处理组织、关键字与状态条件）。
    /// </summary>
    private static void BindCompanyOrganizationBankAccountQueryParams(
        SqlCommand cmd,
        int companyOrganizationId,
        string? keyword,
        string? statusCode)
    {
        cmd.Parameters.Add(new SqlParameter("@CompanyOrganizationId", companyOrganizationId));
        cmd.Parameters.Add(new SqlParameter("@Keyword", ToDbValue(keyword)));
        cmd.Parameters.Add(new SqlParameter("@StatusCode", ToDbValue(statusCode)));
    }

    /// <summary>
    /// 绑定公司组织银行账号保存参数（统一处理银行账号实体入库字段）。
    /// </summary>
    private static void BindCompanyOrganizationBankAccountParams(SqlCommand cmd, CompanyOrganizationBankAccount bankAccount)
    {
        cmd.Parameters.Add(new SqlParameter("@CompanyOrganizationId", bankAccount.CompanyOrganizationId));
        cmd.Parameters.Add(new SqlParameter("@AccountNumber", bankAccount.AccountNumber));
        cmd.Parameters.Add(new SqlParameter("@BankId", ToDbValue(bankAccount.BankId)));
        cmd.Parameters.Add(new SqlParameter("@BranchName", bankAccount.BranchName));
        cmd.Parameters.Add(new SqlParameter("@BranchAddress", ToDbValue(bankAccount.BranchAddress)));
        cmd.Parameters.Add(new SqlParameter("@CurrencyCode", bankAccount.CurrencyCode));
        cmd.Parameters.Add(new SqlParameter("@StatusCode", bankAccount.StatusCode));
        cmd.Parameters.Add(new SqlParameter("@SubjectCode", ToDbValue(bankAccount.SubjectCode)));
        cmd.Parameters.Add(new SqlParameter("@SubjectName", ToDbValue(bankAccount.SubjectName)));
        cmd.Parameters.Add(new SqlParameter("@Remarks", ToDbValue(bankAccount.Remarks)));
        cmd.Parameters.Add(new SqlParameter("@IsDefault", bankAccount.IsDefault));
    }

    /// <summary>
    /// 绑定公司组织单号规则保存参数（统一处理规则实体入库字段）。
    /// </summary>
    private static void BindCompanyOrganizationDocumentNumberRuleParams(SqlCommand cmd, CompanyOrganizationDocumentNumberRule rule)
    {
        cmd.Parameters.Add(new SqlParameter("@CompanyOrganizationId", rule.CompanyOrganizationId));
        cmd.Parameters.Add(new SqlParameter("@DocumentTypeCode", rule.DocumentTypeCode));
        cmd.Parameters.Add(new SqlParameter("@Prefix", rule.Prefix));
        cmd.Parameters.Add(new SqlParameter("@DateFormatCode", rule.DateFormatCode));
        cmd.Parameters.Add(new SqlParameter("@SequenceLength", rule.SequenceLength));
        cmd.Parameters.Add(new SqlParameter("@LastDateSegment", ToDbValue(rule.LastDateSegment)));
        cmd.Parameters.Add(new SqlParameter("@LastSequenceValue", rule.LastSequenceValue));
        cmd.Parameters.Add(new SqlParameter("@LastGeneratedNumber", ToDbValue(rule.LastGeneratedNumber)));
    }

    /// <summary>
    /// 规范公司组织单号规则（补齐默认值并约束前缀、日期格式与流水位数）。
    /// </summary>
    private static CompanyOrganizationDocumentNumberRule NormalizeDocumentNumberRule(int companyOrganizationId, CompanyOrganizationDocumentNumberRule rule)
    {
        var defaultRule = BuildDefaultCompanyOrganizationDocumentNumberRule(companyOrganizationId, rule.DocumentTypeCode);
        var normalizedPrefix = string.IsNullOrWhiteSpace(rule.Prefix)
            ? defaultRule.Prefix
            : rule.Prefix.Trim().ToUpperInvariant();
        var normalizedDateFormatCode = NormalizeDateFormatCode(rule.DateFormatCode);
        var normalizedSequenceLength = rule.SequenceLength switch
        {
            < 1 => defaultRule.SequenceLength,
            > 12 => 12,
            _ => rule.SequenceLength
        };

        return new CompanyOrganizationDocumentNumberRule
        {
            Id = rule.Id,
            CompanyOrganizationId = companyOrganizationId,
            DocumentTypeCode = NormalizeDocumentTypeCode(rule.DocumentTypeCode),
            Prefix = normalizedPrefix,
            DateFormatCode = normalizedDateFormatCode,
            SequenceLength = normalizedSequenceLength,
            LastDateSegment = rule.LastDateSegment,
            LastSequenceValue = rule.LastSequenceValue,
            LastGeneratedNumber = rule.LastGeneratedNumber,
            CreatedAt = rule.CreatedAt,
            UpdatedAt = rule.UpdatedAt,
            CreatedBy = string.IsNullOrWhiteSpace(rule.CreatedBy) ? rule.UpdatedBy : rule.CreatedBy,
            UpdatedBy = rule.UpdatedBy,
            IsDeleted = rule.IsDeleted
        };
    }

    /// <summary>
    /// 获取单据功能默认规则定义（未知功能时回退到通用定义）。
    /// </summary>
    private static CompanyOrganizationDocumentNumberRuleDefinition GetDocumentNumberRuleDefinition(string documentTypeCode)
    {
        var normalizedCode = NormalizeDocumentTypeCode(documentTypeCode);
        return DocumentNumberRuleDefinitions.FirstOrDefault(definition =>
                   string.Equals(definition.DocumentTypeCode, normalizedCode, StringComparison.OrdinalIgnoreCase))
               ?? new CompanyOrganizationDocumentNumberRuleDefinition
               {
                   DocumentTypeCode = normalizedCode,
                   DocumentTypeName = normalizedCode,
                   DefaultPrefix = BuildFallbackPrefix(normalizedCode),
                   DefaultDateFormatCode = "yyMM",
                   DefaultSequenceLength = 5
               };
    }

    /// <summary>
    /// 构建公司组织默认单号规则（当某个单据功能尚未配置时自动使用）。
    /// </summary>
    private static CompanyOrganizationDocumentNumberRule BuildDefaultCompanyOrganizationDocumentNumberRule(int companyOrganizationId, string documentTypeCode)
    {
        var definition = GetDocumentNumberRuleDefinition(documentTypeCode);
        return new CompanyOrganizationDocumentNumberRule
        {
            CompanyOrganizationId = companyOrganizationId,
            DocumentTypeCode = definition.DocumentTypeCode,
            Prefix = definition.DefaultPrefix,
            DateFormatCode = definition.DefaultDateFormatCode,
            SequenceLength = definition.DefaultSequenceLength
        };
    }

    /// <summary>
    /// 构建业务单号（前缀 + 日期片段 + 固定位数流水号）。
    /// </summary>
    private static string BuildDocumentNumber(string prefix, string dateSegment, int sequenceValue, int sequenceLength)
        => $"{prefix}{dateSegment}{Math.Max(sequenceValue, 1).ToString().PadLeft(Math.Max(sequenceLength, 1), '0')}";

    /// <summary>
    /// 按日期格式编码生成单号中的日期片段。
    /// </summary>
    private static string FormatDocumentNumberDateSegment(string? dateFormatCode, DateTime date)
        => NormalizeDateFormatCode(dateFormatCode) switch
        {
            "NONE" => string.Empty,
            "yyMM" => date.ToString("yyMM"),
            "yyyyMM" => date.ToString("yyyyMM"),
            "yyyyMMdd" => date.ToString("yyyyMMdd"),
            _ => date.ToString("yyMM")
        };

    /// <summary>
    /// 规范单据功能编码（统一转为大写下划线形式）。
    /// </summary>
    private static string NormalizeDocumentTypeCode(string? documentTypeCode)
        => string.IsNullOrWhiteSpace(documentTypeCode)
            ? "DOCUMENT"
            : documentTypeCode.Trim().ToUpperInvariant();

    /// <summary>
    /// 规范日期格式编码（不在允许范围内时回退为 yyMM）。
    /// </summary>
    private static string NormalizeDateFormatCode(string? dateFormatCode)
    {
        var normalizedCode = string.IsNullOrWhiteSpace(dateFormatCode)
            ? "yyMM"
            : dateFormatCode.Trim();

        return normalizedCode is "NONE" or "yyMM" or "yyyyMM" or "yyyyMMdd"
            ? normalizedCode
            : "yyMM";
    }

    /// <summary>
    /// 为未知单据功能生成默认前缀（取编码各段首字母并转为大写）。
    /// </summary>
    private static string BuildFallbackPrefix(string documentTypeCode)
    {
        var segments = documentTypeCode
            .Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var prefix = string.Concat(segments.Select(segment => segment[0])).ToUpperInvariant();
        return string.IsNullOrWhiteSpace(prefix) ? "DOC" : prefix;
    }

    /// <summary>
    /// 绑定任务跟进筛选参数（统一处理功能编码、所属个体、状态与关键词条件）。
    /// </summary>
    private static void BindTaskFollowUpQueryParams(SqlCommand cmd, string featureCode, int entityId, string? keyword, string? statusCode)
    {
        cmd.Parameters.Add(new SqlParameter("@FeatureCode", featureCode));
        cmd.Parameters.Add(new SqlParameter("@EntityId", entityId));
        cmd.Parameters.Add(new SqlParameter("@Keyword", ToDbValue(keyword)));
        cmd.Parameters.Add(new SqlParameter("@StatusCode", ToDbValue(statusCode)));
    }

    /// <summary>
    /// 绑定员工培训历程筛选参数（统一处理员工、历程类型与关键字条件）。
    /// </summary>
    private static void BindEmployeeTrainingExperienceQueryParams(SqlCommand cmd, int employeeId, string? keyword, string? experienceTypeCode)
    {
        cmd.Parameters.Add(new SqlParameter("@EmployeeId", employeeId));
        cmd.Parameters.Add(new SqlParameter("@Keyword", ToDbValue(keyword)));
        cmd.Parameters.Add(new SqlParameter("@ExperienceTypeCode", ToDbValue(experienceTypeCode)));
    }

    /// <summary>
    /// 绑定任务跟进保存参数（统一处理任务跟进实体入库参数）。
    /// </summary>
    private static void BindTaskFollowUpParams(SqlCommand cmd, TaskFollowUp taskFollowUp)
    {
        cmd.Parameters.Add(new SqlParameter("@FeatureCode", taskFollowUp.FeatureCode));
        cmd.Parameters.Add(new SqlParameter("@EntityId", taskFollowUp.EntityId));
        cmd.Parameters.Add(new SqlParameter("@TaskCode", taskFollowUp.TaskCode));
        cmd.Parameters.Add(new SqlParameter("@ArchivePath", ToDbValue(taskFollowUp.ArchivePath)));
        cmd.Parameters.Add(new SqlParameter("@StatusCode", taskFollowUp.StatusCode));
        cmd.Parameters.Add(new SqlParameter("@PlannedDate", ToDbValue(taskFollowUp.PlannedDate)));
        cmd.Parameters.Add(new SqlParameter("@TaskTypeCode", taskFollowUp.TaskTypeCode));
        cmd.Parameters.Add(new SqlParameter("@ExecutorName", ToDbValue(taskFollowUp.ExecutorName)));
        cmd.Parameters.Add(new SqlParameter("@Description", ToDbValue(taskFollowUp.Description)));
        cmd.Parameters.Add(new SqlParameter("@PriorityCode", taskFollowUp.PriorityCode));
        cmd.Parameters.Add(new SqlParameter("@ProgressPercent", taskFollowUp.ProgressPercent));
        cmd.Parameters.Add(new SqlParameter("@CompletedDate", ToDbValue(taskFollowUp.CompletedDate)));
        cmd.Parameters.Add(new SqlParameter("@ProjectCode", ToDbValue(taskFollowUp.ProjectCode)));
        cmd.Parameters.Add(new SqlParameter("@InitiatorName", ToDbValue(taskFollowUp.InitiatorName)));
    }

    /// <summary>
    /// 绑定员工培训历程保存参数（统一处理培训历程实体入库参数）。
    /// </summary>
    private static void BindEmployeeTrainingExperienceParams(SqlCommand cmd, EmployeeTrainingExperience experience)
    {
        cmd.Parameters.Add(new SqlParameter("@EmployeeId", experience.EmployeeId));
        cmd.Parameters.Add(new SqlParameter("@ExperienceTypeCode", experience.ExperienceTypeCode));
        cmd.Parameters.Add(new SqlParameter("@StartDate", experience.StartDate));
        cmd.Parameters.Add(new SqlParameter("@EndDate", ToDbValue(experience.EndDate)));
        cmd.Parameters.Add(new SqlParameter("@Description", experience.Description));
        cmd.Parameters.Add(new SqlParameter("@CertificateName", ToDbValue(experience.CertificateName)));
        cmd.Parameters.Add(new SqlParameter("@OrganizationName", ToDbValue(experience.OrganizationName)));
        cmd.Parameters.Add(new SqlParameter("@ArchivePath", ToDbValue(experience.ArchivePath)));
    }

    /// <summary>
    /// 映射任务跟进数据（将数据读取器记录转换为任务跟进实体）。
    /// </summary>
    private static TaskFollowUp MapTaskFollowUp(SqlDataReader reader)
        => new()
        {
            Id = reader.GetInt32(0),
            FeatureCode = ReadNullableString(reader, 1) ?? string.Empty,
            EntityId = reader.GetInt32(2),
            TaskCode = ReadNullableString(reader, 3) ?? string.Empty,
            ArchivePath = ReadNullableString(reader, 4),
            StatusCode = ReadNullableString(reader, 5) ?? string.Empty,
            PlannedDate = ReadNullableDateTime(reader, 6),
            TaskTypeCode = ReadNullableString(reader, 7) ?? string.Empty,
            ExecutorName = ReadNullableString(reader, 8),
            Description = ReadNullableString(reader, 9),
            PriorityCode = ReadNullableString(reader, 10) ?? string.Empty,
            ProgressPercent = reader.IsDBNull(11) ? 0 : reader.GetInt32(11),
            CompletedDate = ReadNullableDateTime(reader, 12),
            ProjectCode = ReadNullableString(reader, 13),
            InitiatorName = ReadNullableString(reader, 14),
            CreatedAt = reader.GetDateTime(15),
            UpdatedAt = ReadNullableDateTime(reader, 16),
            CreatedBy = ReadNullableString(reader, 17),
            UpdatedBy = ReadNullableString(reader, 18),
            IsDeleted = reader.GetBoolean(19)
        };

    /// <summary>
    /// 映射员工培训历程数据（将数据读取器记录转换为员工培训历程实体）。
    /// </summary>
    private static EmployeeTrainingExperience MapEmployeeTrainingExperience(SqlDataReader reader)
        => new()
        {
            Id = reader.GetInt32(0),
            EmployeeId = reader.GetInt32(1),
            ExperienceTypeCode = ReadNullableString(reader, 2) ?? string.Empty,
            StartDate = reader.GetDateTime(3),
            EndDate = ReadNullableDateTime(reader, 4),
            Description = ReadNullableString(reader, 5) ?? string.Empty,
            CertificateName = ReadNullableString(reader, 6),
            OrganizationName = ReadNullableString(reader, 7),
            ArchivePath = ReadNullableString(reader, 8),
            CreatedAt = reader.GetDateTime(9),
            UpdatedAt = ReadNullableDateTime(reader, 10),
            CreatedBy = ReadNullableString(reader, 11),
            UpdatedBy = ReadNullableString(reader, 12),
            IsDeleted = reader.GetBoolean(13)
        };

    /// <summary>
    /// 生成任务跟进编号（按时间戳生成便于人工辨识的编号）。
    /// </summary>
    private static string GenerateTaskFollowUpCode()
        => DateTime.Now.ToString("yyyyMMddHHmmssfff");

    private static void BindEmployeeParams(SqlCommand cmd, Employee employee)
    {
        cmd.Parameters.Add(new SqlParameter("@EmployeeCode", ToDbValue(employee.EmployeeCode)));
        cmd.Parameters.Add(new SqlParameter("@FirstName", employee.FirstName));
        cmd.Parameters.Add(new SqlParameter("@LastName", employee.LastName));
        cmd.Parameters.Add(new SqlParameter("@OrganizationId", ToDbValue(employee.OrganizationId)));
        cmd.Parameters.Add(new SqlParameter("@Email", employee.Email));
        cmd.Parameters.Add(new SqlParameter("@PhoneNumber", ToDbValue(employee.PhoneNumber)));
        cmd.Parameters.Add(new SqlParameter("@PhoneNumber2", ToDbValue(employee.PhoneNumber2)));
        cmd.Parameters.Add(new SqlParameter("@PhoneNumber3", ToDbValue(employee.PhoneNumber3)));
        cmd.Parameters.Add(new SqlParameter("@JobTitle", ToDbValue(employee.JobTitle)));
        cmd.Parameters.Add(new SqlParameter("@DepartmentId", ToDbValue(employee.DepartmentId)));
        cmd.Parameters.Add(new SqlParameter("@PositionId", ToDbValue(employee.PositionId)));
        cmd.Parameters.Add(new SqlParameter("@GroupId", ToDbValue(employee.GroupId)));
        cmd.Parameters.Add(new SqlParameter("@GenderId", ToDbValue(employee.GenderId)));
        cmd.Parameters.Add(new SqlParameter("@AliasName", ToDbValue(employee.AliasName)));
        cmd.Parameters.Add(new SqlParameter("@BirthDate", ToDbValue(employee.BirthDate)));
        cmd.Parameters.Add(new SqlParameter("@IdCardNumber", ToDbValue(employee.IdCardNumber)));
        cmd.Parameters.Add(new SqlParameter("@CardNumber", ToDbValue(employee.CardNumber)));
        cmd.Parameters.Add(new SqlParameter("@EthnicityId", ToDbValue(employee.EthnicityId)));
        cmd.Parameters.Add(new SqlParameter("@MaritalStatusId", ToDbValue(employee.MaritalStatusId)));
        cmd.Parameters.Add(new SqlParameter("@EducationLevelId", ToDbValue(employee.EducationLevelId)));
        cmd.Parameters.Add(new SqlParameter("@EducationCertificateNumber", ToDbValue(employee.EducationCertificateNumber)));
        cmd.Parameters.Add(new SqlParameter("@ProfessionalTitleId", ToDbValue(employee.ProfessionalTitleId)));
        cmd.Parameters.Add(new SqlParameter("@CountryRegionId", ToDbValue(employee.CountryRegionId)));
        cmd.Parameters.Add(new SqlParameter("@CityId", ToDbValue(employee.CityId)));
        cmd.Parameters.Add(new SqlParameter("@CountyId", ToDbValue(employee.CountyId)));
        cmd.Parameters.Add(new SqlParameter("@Address", ToDbValue(employee.Address)));
        cmd.Parameters.Add(new SqlParameter("@EmergencyContact", ToDbValue(employee.EmergencyContact)));
        cmd.Parameters.Add(new SqlParameter("@EmergencyContactPhone", ToDbValue(employee.EmergencyContactPhone)));
        cmd.Parameters.Add(new SqlParameter("@Referrer", ToDbValue(employee.Referrer)));
        cmd.Parameters.Add(new SqlParameter("@ArchivePath", ToDbValue(employee.ArchivePath)));
        cmd.Parameters.Add(new SqlParameter("@PhotoPath", ToDbValue(employee.PhotoPath)));
        cmd.Parameters.Add(new SqlParameter("@Remarks", ToDbValue(employee.Remarks)));
        cmd.Parameters.Add(new SqlParameter("@HireDate", ToDbValue(employee.HireDate)));
        cmd.Parameters.Add(new SqlParameter("@LeaveDate", ToDbValue(employee.LeaveDate)));
        cmd.Parameters.Add(new SqlParameter("@LoginAccount", ToDbValue(employee.LoginAccount)));
        cmd.Parameters.Add(new SqlParameter("@LoginPassword", ToDbValue(employee.LoginPassword)));
        cmd.Parameters.Add(new SqlParameter("@EmploymentTypeId", ToDbValue(employee.EmploymentTypeId)));
        cmd.Parameters.Add(new SqlParameter("@AllowancePackageId", ToDbValue(employee.AllowancePackageId)));
        cmd.Parameters.Add(new SqlParameter("@ProbationEndDate", ToDbValue(employee.ProbationEndDate)));
        cmd.Parameters.Add(new SqlParameter("@AnnualLeaveCalculationMethodId", ToDbValue(employee.AnnualLeaveCalculationMethodId)));
        cmd.Parameters.Add(new SqlParameter("@IsAttendanceRequired", employee.IsAttendanceRequired));
        cmd.Parameters.Add(new SqlParameter("@CurrentYearAnnualLeaveDays", ToDbValue(employee.CurrentYearAnnualLeaveDays)));
        cmd.Parameters.Add(new SqlParameter("@AnnualLeaveMaxAccumulatedDays", ToDbValue(employee.AnnualLeaveMaxAccumulatedDays)));
        cmd.Parameters.Add(new SqlParameter("@AnnualLeaveRemainingDays", ToDbValue(employee.AnnualLeaveRemainingDays)));
        cmd.Parameters.Add(new SqlParameter("@AnnualLeaveIncrementStartYears", ToDbValue(employee.AnnualLeaveIncrementStartYears)));
        cmd.Parameters.Add(new SqlParameter("@AnnualLeaveIncrementPerYearDays", ToDbValue(employee.AnnualLeaveIncrementPerYearDays)));
        cmd.Parameters.Add(new SqlParameter("@AnnualLeaveCapDays", ToDbValue(employee.AnnualLeaveCapDays)));
        cmd.Parameters.Add(new SqlParameter("@DefaultShiftId", ToDbValue(employee.DefaultShiftId)));
        cmd.Parameters.Add(new SqlParameter("@SchedulingGroupId", ToDbValue(employee.SchedulingGroupId)));
        cmd.Parameters.Add(new SqlParameter("@IsAutoSchedulingEnabled", employee.IsAutoSchedulingEnabled));
        cmd.Parameters.Add(new SqlParameter("@SalaryGradeId", ToDbValue(employee.SalaryGradeId)));
        cmd.Parameters.Add(new SqlParameter("@PayrollCompanyId", ToDbValue(employee.PayrollCompanyId)));
        cmd.Parameters.Add(new SqlParameter("@BankAccountNumber", ToDbValue(employee.BankAccountNumber)));
        cmd.Parameters.Add(new SqlParameter("@BankAccountName", ToDbValue(employee.BankAccountName)));
        cmd.Parameters.Add(new SqlParameter("@BankId", ToDbValue(employee.BankId)));
    }

    private static void BindCompanyOrganizationParams(SqlCommand cmd, CompanyOrganization organization)
    {
        cmd.Parameters.Add(new SqlParameter("@OrganizationCode", organization.OrganizationCode));
        cmd.Parameters.Add(new SqlParameter("@OrganizationName", organization.OrganizationName));
        cmd.Parameters.Add(new SqlParameter("@CompanyNatureId", ToDbValue(organization.CompanyNatureId)));
        cmd.Parameters.Add(new SqlParameter("@StatusId", ToDbValue(organization.StatusId)));
        cmd.Parameters.Add(new SqlParameter("@EnterpriseTypeId", ToDbValue(organization.EnterpriseTypeId)));
        cmd.Parameters.Add(new SqlParameter("@BusinessRegistrationNumber", ToDbValue(organization.BusinessRegistrationNumber)));
        cmd.Parameters.Add(new SqlParameter("@BusinessRegistrationExpiryDate", ToDbValue(organization.BusinessRegistrationExpiryDate)));
        cmd.Parameters.Add(new SqlParameter("@RegionId", ToDbValue(organization.RegionId)));
        cmd.Parameters.Add(new SqlParameter("@CityId", ToDbValue(organization.CityId)));
        cmd.Parameters.Add(new SqlParameter("@CountyId", ToDbValue(organization.CountyId)));
        cmd.Parameters.Add(new SqlParameter("@Address", ToDbValue(organization.Address)));
        cmd.Parameters.Add(new SqlParameter("@Principal", ToDbValue(organization.Principal)));
        cmd.Parameters.Add(new SqlParameter("@Phone", ToDbValue(organization.Phone)));
        cmd.Parameters.Add(new SqlParameter("@Fax", ToDbValue(organization.Fax)));
        cmd.Parameters.Add(new SqlParameter("@Email", ToDbValue(organization.Email)));
        cmd.Parameters.Add(new SqlParameter("@Website", ToDbValue(organization.Website)));
        cmd.Parameters.Add(new SqlParameter("@WeeklyWorkDays", ToDbValue(organization.WeeklyWorkDays)));
        cmd.Parameters.Add(new SqlParameter("@LeaveCountBasisType", ToDbValue(organization.LeaveCountBasisType)));
        cmd.Parameters.Add(new SqlParameter("@HolidayType", ToDbValue(organization.HolidayType)));
        cmd.Parameters.Add(new SqlParameter("@AnnualLeaveCalculationMonthDay", ToDbValue(organization.AnnualLeaveCalculationMonthDay)));
        cmd.Parameters.Add(new SqlParameter("@AnnualLeaveGrantRule", ToDbValue(organization.AnnualLeaveGrantRule)));
        cmd.Parameters.Add(new SqlParameter("@AnnualLeaveGrantMonthDay", ToDbValue(organization.AnnualLeaveGrantMonthDay)));
        cmd.Parameters.Add(new SqlParameter("@IsAnnualLeaveClearEnabled", organization.IsAnnualLeaveClearEnabled));
        cmd.Parameters.Add(new SqlParameter("@AnnualLeaveClearMonthDay", ToDbValue(organization.AnnualLeaveClearMonthDay)));
        cmd.Parameters.Add(new SqlParameter("@IsCarryForwardAnnualLeaveAllowed", organization.IsCarryForwardAnnualLeaveAllowed));
        cmd.Parameters.Add(new SqlParameter("@BaseAnnualLeaveDays", ToDbValue(organization.BaseAnnualLeaveDays)));
        cmd.Parameters.Add(new SqlParameter("@AnnualLeaveIncrementStartYears", ToDbValue(organization.AnnualLeaveIncrementStartYears)));
        cmd.Parameters.Add(new SqlParameter("@AnnualLeaveIncrementPerYearDays", ToDbValue(organization.AnnualLeaveIncrementPerYearDays)));
        cmd.Parameters.Add(new SqlParameter("@AnnualLeaveCapDays", ToDbValue(organization.AnnualLeaveCapDays)));
        cmd.Parameters.Add(new SqlParameter("@AnnualLeaveMaxAccumulatedDays", ToDbValue(organization.AnnualLeaveMaxAccumulatedDays)));
        cmd.Parameters.Add(new SqlParameter("@PaidSickLeaveDaysPerYear", ToDbValue(organization.PaidSickLeaveDaysPerYear)));
        cmd.Parameters.Add(new SqlParameter("@PaidSickLeaveSalaryRatio", ToDbValue(organization.PaidSickLeaveSalaryRatio)));
        cmd.Parameters.Add(new SqlParameter("@PaidSickLeaveCalculationMonthDay", ToDbValue(organization.PaidSickLeaveCalculationMonthDay)));
        cmd.Parameters.Add(new SqlParameter("@IsPaidSickLeaveClearEnabled", organization.IsPaidSickLeaveClearEnabled));
        cmd.Parameters.Add(new SqlParameter("@PaidSickLeaveClearMonthDay", ToDbValue(organization.PaidSickLeaveClearMonthDay)));
        cmd.Parameters.Add(new SqlParameter("@EmployeeMpfMinimumSalary", ToDbValue(organization.EmployeeMpfMinimumSalary)));
        cmd.Parameters.Add(new SqlParameter("@Remarks", ToDbValue(organization.Remarks)));
        cmd.Parameters.Add(new SqlParameter("@ArchivePath", ToDbValue(organization.ArchivePath)));
        cmd.Parameters.Add(new SqlParameter("@PrintHeaderContent", ToDbValue(organization.PrintHeaderContent)));
        cmd.Parameters.Add(new SqlParameter("@PrintFooterContent", ToDbValue(organization.PrintFooterContent)));
    }

    /// <summary>
    /// 执行初始化 SQL 语句。
    /// </summary>
    private static async Task ExecuteNonQueryAsync(SqlConnection conn, string sql)
    {
        await using var cmd = new SqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    private static object ToDbValue(string? value) => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value;
    private static object ToDbValue(int? value) => value.HasValue ? value.Value : DBNull.Value;
    private static object ToDbValue(DateTime? value) => value.HasValue ? value.Value : DBNull.Value;
    private static object ToDbValue(decimal? value) => value.HasValue ? value.Value : DBNull.Value;

    private static string? ReadNullableString(SqlDataReader reader, int index)
        => reader.IsDBNull(index) ? null : reader.GetString(index);

    private static int? ReadNullableInt(SqlDataReader reader, int index)
        => reader.IsDBNull(index) ? null : reader.GetInt32(index);

    /// <summary>
    /// 读取可空小数值字段。
    /// </summary>
    private static decimal? ReadNullableDecimal(SqlDataReader reader, int index)
        => reader.IsDBNull(index) ? null : reader.GetDecimal(index);

    private static DateTime? ReadNullableDateTime(SqlDataReader reader, int index)
        => reader.IsDBNull(index) ? null : reader.GetDateTime(index);

    private async Task<SqlConnection> OpenConnectionAsync()
    {
        var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        return conn;
    }
}


