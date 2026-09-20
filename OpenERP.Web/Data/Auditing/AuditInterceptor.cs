/*
 * File: OpenERP.Web/Data/Auditing/AuditInterceptor.cs
 * Description: EF Core 保存变更审计拦截器（自动维护实体的 CreatedAt/CreatedBy/UpdatedAt/UpdatedBy）。
 */

using System.Collections.Concurrent;
using System.Reflection;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace OpenERP.Web.Data.Auditing;

/// <summary>
/// 审计字段拦截器（各 EF 模块实体继承各自的 BaseEntity,分属十个不同类型且无公共接口,
/// 因此按具体实体类型反射解析审计属性并缓存,在保存时统一自动赋值）。
/// </summary>
public sealed class AuditInterceptor : SaveChangesInterceptor
{
    /// <summary>
    /// 实体类型到审计属性的反射缓存（不含审计字段的类型缓存为 null,避免重复反射）。
    /// </summary>
    private static readonly ConcurrentDictionary<Type, AuditProperties?> PropertyCache = new();

    /// <summary>
    /// 当前登录用户显示名（写入 CreatedBy/UpdatedBy;无登录上下文时为 null,如启动初始化场景）。
    /// </summary>
    private readonly string? _userName;

    public AuditInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _userName = httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name);
    }

    /// <summary>
    /// 异步保存前补齐审计字段。
    /// </summary>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// 同步保存前补齐审计字段。
    /// </summary>
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <summary>
    /// 按实体状态自动补齐审计字段（新增写创建信息,修改写更新信息;创建人已有值时不覆盖）。
    /// </summary>
    private void ApplyAudit(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            var properties = PropertyCache.GetOrAdd(entry.Entity.GetType(), ResolveAuditProperties);
            if (properties is null)
            {
                continue;
            }

            var now = DateTime.Now;
            if (entry.State == EntityState.Added)
            {
                properties.CreatedAt?.SetValue(entry.Entity, now);
                if (_userName is not null && string.IsNullOrEmpty(properties.CreatedBy?.GetValue(entry.Entity) as string))
                {
                    properties.CreatedBy?.SetValue(entry.Entity, _userName);
                }
            }
            else
            {
                properties.UpdatedAt?.SetValue(entry.Entity, now);
                if (_userName is not null)
                {
                    properties.UpdatedBy?.SetValue(entry.Entity, _userName);
                }
            }
        }
    }

    /// <summary>
    /// 解析实体类型上的审计属性（任一缺失时按可用部分生效,全部缺失返回 null）。
    /// </summary>
    private static AuditProperties? ResolveAuditProperties(Type entityType)
    {
        PropertyInfo? Get(string name)
            => entityType.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);

        var properties = new AuditProperties(
            Get("CreatedAt"),
            Get("CreatedBy"),
            Get("UpdatedAt"),
            Get("UpdatedBy"));

        return properties.CreatedAt is null && properties.CreatedBy is null
            && properties.UpdatedAt is null && properties.UpdatedBy is null
            ? null
            : properties;
    }

    /// <summary>
    /// 实体审计属性集合（反射解析结果）。
    /// </summary>
    private sealed record AuditProperties(
        PropertyInfo? CreatedAt,
        PropertyInfo? CreatedBy,
        PropertyInfo? UpdatedAt,
        PropertyInfo? UpdatedBy);
}
