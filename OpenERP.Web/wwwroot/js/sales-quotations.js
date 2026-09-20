(() => {
    const quotationTable = document.getElementById("quotationTable");
    const deleteForm = document.getElementById("quotationDeleteForm");
    const deleteIdsContainer = document.getElementById("quotationDeleteIds");
    const selectedText = document.getElementById("quotationSelectedText");

    // 以统一模态弹窗打开报价详情。
    const openQuotationPopup = (url, title) => {
        if (!url || url === "#") return;
        if (typeof window.openBlockingPopup === "function") {
            window.openBlockingPopup(url, { title, dialogClass: "page-popup-lock__dialog--sales-quote" });
            return;
        }
        window.open(url, "_blank", "width=1520,height=820,scrollbars=yes,resizable=yes");
    };

    // 读取当前勾选的报价行集合。
    const getSelectedQuotationRows = () => {
        if (!quotationTable) return [];
        return Array.from(quotationTable.querySelectorAll(".quotation-selector:checked"))
            .map(selector => selector.closest("[data-quotation-row]"))
            .filter(Boolean);
    };

    // 读取只允许单条操作的报价行。
    const getSingleSelectedQuotationRow = actionName => {
        const selectedRows = getSelectedQuotationRows();
        if (selectedRows.length === 0) {
            alert("请先选择一条报价记录。");
            return null;
        }
        if (selectedRows.length > 1) {
            alert(`${actionName}一次只能选择一条报价记录。`);
            return null;
        }
        return selectedRows[0];
    };

    // 同步报价列表页多选状态。
    const syncQuotationSelection = () => {
        if (!quotationTable) return;
        const selectedRows = getSelectedQuotationRows();
        quotationTable.querySelectorAll("[data-quotation-row]").forEach(item => {
            const selector = item.querySelector(".quotation-selector");
            item.classList.toggle("is-selected", selector instanceof HTMLInputElement && selector.checked);
        });
        if (selectedText) {
            selectedText.textContent = `已选择 ${selectedRows.length} 条`;
        }
    };

    // 切换当前查看的报价行（激活高亮）。
    const activateQuotationRow = row => {
        if (!quotationTable || !row) return;
        quotationTable.querySelectorAll("[data-quotation-row]").forEach(item => item.classList.remove("is-active"));
        row.classList.add("is-active");
    };

    // 点击报价行：勾选 → 激活 → 跳转加载下方明细。
    quotationTable?.addEventListener("click", event => {
        // 点击报价单号链接：打开详情弹窗。
        const codeLink = event.target.closest("[data-popup-url]");
        if (codeLink && codeLink.closest("[data-quotation-row]")) {
            event.preventDefault();
            openQuotationPopup(codeLink.dataset.popupUrl, codeLink.dataset.popupTitle || "查看销售报价");
            return;
        }

        const row = event.target.closest("[data-quotation-row]");
        if (!row) return;

        if (event.target instanceof HTMLInputElement && event.target.matches(".quotation-selector")) {
            syncQuotationSelection();
            return;
        }

        activateQuotationRow(row);
        const selector = row.querySelector(".quotation-selector");
        if (selector instanceof HTMLInputElement) {
            selector.checked = true;
        }
        syncQuotationSelection();

        // 点击非链接/按钮区域时跳转，刷新下方明细。
        if (!event.target.closest("a") && !event.target.closest("button")) {
            const activateUrl = row.dataset.activateUrl;
            if (activateUrl) {
                window.location.href = activateUrl;
            }
        }
    });

    // 新增按钮：打开新增弹窗。
    const addButton = document.getElementById("quotationAddButton");
    if (addButton) {
        addButton.addEventListener("click", () => {
            const url = addButton.dataset.popupUrl;
            if (url) {
                openQuotationPopup(url, addButton.dataset.popupTitle || "新增销售报价");
            }
        });
    }

    // 修改按钮：打开选中行的编辑弹窗。
    document.getElementById("quotationEditButton")?.addEventListener("click", () => {
        const selectedRow = getSingleSelectedQuotationRow("修改");
        if (!selectedRow?.dataset.editUrl) return;
        openQuotationPopup(selectedRow.dataset.editUrl, "编辑销售报价");
    });

    // 复制按钮：打开选中行的复制弹窗。
    document.getElementById("quotationCopyButton")?.addEventListener("click", () => {
        const selectedRow = getSingleSelectedQuotationRow("复制");
        if (!selectedRow?.dataset.copyUrl) return;
        openQuotationPopup(selectedRow.dataset.copyUrl, "复制销售报价");
    });

    // 删除按钮：收集选中ID并提交表单。
    document.getElementById("quotationDeleteButton")?.addEventListener("click", () => {
        const selectedRows = getSelectedQuotationRows();
        if (selectedRows.length === 0 || !deleteForm || !deleteIdsContainer) {
            alert("请先选择一条报价记录。");
            return;
        }
        if (!confirm(`确定删除选中的 ${selectedRows.length} 条报价记录吗？`)) return;

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

    // 未实现的操作按钮给出提示。
    const notImplemented = (id, message) => {
        document.getElementById(id)?.addEventListener("click", () => alert(message));
    };
    notImplemented("quotationPrintButton", "打印功能后续可继续扩展。");
    notImplemented("quotationExportButton", "导出功能后续可继续扩展。");
    notImplemented("quotationGenerateOrderButton", "生成销售订单功能后续可继续扩展。");
    notImplemented("quotationAdvancedButton", "高级查询功能后续可继续扩展。");

    // ===== 列表页下方页签切换 =====
    const tabButtons = Array.from(document.querySelectorAll("[data-quotation-tab]"));
    const tabPanels = Array.from(document.querySelectorAll("[data-quotation-panel]"));

    tabButtons.forEach(button => {
        button.addEventListener("click", () => {
            tabButtons.forEach(item => item.classList.toggle("is-active", item === button));
            tabPanels.forEach(panel => panel.classList.toggle("is-active", panel.dataset.quotationPanel === button.dataset.quotationTab));
        });
    });
})();
