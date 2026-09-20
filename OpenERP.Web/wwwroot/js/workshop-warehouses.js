(() => {
    const warehouseTable = document.getElementById("warehouseTable");
    const deleteForm = document.getElementById("warehouseDeleteForm");
    const deleteIdsContainer = document.getElementById("warehouseDeleteIds");
    const selectedText = document.getElementById("warehouseSelectedText");

    // 复用员工详情页的弹窗关闭协议，保持各业务详情弹窗行为一致。
    window.closeWorkshopWarehouseDetailPage = (refreshRequested = false) => {
        if (typeof window.closeEmployeeDetailPage === "function") {
            window.closeEmployeeDetailPage(refreshRequested);
            return;
        }

        window.history.back();
    };

    // 在列表页中以统一模态弹窗打开车间仓库详情。
    const openWarehousePopup = (url, title) => {
        if (!url || url === "#") {
            return;
        }

        if (typeof window.openBlockingPopup === "function") {
            window.openBlockingPopup(url, { title });
            return;
        }

        window.location.href = url;
    };

    // 读取列表页当前勾选的仓库行集合。
    const getSelectedWarehouseRows = () => {
        if (!warehouseTable) {
            return [];
        }

        return Array.from(warehouseTable.querySelectorAll(".warehouse-selector:checked"))
            .map(selector => selector.closest("[data-warehouse-row]"))
            .filter(Boolean);
    };

    // 读取只允许单条操作的仓库行。
    const getSingleSelectedWarehouseRow = actionName => {
        const selectedRows = getSelectedWarehouseRows();
        if (selectedRows.length === 0) {
            alert("请先选择一条仓库资料。");
            return null;
        }

        if (selectedRows.length > 1) {
            alert(`${actionName}一次只能选择一条仓库资料。`);
            return null;
        }

        return selectedRows[0];
    };

    // 同步列表页多选状态。
    const syncWarehouseSelection = () => {
        if (!warehouseTable) {
            return;
        }

        const selectedRows = getSelectedWarehouseRows();
        warehouseTable.querySelectorAll("[data-warehouse-row]").forEach(item => {
            const selector = item.querySelector(".warehouse-selector");
            item.classList.toggle("is-selected", selector instanceof HTMLInputElement && selector.checked);
        });
        if (selectedText) {
            selectedText.textContent = `已选择 ${selectedRows.length} 条`;
        }
    };

    // 切换车间仓库主从列表当前查看的仓库行。
    const activateWarehouseRow = row => {
        if (!warehouseTable || !row) {
            return;
        }

        warehouseTable.querySelectorAll("[data-warehouse-row]").forEach(item => item.classList.remove("is-active"));
        row.classList.add("is-active");
    };

    warehouseTable?.addEventListener("click", event => {
        const row = event.target.closest("[data-warehouse-row]");
        if (!row) {
            return;
        }

        if (event.target instanceof HTMLInputElement && event.target.matches(".warehouse-selector")) {
            syncWarehouseSelection();
            return;
        }

        activateWarehouseRow(row);
        const selector = row.querySelector(".warehouse-selector");
        if (selector instanceof HTMLInputElement) {
            selector.checked = true;
        }
        syncWarehouseSelection();

        if (event.target.closest("a")) {
            return;
        }

        const locationsUrl = row.dataset.locationsUrl;
        if (locationsUrl && !(event.target instanceof HTMLInputElement)) {
            window.location.href = locationsUrl;
        }
    });

    document.getElementById("warehouseEditButton")?.addEventListener("click", () => {
        const selectedRow = getSingleSelectedWarehouseRow("修改");
        if (!selectedRow?.dataset.editUrl) {
            return;
        }

        openWarehousePopup(selectedRow.dataset.editUrl, "编辑车间仓库");
    });

    document.getElementById("warehouseCopyButton")?.addEventListener("click", () => {
        const selectedRow = getSingleSelectedWarehouseRow("复制");
        if (!selectedRow?.dataset.copyUrl) {
            return;
        }

        openWarehousePopup(selectedRow.dataset.copyUrl, "复制车间仓库");
    });

    document.getElementById("warehouseDeleteButton")?.addEventListener("click", () => {
        const selectedRows = getSelectedWarehouseRows();
        if (selectedRows.length === 0 || !deleteForm || !deleteIdsContainer) {
            alert("请先选择一条仓库资料。");
            return;
        }

        if (!confirm(`确定删除选中的 ${selectedRows.length} 条仓库资料及其库位记录吗？`)) {
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

    const tabButtons = Array.from(document.querySelectorAll("[data-workshop-tab]"));
    const tabPanels = Array.from(document.querySelectorAll("[data-workshop-panel]"));

    // 切换详情页基本信息和库位记录页签。
    tabButtons.forEach(button => {
        button.addEventListener("click", () => {
            const target = button.dataset.workshopTab;
            tabButtons.forEach(item => item.classList.toggle("is-active", item === button));
            tabPanels.forEach(panel => panel.classList.toggle("is-active", panel.dataset.workshopPanel === target));
        });
    });

    const locationTable = document.getElementById("warehouseLocationEditTable");
    const locationTemplate = document.getElementById("warehouseLocationRowTemplate");
    const addLocationButton = document.getElementById("addWarehouseLocationButton");

    // 重新编号库位明细行并修正表单字段索引。
    const syncLocationRows = () => {
        if (!locationTable) {
            return;
        }

        Array.from(locationTable.querySelectorAll("[data-location-row]")).forEach((row, index) => {
            const sequenceCell = row.querySelector(".location-sequence");
            if (sequenceCell) {
                sequenceCell.textContent = String(index + 1);
            }

            row.querySelectorAll("input, select").forEach(input => {
                const name = input.getAttribute("name");
                if (name) {
                    input.setAttribute("name", name.replace(/Locations\[\d+\]/, `Locations[${index}]`));
                }

                const id = input.getAttribute("id");
                if (id) {
                    input.setAttribute("id", id.replace(/Locations_\d+__/, `Locations_${index}__`));
                }
            });
        });
    };

    addLocationButton?.addEventListener("click", () => {
        if (!(locationTable instanceof HTMLTableElement) || !(locationTemplate instanceof HTMLTemplateElement)) {
            return;
        }

        const index = locationTable.querySelectorAll("[data-location-row]").length;
        const html = locationTemplate.innerHTML.replaceAll("__index__", String(index));
        const template = document.createElement("template");
        template.innerHTML = html.trim();
        locationTable.tBodies[0].appendChild(template.content.firstElementChild);
        syncLocationRows();
    });

    locationTable?.addEventListener("click", event => {
        const removeButton = event.target.closest("[data-remove-location]");
        if (!removeButton) {
            return;
        }

        const row = removeButton.closest("[data-location-row]");
        const deleteFlag = row?.querySelector(".location-delete-flag");
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

        syncLocationRows();
    });

    syncLocationRows();
})();
