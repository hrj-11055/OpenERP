/*
 * File: OpenERP.Web/Security/LoginThrottler.cs
 * Description: 登录防暴力破解服务（按账号统计连续失败次数并临时锁定账号）。
 */

using Microsoft.Extensions.Caching.Memory;

namespace OpenERP.Web.Security;

/// <summary>
/// 登录节流器（基于内存缓存记录登录失败次数，连续失败达到阈值后临时锁定账号）。
/// </summary>
public sealed class LoginThrottler
{
    /// <summary>
    /// 触发锁定的连续失败次数上限。
    /// </summary>
    private const int MaxFailureCount = 5;

    /// <summary>
    /// 锁定时长（同时作为失败计数器的滑动过期时间）。
    /// </summary>
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    /// <summary>
    /// 失败计数缓存键前缀。
    /// </summary>
    private const string FailureKeyPrefix = "login-failure:";

    /// <summary>
    /// 锁定标记缓存键前缀。
    /// </summary>
    private const string LockoutKeyPrefix = "login-lockout:";

    /// <summary>
    /// 内存缓存（进程内共享，注册为单例使用）。
    /// </summary>
    private readonly IMemoryCache _cache;

    public LoginThrottler(IMemoryCache cache)
    {
        _cache = cache;
    }

    /// <summary>
    /// 判断账号当前是否处于锁定状态（锁定期间直接拒绝登录尝试）。
    /// </summary>
    public bool IsLockedOut(string account)
    {
        return _cache.TryGetValue(BuildKey(LockoutKeyPrefix, account), out _);
    }

    /// <summary>
    /// 记录一次登录失败（达到阈值后写入锁定标记并清除计数器）。
    /// </summary>
    public void RecordFailure(string account)
    {
        var key = BuildKey(FailureKeyPrefix, account);
        var failureCount = _cache.TryGetValue(key, out int current) ? current + 1 : 1;
        _cache.Set(key, failureCount, LockoutDuration);

        if (failureCount >= MaxFailureCount)
        {
            _cache.Set(BuildKey(LockoutKeyPrefix, account), true, LockoutDuration);
            _cache.Remove(key);
        }
    }

    /// <summary>
    /// 登录成功后清空失败计数（不影响已存在的锁定标记，正常不会同时出现）。
    /// </summary>
    public void ResetOnSuccess(string account)
    {
        _cache.Remove(BuildKey(FailureKeyPrefix, account));
    }

    /// <summary>
    /// 生成规范化缓存键（账号去除首尾空格并统一大写，避免大小写绕过）。
    /// </summary>
    private static string BuildKey(string prefix, string account)
    {
        var normalizedAccount = (account ?? string.Empty).Trim().ToUpperInvariant();
        return prefix + normalizedAccount;
    }
}
