using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenERP.CRM.Data;
using OpenERP.CRM.Models.Entities;
using OpenERP.Web.Areas.CRM.ViewModels.Customers;

namespace OpenERP.Web.Areas.CRM.Controllers;

[Authorize]
[Area("CRM")]
public class CustomersController : Controller
{
	private readonly ApplicationDbContext _context;

	public CustomersController(ApplicationDbContext context)
	{
		_context = context;
	}

	public async Task<IActionResult> Index(string? scope, string? keyword, int? selectedCustomerId)
	{
		string queryScope = (string.IsNullOrWhiteSpace(scope) ? "全部" : scope.Trim());
		string queryKeyword = keyword?.Trim();
		IQueryable<Customer> customerQuery = ApplyCustomerSearch(from customer in _context.Customers.AsNoTracking().Include((Customer customer) => customer.Contacts.Where((CustomerContact contact) => !contact.IsDeleted))
			where !customer.IsDeleted && customer.IsCustomerRole
			select customer, queryScope, queryKeyword);
		List<Customer> customers = await customerQuery.OrderBy((Customer customer) => customer.CustomerCode).ToListAsync();
		Customer selectedCustomer = (selectedCustomerId.HasValue ? customers.FirstOrDefault((Customer customer) => customer.Id == selectedCustomerId.Value) : customers.FirstOrDefault());
		CustomerIndexViewModel model = new CustomerIndexViewModel
		{
			Scope = queryScope,
			Keyword = queryKeyword,
			SelectedCustomerId = selectedCustomer?.Id,
			SelectedCustomerName = ((selectedCustomer == null) ? "未选择客户" : (selectedCustomer.CustomerCode + " " + selectedCustomer.CustomerName)),
			Customers = customers.Select((Customer customer, int index) => ToCustomerListItem(customer, index + 1)).ToList(),
			Contacts = ((from contact in selectedCustomer?.Contacts
				where !contact.IsDeleted
				orderby contact.SortOrder, contact.Id
				select contact).Select((CustomerContact contact, int index) => ToContactListItem(contact, index + 1)).ToList() ?? new List<CustomerContactListItemViewModel>())
		};
		return View(model);
	}

	public IActionResult Create(bool popup = false)
	{
		base.ViewBag.IsPopup = popup;
		return View("Edit", new CustomerEditViewModel());
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Create(CustomerEditViewModel model, bool popup = false)
	{
		await ValidateCustomerAsync(model, null);
		if (!base.ModelState.IsValid)
		{
			base.ViewBag.IsPopup = popup;
			return View("Edit", NormalizeContactRows(model));
		}
		Customer customer = new Customer
		{
			CreatedBy = GetCurrentUserName(),
			CreatedAt = DateTime.Now
		};
		ApplyCustomerValues(customer, model);
		ApplyContactRows(customer, model, new List<CustomerContact>());
		_context.Customers.Add(customer);
		await _context.SaveChangesAsync();
		IActionResult result;
		if (!popup)
		{
			IActionResult actionResult = RedirectToAction("Edit", new
			{
				id = customer.Id
			});
			result = actionResult;
		}
		else
		{
			IActionResult actionResult = BuildDetailPageCloseResult();
			result = actionResult;
		}
		return result;
	}

	public async Task<IActionResult> Edit(int id, bool popup = false)
	{
		Customer customer = await _context.Customers.Include((Customer item) => item.Contacts).FirstOrDefaultAsync((Customer item) => item.Id == id && !item.IsDeleted);
		if (customer == null)
		{
			return NotFound();
		}
		base.ViewBag.IsPopup = popup;
		return View(ToEditModel(customer));
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Edit(int id, CustomerEditViewModel model, bool popup = false)
	{
		if (model.Id != id)
		{
			return NotFound();
		}
		Customer customer = await _context.Customers.Include((Customer item) => item.Contacts).FirstOrDefaultAsync((Customer item) => item.Id == id && !item.IsDeleted);
		if (customer == null)
		{
			return NotFound();
		}
		await ValidateCustomerAsync(model, id);
		if (!base.ModelState.IsValid)
		{
			base.ViewBag.IsPopup = popup;
			return View(NormalizeContactRows(model));
		}
		ApplyCustomerValues(customer, model);
		customer.UpdatedBy = GetCurrentUserName();
		customer.UpdatedAt = DateTime.Now;
		ApplyContactRows(customer, model, customer.Contacts.ToList());
		await _context.SaveChangesAsync();
		IActionResult result;
		if (!popup)
		{
			IActionResult actionResult = RedirectToAction("Edit", new
			{
				id = customer.Id
			});
			result = actionResult;
		}
		else
		{
			IActionResult actionResult = BuildDetailPageCloseResult();
			result = actionResult;
		}
		return result;
	}

	public async Task<IActionResult> Details(int id, bool popup = false)
	{
		Customer customer = await _context.Customers.Include((Customer item) => item.Contacts).AsNoTracking().FirstOrDefaultAsync((Customer item) => item.Id == id && !item.IsDeleted);
		if (customer == null)
		{
			return NotFound();
		}
		base.ViewBag.IsPopup = popup;
		base.ViewBag.IsReadOnly = true;
		return View("Edit", ToEditModel(customer));
	}

	public async Task<IActionResult> Copy(int id, bool popup = false)
	{
		Customer customer = await _context.Customers.Include((Customer item) => item.Contacts).AsNoTracking().FirstOrDefaultAsync((Customer item) => item.Id == id && !item.IsDeleted);
		if (customer == null)
		{
			return NotFound();
		}
		CustomerEditViewModel model = ToEditModel(customer);
		model.Id = null;
		model.CustomerCode += "_COPY";
		model.CustomerName += " 副本";
		foreach (CustomerContactInputModel contact in model.Contacts)
		{
			contact.Id = null;
		}
		base.ViewBag.IsPopup = popup;
		return View("Edit", model);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Delete(int[] ids)
	{
		int[] customerIds = ids.Where((int id) => id > 0).Distinct().ToArray();
		if (customerIds.Length == 0)
		{
			return BadRequest("请先选择要删除的客户资料。");
		}
		List<Customer> customers = await (from item in _context.Customers.Include((Customer item) => item.Contacts)
			where customerIds.Contains(item.Id) && !item.IsDeleted
			select item).ToListAsync();
		if (customers.Count == 0)
		{
			return NotFound();
		}
		string currentUserName = GetCurrentUserName();
		DateTime deletedAt = DateTime.Now;
		foreach (Customer customer in customers)
		{
			customer.IsDeleted = true;
			customer.UpdatedBy = currentUserName;
			customer.UpdatedAt = deletedAt;
			foreach (CustomerContact contact in customer.Contacts)
			{
				contact.IsDeleted = true;
				contact.UpdatedBy = currentUserName;
				contact.UpdatedAt = deletedAt;
			}
		}
		await _context.SaveChangesAsync();
		return RedirectToAction("Index");
	}

	public async Task<IActionResult> Export(string? scope, string? keyword)
	{
		List<Customer> customers = await (from customer2 in ApplyCustomerSearch(scope: string.IsNullOrWhiteSpace(scope) ? "全部" : scope.Trim(), keyword: keyword?.Trim(), query: from customer2 in _context.Customers.AsNoTracking().Include((Customer customer2) => customer2.Contacts.Where((CustomerContact contact) => !contact.IsDeleted))
				where !customer2.IsDeleted && customer2.IsCustomerRole
				select customer2)
			orderby customer2.CustomerCode
			select customer2).ToListAsync();
		StringBuilder csvBuilder = new StringBuilder();
		csvBuilder.AppendLine("客户编号,客户名称,状态,别名或简称,性质,企业类型,地区,联系人,电话,传真,电邮,备注,最后修改人,最后修改时间");
		foreach (Customer customer in customers)
		{
			CustomerContact defaultContact = GetDefaultContact(customer);
			string[] row = new string[14]
			{
				customer.CustomerCode,
				customer.CustomerName,
				customer.Status,
				customer.AliasName,
				customer.CustomerNature,
				customer.EnterpriseType,
				BuildRegion(customer),
				defaultContact?.Name,
				customer.Phone ?? defaultContact?.Phone ?? defaultContact?.Mobile,
				customer.Fax ?? defaultContact?.Fax,
				customer.Email ?? defaultContact?.Email,
				customer.Remarks,
				customer.UpdatedBy ?? customer.CreatedBy,
				(customer.UpdatedAt ?? customer.CreatedAt).ToString("yyyy-MM-dd HH:mm")
			};
			csvBuilder.AppendLine(string.Join(",", row.Select(EscapeCsv)));
		}
		byte[] bytes = Encoding.UTF8.GetBytes("\ufeff" + csvBuilder);
		return File(bytes, "text/csv; charset=utf-8", $"客户资料_{DateTime.Now:yyyyMMddHHmmss}.csv");
	}

	private static IQueryable<Customer> ApplyCustomerSearch(IQueryable<Customer> query, string scope, string? keyword)
	{
		if (string.IsNullOrWhiteSpace(keyword))
		{
			return query;
		}
		if (1 == 0)
		{
		}
		IQueryable<Customer> result = scope switch
		{
			"客户编号" => query.Where((Customer customer) => customer.CustomerCode.Contains(keyword)), 
			"客户名称" => query.Where((Customer customer) => customer.CustomerName.Contains(keyword)), 
			"联系人" => query.Where((Customer customer) => customer.Contacts.Any((CustomerContact contact) => !contact.IsDeleted && contact.Name.Contains(keyword))), 
			"电话" => query.Where((Customer customer) => customer.Phone != null && customer.Phone.Contains(keyword)), 
			"电邮" => query.Where((Customer customer) => customer.Email != null && customer.Email.Contains(keyword)), 
			"备注" => query.Where((Customer customer) => customer.Remarks != null && customer.Remarks.Contains(keyword)), 
			_ => query.Where((Customer customer) => customer.CustomerCode.Contains(keyword) || customer.CustomerName.Contains(keyword) || customer.Status.Contains(keyword) || customer.CustomerNature.Contains(keyword) || (customer.EnterpriseType != null && customer.EnterpriseType.Contains(keyword)) || (customer.AliasName != null && customer.AliasName.Contains(keyword)) || (customer.CountryRegion != null && customer.CountryRegion.Contains(keyword)) || (customer.City != null && customer.City.Contains(keyword)) || (customer.District != null && customer.District.Contains(keyword)) || (customer.Phone != null && customer.Phone.Contains(keyword)) || (customer.Fax != null && customer.Fax.Contains(keyword)) || (customer.Email != null && customer.Email.Contains(keyword)) || (customer.Remarks != null && customer.Remarks.Contains(keyword)) || customer.Contacts.Any((CustomerContact contact) => !contact.IsDeleted && contact.Name.Contains(keyword))), 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private async Task ValidateCustomerAsync(CustomerEditViewModel model, int? currentCustomerId)
	{
		if (!string.IsNullOrWhiteSpace(model.CustomerCode) && await _context.Customers.AnyAsync((Customer customer) => !customer.IsDeleted && customer.CustomerCode == model.CustomerCode.Trim() && (!((int?)currentCustomerId).HasValue || customer.Id != ((int?)currentCustomerId).Value)))
		{
			base.ModelState.AddModelError("CustomerCode", "客户编号已存在。");
		}
		List<CustomerContactInputModel> activeContacts = model.Contacts.Where((CustomerContactInputModel customerContactInputModel) => !customerContactInputModel.IsDeleted).ToList();
		foreach (CustomerContactInputModel contact in activeContacts.Where(IsContactRowFilled))
		{
			if (string.IsNullOrWhiteSpace(contact.Name))
			{
				base.ModelState.AddModelError("Contacts", "已填写内容的联络人行必须输入姓名。");
				break;
			}
		}
	}

	private static void ApplyCustomerValues(Customer customer, CustomerEditViewModel model)
	{
		customer.CustomerCode = model.CustomerCode.Trim();
		customer.CustomerName = model.CustomerName.Trim();
		customer.Status = model.Status.Trim();
		customer.CustomerNature = model.CustomerNature.Trim();
		customer.IsCustomerRole = true;
		customer.IsSupplierRole = model.IsSupplierRole;
		customer.EnterpriseType = NormalizeText(model.EnterpriseType);
		customer.AliasName = NormalizeText(model.AliasName);
		customer.BusinessRegistrationNumber = NormalizeText(model.BusinessRegistrationNumber);
		customer.BusinessRegistrationDate = model.BusinessRegistrationDate;
		customer.GroupCode = NormalizeText(model.GroupCode);
		customer.GroupName = NormalizeText(model.GroupName);
		customer.CountryRegion = NormalizeText(model.CountryRegion);
		customer.City = NormalizeText(model.City);
		customer.District = NormalizeText(model.District);
		customer.Address = NormalizeText(model.Address);
		customer.Manager = NormalizeText(model.Manager);
		customer.Phone = NormalizeText(model.Phone);
		customer.Fax = NormalizeText(model.Fax);
		customer.Email = NormalizeText(model.Email);
		customer.Website = NormalizeText(model.Website);
		customer.Remarks = NormalizeText(model.Remarks);
		customer.ArchivePath = NormalizeText(model.ArchivePath);
		customer.PayerCode = NormalizeText(model.PayerCode);
		customer.PayerName = NormalizeText(model.PayerName);
		customer.BankAccount = NormalizeText(model.BankAccount);
		customer.BankName = NormalizeText(model.BankName);
		customer.TransferCode = NormalizeText(model.TransferCode);
		customer.Currency = NormalizeText(model.Currency);
		customer.CreditLimit = model.CreditLimit;
		customer.CreditTerm = NormalizeText(model.CreditTerm);
		customer.DefaultPriceCategory = NormalizeText(model.DefaultPriceCategory);
		customer.PaymentMethod = NormalizeText(model.PaymentMethod);
		customer.MinimumOrderAmount = model.MinimumOrderAmount;
		customer.DefaultTaxRate = NormalizeText(model.DefaultTaxRate);
		customer.IsAccountFrozen = model.IsAccountFrozen;
		customer.AccountRemarks = NormalizeText(model.AccountRemarks);
	}

	private void ApplyContactRows(Customer customer, CustomerEditViewModel model, List<CustomerContact> existingContacts)
	{
		Dictionary<int, CustomerContact> dictionary = existingContacts.ToDictionary((CustomerContact contact) => contact.Id);
		List<CustomerContactInputModel> list = model.Contacts.Where((CustomerContactInputModel contact) => contact.Id.HasValue || contact.IsDeleted || IsContactRowFilled(contact)).ToList();
		EnsureSingleDefaultContact(list);
		int num = 1;
		foreach (CustomerContactInputModel item in list)
		{
			if (item.Id.HasValue && dictionary.TryGetValue(item.Id.Value, out var value))
			{
				if (item.IsDeleted)
				{
					value.IsDeleted = true;
					value.UpdatedBy = GetCurrentUserName();
					value.UpdatedAt = DateTime.Now;
				}
				else if (IsContactRowFilled(item))
				{
					ApplyContactValues(value, item, num++);
					value.UpdatedBy = GetCurrentUserName();
					value.UpdatedAt = DateTime.Now;
				}
			}
			else if (!item.IsDeleted && !string.IsNullOrWhiteSpace(item.Name))
			{
				CustomerContact customerContact = new CustomerContact
				{
					CreatedBy = GetCurrentUserName(),
					CreatedAt = DateTime.Now
				};
				ApplyContactValues(customerContact, item, num++);
				customer.Contacts.Add(customerContact);
			}
		}
	}

	private static void ApplyContactValues(CustomerContact contact, CustomerContactInputModel row, int sortOrder)
	{
		contact.Name = row.Name?.Trim() ?? string.Empty;
		contact.ContactType = NormalizeText(row.ContactType);
		contact.Salutation = NormalizeText(row.Salutation);
		contact.Position = NormalizeText(row.Position);
		contact.Mobile = NormalizeText(row.Mobile);
		contact.Phone = NormalizeText(row.Phone);
		contact.Fax = NormalizeText(row.Fax);
		contact.Email = NormalizeText(row.Email);
		contact.BusinessCardNote = NormalizeText(row.BusinessCardNote);
		contact.Status = NormalizeText(row.Status) ?? "在职";
		contact.Remarks = NormalizeText(row.Remarks);
		contact.IsDefault = row.IsDefault;
		contact.SortOrder = sortOrder;
		contact.IsDeleted = false;
	}

	private static CustomerEditViewModel ToEditModel(Customer customer)
	{
		CustomerEditViewModel customerEditViewModel = new CustomerEditViewModel();
		customerEditViewModel.Id = customer.Id;
		customerEditViewModel.CustomerCode = customer.CustomerCode;
		customerEditViewModel.CustomerName = customer.CustomerName;
		customerEditViewModel.Status = customer.Status;
		customerEditViewModel.CustomerNature = customer.CustomerNature;
		customerEditViewModel.IsCustomerRole = customer.IsCustomerRole;
		customerEditViewModel.IsSupplierRole = customer.IsSupplierRole;
		customerEditViewModel.EnterpriseType = customer.EnterpriseType;
		customerEditViewModel.AliasName = customer.AliasName;
		customerEditViewModel.BusinessRegistrationNumber = customer.BusinessRegistrationNumber;
		customerEditViewModel.BusinessRegistrationDate = customer.BusinessRegistrationDate;
		customerEditViewModel.GroupCode = customer.GroupCode;
		customerEditViewModel.GroupName = customer.GroupName;
		customerEditViewModel.CountryRegion = customer.CountryRegion;
		customerEditViewModel.City = customer.City;
		customerEditViewModel.District = customer.District;
		customerEditViewModel.Address = customer.Address;
		customerEditViewModel.Manager = customer.Manager;
		customerEditViewModel.Phone = customer.Phone;
		customerEditViewModel.Fax = customer.Fax;
		customerEditViewModel.Email = customer.Email;
		customerEditViewModel.Website = customer.Website;
		customerEditViewModel.Remarks = customer.Remarks;
		customerEditViewModel.ArchivePath = customer.ArchivePath;
		customerEditViewModel.PayerCode = customer.PayerCode;
		customerEditViewModel.PayerName = customer.PayerName;
		customerEditViewModel.BankAccount = customer.BankAccount;
		customerEditViewModel.BankName = customer.BankName;
		customerEditViewModel.TransferCode = customer.TransferCode;
		customerEditViewModel.Currency = customer.Currency;
		customerEditViewModel.CreditLimit = customer.CreditLimit;
		customerEditViewModel.CreditTerm = customer.CreditTerm;
		customerEditViewModel.DefaultPriceCategory = customer.DefaultPriceCategory;
		customerEditViewModel.PaymentMethod = customer.PaymentMethod;
		customerEditViewModel.MinimumOrderAmount = customer.MinimumOrderAmount;
		customerEditViewModel.DefaultTaxRate = customer.DefaultTaxRate;
		customerEditViewModel.IsAccountFrozen = customer.IsAccountFrozen;
		customerEditViewModel.AccountRemarks = customer.AccountRemarks;
		customerEditViewModel.Contacts = (from contact in customer.Contacts
			where !contact.IsDeleted
			orderby contact.SortOrder, contact.Id
			select contact).Select(ToContactInputModel).ToList();
		return customerEditViewModel;
	}

	private static CustomerContactInputModel ToContactInputModel(CustomerContact contact)
	{
		return new CustomerContactInputModel
		{
			Id = contact.Id,
			Name = contact.Name,
			ContactType = contact.ContactType,
			Salutation = contact.Salutation,
			Position = contact.Position,
			Mobile = contact.Mobile,
			Phone = contact.Phone,
			Fax = contact.Fax,
			Email = contact.Email,
			BusinessCardNote = contact.BusinessCardNote,
			Status = contact.Status,
			Remarks = contact.Remarks,
			IsDefault = contact.IsDefault
		};
	}

	private static CustomerListItemViewModel ToCustomerListItem(Customer customer, int sequenceNo)
	{
		CustomerContact defaultContact = GetDefaultContact(customer);
		return new CustomerListItemViewModel
		{
			Id = customer.Id,
			SequenceNo = sequenceNo,
			CustomerCode = customer.CustomerCode,
			CustomerName = customer.CustomerName,
			Status = customer.Status,
			AliasName = (customer.AliasName ?? string.Empty),
			CustomerNature = customer.CustomerNature,
			EnterpriseType = (customer.EnterpriseType ?? string.Empty),
			Region = BuildRegion(customer),
			ContactName = (defaultContact?.Name ?? customer.Manager ?? string.Empty),
			Phone = (customer.Phone ?? defaultContact?.Phone ?? defaultContact?.Mobile ?? string.Empty),
			Fax = (customer.Fax ?? defaultContact?.Fax ?? string.Empty),
			Email = (customer.Email ?? defaultContact?.Email ?? string.Empty),
			Remarks = (customer.Remarks ?? string.Empty),
			LastModifiedBy = (customer.UpdatedBy ?? customer.CreatedBy ?? string.Empty),
			LastModifiedAt = (customer.UpdatedAt ?? customer.CreatedAt).ToString("yyyy-MM-dd HH:mm")
		};
	}

	private static CustomerContactListItemViewModel ToContactListItem(CustomerContact contact, int sequenceNo)
	{
		return new CustomerContactListItemViewModel
		{
			Id = contact.Id,
			SequenceNo = sequenceNo,
			Name = contact.Name,
			BusinessCardText = (string.IsNullOrWhiteSpace(contact.BusinessCardNote) ? "查看名片" : contact.BusinessCardNote),
			Salutation = (contact.Salutation ?? string.Empty),
			Status = (contact.Status ?? string.Empty),
			ContactType = (contact.ContactType ?? string.Empty),
			Position = (contact.Position ?? string.Empty),
			Mobile = (contact.Mobile ?? string.Empty),
			Phone = (contact.Phone ?? string.Empty),
			Fax = (contact.Fax ?? string.Empty),
			Email = (contact.Email ?? string.Empty),
			Remarks = (contact.Remarks ?? string.Empty)
		};
	}

	private static CustomerEditViewModel NormalizeContactRows(CustomerEditViewModel model)
	{
		model.Contacts = model.Contacts.Where((CustomerContactInputModel contact) => contact.Id.HasValue || contact.IsDeleted || IsContactRowFilled(contact)).ToList();
		return model;
	}

	private static bool IsContactRowFilled(CustomerContactInputModel contact)
	{
		return !string.IsNullOrWhiteSpace(contact.Name) || !string.IsNullOrWhiteSpace(contact.ContactType) || !string.IsNullOrWhiteSpace(contact.Salutation) || !string.IsNullOrWhiteSpace(contact.Position) || !string.IsNullOrWhiteSpace(contact.Mobile) || !string.IsNullOrWhiteSpace(contact.Phone) || !string.IsNullOrWhiteSpace(contact.Fax) || !string.IsNullOrWhiteSpace(contact.Email) || !string.IsNullOrWhiteSpace(contact.BusinessCardNote) || !string.IsNullOrWhiteSpace(contact.Remarks);
	}

	private static void EnsureSingleDefaultContact(List<CustomerContactInputModel> contacts)
	{
		List<CustomerContactInputModel> list = contacts.Where((CustomerContactInputModel contact) => !contact.IsDeleted && IsContactRowFilled(contact)).ToList();
		if (list.Count == 0)
		{
			return;
		}
		bool flag = false;
		foreach (CustomerContactInputModel item in list)
		{
			if (item.IsDefault && !flag)
			{
				flag = true;
			}
			else
			{
				item.IsDefault = false;
			}
		}
		if (!flag)
		{
			list[0].IsDefault = true;
		}
	}

	private static CustomerContact? GetDefaultContact(Customer customer)
	{
		return (from contact in customer.Contacts
			where !contact.IsDeleted
			orderby contact.IsDefault descending, contact.SortOrder, contact.Id
			select contact).FirstOrDefault();
	}

	private static string BuildRegion(Customer customer)
	{
		return string.Join(" ", new string[3] { customer.CountryRegion, customer.City, customer.District }.Where((string value) => !string.IsNullOrWhiteSpace(value)));
	}

	private static string? NormalizeText(string? value)
	{
		return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
	}

	private static string EscapeCsv(string? value)
	{
		string text = value ?? string.Empty;
		return "\"" + text.Replace("\"", "\"\"") + "\"";
	}

	private ContentResult BuildDetailPageCloseResult()
	{
		string content = "<!DOCTYPE html>\n<html lang=\"zh-CN\">\n<head>\n    <meta charset=\"utf-8\" />\n    <title>处理中</title>\n</head>\n<body>\n    <script>\n        const closeMessage = {\n            type: \"open-erp:employee-modal-close\",\n            refreshRequested: true\n        };\n\n        if (window.parent && window.parent !== window) {\n            window.parent.postMessage(closeMessage, window.location.origin);\n        } else if (window.opener && !window.opener.closed) {\n            window.opener.postMessage(closeMessage, window.location.origin);\n            window.close();\n        }\n    </script>\n</body>\n</html>";
		return Content(content, "text/html; charset=utf-8");
	}

	private string GetCurrentUserName()
	{
		return base.User.Identity?.Name ?? "系统";
	}
}
