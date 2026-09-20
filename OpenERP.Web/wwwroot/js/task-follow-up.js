(() => {
    const panel = document.getElementById("taskFollowUpPanel");
    if (!panel) {
        return;
    }

    const form = panel.closest("form");
    const apiBaseUrl = (panel.dataset.apiBaseUrl || "/api/taskfollowup").replace(/\/$/, "");
    const entityId = panel.dataset.entityId || "0";
    const featureCode = panel.dataset.featureCode || "";
    const documentFeatureCode = panel.dataset.documentFeatureCode || "SYS_TASK_FOLLOW_UP";
    const emptyMessage = panel.dataset.emptyMessage || "请先保存当前资料，再新增任务跟进记录。";
    const state = {
        pageNumber: 1,
        pageSize: 10,
        totalCount: 0,
        keyword: "",
        statusCode: "",
        statusOptions: [],
        taskTypeOptions: [],
        priorityOptions: [],
        items: []
    };

    // 转义接口返回文本，避免动态表格渲染时插入未转义 HTML。
    const escapeHtml = value => String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#39;");

    // 打开通用文档管理弹窗，任务档案与员工资料保持同一能力。
    const openDocumentManager = options => {
        if (!window.openErpDocumentManager?.open) {
            window.alert("档案管理功能尚未载入，请刷新页面后重试。");
            return;
        }

        window.openErpDocumentManager.open(options);
    };

    const isReadOnly = () => form?.classList.contains("is-readonly") || form?.dataset.readOnly === "true";
    const getApiUrl = () => `${apiBaseUrl}?featureCode=${encodeURIComponent(featureCode)}&entityId=${encodeURIComponent(entityId)}&keyword=${encodeURIComponent(state.keyword)}&statusCode=${encodeURIComponent(state.statusCode)}&pageNumber=${encodeURIComponent(state.pageNumber)}&pageSize=${encodeURIComponent(state.pageSize)}`;

    const renderOptionItems = (selectElement, options, includeAll = false) => {
        if (!selectElement) {
            return;
        }

        const previousValue = selectElement.value;
        selectElement.innerHTML = includeAll ? '<option value="">全部</option>' : '<option value="">请选择</option>';
        options.forEach(option => {
            const optionElement = document.createElement("option");
            optionElement.value = option.code;
            optionElement.textContent = option.label;
            selectElement.appendChild(optionElement);
        });
        if ([...selectElement.options].some(option => option.value === previousValue)) {
            selectElement.value = previousValue;
        }
    };

    const getEditorElements = () => ({
        editor: document.getElementById("taskFollowUpEditor"),
        id: document.getElementById("taskFollowUpId"),
        taskCode: document.getElementById("taskFollowUpTaskCode"),
        plannedDate: document.getElementById("taskFollowUpPlannedDate"),
        taskTypeCode: document.getElementById("taskFollowUpTaskTypeCode"),
        executorName: document.getElementById("taskFollowUpExecutorName"),
        statusCode: document.getElementById("taskFollowUpStatusCode"),
        priorityCode: document.getElementById("taskFollowUpPriorityCode"),
        progressPercent: document.getElementById("taskFollowUpProgressPercent"),
        completedDate: document.getElementById("taskFollowUpCompletedDate"),
        projectCode: document.getElementById("taskFollowUpProjectCode"),
        initiatorName: document.getElementById("taskFollowUpInitiatorName"),
        description: document.getElementById("taskFollowUpDescription"),
        archiveButton: document.getElementById("taskFollowUpArchiveButton")
    });

    function syncEditorAccess() {
        const editorElements = getEditorElements();
        ["taskFollowUpAddButton", "taskFollowUpDeleteButton", "taskFollowUpSaveButton"].forEach(id => {
            const button = document.getElementById(id);
            if (button instanceof HTMLButtonElement) {
                button.disabled = isReadOnly();
            }
        });

        Object.values(editorElements).forEach(element => {
            if (!(element instanceof HTMLElement)) {
                return;
            }

            if (element instanceof HTMLInputElement || element instanceof HTMLTextAreaElement || element instanceof HTMLSelectElement) {
                element.disabled = isReadOnly();
            }
        });

        if (editorElements.archiveButton instanceof HTMLButtonElement) {
            const targetEntityId = Number.parseInt(editorElements.archiveButton.dataset.entityId || "0", 10);
            editorElements.archiveButton.disabled = !Number.isInteger(targetEntityId) || targetEntityId <= 0;
            editorElements.archiveButton.title = editorElements.archiveButton.disabled ? "请先保存任务跟进记录，再管理档案。" : "打开档案管理";
        }

        const selectAll = document.getElementById("taskFollowUpSelectAll");
        if (selectAll instanceof HTMLInputElement) {
            selectAll.disabled = isReadOnly();
            if (isReadOnly()) {
                selectAll.checked = false;
            }
        }

        document.querySelectorAll(".task-follow-up-row-selector").forEach(checkbox => {
            if (!(checkbox instanceof HTMLInputElement)) {
                return;
            }

            checkbox.disabled = isReadOnly();
            if (isReadOnly()) {
                checkbox.checked = false;
            }
        });
    }

    const hideEditor = () => {
        const { editor } = getEditorElements();
        if (editor instanceof HTMLElement) {
            editor.hidden = true;
        }
    };

    const showEditor = item => {
        const editorElements = getEditorElements();
        if (!(editorElements.editor instanceof HTMLElement)) {
            return;
        }

        editorElements.editor.hidden = false;
        editorElements.id.value = item?.id ? String(item.id) : "";
        editorElements.taskCode.value = item?.taskCode || "";
        editorElements.plannedDate.value = item?.plannedDate || "";
        editorElements.taskTypeCode.value = item?.taskTypeCode || "TASK";
        editorElements.executorName.value = item?.executorName || "";
        editorElements.statusCode.value = item?.statusCode || "PENDING";
        editorElements.priorityCode.value = item?.priorityCode || "NORMAL";
        editorElements.progressPercent.value = String(Number.isInteger(item?.progressPercent) ? item.progressPercent : 0);
        editorElements.completedDate.value = item?.completedDate || "";
        editorElements.projectCode.value = item?.projectCode || "";
        editorElements.initiatorName.value = item?.initiatorName || "";
        editorElements.description.value = item?.description || "";
        if (editorElements.archiveButton instanceof HTMLButtonElement) {
            editorElements.archiveButton.dataset.entityId = item?.id ? String(item.id) : "";
        }
        syncEditorAccess();
        editorElements.description.focus();
        editorElements.editor.scrollIntoView({ behavior: "smooth", block: "nearest" });
    };

    const renderPagination = () => {
        const pageNumbers = document.getElementById("taskFollowUpPageNumbers");
        const prevButton = document.getElementById("taskFollowUpPrevPage");
        const nextButton = document.getElementById("taskFollowUpNextPage");
        const summary = document.getElementById("taskFollowUpSummary");
        if (!pageNumbers || !prevButton || !nextButton || !summary) {
            return;
        }

        const totalPages = Math.max(1, Math.ceil(state.totalCount / state.pageSize));
        const currentPage = Math.min(state.pageNumber, totalPages);
        state.pageNumber = currentPage;
        summary.textContent = `共 ${state.totalCount} 条，第 ${currentPage} / ${totalPages} 页`;
        prevButton.disabled = currentPage <= 1;
        nextButton.disabled = currentPage >= totalPages;

        pageNumbers.innerHTML = "";
        const startPage = Math.max(1, currentPage - 2);
        const endPage = Math.min(totalPages, startPage + 4);
        for (let page = startPage; page <= endPage; page += 1) {
            const button = document.createElement("button");
            button.type = "button";
            button.className = `task-follow-up-pagebtn${page === currentPage ? " is-active" : ""}`;
            button.textContent = String(page);
            button.dataset.pageNumber = String(page);
            pageNumbers.appendChild(button);
        }
    };

    const renderTable = () => {
        const tbody = document.getElementById("taskFollowUpTableBody");
        const selectAll = document.getElementById("taskFollowUpSelectAll");
        if (!tbody) {
            return;
        }

        if (state.items.length === 0) {
            tbody.innerHTML = '<tr><td colspan="15" class="task-follow-up-table__empty">当前没有任务跟进记录。</td></tr>';
            if (selectAll instanceof HTMLInputElement) {
                selectAll.checked = false;
            }
            renderPagination();
            return;
        }

        tbody.innerHTML = state.items.map(item => `
            <tr data-task-follow-up-id="${item.id}">
                <td class="task-follow-up-table__seq">${item.sequenceNo}</td>
                <td class="task-follow-up-table__checkbox"><input type="checkbox" class="task-follow-up-row-selector" value="${item.id}" ${isReadOnly() ? "disabled" : ""} /></td>
                <td><button type="button" class="task-follow-up-link" data-action="editTaskFollowUp" data-task-follow-up-id="${item.id}">${escapeHtml(item.taskCode)}</button></td>
                <td><button type="button" class="task-follow-up-archive-btn" data-action="viewTaskArchive" data-task-follow-up-id="${item.id}">档案</button></td>
                <td>${escapeHtml(item.statusLabel || "")}</td>
                <td>${escapeHtml(item.plannedDate || "")}</td>
                <td>${escapeHtml(item.taskTypeLabel || "")}</td>
                <td>${escapeHtml(item.executorName || "")}</td>
                <td class="task-follow-up-table__desc">${escapeHtml(item.description || "")}</td>
                <td>${escapeHtml(item.priorityLabel || "")}</td>
                <td>${escapeHtml(item.progressPercent ?? 0)}%</td>
                <td>${escapeHtml(item.completedDate || "")}</td>
                <td>${escapeHtml(item.projectCode || "")}</td>
                <td>${escapeHtml(item.initiatorName || "")}</td>
                <td>${escapeHtml(item.createdAt || "")}</td>
            </tr>
        `).join("");

        if (selectAll instanceof HTMLInputElement) {
            selectAll.checked = false;
        }
        renderPagination();
    };

    const loadPanel = async () => {
        if (entityId === "0") {
            panel.innerHTML = `<div class="account-panel__notice">${escapeHtml(emptyMessage)}</div>`;
            return;
        }

        try {
            const response = await fetch(getApiUrl(), { headers: { "X-Requested-With": "XMLHttpRequest" } });
            const result = await response.json().catch(() => ({}));
            if (!response.ok) {
                panel.innerHTML = `<div class="account-panel__notice">${escapeHtml(result.message || "加载任务跟进数据失败，请稍后重试。")}</div>`;
                return;
            }

            state.totalCount = Number(result.totalCount) || 0;
            state.pageNumber = Number(result.pageNumber) || 1;
            state.pageSize = Number(result.pageSize) || 10;
            state.statusOptions = result.statusOptions || [];
            state.taskTypeOptions = result.taskTypeOptions || [];
            state.priorityOptions = result.priorityOptions || [];
            state.items = result.items || [];

            renderOptionItems(document.getElementById("taskFollowUpStatusFilter"), state.statusOptions, true);
            renderOptionItems(document.getElementById("taskFollowUpTaskTypeCode"), state.taskTypeOptions);
            renderOptionItems(document.getElementById("taskFollowUpStatusCode"), state.statusOptions);
            renderOptionItems(document.getElementById("taskFollowUpPriorityCode"), state.priorityOptions);

            const pageSizeSelect = document.getElementById("taskFollowUpPageSize");
            if (pageSizeSelect instanceof HTMLSelectElement) {
                pageSizeSelect.value = String(state.pageSize);
            }

            renderTable();
            syncEditorAccess();

            const totalPages = Math.max(1, Math.ceil(state.totalCount / state.pageSize));
            if (state.items.length === 0 && state.totalCount > 0 && state.pageNumber > totalPages) {
                state.pageNumber = totalPages;
                await loadPanel();
            }
        } catch {
            panel.innerHTML = '<div class="account-panel__notice">网络请求失败，请稍后重试。</div>';
        }
    };

    const collectPayload = () => {
        const editorElements = getEditorElements();
        const progressPercent = Number.parseInt(editorElements.progressPercent.value || "0", 10);
        if (!editorElements.description.value.trim()) {
            window.alert("任务描述不能为空。");
            editorElements.description.focus();
            return null;
        }
        if (!Number.isInteger(progressPercent) || progressPercent < 0 || progressPercent > 100) {
            window.alert("执行进程必须输入 0 到 100 的整数。");
            editorElements.progressPercent.focus();
            return null;
        }

        return {
            id: editorElements.id.value ? Number.parseInt(editorElements.id.value, 10) : null,
            featureCode,
            entityId: Number.parseInt(entityId, 10),
            taskCode: editorElements.taskCode.value || null,
            archivePath: null,
            statusCode: editorElements.statusCode.value || null,
            plannedDate: editorElements.plannedDate.value || null,
            taskTypeCode: editorElements.taskTypeCode.value || null,
            executorName: editorElements.executorName.value.trim() || null,
            description: editorElements.description.value.trim(),
            priorityCode: editorElements.priorityCode.value || null,
            progressPercent,
            completedDate: editorElements.completedDate.value || null,
            projectCode: editorElements.projectCode.value.trim() || null,
            initiatorName: editorElements.initiatorName.value.trim() || null
        };
    };

    const saveTask = async () => {
        const payload = collectPayload();
        if (!payload) {
            return;
        }

        try {
            const response = await fetch(apiBaseUrl, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "X-Requested-With": "XMLHttpRequest"
                },
                body: JSON.stringify(payload)
            });
            const result = await response.json().catch(() => ({}));
            if (!response.ok) {
                window.alert(result.message || "保存任务跟进失败，请稍后重试。");
                return;
            }

            hideEditor();
            await loadPanel();
            window.alert(result.message || "任务跟进已保存。");
        } catch {
            window.alert("网络请求失败，请稍后重试。");
        }
    };

    const deleteTasks = async () => {
        const selectedIds = [...document.querySelectorAll(".task-follow-up-row-selector:checked")]
            .map(checkbox => Number.parseInt(checkbox.value, 10))
            .filter(Number.isInteger);

        if (selectedIds.length === 0) {
            window.alert("请先勾选要删除的任务跟进记录。");
            return;
        }

        if (!window.confirm(`确定删除已选中的 ${selectedIds.length} 条任务跟进记录吗？`)) {
            return;
        }

        try {
            const response = await fetch(`${apiBaseUrl}/delete`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "X-Requested-With": "XMLHttpRequest"
                },
                body: JSON.stringify({
                    featureCode,
                    entityId: Number.parseInt(entityId, 10),
                    ids: selectedIds
                })
            });
            const result = await response.json().catch(() => ({}));
            if (!response.ok) {
                window.alert(result.message || "删除任务跟进失败，请稍后重试。");
                return;
            }

            const totalPagesAfterDelete = Math.max(1, Math.ceil(Math.max(0, state.totalCount - selectedIds.length) / state.pageSize));
            state.pageNumber = Math.min(state.pageNumber, totalPagesAfterDelete);
            await loadPanel();
            hideEditor();
            window.alert(result.message || "任务跟进已删除。");
        } catch {
            window.alert("网络请求失败，请稍后重试。");
        }
    };

    const findItem = taskFollowUpId => state.items.find(item => Number(item.id) === Number(taskFollowUpId));

    document.getElementById("taskFollowUpSearchButton")?.addEventListener("click", async () => {
        state.keyword = document.getElementById("taskFollowUpKeyword")?.value?.trim() || "";
        state.statusCode = document.getElementById("taskFollowUpStatusFilter")?.value || "";
        state.pageNumber = 1;
        await loadPanel();
    });

    document.getElementById("taskFollowUpRefreshButton")?.addEventListener("click", async () => {
        const keywordInput = document.getElementById("taskFollowUpKeyword");
        const statusFilter = document.getElementById("taskFollowUpStatusFilter");
        if (keywordInput instanceof HTMLInputElement) {
            keywordInput.value = "";
        }
        if (statusFilter instanceof HTMLSelectElement) {
            statusFilter.value = "";
        }
        state.keyword = "";
        state.statusCode = "";
        state.pageNumber = 1;
        hideEditor();
        await loadPanel();
    });

    document.getElementById("taskFollowUpAddButton")?.addEventListener("click", () => showEditor(null));
    document.getElementById("taskFollowUpDeleteButton")?.addEventListener("click", deleteTasks);
    document.getElementById("taskFollowUpSaveButton")?.addEventListener("click", saveTask);
    document.getElementById("taskFollowUpCancelButton")?.addEventListener("click", hideEditor);

    document.getElementById("taskFollowUpKeyword")?.addEventListener("keydown", event => {
        if (event.key === "Enter") {
            event.preventDefault();
            document.getElementById("taskFollowUpSearchButton")?.click();
        }
    });

    document.getElementById("taskFollowUpPageSize")?.addEventListener("change", async event => {
        const nextPageSize = Number.parseInt(event.target.value, 10);
        state.pageSize = Number.isInteger(nextPageSize) ? nextPageSize : 10;
        state.pageNumber = 1;
        await loadPanel();
    });

    document.getElementById("taskFollowUpPrevPage")?.addEventListener("click", async () => {
        if (state.pageNumber <= 1) {
            return;
        }

        state.pageNumber -= 1;
        await loadPanel();
    });

    document.getElementById("taskFollowUpNextPage")?.addEventListener("click", async () => {
        const totalPages = Math.max(1, Math.ceil(state.totalCount / state.pageSize));
        if (state.pageNumber >= totalPages) {
            return;
        }

        state.pageNumber += 1;
        await loadPanel();
    });

    document.getElementById("taskFollowUpPageNumbers")?.addEventListener("click", async event => {
        const button = event.target.closest("[data-page-number]");
        if (!button) {
            return;
        }

        state.pageNumber = Number.parseInt(button.dataset.pageNumber, 10) || 1;
        await loadPanel();
    });

    document.getElementById("taskFollowUpSelectAll")?.addEventListener("change", event => {
        document.querySelectorAll(".task-follow-up-row-selector").forEach(checkbox => {
            checkbox.checked = event.target.checked;
        });
    });

    document.getElementById("taskFollowUpTableBody")?.addEventListener("click", event => {
        const actionTrigger = event.target.closest("[data-action]");
        if (!actionTrigger) {
            return;
        }

        const action = actionTrigger.dataset.action;
        if (action === "editTaskFollowUp") {
            const taskItem = findItem(actionTrigger.dataset.taskFollowUpId);
            if (taskItem) {
                showEditor(taskItem);
            }
        }
        if (action === "viewTaskArchive") {
            openDocumentManager({
                featureCode: documentFeatureCode,
                entityId: actionTrigger.dataset.taskFollowUpId || "0",
                title: "任务跟进档案管理",
                emptyMessage: "请先保存任务跟进记录，再管理档案。"
            });
        }
    });

    document.getElementById("taskFollowUpArchiveButton")?.addEventListener("click", event => {
        const button = event.currentTarget;
        openDocumentManager({
            featureCode: documentFeatureCode,
            entityId: button?.dataset?.entityId || "0",
            title: "任务跟进档案管理",
            emptyMessage: "请先保存任务跟进记录，再管理档案。"
        });
    });

    loadPanel();
})();
