# OpenERP.Web Threat Model (Trial) - 2026-03-12

## 1) Scope
- In scope: `OpenERP.Web` ASP.NET Core MVC host and area controllers.
- Evidence:
  - `OpenERP.Web/Program.cs:33` (`AddControllersWithViews`)
  - `OpenERP.Web/Program.cs:94` / `:98` (area/default routes)
  - `OpenERP.Web/OpenERP.Web.csproj:22-33` (references to multi-module projects)

## 2) System Model (Repository-Grounded)
- Runtime: ASP.NET Core `net10.0` web app (`OpenERP.Web/OpenERP.Web.csproj:4`).
- Data access: multiple module DbContexts wired in one host, all using same `DefaultConnection` (`OpenERP.Web/Program.cs:36-63`, `OpenERP.Web/appsettings.json:9`).
- Middleware pipeline: `UseHttpsRedirection` -> `UseRouting` -> `UseAuthorization` -> route mapping (`OpenERP.Web/Program.cs:87-100`).
- Not observed in pipeline: `UseAuthentication`.

## 3) Trust Boundaries & Entry Points
- Boundary A: Browser -> Web Host (HTTP/HTTPS endpoints)
  - Entry points: area controllers and default route.
- Boundary B: Web Host -> SQL Server(LocalDB in current config)
  - Entry point: EF Core DbContexts using one connection string.
- Boundary C: Browser -> third-party CDN assets
  - Evidence: runtime console error for bootstrap-icons CDN failure in `.playwright-cli/console-2026-03-12T15-48-13-843Z.log:1`.

## 4) Key Assets
- ERP business data across HR/Sales/Finance/Asset/Transport etc.
- Integrity of cross-module operations.
- Availability of web UI and static resources.

## 5) Threats (Prioritized)
1. Authentication bypass risk due to missing auth middleware chain.
   - Evidence: `UseAuthorization` exists (`OpenERP.Web/Program.cs:90`) but `UseAuthentication` not found in pipeline.
   - Likelihood: High; Impact: High; Priority: High.
2. Lateral impact blast radius due to single shared DB connection across modules.
   - Evidence: many DbContexts share `DefaultConnection` (`OpenERP.Web/Program.cs:36-63`).
   - Likelihood: Medium; Impact: High; Priority: High.
3. Host-header/domain exposure from permissive host config.
   - Evidence: `AllowedHosts = "*"` (`OpenERP.Web/appsettings.json:11`).
   - Likelihood: Medium; Impact: Medium; Priority: Medium.
4. Availability degradation from external static dependency failure.
   - Evidence: bootstrap-icons CDN load error (Playwright console log).
   - Likelihood: Medium; Impact: Medium; Priority: Medium.

## 6) Existing Mitigations Observed
- HTTPS redirection enabled (`OpenERP.Web/Program.cs:87`).
- Production exception handler and HSTS enabled for non-development (`OpenERP.Web/Program.cs:78-84`).

## 7) Recommended Mitigations
1. Add and configure authentication explicitly (`AddAuthentication` + `UseAuthentication`) before authorization in middleware order.
2. Define module-level data boundary roadmap: short-term connection segregation by module; long-term least-privilege DB access strategy.
3. Restrict `AllowedHosts` in production environment settings to explicit domain list.
4. Host critical static assets locally or via controlled mirror to reduce third-party outage impact.

## 8) Assumptions
- This trial model is repo-grounded and based on current local configuration and code.
- Deployment topology, internet exposure, and tenant model were not provided; risk ranking may change after those inputs are confirmed.
