using OpenERP.HR.Models.Entities;

namespace OpenERP.Web.Data.HR;

public interface IHrRepository
{
    /// <summary>
    /// 初始化人资模块基础表结构（含公司组织成员表）。
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// 获取公司组织列表（来自公司组织主数据表）。
    /// </summary>
    Task<List<CompanyOrganization>> GetCompanyOrganizationsAsync();
    /// <summary>
    /// 鎸夊叕鍙哥粍缁嘔D鑾峰彇璇︾粏璁板綍銆?
    /// </summary>
    Task<CompanyOrganization?> GetCompanyOrganizationByIdAsync(int id);
    /// <summary>
    /// 按组织编码获取公司组织资料（对应 CompanyOrganization.OrganizationCode）。
    /// </summary>
    Task<CompanyOrganization?> GetCompanyOrganizationByCodeAsync(string organizationCode);
    /// <summary>
    /// 鏂板鍏徃缁勭粐涓绘暟鎹褰曘€?
    /// </summary>
    Task<int> CreateCompanyOrganizationAsync(CompanyOrganization organization);
    /// <summary>
    /// 鏇存柊鍏徃缁勭粐涓绘暟鎹褰曘€?
    /// </summary>
    Task<bool> UpdateCompanyOrganizationAsync(CompanyOrganization organization);
    /// <summary>
    /// 鍒犻櫎鍏徃缁勭粐璁板綍锛堥€昏緫鍒犻櫎锛夈€?
    /// </summary>
    Task<bool> DeleteCompanyOrganizationAsync(int id);

    /// <summary>
    /// 查询公司组织银行账号分页数据（按组织、状态与关键字筛选）。
    /// </summary>
    Task<CompanyOrganizationBankAccountPageResult> GetCompanyOrganizationBankAccountsAsync(
        int companyOrganizationId,
        string? keyword,
        string? statusCode,
        int pageNumber,
        int pageSize);

    /// <summary>
    /// 保存公司组织银行账号记录（新增时写入，编辑时更新所属组织下的现有记录）。
    /// </summary>
    Task<int> SaveCompanyOrganizationBankAccountAsync(CompanyOrganizationBankAccount bankAccount);

    /// <summary>
    /// 删除公司组织银行账号记录（仅删除指定组织下选中的记录）。
    /// </summary>
    Task<int> DeleteCompanyOrganizationBankAccountsAsync(int companyOrganizationId, List<int> ids);

    /// <summary>
    /// 查询公司组织单号规则列表（按组织返回当前可用的单号规则配置）。
    /// </summary>
    Task<List<CompanyOrganizationDocumentNumberRule>> GetCompanyOrganizationDocumentNumberRulesAsync(int companyOrganizationId);

    /// <summary>
    /// 保存公司组织单号规则列表（按单据功能逐条新增或更新规则）。
    /// </summary>
    Task<int> SaveCompanyOrganizationDocumentNumberRulesAsync(int companyOrganizationId, IReadOnlyCollection<CompanyOrganizationDocumentNumberRule> rules);

    /// <summary>
    /// 按公司组织与单据功能生成下一个业务单号（供各单据模块统一调用）。
    /// </summary>
    Task<string> GenerateDocumentNumberAsync(int companyOrganizationId, string documentTypeCode, DateTime? businessDate = null);

    Task<List<Department>> GetDepartmentsAsync();

    /// <summary>
    /// 获取职位列表（来自职位主数据表）。
    /// </summary>
    Task<List<Position>> GetPositionsAsync();

    /// <summary>
    /// 按职位ID获取职位数据（对应 Position 实体）。
    /// </summary>
    Task<Position?> GetPositionByIdAsync(int id);

    /// <summary>
    /// 新增职位数据（写入 HR_Position 表）。
    /// </summary>
    Task<int> CreatePositionAsync(Position position);

    /// <summary>
    /// 更新职位数据（更新 HR_Position 表）。
    /// </summary>
    Task<bool> UpdatePositionAsync(Position position);

    /// <summary>
    /// 删除职位数据（逻辑删除 HR_Position 表记录）。
    /// </summary>
    Task<bool> DeletePositionAsync(int id);

    Task CreateDepartmentAsync(Department department);

    Task<List<Employee>> GetEmployeesAsync();
    Task<Employee?> GetEmployeeByIdAsync(int id);
    Task<Employee?> GetEmployeeDetailsAsync(int id);
    Task<int> CreateEmployeeAsync(Employee employee);
    Task<bool> UpdateEmployeeAsync(Employee employee);
    /// <summary>
    /// 更新员工主图路径（对应 HR_Employee.PhotoPath 字段，路径来自受控文档打开地址）。
    /// </summary>
    Task<bool> UpdateEmployeePhotoPathAsync(int employeeId, string? photoPath, string? updatedBy);
    Task<bool> DeleteEmployeeAsync(int id);
    /// <summary>
    /// 检查在职员工卡号是否重复（仅校验未删除且未离职的员工）。
    /// </summary>
    Task<bool> ExistsActiveEmployeeCardNumberAsync(string cardNumber, int? excludedEmployeeId = null);

    /// <summary>
    /// 获取所有角色列表。
    /// </summary>
    Task<List<Role>> GetRolesAsync();

    /// <summary>
    /// 获取所有功能权限列表。
    /// </summary>
    Task<List<Permission>> GetPermissionsAsync();

    /// <summary>
    /// 获取用户个人权限ID列表。
    /// </summary>
    Task<List<int>> GetUserPermissionIdsAsync(int employeeId);

    /// <summary>
    /// 获取用户管辖公司组织ID列表。
    /// </summary>
    Task<List<int>> GetUserCompanyIdsAsync(int employeeId);

    /// <summary>
    /// 保存用户账号及权限设置（含登录账号、密码、角色、有效期、冻结状态、功能权限、管辖公司）。
    /// </summary>
    Task<bool> SaveUserAccountAsync(int employeeId, string? loginAccount, string? hashedPassword,
        int? roleId, DateTime? accountValidUntil, bool isAccountFrozen, int? forceViewRecordDays,
        List<int> permissionIds, List<int> companyIds);

    /// <summary>
    /// 按登录账号查找员工（含冻结、有效期等账号字段，用于登录校验）。
    /// </summary>
    Task<Employee?> GetEmployeeByLoginAccountAsync(string loginAccount);

    /// <summary>
    /// 更新员工登录密码。
    /// </summary>
    Task<bool> UpdateEmployeePasswordAsync(int employeeId, string hashedPassword);

    /// <summary>
    /// 获取用户管辖公司组织编码列表。
    /// </summary>
    Task<List<string>> GetUserCompanyCodesAsync(int employeeId);
    /// <summary>
    /// 获取用户生效权限编码列表（角色权限与用户个人权限合并去重）。
    /// </summary>
    Task<List<string>> GetEffectivePermissionCodesAsync(int employeeId);
    /// <summary>
    /// 查询任务跟进分页数据（按功能编码、所属个体与筛选条件返回任务记录）。
    /// </summary>
    Task<TaskFollowUpPageResult> GetTaskFollowUpsAsync(string featureCode, int entityId, string? keyword, string? statusCode, int pageNumber, int pageSize);

    /// <summary>
    /// 保存任务跟进记录（新增时创建新记录，编辑时更新当前功能与所属个体下的既有记录）。
    /// </summary>
    Task<int> SaveTaskFollowUpAsync(TaskFollowUp taskFollowUp);

    /// <summary>
    /// 删除任务跟进记录（仅删除当前功能与所属个体下选中的记录）。
    /// </summary>
    Task<int> DeleteTaskFollowUpsAsync(string featureCode, int entityId, List<int> ids);

    /// <summary>
    /// 查询员工培训历程分页数据（按员工、历程类型与关键字返回工作/培训/教育经历）。
    /// </summary>
    Task<EmployeeTrainingExperiencePageResult> GetEmployeeTrainingExperiencesAsync(
        int employeeId,
        string? keyword,
        string? experienceTypeCode,
        int pageNumber,
        int pageSize);

    /// <summary>
    /// 保存员工培训历程记录（新增时创建记录，编辑时更新当前员工名下的既有记录）。
    /// </summary>
    Task<int> SaveEmployeeTrainingExperienceAsync(EmployeeTrainingExperience experience);

    /// <summary>
    /// 删除员工培训历程记录（仅删除当前员工名下选中的记录）。
    /// </summary>
    Task<int> DeleteEmployeeTrainingExperiencesAsync(int employeeId, List<int> ids);
}
