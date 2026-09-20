/* 销售报价详情页交互逻辑（页签切换、货品行编辑、自动计算金额、只读模式）。 */

(() => {
    const detailForm = document.getElementById("quotationDetailForm");
    if (!detailForm) return;

    const itemTable = document.getElementById("quotationItemEditTable");
    const itemTemplate = document.getElementById("quotationItemRowTemplate");
    const addItemButton = document.getElementById("addQuotationItemButton");
    const deleteItemButton = document.getElementById("deleteQuotationItemButton");
    const itemKeywordInput = document.getElementById("quotationItemKeyword");

    // 页签切换。
    const tabButtons = Array.from(document.querySelectorAll("[data-quotation-tab]"));
    const tabPanels = Array.from(document.querySelectorAll("[data-quotation-panel]"));

    tabButtons.forEach(button => {
        button.addEventListener("click", () => {
            tabButtons.forEach(item => item.classList.toggle("is-active", item === button));
            tabPanels.forEach(panel => {
                panel.classList.toggle("is-active", panel.dataset.quotationPanel === button.dataset.quotationTab);
            });
        });
    });

    // 只读模式：查看报价单时禁止修改表单内容，保留关闭、编辑等工具按钮。
    const applyReadOnlyState = () => {
        if (detailForm.dataset.readOnly !== "true") return;

        detailForm.querySelectorAll("input, textarea").forEach(input => {
            if (!(input instanceof HTMLInputElement || input instanceof HTMLTextAreaElement) || input.type === "hidden") return;
            if (input.matches("[data-readonly-toolbar]")) return;
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
            if (button.matches("[data-readonly-toolbar]") || button.closest(".task-follow-up-panel")) return;
            button.disabled = true;
        });
    };

    // 新增货品行。
    addItemButton?.addEventListener("click", () => {
        if (!(itemTable instanceof HTMLTableElement) || !(itemTemplate instanceof HTMLTemplateElement)) return;

        const index = itemTable.querySelectorAll("[data-item-row]").length;
        const html = itemTemplate.innerHTML.replaceAll("__index__", String(index));
        const template = document.createElement("template");
        template.innerHTML = html.trim();
        const newRow = template.content.firstElementChild;
        itemTable.tBodies[0].appendChild(newRow);
        syncItemRows();
        bindItemCalculation(newRow);
    });

    // 批量删除货品行。
    deleteItemButton?.addEventListener("click", () => {
        if (!itemTable) return;

        const selectedRows = Array.from(itemTable.querySelectorAll(".item-row-selector:checked"))
            .map(checkbox => checkbox.closest("[data-item-row]"))
            .filter(Boolean);
        if (selectedRows.length === 0) {
            alert("请先选择要删除的货品。");
            return;
        }

        selectedRows.forEach(row => {
            const deleteFlag = row.querySelector(".item-delete-flag");
            const idInput = row.querySelector("input[name$='.Id']");
            if (idInput instanceof HTMLInputElement && idInput.value && deleteFlag instanceof HTMLInputElement) {
                deleteFlag.value = "true";
                row.hidden = true;
            } else {
                row.remove();
            }
        });
        syncItemRows();
    });

    // 单行删除货品行。
    itemTable?.addEventListener("click", event => {
        const removeButton = event.target.closest("[data-remove-item]");
        if (!removeButton) return;

        const row = removeButton.closest("[data-item-row]");
        const deleteFlag = row?.querySelector(".item-delete-flag");
        const idInput = row?.querySelector("input[name$='.Id']");
        if (!row || !(deleteFlag instanceof HTMLInputElement)) return;

        if (idInput instanceof HTMLInputElement && idInput.value) {
            deleteFlag.value = "true";
            row.hidden = true;
        } else {
            row.remove();
        }
        syncItemRows();
    });

    // 重新编号货品行，并修正表单字段索引。
    const syncItemRows = () => {
        if (!itemTable) return;

        Array.from(itemTable.querySelectorAll("[data-item-row]")).forEach((row, index) => {
            const sequenceCell = row.querySelector(".item-sequence");
            if (sequenceCell) {
                sequenceCell.textContent = String(index + 1);
            }

            row.querySelectorAll("input, select").forEach(input => {
                const name = input.getAttribute("name");
                if (name) {
                    input.setAttribute("name", name.replace(/Items\[\d+\]/, `Items[${index}]`));
                }
                const id = input.getAttribute("id");
                if (id) {
                    input.setAttribute("id", id.replace(/Items_\d+__/, `Items_${index}__`));
                }
            });
        });
    };

    // 金额自动计算：数量 x 单价 x 折扣 / 100。
    const calculateRowAmount = row => {
        const qtyInput = row.querySelector("[data-item-quantity]");
        const priceInput = row.querySelector("[data-item-unit-price]");
        const discountInput = row.querySelector("[data-item-discount]");
        const amountInput = row.querySelector("[data-item-amount]");
        if (!qtyInput || !priceInput || !discountInput || !amountInput) return;

        const qty = parseFloat(qtyInput.value) || 0;
        const price = parseFloat(priceInput.value) || 0;
        const discount = parseFloat(discountInput.value) || 0;
        amountInput.value = (qty * price * discount / 100).toFixed(2);
    };

    const bindItemCalculation = row => {
        if (!row) return;
        ["[data-item-quantity]", "[data-item-unit-price]", "[data-item-discount]"].forEach(selector => {
            const input = row.querySelector(selector);
            if (input) {
                input.addEventListener("input", () => calculateRowAmount(row));
            }
        });
    };

    itemTable?.querySelectorAll("[data-item-row]").forEach(row => bindItemCalculation(row));

    // 货品行关键字过滤。
    const filterItemRows = () => {
        if (!itemTable || !(itemKeywordInput instanceof HTMLInputElement)) return;

        const keyword = itemKeywordInput.value.trim().toLowerCase();
        itemTable.querySelectorAll("[data-item-row]").forEach(row => {
            if (!keyword) {
                row.hidden = row.querySelector(".item-delete-flag")?.value === "true";
                return;
            }
            const rowText = Array.from(row.querySelectorAll("input, select"))
                .map(input => input.value || "")
                .join(" ")
                .toLowerCase();
            row.hidden = !rowText.includes(keyword);
        });
    };

    document.querySelector("[data-quotation-tool='filterItems']")?.addEventListener("click", filterItemRows);
    document.querySelector("[data-quotation-tool='resetItems']")?.addEventListener("click", () => {
        if (itemKeywordInput instanceof HTMLInputElement) {
            itemKeywordInput.value = "";
        }
        filterItemRows();
    });

    // 尚未接入后端的工具按钮提示。
    const notImplemented = (toolType, message) => {
        document.querySelectorAll(`[data-quotation-tool='${toolType}']`).forEach(btn => {
            btn.addEventListener("click", () => alert(message));
        });
    };
    notImplemented("print", "打印功能后续可继续扩展。");
    notImplemented("mail", "邮件发送功能后续可继续扩展。");
    notImplemented("info", "报价单信息功能后续可继续扩展。");
    notImplemented("calendar", "日历查看功能后续可继续扩展。");
    notImplemented("void", "作废功能后续可继续扩展。");
    notImplemented("creditTerm", "信贷期限选择功能后续可继续扩展。");
    notImplemented("archive", "档案管理功能后续可继续扩展。");
    notImplemented("customField", "自定义字段配置功能后续可继续扩展。");

    // 弹窗关闭协议。
    window.closeQuotationDetailPage = (refreshRequested = false) => {
        if (typeof window.closeEmployeeDetailPage === "function") {
            window.closeEmployeeDetailPage(refreshRequested);
            return;
        }
        window.history.back();
    };

    syncItemRows();
    applyReadOnlyState();
})();
