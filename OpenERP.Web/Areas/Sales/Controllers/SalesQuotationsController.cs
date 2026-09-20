using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenERP.Sales.Data;
using OpenERP.Sales.Models.Entities;
using OpenERP.Web.Areas.Sales.ViewModels.SalesQuotations;

namespace OpenERP.Web.Areas.Sales.Controllers;

[Authorize]
[Area("Sales")]
public class SalesQuotationsController : Controller
{
    // 销售模块数据库上下文（用于维护销售报价主表与货品明细）。
    private readonly ApplicationDbContext _context;

    public SalesQuotationsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string scope = "全部", string? keyword = null, string statusFilter = "全部", int? selectedQuotationId = null, string activeTab = "items")
    {
        IQueryable<SalesQuotation> query = _context.SalesQuotations.Where(q => !q.IsDeleted).AsQueryable();
        if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "全部")
        {
            query = query.Where(q => q.Status == statusFilter);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            string kw = keyword.Trim();
            query = scope switch
            {
                "报价单号" => query.Where(q => q.QuotationNumber.Contains(kw)),
                "客户编号" => query.Where(q => q.CustomerCode != null && q.CustomerCode.Contains(kw)),
                "客户名称" => query.Where(q => q.CustomerName.Contains(kw)),
                "联系人" => query.Where(q => q.BillingContact != null && q.BillingContact.Contains(kw)),
                "电话" => query.Where(q => q.Phone != null && q.Phone.Contains(kw)),
                "电邮" => query.Where(q => q.Email != null && q.Email.Contains(kw)),
                "备注" => query.Where(q => q.Remarks != null && q.Remarks.Contains(kw)),
                _ => query.Where(q =>
                    q.QuotationNumber.Contains(kw)
                    || (q.CustomerCode != null && q.CustomerCode.Contains(kw))
                    || q.CustomerName.Contains(kw)
                    || (q.BillingContact != null && q.BillingContact.Contains(kw))
                    || (q.Phone != null && q.Phone.Contains(kw))
                    || (q.Remarks != null && q.Remarks.Contains(kw))),
            };
        }

        List<SalesQuotation> quotations = await query
            .OrderByDescending(q => q.QuotationDate)
            .ThenByDescending(q => q.Id)
            .ToListAsync();

        if (!selectedQuotationId.HasValue && quotations.Count > 0)
        {
            selectedQuotationId = quotations[0].Id;
        }

        List<SalesQuotationItem> items = [];
        string selectedQuotationNumber = "未选择报价单";
        if (selectedQuotationId.HasValue)
        {
            items = await _context.SalesQuotationItems
                .Where(i => i.SalesQuotationId == selectedQuotationId.Value && !i.IsDeleted)
                .OrderBy(i => i.Id)
                .ToListAsync();

            SalesQuotation? selected = quotations.FirstOrDefault(q => q.Id == selectedQuotationId.Value);
            if (selected is not null)
            {
                selectedQuotationNumber = selected.QuotationNumber;
            }
        }

        SalesQuotationIndexViewModel viewModel = new()
        {
            Scope = scope,
            Keyword = keyword,
            StatusFilter = statusFilter,
            SelectedQuotationId = selectedQuotationId,
            SelectedQuotationNumber = selectedQuotationNumber,
            ActiveTab = activeTab,
            Quotations = quotations.Select((q, index) => new SalesQuotationListItemViewModel
            {
                Id = q.Id,
                SequenceNo = index + 1,
                QuotationNumber = q.QuotationNumber,
                QuotationDate = q.QuotationDate.ToString("yyyy/MM/dd"),
                CustomerCode = q.CustomerCode ?? string.Empty,
                CustomerName = q.CustomerName,
                Status = q.Status,
                ValidUntil = q.ValidUntil.HasValue ? q.ValidUntil.Value.ToString("yyyy/MM/dd") : string.Empty,
                BillingContact = q.BillingContact ?? string.Empty,
                Phone = q.Phone ?? string.Empty,
                Email = q.Email ?? string.Empty,
                Remarks = q.Remarks ?? string.Empty,
                LastModifiedBy = q.UpdatedBy ?? q.CreatedBy ?? string.Empty
            }).ToList(),
            Items = items.Select((item, index) => new SalesQuotationItemViewModel
            {
                Id = item.Id,
                SequenceNo = index + 1,
                ProductCode = item.ProductCode,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                Unit = item.Unit,
                UnitPrice = item.UnitPrice,
                DiscountPercent = item.DiscountPercent,
                Amount = item.Amount,
                Currency = item.Currency,
                ExchangeRate = item.ExchangeRate,
                Notes = item.Notes ?? string.Empty
            }).ToList()
        };
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int[] ids)
    {
        if (ids == null || ids.Length == 0)
        {
            return RedirectToAction("Index");
        }

        foreach (SalesQuotation quotation in await _context.SalesQuotations.Where(q => ids.Contains(q.Id) && !q.IsDeleted).ToListAsync())
        {
            quotation.IsDeleted = true;
            quotation.UpdatedAt = DateTime.Now;
            quotation.UpdatedBy = GetCurrentUserName();
        }

        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }

    public IActionResult Create(bool popup = false)
    {
        ViewBag.IsPopup = popup;
        return View("Edit", new SalesQuotationEditViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SalesQuotationEditViewModel model, bool popup = false)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.IsPopup = popup;
            return View("Edit", NormalizeItemRows(model));
        }

        SalesQuotation quotation = new()
        {
            CreatedBy = GetCurrentUserName(),
            CreatedAt = DateTime.Now
        };
        ApplyQuotationValues(quotation, model);
        ApplyItemRows(quotation, model, []);
        _context.SalesQuotations.Add(quotation);
        await _context.SaveChangesAsync();

        if (!popup)
        {
            return RedirectToAction("Edit", new { id = quotation.Id });
        }

        return BuildDetailPageCloseResult();
    }

    public async Task<IActionResult> Edit(int id, bool popup = false)
    {
        SalesQuotation? quotation = await _context.SalesQuotations
            .Include(q => q.Items)
            .FirstOrDefaultAsync(q => q.Id == id && !q.IsDeleted);
        if (quotation is null)
        {
            return NotFound();
        }

        ViewBag.IsPopup = popup;
        return View("Edit", ToEditModel(quotation));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SalesQuotationEditViewModel model, bool popup = false)
    {
        if (model.Id != id)
        {
            return NotFound();
        }

        SalesQuotation? quotation = await _context.SalesQuotations
            .Include(q => q.Items)
            .FirstOrDefaultAsync(q => q.Id == id && !q.IsDeleted);
        if (quotation is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.IsPopup = popup;
            return View("Edit", NormalizeItemRows(model));
        }

        ApplyQuotationValues(quotation, model);
        quotation.UpdatedBy = GetCurrentUserName();
        quotation.UpdatedAt = DateTime.Now;
        ApplyItemRows(quotation, model, quotation.Items.ToList());
        await _context.SaveChangesAsync();

        if (!popup)
        {
            return RedirectToAction("Edit", new { id = quotation.Id });
        }

        return BuildDetailPageCloseResult();
    }

    public async Task<IActionResult> Details(int id, bool popup = false)
    {
        SalesQuotation? quotation = await _context.SalesQuotations
            .Include(q => q.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == id && !q.IsDeleted);
        if (quotation is null)
        {
            return NotFound();
        }

        ViewBag.IsPopup = popup;
        ViewBag.IsReadOnly = true;
        return View("Edit", ToEditModel(quotation));
    }

    public async Task<IActionResult> Copy(int id, bool popup = false)
    {
        SalesQuotation? quotation = await _context.SalesQuotations
            .Include(q => q.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == id && !q.IsDeleted);
        if (quotation is null)
        {
            return NotFound();
        }

        SalesQuotationEditViewModel model = ToEditModel(quotation);
        model.Id = null;
        model.QuotationNumber += "_COPY";
        foreach (SalesQuotationItemInputModel item in model.Items)
        {
            item.Id = null;
        }

        ViewBag.IsPopup = popup;
        return View("Edit", model);
    }

    private static SalesQuotationEditViewModel ToEditModel(SalesQuotation quotation)
    {
        return new SalesQuotationEditViewModel
        {
            Id = quotation.Id,
            QuotationNumber = quotation.QuotationNumber,
            QuotationDate = quotation.QuotationDate,
            CustomerCode = quotation.CustomerCode,
            CustomerName = quotation.CustomerName,
            Status = quotation.Status,
            ValidUntil = quotation.ValidUntil,
            BillingContact = quotation.BillingContact,
            Phone = quotation.Phone,
            Email = quotation.Email,
            Remarks = quotation.Remarks,
            Items = quotation.Items
                .Where(i => !i.IsDeleted)
                .OrderBy(i => i.Id)
                .Select(i => new SalesQuotationItemInputModel
                {
                    Id = i.Id,
                    ProductCode = i.ProductCode,
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    Unit = i.Unit,
                    UnitPrice = i.UnitPrice,
                    DiscountPercent = i.DiscountPercent,
                    Amount = i.Amount,
                    Currency = i.Currency,
                    ExchangeRate = i.ExchangeRate,
                    Notes = i.Notes
                })
                .ToList()
        };
    }

    private static void ApplyQuotationValues(SalesQuotation quotation, SalesQuotationEditViewModel model)
    {
        quotation.QuotationNumber = model.QuotationNumber;
        quotation.QuotationDate = model.QuotationDate;
        quotation.CustomerCode = model.CustomerCode;
        quotation.CustomerName = model.CustomerName;
        quotation.Status = model.Status;
        quotation.ValidUntil = model.ValidUntil;
        quotation.BillingContact = model.BillingContact;
        quotation.Phone = model.Phone;
        quotation.Email = model.Email;
        quotation.Remarks = model.Remarks;
    }

    private void ApplyItemRows(SalesQuotation quotation, SalesQuotationEditViewModel model, List<SalesQuotationItem> existingItems)
    {
        Dictionary<int, SalesQuotationItem> existingItemMap = existingItems
            .Where(i => i.Id > 0)
            .ToDictionary(i => i.Id);

        foreach (SalesQuotationItemInputModel inputRow in model.Items)
        {
            if (inputRow.Id.HasValue && existingItemMap.TryGetValue(inputRow.Id.Value, out SalesQuotationItem? existingItem))
            {
                if (inputRow.IsDeleted)
                {
                    existingItem.IsDeleted = true;
                    existingItem.UpdatedBy = GetCurrentUserName();
                    existingItem.UpdatedAt = DateTime.Now;
                }
                else
                {
                    ApplyItemValues(existingItem, inputRow);
                    existingItem.UpdatedBy = GetCurrentUserName();
                    existingItem.UpdatedAt = DateTime.Now;
                }
            }
            else if (!inputRow.IsDeleted && !string.IsNullOrWhiteSpace(inputRow.ProductCode))
            {
                SalesQuotationItem item = new()
                {
                    CreatedBy = GetCurrentUserName(),
                    CreatedAt = DateTime.Now
                };
                ApplyItemValues(item, inputRow);
                quotation.Items.Add(item);
            }
        }
    }

    private static void ApplyItemValues(SalesQuotationItem item, SalesQuotationItemInputModel row)
    {
        item.ProductCode = row.ProductCode;
        item.ProductName = row.ProductName;
        item.Quantity = row.Quantity;
        item.Unit = row.Unit;
        item.UnitPrice = row.UnitPrice;
        item.DiscountPercent = row.DiscountPercent;
        item.Amount = row.Amount;
        item.Currency = row.Currency;
        item.ExchangeRate = row.ExchangeRate;
        item.Notes = row.Notes;
    }

    private static SalesQuotationEditViewModel NormalizeItemRows(SalesQuotationEditViewModel model)
    {
        model.Items = model.Items.Where(i => !i.IsDeleted).ToList();
        return model;
    }

    private string GetCurrentUserName()
    {
        return User.Identity?.Name ?? "系统";
    }

    private ContentResult BuildDetailPageCloseResult()
    {
        const string content = """
            <!DOCTYPE html>
            <html lang="zh-CN">
            <head>
                <meta charset="utf-8" />
                <title>保存成功</title>
            </head>
            <body>
                <script>
                    if (window.opener) {
                        window.opener.postMessage({ type: 'erp-detail-saved' }, '*');
                    }
                    window.close();
                </script>
                <p style="font-family:sans-serif;text-align:center;padding-top:3rem;">保存成功，正在关闭。</p>
            </body>
            </html>
            """;
        return Content(content, "text/html");
    }
}
