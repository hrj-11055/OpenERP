# OpenERP — 模块化 ERP 系统

基于 **ASP.NET Core MVC (.NET 10)** + **SQL Server** + **EF Core** 构建的模块化企业资源计划（ERP）系统，覆盖销售、采购、生产、财务、人事、物流等业务领域，内置中 / 繁 / 英三语支持。

## 功能模块

| 模块 | 项目 | 说明 |
|------|------|------|
| 销售 | `OpenERP.Sales` | 销售报价、销售订单、客户 |
| 采购 | `OpenERP.Purchasing` | 采购订单、供应商 |
| 生产 | `OpenERP.Production` | 生产订单、工作中心 |
| 财务 | `OpenERP.Finance` | 账户、交易 |
| 人事 | `OpenERP.HR` | 员工、部门、公司组织 |
| 客户关系 | `OpenERP.CRM` | 线索、商机 |
| 物流 | `OpenERP.Logistics` | 发运、承运商 |
| 运输 | `OpenERP.Transport` | 车辆、运输申请 |
| 资产 | `OpenERP.Asset` | 资产、维保记录 |
| 服务 | `OpenERP.Service` | 服务合同、服务请求 |
| 办公 | `OpenERP.Office` | 会议、任务 |
| 基础数据 | `OpenERP.BasicData` | 数据字典、物料、类型 |

另有报表中心、任务中心、系统管理等 Area 直接位于 `OpenERP.Web` 中（无独立类库项目）。

---

## 快速上手

### 环境要求

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server（开发默认使用 LocalDB，Visual Studio 自带）
- Node.js（仅登录页前端构建需要）

### 三步跑起来

```bash
# 1. 还原并构建整个解决方案
dotnet build OpenERP.slnx

# 2. 运行 Web 项目（首次启动会自动建表并写入种子数据）
dotnet run --project OpenERP.Web/OpenERP.Web.csproj

# 3. 浏览器打开 https://localhost:xxxx（控制台输出的地址）
```

- **演示账号**：`admin` / `123456`
- 登录页由 Vue 3 + Element Plus 构建，源码在 `OpenERP.Frontend/`，构建产物输出到 `OpenERP.Web/wwwroot/vue-login/`（已 gitignore）。若登录页样式丢失，执行：

```bash
cd OpenERP.Frontend
npm install
npm run build
```

- 数据库连接串在 [OpenERP.Web/appsettings.json](OpenERP.Web/appsettings.json)，默认 `(localdb)\mssqllocaldb`，库名 `OpenERP`。

### 常用命令

```bash
dotnet watch --project OpenERP.Web/OpenERP.Web.csproj   # 开发热重载

# EF Core 迁移（以 Sales 模块为例）
dotnet ef migrations add MigrationName --project OpenERP.Sales --startup-project OpenERP.Web
dotnet ef database update --project OpenERP.Sales --startup-project OpenERP.Web

dotnet publish OpenERP.Web/OpenERP.Web.csproj -c Release -o ./output   # 发布
```

---

## 代码架构

### 整体结构

```
OpenERP.slnx                     # 解决方案（新版 XML 格式）
├── OpenERP.Web/                 # ★ 唯一的可执行项目：MVC 宿主，所有控制器和视图都在这里
│   ├── Areas/{模块}/Controllers/ # 各模块的 MVC 控制器
│   ├── Areas/{模块}/Views/       # Razor 视图
│   ├── Controllers/              # 根控制器（登录 Account、认证 API AuthApi 等）
│   ├── Data/HR/                  # HR 模块的 ADO.NET 仓储实现（SQL 版）
│   ├── Resources/Localization/   # 多语言资源文件（.resx）
│   ├── Security/                 # 登录防暴力破解（LoginThrottler）
│   ├── Printing/ 、Documents/    # 打印模板、单据服务
│   └── wwwroot/                  # 静态资源（css / js / vue-login）
├── OpenERP.Sales/ ... OpenERP.Office/   # ★ 12 个业务模块类库：实体 + DbContext + 迁移
│   ├── Models/Entities/          #   领域实体（继承 BaseEntity）
│   ├── Data/                     #   ApplicationDbContext（EF Core 模块）
│   └── Migrations/               #   EF Core 迁移
├── OpenERP.Frontend/             # Vue 3 + Element Plus 登录页源码（Vite 构建）
├── docs/                         # 设计原型（墨刀 HTML）、评审报告
└── scripts/                      # AI 日报定时任务的 PowerShell 脚本
```

**核心思路**：每个业务模块是一个独立类库，只放「实体 + DbContext + 迁移」；所有控制器、视图集中在 `OpenERP.Web` 的 Area 中。看懂一个模块，就看懂了所有模块。

### 一个请求的完整链路

以访问 `/Sales/SalesOrders` 为例：

```
浏览器
  → 路由 {area:exists}/{controller}/{action}（Program.cs）
  → OpenERP.Web/Areas/Sales/Controllers/SalesOrdersController.cs（业务逻辑）
  → OpenERP.Sales/Data/ApplicationDbContext（EF Core 查询）
  → SQL Server
  → Areas/Sales/Views/*.cshtml（Razor 渲染）
  → 浏览器（Bootstrap + wwwroot/js 模块页脚本）
```

### 两种数据访问模式（重要！）

项目中**并存**两种访问数据库的方式：

| 模式 | 使用模块 | 位置 |
|------|---------|------|
| **EF Core**（推荐，新模块用它） | Sales、Purchasing、Finance、CRM、Asset、Service、Transport、Production、Office、Logistics | 各模块 `Data/ApplicationDbContext.cs` |
| **原生 ADO.NET**（SqlConnection + 参数化 SQL） | HR、BasicData | 接口在模块内，实现在 `OpenERP.Web/Data/HR/`、模块内部 |

所有模块共用同一个连接串。**没有共享基础库**——`BaseEntity` 在每个 EF 模块中都有一份相同的拷贝，各模块的 DbContext 都叫 `ApplicationDbContext`，因此在 [Program.cs](OpenERP.Web/Program.cs) 中通过 `using` 别名区分（如 `using SalesDbContext = OpenERP.Sales.Data.ApplicationDbContext;`）。

### 关键横切机制

- **认证**：Cookie 认证（`OpenERP.Auth`），8 小时滑动过期。登录有双重防护——账号连续失败 5 次锁定 15 分钟（`LoginThrottler`），登录接口按 IP 限流每分钟 10 次。演示账号 `admin/123456`。
- **Claims**：`NameIdentifier`（用户）、`erp:account`（账号）、`erp:company`（公司）、`erp:fiscalYear`（会计年度）。
- **多语言**：zh-Hans（默认）/ zh-Hant / en，解析顺序 QueryString → Cookie → Accept-Language。文案统一在 `OpenERP.Web/Resources/Localization/SharedResource.*.resx`。
- **软删除**：实体带 `IsDeleted` 标记；Logistics 模块通过 `HasQueryFilter` 全局过滤，**其余模块需在查询中手动过滤**。
- **审计字段**：`CreatedAt / CreatedBy / UpdatedAt / UpdatedBy` 目前由控制器手动赋值。

### 命名约定（务必遵守）

- 所有类 / 属性 / 方法必须有**中文注释**说明业务含义，详见 [AGENTS.md](AGENTS.md)。
- 业务表名：`<模块前缀>_<实体名>`，如 `HR_Employee`、`SA_SalesOrder`（完整前缀表见 CLAUDE.md）。
- 实体继承 `BaseEntity`，自动获得 Id、审计字段、软删除标记。

---

## 新手导航：我想改 XX，去哪找？

| 我想… | 去这里 |
|-------|--------|
| 改某个页面的 HTML / 样式 | `OpenERP.Web/Areas/{模块}/Views/` + `wwwroot/css/` |
| 改页面交互逻辑（JS） | `OpenERP.Web/wwwroot/js/{模块}*.js` |
| 改业务逻辑（增删改查） | `OpenERP.Web/Areas/{模块}/Controllers/` |
| 加 / 改数据表字段 | 模块项目 `Models/Entities/` → 新建迁移 → `dotnet ef database update` |
| 改登录页 | `OpenERP.Frontend/src/`（改完 `npm run build`） |
| 改登录 / 认证逻辑 | `OpenERP.Web/Controllers/AccountController.cs`、`AuthApiController.cs` |
| 加 / 改界面文案（多语言） | `OpenERP.Web/Resources/Localization/SharedResource.*.resx` |
| 看 DI 注册、中间件顺序、路由 | [OpenERP.Web/Program.cs](OpenERP.Web/Program.cs) |
| 修改 HR 模块查询 SQL | `OpenERP.Web/Data/HR/HrSqlRepository.cs` |
| 看数据库表结构 | 各模块 `Migrations/`，或 SSMS 直连 LocalDB |

### 新增一个业务模块（概要）

1. `dotnet new classlib -n OpenERP.{Module} -f net10.0` 并加入解决方案
2. `OpenERP.Web` 添加项目引用
3. 创建实体（继承 BaseEntity，带中文注释）
4. 创建 `Data/ApplicationDbContext.cs`，在 `OnModelCreating` 中配置 `ToTable("前缀_表名")`
5. 在 `Program.cs` 注册 DbContext（记得加 `using` 别名）
6. 在 `OpenERP.Web/Areas/{Module}/` 创建 Controllers 和 Views
7. 生成初始迁移并更新数据库

完整细节见 [CLAUDE.md](CLAUDE.md)（AI 辅助开发必读）。

---

## 后续优化迭代方向

按优先级从高到低排列，供规划参考：

### 🔴 高优先级（架构债）

1. **抽取共享基础库 `OpenERP.Shared`**
   `BaseEntity` 在 12 个 EF 模块中各有一份完全相同的拷贝，`ApplicationDbContext` 全部同名导致 `Program.cs` 需要 10 个 `using` 别名。抽公共项目后：改一处生效全局、消除命名冲突、新模块不必复制粘贴。

2. **统一数据访问模式**
   HR / BasicData 用原生 ADO.NET（SQL 字符串拼接在仓储里），其余模块用 EF Core；且 HR 的 SQL 实现放在 `OpenERP.Web/Data/HR/` 而非模块项目内，导致模块不自治。建议逐步迁移到 EF Core + 迁移管理，让每个模块可独立测试与复用。

3. **补齐自动化测试**
   当前没有任何测试项目，`dotnet build` 是唯一的自动化验证。建议先建 `OpenERP.Tests` 集成测试项目，优先覆盖：登录认证、销售订单生命周期、软删除行为、多语言切换。

### 🟡 中优先级（一致性 / 工程化）

4. **老模块表名补前缀**
   Sales、Purchasing、Finance、Logistics 早期未按 `<前缀>_<实体名>` 约定建表，与其他模块不一致，跨模块查询时容易混淆。需通过 EF 迁移重命名（注意数据保留）。

5. **审计字段与软删除改为框架级处理**
   目前 `CreatedAt / UpdatedBy` 等在控制器手动赋值容易遗漏；软删除仅 Logistics 有全局 `HasQueryFilter`。建议统一在 `DbContext.SaveChanges` 重写中自动填充审计字段，并为所有实体配置全局软删除过滤器。

6. **搭建 CI 流水线**
   仓库暂无 CI 配置。建议用 GitHub Actions：PR 触发 `dotnet build` + 测试，main 分支自动发布产物。

7. **前端技术栈收敛**
   登录页是 Vue 3 + Element Plus，主界面是 Razor + Bootstrap，双栈并存意味着两套构建链和技能要求。建议明确边界（如新页面一律 Vue）或渐进式统一。

### 🟢 低优先级（规模化后考虑）

8. **系统性 REST API 层**
   目前 API 控制器仅服务于登录页。若需要移动端 / 第三方集成，需整体设计 API 版本化、Token 鉴权（现在只有 Cookie）和统一响应格式。

9. **跨模块事务与查询**
   12 个 DbContext 共享一个连接但相互独立，跨模块业务（如「销售出库 → 扣减库存 → 记账」）无法在单事务内完成。可考虑单一 DbContext、或跨模块事件 / 工作单元模式。

10. **部署态安全增强**
    `LoginThrottler` 基于内存缓存，仅单实例有效；多实例部署需换分布式存储（如 Redis）。生产环境建议完善日志 / 审计链路（可参考 [OpenERP.Web-threat-model.md](OpenERP.Web-threat-model.md)）。

---

## 相关文档

- [CLAUDE.md](CLAUDE.md) — AI 辅助开发指南（构建命令、架构细节、新增模块步骤）
- [AGENTS.md](AGENTS.md) — 中文注释与数据库表命名规范
- [OpenERP.Web-threat-model.md](OpenERP.Web-threat-model.md) — 威胁模型分析
- `docs/design/` — 墨刀设计原型（HR 页面）
- `docs/review/` — 项目评审报告与问题矩阵

## 版本控制约定

- 提交信息格式：`模块名: 说明`（中文），如 `HR: 新增员工导出功能`
- 远程仓库为私有 GitHub 仓库，`OpenERP.Web/wwwroot/vue-login/` 等构建产物不入库
