(() => {
    const customerTable = document.getElementById("customerTable");
    const deleteForm = document.getElementById("customerDeleteForm");
    const deleteIdsContainer = document.getElementById("customerDeleteIds");
    const selectedText = document.getElementById("customerSelectedText");
    const customerDocumentFeatureCode = "CRM_CUSTOMER";

    // 复用员工详情页的弹窗关闭协议，保持客户详情弹窗与其他详情页一致。
    window.closeCustomerDetailPage = (refreshRequested = false) => {
        if (typeof window.closeEmployeeDetailPage === "function") {
            window.closeEmployeeDetailPage(refreshRequested);
            return;
        }

        window.history.back();
    };

    // 在列表页中以统一模态弹窗打开客户详情。
    const openCustomerPopup = (url, title) => {
        if (!url || url === "#") {
            return;
        }

        if (typeof window.openBlockingPopup === "function") {
            window.openBlockingPopup(url, { title });
            return;
        }

        window.location.href = url;
    };

    // 读取客户列表页当前勾选的客户行集合。
    const getSelectedCustomerRows = () => {
        if (!customerTable) {
            return [];
        }

        return Array.from(customerTable.querySelectorAll(".customer-selector:checked"))
            .map(selector => selector.closest("[data-customer-row]"))
            .filter(Boolean);
    };

    // 读取只允许单条操作的客户行。
    const getSingleSelectedCustomerRow = actionName => {
        const selectedRows = getSelectedCustomerRows();
        if (selectedRows.length === 0) {
            alert("请先选择一条客户资料。");
            return null;
        }

        if (selectedRows.length > 1) {
            alert(`${actionName}一次只能选择一条客户资料。`);
            return null;
        }

        return selectedRows[0];
    };

    // 同步客户列表页多选状态。
    const syncCustomerSelection = () => {
        if (!customerTable) {
            return;
        }

        const selectedRows = getSelectedCustomerRows();
        customerTable.querySelectorAll("[data-customer-row]").forEach(item => {
            const selector = item.querySelector(".customer-selector");
            item.classList.toggle("is-selected", selector instanceof HTMLInputElement && selector.checked);
        });
        if (selectedText) {
            selectedText.textContent = `已选择 ${selectedRows.length} 条`;
        }
    };

    // 切换客户主从列表当前查看的客户行。
    const activateCustomerRow = row => {
        if (!customerTable || !row) {
            return;
        }

        customerTable.querySelectorAll("[data-customer-row]").forEach(item => item.classList.remove("is-active"));
        row.classList.add("is-active");
    };

    // 打开通用文档管理弹窗。
    const openDocumentManager = options => {
        if (!window.openErpDocumentManager?.open) {
            window.alert("档案管理功能尚未载入，请刷新页面后重试。");
            return;
        }

        window.openErpDocumentManager.open(options);
    };

    customerTable?.addEventListener("click", event => {
        const archiveButton = event.target.closest("[data-customer-archive]");
        if (archiveButton) {
            event.preventDefault();
            event.stopPropagation();
            openDocumentManager({
                featureCode: customerDocumentFeatureCode,
                entityId: archiveButton.dataset.customerId || "0",
                title: "客户档案管理",
                emptyMessage: "请先保存客户资料，再管理客户档案。"
            });
            return;
        }

        const row = event.target.closest("[data-customer-row]");
        if (!row) {
            return;
        }

        if (event.target instanceof HTMLInputElement && event.target.matches(".customer-selector")) {
            syncCustomerSelection();
            return;
        }

        activateCustomerRow(row);
        const selector = row.querySelector(".customer-selector");
        if (selector instanceof HTMLInputElement) {
            selector.checked = true;
        }
        syncCustomerSelection();

        if (event.target.closest("a")) {
            return;
        }

        const contactsUrl = row.dataset.contactsUrl;
        if (contactsUrl && !(event.target instanceof HTMLInputElement)) {
            window.location.href = contactsUrl;
        }
    });

    document.getElementById("customerEditButton")?.addEventListener("click", () => {
        const selectedRow = getSingleSelectedCustomerRow("修改");
        if (!selectedRow?.dataset.editUrl) {
            return;
        }

        openCustomerPopup(selectedRow.dataset.editUrl, "编辑客户/友商资料");
    });

    document.getElementById("customerCopyButton")?.addEventListener("click", () => {
        const selectedRow = getSingleSelectedCustomerRow("复制");
        if (!selectedRow?.dataset.copyUrl) {
            return;
        }

        openCustomerPopup(selectedRow.dataset.copyUrl, "复制客户/友商资料");
    });

    document.getElementById("customerDeleteButton")?.addEventListener("click", () => {
        const selectedRows = getSelectedCustomerRows();
        if (selectedRows.length === 0 || !deleteForm || !deleteIdsContainer) {
            alert("请先选择一条客户资料。");
            return;
        }

        if (!confirm(`确定删除选中的 ${selectedRows.length} 条客户资料及其联络人记录吗？`)) {
            return;
        }

        deleteIdsContainer.innerHTML = "";
        selectedRows.forEach(row => {
            const idInput = document.createElement("input");
            idInput.type = "hidden";
            idInput.name = "ids";
            idInput.value = row.dataset.id || "";
            deleteIdsContainer.appendChild(idInput);
        });
        deleteForm.submit();
    });

    document.getElementById("customerAdvancedButton")?.addEventListener("click", () => {
        alert("高级查询后续可继续接入更多客户筛选条件。");
    });

    document.getElementById("customerImportButton")?.addEventListener("click", () => {
        alert("导入功能后续可接入客户资料 Excel 模板。");
    });

    const detailForm = document.getElementById("customerDetailForm");
    if (!detailForm) {
        return;
    }

    const tabButtons = Array.from(document.querySelectorAll("[data-customer-tab]"));
    const tabPanels = Array.from(document.querySelectorAll("[data-customer-panel]"));
    const contactTable = document.getElementById("customerContactEditTable");
    const contactTemplate = document.getElementById("customerContactRowTemplate");
    const addContactButton = document.getElementById("addCustomerContactButton");
    const deleteContactButton = document.getElementById("deleteCustomerContactButton");
    const contactKeywordInput = document.getElementById("customerContactKeyword");

    // 查看模式下只保留页签、关闭和顶部工具按钮可用，业务字段不可编辑。
    const applyReadOnlyState = () => {
        if (detailForm.dataset.readOnly !== "true") {
            return;
        }

        detailForm.querySelectorAll("input, textarea").forEach(input => {
            if (!(input instanceof HTMLInputElement || input instanceof HTMLTextAreaElement) || input.type === "hidden") {
                return;
            }

            if (input.matches("[data-readonly-toolbar]")) {
                return;
            }

            if (input.type === "checkbox" || input.type === "radio") {
                input.disabled = true;
                return;
            }

            input.readOnly = true;
        });

        detailForm.querySelectorAll("select").forEach(select => {
            select.disabled = true;
        });

        detailForm.querySelectorAll("button").forEach(button => {
            if (button.matches("[data-readonly-toolbar], [data-customer-tab], [data-customer-tool='archive']")
                || button.closest(".task-follow-up-panel")) {
                return;
            }

            button.disabled = true;
        });
    };

    // 切换客户详情页页签。
    tabButtons.forEach(button => {
        button.addEventListener("click", () => {
            const target = button.dataset.customerTab;
            if (target === "inspection") {
                alert("该页签后续可继续扩展。");
            }

            tabButtons.forEach(item => item.classList.toggle("is-active", item === button));
            tabPanels.forEach(panel => panel.classList.toggle("is-active", panel.dataset.customerPanel === target));
        });
    });

    // 重新编号联络人明细行并修正表单字段索引。
    const syncContactRows = () => {
        if (!contactTable) {
            return;
        }

        Array.from(contactTable.querySelectorAll("[data-contact-row]")).forEach((row, index) => {
            const sequenceCell = row.querySelector(".contact-sequence");
            if (sequenceCell) {
                sequenceCell.textContent = String(index + 1);
            }

            row.querySelectorAll("input, select").forEach(input => {
                const name = input.getAttribute("name");
                if (name) {
                    input.setAttribute("name", name.replace(/Contacts\[\d+\]/, `Contacts[${index}]`));
                }

                const id = input.getAttribute("id");
                if (id) {
                    input.setAttribute("id", id.replace(/Contacts_\d+__/, `Contacts_${index}__`));
                }
            });
        });
    };

    // 保证同一客户只勾选一个默认联络人。
    const syncDefaultContact = changedCheckbox => {
        if (!(changedCheckbox instanceof HTMLInputElement) || !changedCheckbox.checked || !contactTable) {
            return;
        }

        contactTable.querySelectorAll(".contact-default-selector").forEach(checkbox => {
            if (checkbox !== changedCheckbox && checkbox instanceof HTMLInputElement) {
                checkbox.checked = false;
            }
        });
    };

    addContactButton?.addEventListener("click", () => {
        if (!(contactTable instanceof HTMLTableElement) || !(contactTemplate instanceof HTMLTemplateElement)) {
            return;
        }

        const index = contactTable.querySelectorAll("[data-contact-row]").length;
        const html = contactTemplate.innerHTML.replaceAll("__index__", String(index));
        const template = document.createElement("template");
        template.innerHTML = html.trim();
        contactTable.tBodies[0].appendChild(template.content.firstElementChild);
        syncContactRows();
    });

    contactTable?.addEventListener("change", event => {
        if (event.target.matches(".contact-default-selector")) {
            syncDefaultContact(event.target);
        }
    });

    contactTable?.addEventListener("click", event => {
        const removeButton = event.target.closest("[data-remove-contact]");
        if (!removeButton) {
            return;
        }

        const row = removeButton.closest("[data-contact-row]");
        const deleteFlag = row?.querySelector(".contact-delete-flag");
        const idInput = row?.querySelector("input[name$='.Id']");
        if (!row || !(deleteFlag instanceof HTMLInputElement)) {
            return;
        }

        if (idInput instanceof HTMLInputElement && idInput.value) {
            deleteFlag.value = "true";
            row.hidden = true;
        } else {
            row.remove();
        }

        syncContactRows();
    });

    deleteContactButton?.addEventListener("click", () => {
        if (!contactTable) {
            return;
        }

        const selectedRows = Array.from(contactTable.querySelectorAll(".contact-row-selector:checked"))
            .map(checkbox => checkbox.closest("[data-contact-row]"))
            .filter(Boolean);
        if (selectedRows.length === 0) {
            alert("请先选择要删除的联络人。");
            return;
        }

        selectedRows.forEach(row => {
            const deleteFlag = row.querySelector(".contact-delete-flag");
            const idInput = row.querySelector("input[name$='.Id']");
            if (idInput instanceof HTMLInputElement && idInput.value && deleteFlag instanceof HTMLInputElement) {
                deleteFlag.value = "true";
                row.hidden = true;
            } else {
                row.remove();
            }
        });
        syncContactRows();
    });

    // 根据关键字在详情页本地过滤联络人编辑行。
    const filterContactRows = () => {
        if (!contactTable || !(contactKeywordInput instanceof HTMLInputElement)) {
            return;
        }

        const keyword = contactKeywordInput.value.trim().toLowerCase();
        contactTable.querySelectorAll("[data-contact-row]").forEach(row => {
            if (!keyword) {
                row.hidden = row.querySelector(".contact-delete-flag")?.value === "true";
                return;
            }

            const rowText = Array.from(row.querySelectorAll("input, select"))
                .map(input => input.value || "")
                .join(" ")
                .toLowerCase();
            row.hidden = !rowText.includes(keyword);
        });
    };

    document.querySelector("[data-customer-tool='filterContacts']")?.addEventListener("click", filterContactRows);
    document.querySelector("[data-customer-tool='resetContacts']")?.addEventListener("click", () => {
        if (contactKeywordInput instanceof HTMLInputElement) {
            contactKeywordInput.value = "";
        }
        filterContactRows();
    });

    document.addEventListener("click", event => {
        const trigger = event.target.closest("[data-customer-tool]");
        if (!trigger || !detailForm.contains(trigger)) {
            return;
        }

        const toolType = trigger.getAttribute("data-customer-tool");
        if (toolType === "lookup") {
            alert("客户编号查询功能后续可接入编号规则与快速检索。");
        }

        if (toolType === "mail") {
            alert("邮件功能待接入企业邮箱或通知中心。");
        }

        if (toolType === "info") {
            alert("信息功能待接入内部消息或备注流转。");
        }

        if (toolType === "archive") {
            openDocumentManager({
                featureCode: detailForm.dataset.customerDocumentFeatureCode || customerDocumentFeatureCode,
                entityId: detailForm.dataset.customerId || "0",
                title: "客户档案管理",
                emptyMessage: "请先保存客户基本资料，再管理客户档案。"
            });
        }
    });

    syncContactRows();
    applyReadOnlyState();
})();
