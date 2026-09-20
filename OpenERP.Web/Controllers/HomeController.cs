/*
 * File: OpenERP.Web/Controllers/HomeController.cs
 * Description: Controller that handles HTTP actions for HomeController.
 */

using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using OpenERP.Web.Models;

namespace OpenERP.Web.Controllers;

/// <summary>
/// 首页控制器（负责首页、隐私页和语言切换功能）。
/// </summary>
[Authorize]
public class HomeController : Controller
{
    /// <summary>
    /// 首页仪表板页面。
    /// </summary>
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// 隐私政策页面。
    /// </summary>
    public IActionResult Privacy()
    {
        return View();
    }

    /// <summary>
    /// 切换界面语言并写入用户语言 Cookie。
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public IActionResult SetLanguage(string culture, string? returnUrl = null)
    {
        var supportedCultures = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "zh-Hans",
            "zh-Hant",
            "en"
        };

        var safeCulture = supportedCultures.Contains(culture) ? culture : "zh-Hans";
        var cookieValue = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(safeCulture));

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            cookieValue,
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true
            });

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// 错误页。
    /// </summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
