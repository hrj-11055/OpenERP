(() => {
    const materialTable = document.getElementById("materialTable");
    const deleteForm = document.getElementById("materialDeleteForm");
    const deleteIdsContainer = document.getElementById("materialDeleteIds");
    const selectedText = document.getElementById("materialSelectedText");
    const materialDocumentFeatureCode = "BD_ITEM_MASTER";

    // 复用详情弹窗关闭协议，确保弹窗保存后能刷新父页面。
    window.closeMaterialDetailPage = (refreshRequested = false) => {
        if (typeof window.closeEmployeeDetailPage === "function") {
            window.closeEmployeeDetailPage(refreshRequested);
            return;
        }

        window.history.back();
    };

    // 在列表页中以统一模态弹窗打开产品详情。
    const openMaterialPopup = (url, title) => {
        if (!url || url === "#") {
            return;
        }

        if (typeof window.openBlockingPopup === "function") {
            window.openBlockingPopup(url, { title });
            return;
        }

        window.location.href = url;
    };

    // 读取当前勾选的产品资料行。
    const getSelectedMaterialRows = () => {
        if (!materialTable) {
            return [];
        }

        return Array.from(materialTable.querySelectorAll(".material-selector:checked"))
            .map(selector => selector.closest("[data-material-row]"))
            .filter(Boolean);
    };

    // 读取只允许单条操作的产品资料行。
    const getSingleSelectedMaterialRow = actionName => {
        const selectedRows = getSelectedMaterialRows();
        if (selectedRows.length === 0) {
            alert("请先选择一条产品资料。");
            return null;
        }

        if (selectedRows.length > 1) {
            alert(`${actionName}一次只能选择一条产品资料。`);
            return null;
        }

        return selectedRows[0];
    };

    // 同步列表选择状态。
    const syncMaterialSelection = () => {
        if (!materialTable) {
            return;
        }

        const selectedRows = getSelectedMaterialRows();
        materialTable.querySelectorAll("[data-material-row]").forEach(item => {
            const selector = item.querySelector(".material-selector");
            item.classList.toggle("is-selected", selector instanceof HTMLInputElement && selector.checked);
        });
        if (selectedText) {
            selectedText.textContent = `已选择 ${selectedRows.length} 条`;
        }
    };

    // 打开通用文档管理弹窗。
    const openDocumentManager = options => {
        if (!window.openErpDocumentManager?.open) {
            window.alert("档案管理功能尚未载入，请刷新页面后重试。");
            return;
        }

        window.openErpDocumentManager.open(options);
    };

    materialTable?.addEventListener("click", event => {
        const archiveButton = event.target.closest("[data-material-archive]");
        if (archiveButton) {
            event.preventDefault();
            event.stopPropagation();
            openDocumentManager({
                featureCode: materialDocumentFeatureCode,
                entityId: archiveButton.dataset.materialId || "0",
                title: "产品档案管理",
                emptyMessage: "请先保存产品资料，再管理产品档案。"
            });
            return;
        }

        const row = event.target.closest("[data-material-row]");
        if (!row) {
            return;
        }

        if (event.target instanceof HTMLInputElement && event.target.matches(".material-selector")) {
            syncMaterialSelection();
            return;
        }

        materialTable.querySelectorAll("[data-material-row]").forEach(item => item.classList.remove("is-active"));
        row.classList.add("is-active");
        const selector = row.querySelector(".material-selector");
        if (selector instanceof HTMLInputElement) {
            selector.checked = true;
        }
        syncMaterialSelection();

        if (event.target.closest("a")) {
            return;
        }

        const relatedUrl = row.dataset.relatedUrl;
        if (relatedUrl && !(event.target instanceof HTMLInputElement)) {
            window.location.href = relatedUrl;
        }
    });

    document.getElementById("materialEditButton")?.addEventListener("click", () => {
        const selectedRow = getSingleSelectedMaterialRow("修改");
        if (selectedRow?.dataset.editUrl) {
            openMaterialPopup(selectedRow.dataset.editUrl, "编辑产品资料");
        }
    });

    document.getElementById("materialCopyButton")?.addEventListener("click", () => {
        const selectedRow = getSingleSelectedMaterialRow("复制");
        if (selectedRow?.dataset.copyUrl) {
            openMaterialPopup(selectedRow.dataset.copyUrl, "复制产品资料");
        }
    });

    document.getElementById("materialDeleteButton")?.addEventListener("click", () => {
        const selectedRows = getSelectedMaterialRows();
        if (selectedRows.length === 0 || !deleteForm || !deleteIdsContainer) {
            alert("请先选择一条产品资料。");
            return;
        }

        if (!confirm(`确定删除选中的 ${selectedRows.length} 条产品资料吗？`)) {
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

    document.getElementById("materialAdvancedButton")?.addEventListener("click", () => {
        alert("高级查询后续可继续接入更多产品筛选条件。");
    });

    document.getElementById("materialImportButton")?.addEventListener("click", () => {
        alert("导入功能后续可接入产品资料 Excel 模板。");
    });

    const detailForm = document.getElementById("materialDetailForm");
    if (!detailForm) {
        return;
    }

    const tabButtons = Array.from(document.querySelectorAll("[data-material-tab]"));
    const tabPanels = Array.from(document.querySelectorAll("[data-material-panel]"));

    // 查看模式下只保留页签、关闭和档案等工具按钮可用。
    const applyReadOnlyState = () => {
        if (detailForm.dataset.readOnly !== "true") {
            return;
        }

        detailForm.querySelectorAll("input, textarea").forEach(input => {
            if (!(input instanceof HTMLInputElement || input instanceof HTMLTextAreaElement) || input.type === "hidden") {
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
            if (button.matches("[data-readonly-toolbar], [data-material-tab], [data-material-tool='archive']")
                || button.closest(".task-follow-up-panel")) {
                return;
            }

            button.disabled = true;
        });
    };

    // 切换产品详情页签。
    tabButtons.forEach(button => {
        button.addEventListener("click", () => {
            const target = button.dataset.materialTab;
            tabButtons.forEach(item => item.classList.toggle("is-active", item === button));
            tabPanels.forEach(panel => panel.classList.toggle("is-active", panel.dataset.materialPanel === target));
        });
    });

    // 同步动态明细行编号和字段索引。
    const syncRows = (tableId, rowSelector, sequenceSelector, collectionName) => {
        const table = document.getElementById(tableId);
        if (!table) {
            return;
        }

        Array.from(table.querySelectorAll(rowSelector)).forEach((row, index) => {
            const sequenceCell = row.querySelector(sequenceSelector);
            if (sequenceCell) {
                sequenceCell.textContent = String(index + 1);
            }

            row.querySelectorAll("input, select").forEach(input => {
                const name = input.getAttribute("name");
                if (name) {
                    input.setAttribute("name", name.replace(new RegExp(`${collectionName}\\[\\d+\\]`), `${collectionName}[${index}]`));
                }

                const id = input.getAttribute("id");
                if (id) {
                    input.setAttribute("id", id.replace(new RegExp(`${collectionName}_\\d+__`), `${collectionName}_${index}__`));
                }
            });
        });
    };

    // 为指定明细表新增一行。
    const addRow = (tableId, templateId, collectionName, rowSelector, sequenceSelector) => {
        const table = document.getElementById(tableId);
        const template = document.getElementById(templateId);
        if (!(table instanceof HTMLTableElement) || !(template instanceof HTMLTemplateElement)) {
            return;
        }

        const index = table.querySelectorAll(rowSelector).length;
        const html = template.innerHTML.replaceAll("__index__", String(index));
        const holder = document.createElement("template");
        holder.innerHTML = html.trim();
        table.tBodies[0].appendChild(holder.content.firstElementChild);
        syncRows(tableId, rowSelector, sequenceSelector, collectionName);
    };

    // 删除单行或多选明细行。
    const removeRows = (tableId, selectedSelector, removeButtonSelector, rowSelector, deleteFlagSelector, collectionName, sequenceSelector) => {
        const table = document.getElementById(tableId);
        if (!table) {
            return;
        }

        const removeRow = row => {
            const deleteFlag = row.querySelector(deleteFlagSelector);
            const idInput = row.querySelector("input[name$='.Id']");
            if (idInput instanceof HTMLInputElement && idInput.value && deleteFlag instanceof HTMLInputElement) {
                deleteFlag.value = "true";
                row.hidden = true;
                return;
            }

            row.remove();
        };

        table.addEventListener("click", event => {
            const removeButton = event.target.closest(removeButtonSelector);
            if (!removeButton) {
                return;
            }

            const row = removeButton.closest(rowSelector);
            if (row) {
                removeRow(row);
                syncRows(tableId, rowSelector, sequenceSelector, collectionName);
            }
        });

        return () => {
            const selectedRows = Array.from(table.querySelectorAll(selectedSelector))
                .map(checkbox => checkbox.closest(rowSelector))
                .filter(Boolean);
            if (selectedRows.length === 0) {
                alert("请先选择要删除的明细行。");
                return;
            }

            selectedRows.forEach(removeRow);
            syncRows(tableId, rowSelector, sequenceSelector, collectionName);
        };
    };

    document.getElementById("addBomLineButton")?.addEventListener("click", () => addRow("materialBomTable", "materialBomRowTemplate", "BomLines", "[data-bom-row]", ".bom-sequence"));
    document.getElementById("addPriceLineButton")?.addEventListener("click", () => addRow("materialPriceTable", "materialPriceRowTemplate", "PriceLines", "[data-price-row]", ".price-sequence"));
    

    const deleteSelectedBomRows = removeRows("materialBomTable", ".bom-row-selector:checked", "[data-remove-bom]", "[data-bom-row]", ".bom-delete-flag", "BomLines", ".bom-sequence");
    const deleteSelectedPriceRows = removeRows("materialPriceTable", ".price-row-selector:checked", "[data-remove-price]", "[data-price-row]", ".price-delete-flag", "PriceLines", ".price-sequence");
    

    document.getElementById("deleteBomLineButton")?.addEventListener("click", () => deleteSelectedBomRows?.());
    document.getElementById("deletePriceLineButton")?.addEventListener("click", () => deleteSelectedPriceRows?.());
    

    document.addEventListener("click", event => {
        const trigger = event.target.closest("[data-material-tool]");
        if (!trigger || !detailForm.contains(trigger)) {
            return;
        }

        const toolType = trigger.getAttribute("data-material-tool");
        if (toolType === "lookup") {
            alert("产品编号查询功能后续可接入编号规则与快速检索。");
        }

        if (toolType === "dictionary") {
            alert("字典选择后续可接入基础数据。");
        }

        if (toolType === "mail") {
            alert("邮件功能待接入企业邮箱或通知中心。");
        }

        if (toolType === "info") {
            alert("信息功能待接入内部消息或备注流转。");
        }

        if (toolType === "moreImages") {
            alert("更多图片后续将复用通用文档中的图片分类。");
        }

        if (toolType === "archive") {
            openDocumentManager({
                featureCode: detailForm.dataset.materialDocumentFeatureCode || materialDocumentFeatureCode,
                entityId: detailForm.dataset.materialId || "0",
                title: "产品档案管理",
                emptyMessage: "请先保存产品资料，再管理产品档案。"
            });
        }
    });

    syncRows("materialBomTable", "[data-bom-row]", ".bom-sequence", "BomLines");
    syncRows("materialPriceTable", "[data-price-row]", ".price-sequence", "PriceLines");
    applyReadOnlyState();
})();
