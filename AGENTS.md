# OpenERP 项目协作约定

## 命名与注释规则（长期生效）
- 新增或修改源码中的命名（类、属性、字段、方法、关键变量）时，必须在源代码中提供中文注释。
- 注释应写在被注释成员上方，简洁说明业务含义，不写无意义注释。
- 由 Codex、脚本或其他自动化工具新增或修改的源码注释、提示文案、资源文案必须生成中文，不得生成英文占位说明或乱码文本。
- 修改源码注释、提示文案、资源文件、Markdown 协作文档时，文件必须保持 UTF-8 编码；发现已有乱码时，应优先修复为可读中文后再继续修改。
- 提交或交付前必须检查新增/修改内容是否存在乱码。若出现 mojibake、替换字符或不可读文本，必须立即修正，不能把乱码留在源码、页面提示或资源文件中。
- 业务字段优先注明：
  - 业务含义（例如“民族ID（基础数据）”）
  - 数据来源（例如“来自基础数据字典”）
  - 关键约束（如“只读/不入库”）
- 对于 `Id` 类外键字段，注释中应明确“对应哪个字典/实体”。
- 对于缩写或英文术语，注释中应给出中文解释。

## 数据库表命名统一规则（长期生效）
- 所有业务表统一采用：`<业务前缀>_<实体名>`。
- 业务前缀统一大写英文缩写，建议 2-4 位，例如：
  - `AS`：资产模块（Asset）
  - `CRM`：客户关系模块（CRM）
  - `OF`：办公模块（Office）
  - `PRD`：生产模块（Production）
  - `SV`：服务模块（Service）
  - `TR`：运输模块（Transport）
  - `BD`：基础数据模块（BasicData）
  - `HR`：人资模块（Human Resources）
  - `FIN`：财务模块（Finance）
  - `LOG`：物流模块（Logistics）
  - `PO`：采购模块（Purchasing）
  - `SA`：销售模块（Sales）
- 实体名统一使用 PascalCase 单数形式，不使用复数。
- 示例：
  - `OF_Meeting`
  - `SV_ServiceContract`
  - `PRD_ProductionOrderItem`
  - `BD_BasicDataType`
  - `HR_Employee`
- 新增模块时，需先确定模块前缀，再创建或映射数据库表。
- 注：Sales、Purchasing、Finance、Logistics 等早期 EF Core 模块尚未迁移到前缀表名；新模块必须在 `OnModelCreating` 中用 `modelBuilder.Entity<T>().ToTable("前缀_实体名")` 显式配置表名。
- 系统级共享表使用 `SYS_` 前缀，例如通用文档表 `SYS_Document`。

## ASP.NET Core 控制器与权限规则
- 除登录、静态公开资源、健康检查等明确公开入口外，MVC 控制器和 API 控制器默认必须要求登录访问。
- 新增控制器优先在类级别添加 `[Authorize]`；确需公开访问的 Action 必须显式添加 `[AllowAnonymous]`，并在注释中说明公开原因。
- 使用 Cookie 认证的 POST、PUT、PATCH、DELETE 接口必须有 CSRF 防护策略：
  - Razor 表单使用 `[ValidateAntiForgeryToken]`。
  - JSON/API 请求应统一使用防伪 Token Header，或明确改为不依赖 Cookie 的认证方式。
- 控制器只负责 HTTP 编排；业务规则、文件存储、数据库访问应下沉到服务或仓储。
- API 返回结果应使用明确的状态码与结构化错误消息，不直接泄露物理路径、连接字符串、堆栈信息或内部实现细节。

## 文件上传与通用文档管理
- 单据、员工资料、客户资料、供应商资料等页面需要附件时，统一复用通用文档管理能力，不再新增“档案路径”文本框。
- 通用文档以 `FeatureCode + EntityId` 关联业务记录；新增业务入口前必须确定稳定的 `FeatureCode`。
- 文件上传必须校验：
  - 关联业务记录存在且当前用户有权访问。
  - 文件大小上限。
  - 文件扩展名和内容类型。
  - 原始文件名仅用于显示和下载，不得直接作为物理存储名。
- 物理文件必须存放在非 `wwwroot` 目录，下载或预览必须通过受控 API。
- 删除文档应采用元数据软删除，并同步尝试清理物理文件；物理文件删除失败不得导致业务记录状态不一致。

## 数据访问规则
- EF Core 模块优先使用 `DbContext`、实体映射和迁移管理表结构。
- HR、BasicData 等历史 ADO.NET 仓储允许继续使用参数化 SQL，但不得拼接用户输入到 SQL 文本中。
- `AddWithValue` 仅可用于低风险临时查询；新增复杂查询应使用显式 `SqlParameter` 类型和长度。
- 初始化 SQL 必须可重复执行，新增字段、索引、约束前先判断是否存在。
- 软删除表查询默认过滤 `IsDeleted = 0`，除审计或恢复场景外不得返回已删除记录。

## 前端页面与脚本规则
- 页面功能优先复用现有共享脚本、样式和组件；新共享能力应放入独立文件，例如 `document-manager.js` 和 `document-manager.css`。
- 通过 `innerHTML` 渲染动态数据时，必须先进行 HTML 转义；文件名、用户输入、备注、描述等不得直接插入 HTML。
- 新增按钮、弹窗、上传入口必须处理只读态、空记录态、加载中、失败提示和重复点击。
- 前端不保存或显示后端物理路径；下载、打开、删除等操作只使用后端返回的受控 URL 或文档 ID。

## 通用页面视觉风格（长期生效）
- 页面必须优先复用项目既有样式，不得随意引入新的视觉体系：
  - 列表/主从列表页面优先使用 `company-organizations.css` 中的 `company-org-*` 风格，包括工具栏、查询区、数据表格、状态标签、分页和选中行。
  - 详情/编辑页面优先使用 `employee-detail.css` 中的 `employee-detail-*` 风格，包括顶部按钮条、基础信息区、页签、表单网格、明细表格。
  - 公司组织详情类页面优先使用 `company-organization-detail.css` 中的 `company-org-detail-*` 风格。
  - 通用文档、图片、档案入口统一使用 `document-manager.css` 与 `document-manager.js`，不得在业务页重复实现一套上传/预览/删除界面。
- 卡片页面固定使用 `site.css` 中的 `erp-card-page` 风格：
  - 页面根容器使用 `erp-card-page`。
  - 页面标题使用 `erp-card-page__title`，面包屑使用 `erp-card-page__breadcrumb`。
  - 指标卡片使用 `erp-metric-card`，按语义选择 `erp-metric-card--blue`、`erp-metric-card--amber`、`erp-metric-card--green`、`erp-metric-card--red`。
  - 普通内容卡片使用 `erp-content-card`，卡片头、主体、底部分别使用 `erp-content-card__header`、`erp-content-card__body`、`erp-content-card__footer`。
  - 卡片页不得直接使用 Bootstrap 的 `card bg-primary/bg-warning/bg-success/bg-danger` 作为最终样式；如需 Bootstrap 网格，可只复用 `row`、`col-*` 等布局类。
- 新增 Area 视图目录时必须检查 `Views/_ViewStart.cshtml` 是否存在；若不存在，应补充并指向 `~/Views/Shared/_Layout.cshtml`。弹窗/详情页如需独立弹窗外壳，应显式使用 `~/Views/Shared/_PopupLayout.cshtml`。
- 新增或修改 Razor 页面时，必须确认 `@section Styles` 能通过当前 Layout 输出；页面出现原生 HTML 样式时，优先排查 `_ViewStart.cshtml`、`Layout`、`RenderSectionAsync("Styles")`。
- 项目控件边角风格保持克制：业务页面主面板和卡片使用 8px 左右圆角，详情表单输入框和表格保持方正、紧凑、可扫描；不要使用大圆角、大面积装饰渐变或与现有页面不一致的强视觉效果。
- 页面主色延续当前项目的蓝灰管理后台风格：主操作使用蓝色渐变，状态成功使用绿色，风险/删除使用红色或粉红警示；避免新增大面积紫色、棕橙色、米色或单一色系页面。
- 页面文字、按钮、表格列标题和提示文案必须为可读中文，不得出现乱码、英文占位或无意义文案。

## 构建、验证与产物清理
- 修改 C# 或 Razor 后至少执行：
  - `dotnet build OpenERP.Web/OpenERP.Web.csproj`
- 若本地正在运行 `OpenERP.Web` 导致输出 DLL 被锁定，可使用临时输出目录验证，例如：
  - `dotnet build OpenERP.Web/OpenERP.Web.csproj -p:OutDir=D:/Project/OpenERP/.tmp_build_verify/`
- 临时构建目录、截图、浏览器验证输出、报告产物不得混入源码目录；任务完成前应清理不再需要的验证产物。
- 长期保留的报告或审查材料放入 `output/`；临时验证材料放入 `.tmp_*` 或任务专用临时目录，并在最终回复说明是否已清理。

## 代码审查清单
- 权限：控制器和 API 是否默认要求登录，是否存在越权访问业务记录的路径。
- 防伪：Cookie 认证下的状态变更接口是否具备 CSRF 防护。
- 数据：查询是否过滤软删除，保存是否校验业务归属与唯一约束。
- 文件：上传是否限制大小、类型、存储位置和访问权限。
- 前端：动态 HTML 是否转义，按钮状态是否覆盖只读、加载、失败和空数据。
- 可维护性：新增共享能力是否复用，是否避免把同一业务逻辑散落到多个页面。
