(() => {
    const form = document.getElementById("companyOrganizationDetailForm");
    if (!form) {
        return;
    }

    const createUrl = form.dataset.createUrl || "";
    const locationOptionsUrl = form.dataset.locationOptionsUrl || "";
    const bankAccountApiBaseUrl = form.dataset.bankAccountApiBaseUrl || "";
    const employeeRecordApiBaseUrl = form.dataset.employeeApiBaseUrl || "/api/companyorganizationemployee";
    const documentNumberRuleApiBaseUrl = form.dataset.documentNumberRuleApiBaseUrl || "";
    const printTemplateApiBaseUrl = form.dataset.printTemplateApiBaseUrl || "";

    const countryRegionSelect = form.querySelector("[name='RegionId']");
    const citySelect = form.querySelector("[name='CityId']");
    const countySelect = form.querySelector("[name='CountyId']");
    const placeholderTabButtons = [...form.querySelectorAll(".company-org-detail-tabs__item[data-company-tool='tab']")];
    const employeeRecordPlaceholderButton = placeholderTabButtons.at(-1) || null;
    if (employeeRecordPlaceholderButton && !employeeRecordPlaceholderButton.dataset.tabPanel) {
        employeeRecordPlaceholderButton.dataset.tabPanel = "employeeRecords";
        employeeRecordPlaceholderButton.removeAttribute("data-company-tool");
        employeeRecordPlaceholderButton.removeAttribute("data-tab-label");
    }
    const tabButtons = [...form.querySelectorAll("[data-tab-panel]")];
    const tabPanels = [...form.querySelectorAll("[data-tab-content]")];

    const financePanel = document.getElementById("companyOrgFinancePanel");
    const financePendingHint = document.getElementById("companyBankAccountPendingHint");
    const financeTableBody = document.getElementById("companyBankAccountTableBody");
    const financeSummary = document.getElementById("companyBankAccountSummary");
    const financePageNumbers = document.getElementById("companyBankAccountPageNumbers");
    const financePrevPage = document.getElementById("companyBankAccountPrevPage");
    const financeNextPage = document.getElementById("companyBankAccountNextPage");
    const financePageSize = document.getElementById("companyBankAccountPageSize");
    const financeStatusFilter = document.getElementById("companyBankAccountStatusFilter");
    const financeKeyword = document.getElementById("companyBankAccountKeyword");
    const financeSearchButton = document.getElementById("companyBankAccountSearchButton");
    const financeRefreshButton = document.getElementById("companyBankAccountRefreshButton");
    const financeAddButton = document.getElementById("companyBankAccountAddButton");
    const financeDeleteButton = document.getElementById("companyBankAccountDeleteButton");
    const financeSelectAll = document.getElementById("companyBankAccountSelectAll");
    const financeEditor = document.getElementById("companyBankAccountEditor");
    const financeSaveButton = document.getElementById("companyBankAccountSaveButton");
    const financeCancelButton = document.getElementById("companyBankAccountCancelButton");

    const editorId = document.getElementById("companyBankAccountId");
    const editorAccountNumber = document.getElementById("companyBankAccountNumber");
    const editorBankId = document.getElementById("companyBankAccountBankId");
    const editorBranchName = document.getElementById("companyBankAccountBranchName");
    const editorBranchAddress = document.getElementById("companyBankAccountBranchAddress");
    const editorCurrencyCode = document.getElementById("companyBankAccountCurrencyCode");
    const editorStatusCode = document.getElementById("companyBankAccountStatusCode");
    const editorSubjectCode = document.getElementById("companyBankAccountSubjectCode");
    const editorSubjectName = document.getElementById("companyBankAccountSubjectName");
    const editorRemarks = document.getElementById("companyBankAccountRemarks");
    const editorIsDefault = document.getElementById("companyBankAccountIsDefault");

    const documentRulePanel = document.getElementById("companyOrgDocumentRulePanel");
    const documentRulePendingHint = document.getElementById("companyDocumentRulePendingHint");
    const documentRuleTableBody = document.getElementById("companyDocumentRuleTableBody");
    const documentRuleRefreshButton = document.getElementById("companyDocumentRuleRefreshButton");
    const documentRuleSaveButton = document.getElementById("companyDocumentRuleSaveButton");

    const documentPrintPanel = document.getElementById("companyOrgPrintPanel");
    const documentPrintPendingHint = document.getElementById("companyPrintPendingHint");
    const documentPrintLogoFile = document.getElementById("companyPrintLogoFile");
    const documentPrintLogoChooseButton = document.getElementById("companyPrintLogoChooseButton");
    const documentPrintLogoUploadButton = document.getElementById("companyPrintLogoUploadButton");
    const documentPrintLogoDeleteButton = document.getElementById("companyPrintLogoDeleteButton");
    const documentPrintLogoImage = document.getElementById("companyPrintLogoImage");
    const documentPrintLogoEmpty = document.getElementById("companyPrintLogoEmpty");
    const documentPrintLogoSelected = document.getElementById("companyPrintLogoSelected");
    const documentPrintLogoPath = document.getElementById("companyPrintLogoPath");

    const employeePanel = document.getElementById("companyOrgEmployeePanel");
    const employeePendingHint = document.getElementById("companyEmployeePendingHint");
    const employeeTableBody = document.getElementById("companyEmployeeTableBody");
    const employeeSummary = document.getElementById("companyEmployeeSummary");
    const employeePageNumbers = document.getElementById("companyEmployeePageNumbers");
    const employeePrevPage = document.getElementById("companyEmployeePrevPage");
    const employeeNextPage = document.getElementById("companyEmployeeNextPage");
    const employeePageSize = document.getElementById("companyEmployeePageSize");
    const employeeStatusFilter = document.getElementById("companyEmployeeStatusFilter");
    const employeeKeyword = document.getElementById("companyEmployeeKeyword");
    const employeeSearchButton = document.getElementById("companyEmployeeSearchButton");
    const employeeRefreshButton = document.getElementById("companyEmployeeRefreshButton");
    const employeePhotoModal = document.getElementById("companyEmployeePhotoModal");
    const employeePhotoTitle = document.getElementById("companyEmployeePhotoTitle");
    const employeePhotoImage = document.getElementById("companyEmployeePhotoImage");
    const employeePhotoClose = document.getElementById("companyEmployeePhotoClose");

    const bankAccountState = {
        loaded: false,
        pageNumber: 1,
        pageSize: Number(financePageSize?.value || 10),
        totalCount: 0,
        items: [],
        bankOptions: [],
        currencyOptions: [],
        statusOptions: []
    };

    const documentRuleState = {
        loaded: false,
        dirty: false,
        items: [],
        dateFormatOptions: []
    };

    const documentPrintState = {
        loaded: false,
        hasLogo: false,
        logoUrl: "",
        logoStoragePath: "",
        selectedFileName: ""
    };

    const employeeState = {
        loaded: false,
        pageNumber: 1,
        pageSize: Number(employeePageSize?.value || 10),
        totalCount: 0,
        items: [],
        statusOptions: []
    };

    const getCompanyOrganizationId = () => Number(form.querySelector("input[name='Id']")?.value || financePanel?.dataset.companyOrganizationId || employeePanel?.dataset.companyOrganizationId || documentRulePanel?.dataset.companyOrganizationId || documentPrintPanel?.dataset.companyOrganizationId || 0);
    const isReadOnlyMode = () => form.classList.contains("is-readonly");
    const canEditChildPanels = () => !isReadOnlyMode() && getCompanyOrganizationId() > 0;
    const escapeHtml = value => String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#39;");

    const bindSelectOptions = (selectElement, options, selectedValue, includeEmpty = true, emptyText = "璇烽€夋嫨") => {
        if (!selectElement) {
            return;
        }

        selectElement.innerHTML = "";
        if (includeEmpty) {
            const emptyOption = document.createElement("option");
            emptyOption.value = "";
            emptyOption.textContent = emptyText;
            selectElement.appendChild(emptyOption);
        }

        options.forEach(option => {
            const optionElement = document.createElement("option");
            optionElement.value = String(option.value);
            optionElement.textContent = option.text;
            selectElement.appendChild(optionElement);
        });

        selectElement.value = selectedValue ?? "";
    };

    const refreshLocationSelect = async (selectElement, level, parentId, placeholderText, keepSelectedValue = true) => {
        if (!selectElement) {
            return;
        }

        const previousValue = keepSelectedValue ? selectElement.value : "";
        selectElement.innerHTML = "";

        const placeholderOption = document.createElement("option");
        placeholderOption.value = "";
        placeholderOption.textContent = placeholderText;
        selectElement.appendChild(placeholderOption);

        if (!locationOptionsUrl || !parentId) {
            selectElement.value = "";
            return;
        }

        const requestUrl = new URL(locationOptionsUrl, window.location.origin);
        requestUrl.searchParams.set("level", level);
        requestUrl.searchParams.set("parentId", parentId);

        const response = await fetch(requestUrl.toString(), {
            headers: { "X-Requested-With": "XMLHttpRequest" }
        });

        if (!response.ok) {
            return;
        }

        const options = await response.json();
        options.forEach(option => {
            const optionElement = document.createElement("option");
            optionElement.value = String(option.value);
            optionElement.textContent = option.text;
            selectElement.appendChild(optionElement);
        });

        if ([...selectElement.options].some(option => option.value === previousValue)) {
            selectElement.value = previousValue;
        }
    };

    const syncLocationCascade = async () => {
        await refreshLocationSelect(citySelect, "CITY", countryRegionSelect?.value || "", countryRegionSelect?.value ? "璇烽€夋嫨" : "璇峰厛閫夋嫨鍥藉/鍦板尯");
        await refreshLocationSelect(countySelect, "COUNTY", citySelect?.value || "", citySelect?.value ? "璇烽€夋嫨" : "璇峰厛閫夋嫨鍩庡競");
    };

    const setFinanceEmptyMessage = message => {
        if (!financeTableBody) {
            return;
        }

        financeTableBody.innerHTML = `<tr><td colspan="12" class="company-org-finance-table__empty">${message}</td></tr>`;
    };

    const resolveFinanceStatusText = statusCode => {
        const option = bankAccountState.statusOptions.find(item => item.value === statusCode);
        return option?.text || statusCode || "-";
    };

    const renderFinancePagination = () => {
        if (!financePageNumbers || !financePrevPage || !financeNextPage) {
            return;
        }

        const totalPages = Math.max(1, Math.ceil(bankAccountState.totalCount / bankAccountState.pageSize));
        financePrevPage.disabled = bankAccountState.pageNumber <= 1;
        financeNextPage.disabled = bankAccountState.pageNumber >= totalPages;
        financePageNumbers.innerHTML = "";

        const startPage = Math.max(1, bankAccountState.pageNumber - 2);
        const endPage = Math.min(totalPages, startPage + 4);
        for (let page = startPage; page <= endPage; page += 1) {
            const button = document.createElement("button");
            button.type = "button";
            button.className = `company-org-finance-pagebtn ${page === bankAccountState.pageNumber ? "is-active" : ""}`;
            button.dataset.pageNumber = String(page);
            button.textContent = String(page);
            financePageNumbers.appendChild(button);
        }
    };

    const syncFinanceAccess = () => {
        const canEdit = canEditChildPanels();
        if (financePendingHint) {
            financePendingHint.hidden = getCompanyOrganizationId() > 0;
        }

        [financeAddButton, financeDeleteButton, financeSaveButton, editorAccountNumber, editorBankId, editorBranchName, editorBranchAddress, editorCurrencyCode, editorStatusCode, editorSubjectCode, editorSubjectName, editorRemarks, editorIsDefault]
            .forEach(element => {
                if (element) {
                    element.disabled = !canEdit;
                }
            });

        if (financeSelectAll) {
            financeSelectAll.disabled = !canEdit || bankAccountState.items.length === 0;
            if (!canEdit) {
                financeSelectAll.checked = false;
            }
        }

        form.querySelectorAll(".company-bank-account-row-selector").forEach(checkbox => {
            checkbox.disabled = !canEdit;
            if (!canEdit) {
                checkbox.checked = false;
            }
        });
    };

    const hideFinanceEditor = () => {
        if (financeEditor) {
            financeEditor.hidden = true;
        }
        if (editorId) {
            editorId.value = "0";
        }
    };

    const showFinanceEditor = item => {
        if (!financeEditor) {
            return;
        }

        if (editorId) editorId.value = String(item?.id || 0);
        if (editorAccountNumber) editorAccountNumber.value = item?.accountNumber || "";
        if (editorBranchName) editorBranchName.value = item?.branchName || "";
        if (editorBranchAddress) editorBranchAddress.value = item?.branchAddress || "";
        if (editorSubjectCode) editorSubjectCode.value = item?.subjectCode || "";
        if (editorSubjectName) editorSubjectName.value = item?.subjectName || "";
        if (editorRemarks) editorRemarks.value = item?.remarks || "";
        if (editorIsDefault) editorIsDefault.checked = Boolean(item?.isDefault);

        bindSelectOptions(editorBankId, bankAccountState.bankOptions, item?.bankId ? String(item.bankId) : "");
        bindSelectOptions(editorCurrencyCode, bankAccountState.currencyOptions, item?.currencyCode || "RMB", false);
        bindSelectOptions(editorStatusCode, bankAccountState.statusOptions, item?.statusCode || "NORMAL", false);

        financeEditor.hidden = false;
        syncFinanceAccess();
        editorAccountNumber?.focus();
    };

    const renderFinanceTable = () => {
        if (!financeTableBody) {
            return;
        }

        if (bankAccountState.items.length === 0) {
            setFinanceEmptyMessage(getCompanyOrganizationId() > 0 ? "\u6682\u65e0\u94f6\u884c\u8d26\u53f7\u8bb0\u5f55\u3002" : "\u8bf7\u5148\u4fdd\u5b58\u516c\u53f8\u7ec4\u7ec7\u540e\u518d\u7ef4\u62a4\u94f6\u884c\u8d26\u53f7\u3002");
            if (financeSummary) {
                financeSummary.textContent = "\u5171 0 \u6761";
            }
            renderFinancePagination();
            syncFinanceAccess();
            return;
        }

        const readOnly = isReadOnlyMode();
        const startIndex = (bankAccountState.pageNumber - 1) * bankAccountState.pageSize;
        financeTableBody.innerHTML = bankAccountState.items.map((item, index) => `
            <tr>
                <td class="company-org-finance-table__seq">${startIndex + index + 1}</td>
                <td class="company-org-finance-table__checkbox"><input class="company-bank-account-row-selector" type="checkbox" value="${item.id}" ${readOnly ? "disabled" : ""} /></td>
                <td><button type="button" class="company-org-finance-link" data-company-bank-account-edit="${item.id}" ${readOnly ? "disabled" : ""}>${item.accountNumber}</button></td>
                <td>${item.bankName || "-"}</td>
                <td>${item.branchName || "-"}</td>
                <td>${item.branchAddress || "-"}</td>
                <td>${item.currencyCode || "-"}</td>
                <td>${resolveFinanceStatusText(item.statusCode)}</td>
                <td>${item.subjectCode || "-"}</td>
                <td>${item.subjectName || "-"}</td>
                <td>${item.remarks || "-"}</td>
                <td><input type="checkbox" ${item.isDefault ? "checked" : ""} disabled /></td>
            </tr>`).join("");

        if (financeSummary) {
            financeSummary.textContent = `\u5171 ${bankAccountState.totalCount} \u6761`;
        }

        renderFinancePagination();
        syncFinanceAccess();
    };

    const loadFinancePanel = async () => {
        const companyOrganizationId = getCompanyOrganizationId();
        if (companyOrganizationId <= 0) {
            bankAccountState.loaded = true;
            bankAccountState.items = [];
            bankAccountState.totalCount = 0;
            renderFinanceTable();
            return;
        }

        setFinanceEmptyMessage("鍔犺浇涓?..");

        const requestUrl = new URL(`${bankAccountApiBaseUrl}/${companyOrganizationId}`, window.location.origin);
        requestUrl.searchParams.set("keyword", financeKeyword?.value?.trim() || "");
        requestUrl.searchParams.set("statusCode", financeStatusFilter?.value || "");
        requestUrl.searchParams.set("pageNumber", String(bankAccountState.pageNumber));
        requestUrl.searchParams.set("pageSize", String(bankAccountState.pageSize));

        const response = await fetch(requestUrl.toString(), {
            headers: { "X-Requested-With": "XMLHttpRequest" }
        });

        const payload = await response.json().catch(() => null);
        if (!response.ok) {
            setFinanceEmptyMessage(payload?.message || "\u94f6\u884c\u8d26\u53f7\u6570\u636e\u52a0\u8f7d\u5931\u8d25\u3002");
            return;
        }

        bankAccountState.loaded = true;
        bankAccountState.items = payload.items || [];
        bankAccountState.totalCount = payload.totalCount || 0;
        bankAccountState.pageNumber = payload.pageNumber || 1;
        bankAccountState.pageSize = payload.pageSize || bankAccountState.pageSize;
        bankAccountState.bankOptions = payload.bankOptions || [];
        bankAccountState.currencyOptions = payload.currencyOptions || [];
        bankAccountState.statusOptions = payload.statusOptions || [];

        bindSelectOptions(financeStatusFilter, bankAccountState.statusOptions, financeStatusFilter?.value || "", true, "鍏ㄩ儴");
        bindSelectOptions(editorBankId, bankAccountState.bankOptions, editorBankId?.value || "");
        bindSelectOptions(editorCurrencyCode, bankAccountState.currencyOptions, editorCurrencyCode?.value || "RMB", false);
        bindSelectOptions(editorStatusCode, bankAccountState.statusOptions, editorStatusCode?.value || "NORMAL", false);
        renderFinanceTable();
    };

    const collectSelectedFinanceIds = () => [...form.querySelectorAll(".company-bank-account-row-selector:checked")]
        .map(checkbox => Number(checkbox.value))
        .filter(value => value > 0);

    const findFinanceItem = itemId => bankAccountState.items.find(item => item.id === itemId);

    const saveFinanceRecord = async () => {
        const companyOrganizationId = getCompanyOrganizationId();
        if (companyOrganizationId <= 0) {
            window.alert("\u8bf7\u5148\u4fdd\u5b58\u516c\u53f8\u7ec4\u7ec7\u540e\uff0c\u518d\u7ef4\u62a4\u94f6\u884c\u8d26\u53f7\u3002");
            return;
        }

        const accountNumber = editorAccountNumber?.value?.trim() || "";
        const branchName = editorBranchName?.value?.trim() || "";
        if (!accountNumber) {
            window.alert("\u8bf7\u8f93\u5165\u94f6\u884c\u8d26\u53f7\u3002");
            editorAccountNumber?.focus();
            return;
        }

        if (!branchName) {
            window.alert("\u8bf7\u8f93\u5165\u5f00\u6237\u652f\u884c\u540d\u79f0\u3002");
            editorBranchName?.focus();
            return;
        }

        const response = await fetch(`${bankAccountApiBaseUrl}/${companyOrganizationId}`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "X-Requested-With": "XMLHttpRequest"
            },
            body: JSON.stringify({
                id: Number(editorId?.value || 0),
                accountNumber,
                bankId: editorBankId?.value ? Number(editorBankId.value) : null,
                branchName,
                branchAddress: editorBranchAddress?.value?.trim() || null,
                currencyCode: editorCurrencyCode?.value || "RMB",
                statusCode: editorStatusCode?.value || "NORMAL",
                subjectCode: editorSubjectCode?.value?.trim() || null,
                subjectName: editorSubjectName?.value?.trim() || null,
                remarks: editorRemarks?.value?.trim() || null,
                isDefault: Boolean(editorIsDefault?.checked)
            })
        });

        const payload = await response.json().catch(() => null);
        if (!response.ok) {
            window.alert(payload?.message || "\u94f6\u884c\u8d26\u53f7\u4fdd\u5b58\u5931\u8d25\u3002");
            return;
        }

        hideFinanceEditor();
        await loadFinancePanel();
    };

    const deleteFinanceRecords = async () => {
        const companyOrganizationId = getCompanyOrganizationId();
        const ids = collectSelectedFinanceIds();
        if (companyOrganizationId <= 0 || ids.length === 0) {
            window.alert("\u8bf7\u9009\u62e9\u8981\u5220\u9664\u7684\u94f6\u884c\u8d26\u53f7\u8bb0\u5f55\u3002");
            return;
        }

        if (!window.confirm(`\u786e\u5b9a\u5220\u9664\u5df2\u9009\u4e2d\u7684 ${ids.length} \u6761\u94f6\u884c\u8d26\u53f7\u8bb0\u5f55\u5417\uff1f`)) {
            return;
        }

        const response = await fetch(`${bankAccountApiBaseUrl}/${companyOrganizationId}/delete`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "X-Requested-With": "XMLHttpRequest"
            },
            body: JSON.stringify({ ids })
        });

        const payload = await response.json().catch(() => null);
        if (!response.ok) {
            window.alert(payload?.message || "\u5220\u9664\u94f6\u884c\u8d26\u53f7\u5931\u8d25\u3002");
            return;
        }

        hideFinanceEditor();
        await loadFinancePanel();
    };

    const setDocumentRuleEmptyMessage = message => {
        if (!documentRuleTableBody) {
            return;
        }

        documentRuleTableBody.innerHTML = `<tr><td colspan="7" class="company-org-finance-table__empty">${message}</td></tr>`;
    };

    const buildDocumentRuleSample = (prefix, dateFormatCode, sequenceLength) => {
        const today = new Date();
        let dateSegment = "";
        if (dateFormatCode === "yyyyMM") {
            dateSegment = `${today.getFullYear()}${String(today.getMonth() + 1).padStart(2, "0")}`;
        } else if (dateFormatCode === "yyyyMMdd") {
            dateSegment = `${today.getFullYear()}${String(today.getMonth() + 1).padStart(2, "0")}${String(today.getDate()).padStart(2, "0")}`;
        } else if (dateFormatCode !== "NONE") {
            dateSegment = `${String(today.getFullYear()).slice(-2)}${String(today.getMonth() + 1).padStart(2, "0")}`;
        }

        return `${prefix || ""}${dateSegment}${String(1).padStart(Math.max(Number(sequenceLength) || 1, 1), "0")}`;
    };

    const syncDocumentRuleAccess = () => {
        const canEdit = canEditChildPanels();
        if (documentRulePendingHint) {
            documentRulePendingHint.hidden = getCompanyOrganizationId() > 0;
        }

        if (documentRuleSaveButton) {
            documentRuleSaveButton.disabled = !canEdit;
        }

        form.querySelectorAll(".company-doc-rule-input").forEach(element => {
            element.disabled = !canEdit;
        });
    };

    const renderDocumentPrintPanel = () => {
        if (documentPrintLogoImage) {
            documentPrintLogoImage.hidden = !documentPrintState.hasLogo || !documentPrintState.logoUrl;
            if (documentPrintState.hasLogo && documentPrintState.logoUrl) {
                documentPrintLogoImage.src = documentPrintState.logoUrl;
            } else {
                documentPrintLogoImage.removeAttribute("src");
            }
        }

        if (documentPrintLogoEmpty) {
            documentPrintLogoEmpty.hidden = documentPrintState.hasLogo;
        }

        if (documentPrintLogoSelected) {
            documentPrintLogoSelected.textContent = documentPrintState.selectedFileName
                ? `寰呬笂浼犳枃浠讹細${documentPrintState.selectedFileName}`
                : "\u652f\u6301 JPG / PNG\uff0c\u6587\u4ef6\u540d\u81ea\u52a8\u4f7f\u7528\u7ec4\u7ec7\u7f16\u53f7\u3002";
        }

        if (documentPrintLogoPath) {
            documentPrintLogoPath.textContent = documentPrintState.logoStoragePath
                ? `瀛樺偍璺緞锛?{documentPrintState.logoStoragePath}`
                : "";
        }
    };

    const clearDocumentPrintSelectedFile = () => {
        documentPrintState.selectedFileName = "";
        if (documentPrintLogoFile) {
            documentPrintLogoFile.value = "";
        }
        renderDocumentPrintPanel();
    };

    const syncDocumentPrintAccess = () => {
        const canEdit = canEditChildPanels();
        const hasOrganization = getCompanyOrganizationId() > 0;

        if (documentPrintPendingHint) {
            documentPrintPendingHint.hidden = hasOrganization;
        }

        [documentPrintLogoChooseButton, documentPrintLogoUploadButton, documentPrintLogoDeleteButton].forEach(element => {
            if (element) {
                element.disabled = !canEdit || !hasOrganization;
            }
        });
    };

    const loadDocumentPrintSettings = async () => {
        const companyOrganizationId = getCompanyOrganizationId();
        if (companyOrganizationId <= 0) {
            documentPrintState.loaded = true;
            documentPrintState.hasLogo = false;
            documentPrintState.logoUrl = "";
            documentPrintState.logoStoragePath = "";
            clearDocumentPrintSelectedFile();
            renderDocumentPrintPanel();
            syncDocumentPrintAccess();
            return;
        }

        const response = await fetch(`${printTemplateApiBaseUrl}/${companyOrganizationId}`, {
            headers: { "X-Requested-With": "XMLHttpRequest" }
        });

        const payload = await response.json().catch(() => null);
        if (!response.ok) {
            window.alert(payload?.message || "\u5355\u636e\u6253\u5370\u914d\u7f6e\u52a0\u8f7d\u5931\u8d25\u3002");
            return;
        }

        documentPrintState.loaded = true;
        documentPrintState.hasLogo = Boolean(payload.hasLogo);
        documentPrintState.logoUrl = payload.logoUrl || "";
        documentPrintState.logoStoragePath = payload.logoStoragePath || "";
        clearDocumentPrintSelectedFile();
        renderDocumentPrintPanel();
        syncDocumentPrintAccess();
    };

    const uploadDocumentPrintLogo = async () => {
        const companyOrganizationId = getCompanyOrganizationId();
        if (companyOrganizationId <= 0) {
            window.alert("\u8bf7\u5148\u4fdd\u5b58\u516c\u53f8\u7ec4\u7ec7\u540e\uff0c\u518d\u4e0a\u4f20 LOGO\u3002");
            return;
        }

        const file = documentPrintLogoFile?.files?.[0];
        if (!file) {
            window.alert("\u8bf7\u9009\u62e9\u8981\u4e0a\u4f20\u7684 LOGO \u56fe\u7247\u3002");
            return;
        }

        const formData = new FormData();
        formData.append("file", file);

        const response = await fetch(`${printTemplateApiBaseUrl}/${companyOrganizationId}/logo`, {
            method: "POST",
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            },
            body: formData
        });

        const payload = await response.json().catch(() => null);
        if (!response.ok) {
            window.alert(payload?.message || "LOGO \u4e0a\u4f20\u5931\u8d25\u3002");
            return;
        }

        documentPrintState.hasLogo = Boolean(payload.hasLogo);
        documentPrintState.logoUrl = payload.logoUrl || "";
        documentPrintState.logoStoragePath = payload.logoStoragePath || "";
        clearDocumentPrintSelectedFile();
        renderDocumentPrintPanel();
        window.alert(payload?.message || "LOGO \u5df2\u4e0a\u4f20\u3002");
    };

    const deleteDocumentPrintLogo = async () => {
        const companyOrganizationId = getCompanyOrganizationId();
        if (companyOrganizationId <= 0) {
            window.alert("\u8bf7\u5148\u4fdd\u5b58\u516c\u53f8\u7ec4\u7ec7\u540e\uff0c\u518d\u5220\u9664 LOGO\u3002");
            return;
        }

        if (!window.confirm("纭畾鍒犻櫎褰撳墠鍏徃鐨勬墦鍗?LOGO 鍚楋紵")) {
            return;
        }

        const response = await fetch(`${printTemplateApiBaseUrl}/${companyOrganizationId}/logo/delete`, {
            method: "POST",
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            }
        });

        const payload = await response.json().catch(() => null);
        if (!response.ok) {
            window.alert(payload?.message || "LOGO \u5220\u9664\u5931\u8d25\u3002");
            return;
        }

        documentPrintState.hasLogo = false;
        documentPrintState.logoUrl = "";
        documentPrintState.logoStoragePath = "";
        clearDocumentPrintSelectedFile();
        renderDocumentPrintPanel();
        window.alert(payload?.message || "LOGO \u5df2\u5220\u9664\u3002");
    };

    const employeeText = {
        all: "\u5168\u90e8",
        empty: "\u6682\u65e0\u5458\u5de5\u8bb0\u5f55\u3002",
        saveFirst: "\u8bf7\u5148\u4fdd\u5b58\u516c\u53f8\u7ec4\u7ec7\u540e\u518d\u67e5\u770b\u5458\u5de5\u8bb0\u5f55\u3002",
        loading: "\u52a0\u8f7d\u4e2d...",
        loadFailed: "\u5458\u5de5\u8bb0\u5f55\u52a0\u8f7d\u5931\u8d25\u3002",
        employee: "\u5458\u5de5",
        detailTitle: "\u5458\u5de5\u8d44\u6599",
        photo: "\u56fe\u7247",
        view: "\u67e5\u770b",
        noPhoto: "\u8be5\u5458\u5de5\u672a\u7ef4\u62a4\u56fe\u7247\u3002"
    };

    const setEmployeeEmptyMessage = message => {
        if (!employeeTableBody) {
            return;
        }

        employeeTableBody.innerHTML = `<tr><td colspan="14" class="company-org-finance-table__empty">${message}</td></tr>`;
    };

    const syncEmployeeAccess = () => {
        if (employeePendingHint) {
            employeePendingHint.hidden = getCompanyOrganizationId() > 0;
        }
    };

    const renderEmployeePagination = () => {
        if (!employeePageNumbers || !employeePrevPage || !employeeNextPage) {
            return;
        }

        const totalPages = Math.max(1, Math.ceil(employeeState.totalCount / employeeState.pageSize));
        employeePrevPage.disabled = employeeState.pageNumber <= 1;
        employeeNextPage.disabled = employeeState.pageNumber >= totalPages;
        employeePageNumbers.innerHTML = "";

        const startPage = Math.max(1, employeeState.pageNumber - 2);
        const endPage = Math.min(totalPages, startPage + 4);
        for (let page = startPage; page <= endPage; page += 1) {
            const button = document.createElement("button");
            button.type = "button";
            button.className = `company-org-finance-pagebtn ${page === employeeState.pageNumber ? "is-active" : ""}`;
            button.dataset.employeePageNumber = String(page);
            button.textContent = String(page);
            employeePageNumbers.appendChild(button);
        }
    };

    const renderEmployeeTable = () => {
        if (!employeeTableBody) {
            return;
        }

        if (employeeState.items.length === 0) {
            setEmployeeEmptyMessage(getCompanyOrganizationId() > 0 ? employeeText.empty : employeeText.saveFirst);
            if (employeeSummary) {
                employeeSummary.textContent = `\u5171 0 \u6761`;
            }
            renderEmployeePagination();
            syncEmployeeAccess();
            return;
        }

        employeeTableBody.innerHTML = employeeState.items.map(item => `
            <tr>
                <td class="company-org-employee-table__seq">${item.sequenceNo}</td>
                <td>
                    <a href="${escapeHtml(item.detailsUrl || "#")}"
                       data-popup-url="${escapeHtml(item.detailsUrl || "#")}"
                       data-popup-title="${employeeText.detailTitle}"
                       class="company-org-employee-link">${escapeHtml(item.employeeCode || "-")}</a>
                </td>
                <td>${escapeHtml(item.displayName || "-")}</td>
                <td>${escapeHtml(item.genderName || "-")}</td>
                <td>${escapeHtml(item.cardNumber || "-")}</td>
                <td>${escapeHtml(item.departmentName || "-")}</td>
                <td>${escapeHtml(item.groupName || "-")}</td>
                <td>${escapeHtml(item.positionName || "-")}</td>
                <td>${escapeHtml(item.mobile || "-")}</td>
                <td>${escapeHtml(item.telephone || "-")}</td>
                <td>${escapeHtml(item.email || "-")}</td>
                <td>
                    <button type="button"
                            class="company-org-employee-photo-btn"
                            data-company-employee-photo="${escapeHtml(item.photoPath || "")}"
                            data-company-employee-name="${escapeHtml(item.displayName || item.employeeCode || employeeText.employee)}"
                            ${item.hasPhoto ? "" : "disabled"}>
                        ${employeeText.view}
                    </button>
                </td>
                <td class="company-org-employee-table__remarks">${escapeHtml(item.remarks || "-")}</td>
                <td>${escapeHtml(item.statusName || "-")}</td>
            </tr>`).join("");

        if (employeeSummary) {
            employeeSummary.textContent = `\u5171 ${employeeState.totalCount} \u6761`;
        }

        renderEmployeePagination();
        syncEmployeeAccess();
    };

    const loadEmployeePanel = async () => {
        const companyOrganizationId = getCompanyOrganizationId();
        if (companyOrganizationId <= 0) {
            employeeState.loaded = true;
            employeeState.items = [];
            employeeState.totalCount = 0;
            renderEmployeeTable();
            return;
        }

        setEmployeeEmptyMessage(employeeText.loading);

        const requestUrl = new URL(`${employeeRecordApiBaseUrl}/${companyOrganizationId}`, window.location.origin);
        requestUrl.searchParams.set("keyword", employeeKeyword?.value?.trim() || "");
        requestUrl.searchParams.set("statusCode", employeeStatusFilter?.value || "");
        requestUrl.searchParams.set("pageNumber", String(employeeState.pageNumber));
        requestUrl.searchParams.set("pageSize", String(employeeState.pageSize));

        const response = await fetch(requestUrl.toString(), {
            headers: { "X-Requested-With": "XMLHttpRequest" }
        });

        const payload = await response.json().catch(() => null);
        if (!response.ok) {
            setEmployeeEmptyMessage(payload?.message || employeeText.loadFailed);
            return;
        }

        employeeState.loaded = true;
        employeeState.items = payload.items || [];
        employeeState.totalCount = payload.totalCount || 0;
        employeeState.pageNumber = payload.pageNumber || 1;
        employeeState.pageSize = payload.pageSize || employeeState.pageSize;
        employeeState.statusOptions = payload.statusOptions || [];

        bindSelectOptions(employeeStatusFilter, employeeState.statusOptions, employeeStatusFilter?.value || "", true, employeeText.all);
        renderEmployeeTable();
    };

    const hideEmployeePhotoModal = () => {
        if (!employeePhotoModal) {
            return;
        }

        employeePhotoModal.hidden = true;
        document.body.classList.remove("company-org-photo-modal-open");
        if (employeePhotoImage) {
            employeePhotoImage.removeAttribute("src");
        }
    };

    const showEmployeePhotoModal = (photoPath, employeeName) => {
        if (!employeePhotoModal || !employeePhotoImage || !photoPath) {
            window.alert(employeeText.noPhoto);
            return;
        }

        employeePhotoImage.src = photoPath;
        employeePhotoImage.alt = `${employeeName} ${employeeText.photo}`;
        if (employeePhotoTitle) {
            employeePhotoTitle.textContent = `${employeeName} ${employeeText.photo}`;
        }

        employeePhotoModal.hidden = false;
        document.body.classList.add("company-org-photo-modal-open");
    };

    const renderDocumentRuleTable = () => {
        if (!documentRuleTableBody) {
            return;
        }

        if (getCompanyOrganizationId() <= 0) {
            setDocumentRuleEmptyMessage("\u8bf7\u5148\u4fdd\u5b58\u516c\u53f8\u7ec4\u7ec7\u540e\u518d\u7ef4\u62a4\u5355\u53f7\u89c4\u5219\u3002");
            syncDocumentRuleAccess();
            return;
        }

        if (documentRuleState.items.length === 0) {
            setDocumentRuleEmptyMessage("\u6682\u65e0\u5355\u53f7\u89c4\u5219\u8bb0\u5f55\u3002");
            syncDocumentRuleAccess();
            return;
        }

        const canEdit = canEditChildPanels();
        documentRuleTableBody.innerHTML = documentRuleState.items.map((item, index) => `
            <tr data-document-type-code="${item.documentTypeCode}" data-document-rule-id="${item.id || 0}">
                <td>${index + 1}</td>
                <td>${item.documentTypeName}</td>
                <td><input type="text" class="form-control company-doc-rule-input" data-rule-field="prefix" value="${item.prefix || ""}" ${canEdit ? "" : "disabled"} /></td>
                <td>
                    <select class="form-select company-doc-rule-input" data-rule-field="dateFormatCode" ${canEdit ? "" : "disabled"}>
                        ${documentRuleState.dateFormatOptions.map(option => `<option value="${option.value}" ${option.value === item.dateFormatCode ? "selected" : ""}>${option.text}</option>`).join("")}
                    </select>
                </td>
                <td><input type="number" min="1" max="12" class="form-control company-doc-rule-input" data-rule-field="sequenceLength" value="${item.sequenceLength || 5}" ${canEdit ? "" : "disabled"} /></td>
                <td data-rule-sample>${item.sampleNumber || "-"}</td>
                <td data-rule-last>${item.lastGeneratedNumber || "-"}</td>
            </tr>`).join("");

        syncDocumentRuleAccess();
    };

    const loadDocumentRules = async () => {
        const companyOrganizationId = getCompanyOrganizationId();
        if (companyOrganizationId <= 0) {
            documentRuleState.loaded = true;
            documentRuleState.items = [];
            renderDocumentRuleTable();
            return;
        }

        setDocumentRuleEmptyMessage("鍔犺浇涓?..");
        const response = await fetch(`${documentNumberRuleApiBaseUrl}/${companyOrganizationId}`, {
            headers: { "X-Requested-With": "XMLHttpRequest" }
        });

        const payload = await response.json().catch(() => null);
        if (!response.ok) {
            setDocumentRuleEmptyMessage(payload?.message || "\u5355\u53f7\u89c4\u5219\u52a0\u8f7d\u5931\u8d25\u3002");
            return;
        }

        documentRuleState.loaded = true;
        documentRuleState.dirty = false;
        documentRuleState.items = payload.items || [];
        documentRuleState.dateFormatOptions = payload.dateFormatOptions || [];
        renderDocumentRuleTable();
    };

    const collectDocumentRules = () => [...documentRuleTableBody.querySelectorAll("tr[data-document-type-code]")]
        .map(row => ({
            id: Number(row.dataset.documentRuleId || 0),
            documentTypeCode: row.dataset.documentTypeCode || "",
            prefix: row.querySelector("[data-rule-field='prefix']")?.value?.trim() || "",
            dateFormatCode: row.querySelector("[data-rule-field='dateFormatCode']")?.value || "yyMM",
            sequenceLength: Number(row.querySelector("[data-rule-field='sequenceLength']")?.value || 5)
        }));

    const saveDocumentRules = async ({ silent = false } = {}) => {
        const companyOrganizationId = getCompanyOrganizationId();
        if (companyOrganizationId <= 0) {
            if (!silent) {
                window.alert("\u8bf7\u5148\u4fdd\u5b58\u516c\u53f8\u7ec4\u7ec7\u540e\uff0c\u518d\u7ef4\u62a4\u5355\u53f7\u89c4\u5219\u3002");
            }
            return false;
        }

        const response = await fetch(`${documentNumberRuleApiBaseUrl}/${companyOrganizationId}`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "X-Requested-With": "XMLHttpRequest"
            },
            body: JSON.stringify({ items: collectDocumentRules() })
        });

        const payload = await response.json().catch(() => null);
        if (!response.ok) {
            if (!silent) {
                window.alert(payload?.message || "\u5355\u53f7\u89c4\u5219\u4fdd\u5b58\u5931\u8d25\u3002");
            }
            syncDocumentRuleAccess();
            return false;
        }

        documentRuleState.dirty = false;
        await loadDocumentRules();
        if (!silent) {
            window.alert("\u5355\u53f7\u89c4\u5219\u5df2\u4fdd\u5b58\u3002");
        }
        return true;
    };

    const activateTab = async panelName => {
        tabButtons.forEach(button => {
            button.classList.toggle("is-active", button.dataset.tabPanel === panelName);
        });

        tabPanels.forEach(panel => {
            panel.style.display = panel.dataset.tabContent === panelName ? "" : "none";
        });

        if (panelName === "financeInfo") {
            await loadFinancePanel();
        }

        if (panelName === "documentNumberRule") {
            await loadDocumentRules();
        }

        if (panelName === "documentPrint") {
            await loadDocumentPrintSettings();
        }

        if (panelName === "employeeRecords") {
            await loadEmployeePanel();
        }
    };

    const editButton = document.getElementById("btnCompanyEdit");
    const toolbar = form.querySelector(".company-org-detail-toolbar");
    editButton?.addEventListener("click", async () => {
        form.querySelectorAll("input:not([type='hidden']), textarea").forEach(element => element.removeAttribute("readonly"));
        form.querySelectorAll("select").forEach(element => element.removeAttribute("disabled"));

        const lookupButton = form.querySelector("[data-company-tool='lookup']");
        if (lookupButton) {
            lookupButton.style.display = "";
        }

        if (toolbar) {
            toolbar.querySelectorAll("[data-company-tool]").forEach(button => button.remove());
            editButton.remove();

            const saveButton = document.createElement("button");
            saveButton.type = "submit";
            saveButton.className = "company-org-detail-toolbar__btn";
            saveButton.id = "btnCompanySave";
            saveButton.textContent = "淇濆瓨";
            toolbar.insertBefore(saveButton, toolbar.firstChild);
        }

        form.classList.remove("is-readonly");
        await syncLocationCascade();
        syncFinanceAccess();
        syncDocumentRuleAccess();
        syncDocumentPrintAccess();
    });

    countryRegionSelect?.addEventListener("change", async () => {
        await refreshLocationSelect(citySelect, "CITY", countryRegionSelect.value, countryRegionSelect.value ? "璇烽€夋嫨" : "璇峰厛閫夋嫨鍥藉/鍦板尯", false);
        await refreshLocationSelect(countySelect, "COUNTY", "", "璇峰厛閫夋嫨鍩庡競", false);
    });

    citySelect?.addEventListener("change", async () => {
        await refreshLocationSelect(countySelect, "COUNTY", citySelect.value, citySelect.value ? "璇烽€夋嫨" : "璇峰厛閫夋嫨鍩庡競", false);
    });

    tabButtons.forEach(button => {
        button.addEventListener("click", async () => {
            await activateTab(button.dataset.tabPanel);
        });
    });

    financeSearchButton?.addEventListener("click", async () => {
        bankAccountState.pageNumber = 1;
        await loadFinancePanel();
    });

    financeRefreshButton?.addEventListener("click", async () => {
        if (financeKeyword) financeKeyword.value = "";
        if (financeStatusFilter) financeStatusFilter.value = "";
        bankAccountState.pageNumber = 1;
        await loadFinancePanel();
    });

    financeAddButton?.addEventListener("click", () => {
        if (canEditChildPanels()) {
            showFinanceEditor(null);
        }
    });

    financeDeleteButton?.addEventListener("click", async () => {
        if (canEditChildPanels()) {
            await deleteFinanceRecords();
        }
    });

    financeSaveButton?.addEventListener("click", async () => {
        await saveFinanceRecord();
    });

    financeCancelButton?.addEventListener("click", () => {
        hideFinanceEditor();
    });

    financePageSize?.addEventListener("change", async () => {
        bankAccountState.pageSize = Number(financePageSize.value || 10);
        bankAccountState.pageNumber = 1;
        await loadFinancePanel();
    });

    financePrevPage?.addEventListener("click", async () => {
        if (bankAccountState.pageNumber > 1) {
            bankAccountState.pageNumber -= 1;
            await loadFinancePanel();
        }
    });

    financeNextPage?.addEventListener("click", async () => {
        const totalPages = Math.max(1, Math.ceil(bankAccountState.totalCount / bankAccountState.pageSize));
        if (bankAccountState.pageNumber < totalPages) {
            bankAccountState.pageNumber += 1;
            await loadFinancePanel();
        }
    });

    financePageNumbers?.addEventListener("click", async event => {
        const button = event.target.closest("[data-page-number]");
        if (!button) {
            return;
        }

        bankAccountState.pageNumber = Number(button.dataset.pageNumber || 1);
        await loadFinancePanel();
    });

    financeSelectAll?.addEventListener("change", () => {
        form.querySelectorAll(".company-bank-account-row-selector").forEach(checkbox => {
            if (!checkbox.disabled) {
                checkbox.checked = financeSelectAll.checked;
            }
        });
    });

    employeeSearchButton?.addEventListener("click", async () => {
        employeeState.pageNumber = 1;
        await loadEmployeePanel();
    });

    employeeRefreshButton?.addEventListener("click", async () => {
        if (employeeKeyword) employeeKeyword.value = "";
        if (employeeStatusFilter) employeeStatusFilter.value = "";
        employeeState.pageNumber = 1;
        await loadEmployeePanel();
    });

    employeeKeyword?.addEventListener("keydown", async event => {
        if (event.key !== "Enter") {
            return;
        }

        event.preventDefault();
        employeeState.pageNumber = 1;
        await loadEmployeePanel();
    });

    employeePageSize?.addEventListener("change", async () => {
        employeeState.pageSize = Number(employeePageSize.value || 10);
        employeeState.pageNumber = 1;
        await loadEmployeePanel();
    });

    employeePrevPage?.addEventListener("click", async () => {
        if (employeeState.pageNumber > 1) {
            employeeState.pageNumber -= 1;
            await loadEmployeePanel();
        }
    });

    employeeNextPage?.addEventListener("click", async () => {
        const totalPages = Math.max(1, Math.ceil(employeeState.totalCount / employeeState.pageSize));
        if (employeeState.pageNumber < totalPages) {
            employeeState.pageNumber += 1;
            await loadEmployeePanel();
        }
    });

    employeePageNumbers?.addEventListener("click", async event => {
        const button = event.target.closest("[data-employee-page-number]");
        if (!button) {
            return;
        }

        employeeState.pageNumber = Number(button.dataset.employeePageNumber || 1);
        await loadEmployeePanel();
    });

    employeePhotoClose?.addEventListener("click", () => {
        hideEmployeePhotoModal();
    });

    employeePhotoModal?.addEventListener("click", event => {
        if (event.target === employeePhotoModal) {
            hideEmployeePhotoModal();
        }
    });

    documentRuleRefreshButton?.addEventListener("click", async () => {
        await loadDocumentRules();
    });

    documentRuleSaveButton?.addEventListener("click", async () => {
        await saveDocumentRules();
    });

    documentPrintLogoChooseButton?.addEventListener("click", () => {
        if (canEditChildPanels()) {
            documentPrintLogoFile?.click();
        }
    });

    documentPrintLogoFile?.addEventListener("change", () => {
        const selectedFile = documentPrintLogoFile.files?.[0];
        documentPrintState.selectedFileName = selectedFile?.name || "";
        renderDocumentPrintPanel();
    });

    documentPrintLogoUploadButton?.addEventListener("click", async () => {
        if (canEditChildPanels()) {
            await uploadDocumentPrintLogo();
        }
    });

    documentPrintLogoDeleteButton?.addEventListener("click", async () => {
        if (canEditChildPanels()) {
            await deleteDocumentPrintLogo();
        }
    });

    documentRuleTableBody?.addEventListener("input", event => {
        const input = event.target.closest(".company-doc-rule-input");
        if (!input) {
            return;
        }

        documentRuleState.dirty = true;
        const row = input.closest("tr[data-document-type-code]");
        if (!row) {
            return;
        }

        const prefix = row.querySelector("[data-rule-field='prefix']")?.value?.trim() || "";
        const dateFormatCode = row.querySelector("[data-rule-field='dateFormatCode']")?.value || "yyMM";
        const sequenceLength = Number(row.querySelector("[data-rule-field='sequenceLength']")?.value || 5);
        const sampleCell = row.querySelector("[data-rule-sample]");
        if (sampleCell) {
            sampleCell.textContent = buildDocumentRuleSample(prefix, dateFormatCode, sequenceLength);
        }
    });

    document.addEventListener("click", async event => {
        const trigger = event.target.closest("[data-company-tool], [data-company-bank-account-edit], [data-company-employee-photo]");
        if (!trigger || !form.contains(trigger)) {
            return;
        }

        if (trigger.dataset.companyBankAccountEdit) {
            if (canEditChildPanels()) {
                showFinanceEditor(findFinanceItem(Number(trigger.dataset.companyBankAccountEdit)) || null);
            }
            return;
        }

        if (Object.prototype.hasOwnProperty.call(trigger.dataset, "companyEmployeePhoto")) {
            showEmployeePhotoModal(
                trigger.dataset.companyEmployeePhoto || "",
                trigger.dataset.companyEmployeeName || employeeText.employee);
            return;
        }

        const toolType = trigger.getAttribute("data-company-tool");
        if (toolType === "new") {
            const targetUrl = trigger.getAttribute("data-target-url");
            if (targetUrl) {
                window.location.href = targetUrl;
            }
            return;
        }

        if (toolType === "copy") {
            const organizationId = form.querySelector("input[name='Id']")?.value;
            if (!organizationId) {
                return;
            }

            const copyUrl = new URL(createUrl, window.location.origin);
            copyUrl.searchParams.set("copyFromId", organizationId);
            window.location.href = copyUrl.toString();
            return;
        }

        if (toolType === "lookup") return void window.alert("\u7ec4\u7ec7\u7f16\u53f7\u67e5\u8be2\u529f\u80fd\u540e\u7eed\u53ef\u7ee7\u7eed\u63a5\u5165\u7f16\u53f7\u89c4\u5219\u548c\u5feb\u901f\u68c0\u7d22\u3002");
        if (toolType === "mail") return void window.alert("\u90ae\u4ef6\u529f\u80fd\u5f85\u63a5\u5165\u4f01\u4e1a\u90ae\u7bb1\u6216\u901a\u77e5\u4e2d\u5fc3\u3002");
        if (toolType === "message") return void window.alert("\u4fe1\u606f\u529f\u80fd\u5f85\u63a5\u5165\u5185\u90e8\u6d88\u606f\u6216\u8865\u5145\u8bf4\u660e\u9762\u677f\u3002");
        if (toolType === "tab") return void window.alert(`${trigger.dataset.tabLabel || "\u8be5"}\u529f\u80fd\u540e\u7eed\u7ee7\u7eed\u63a5\u5165\u3002`);
        if (toolType === "archive") {
            const archivePathInput = form.querySelector("[name='ArchivePath']");
            archivePathInput?.focus();
            archivePathInput?.scrollIntoView({ behavior: "smooth", block: "center" });
        }
    });

    let submitAfterRuleSave = false;
    form.addEventListener("submit", async event => {
        if (submitAfterRuleSave || !documentRuleState.dirty || getCompanyOrganizationId() <= 0 || !form.checkValidity()) {
            return;
        }

        event.preventDefault();
        const saved = await saveDocumentRules({ silent: true });
        if (!saved) {
            return;
        }

        submitAfterRuleSave = true;
        HTMLFormElement.prototype.submit.call(form);
    });

    void syncLocationCascade();
    syncFinanceAccess();
    syncEmployeeAccess();
    syncDocumentRuleAccess();
    syncDocumentPrintAccess();
    renderDocumentPrintPanel();
    if (tabButtons.length > 0) {
        void activateTab(tabButtons.find(button => button.classList.contains("is-active"))?.dataset.tabPanel || tabButtons[0].dataset.tabPanel);
    }
})();

