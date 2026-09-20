(() => {
    const form = document.getElementById('employeeDetailForm');
    if (!form) return;

    const customOptionTriggers = [...form.querySelectorAll('[data-custom-option-trigger]')];
    const locationOptionsUrl = form.dataset.locationOptionsUrl || '';
    const accountApiBaseUrl = (form.dataset.accountApiBaseUrl || '/api/employeeaccount').replace(/\/$/, '');
    const trainingExperienceApiBaseUrl = (form.dataset.trainingExperienceApiBaseUrl || '/api/employeetrainingexperience').replace(/\/$/, '');
    const taskFollowUpApiBaseUrl = (form.dataset.taskFollowUpApiBaseUrl || '/api/taskfollowup').replace(/\/$/, '');
    const countryRegionSelect = form.querySelector("[name='CountryRegionId']");
    const citySelect = form.querySelector("[name='CityId']");
    const countySelect = form.querySelector("[name='CountyId']");
    const btnEdit = document.getElementById('btnEdit');
    const toolbar = form.querySelector('.employee-detail-toolbar');
    const leaveDateInput = form.querySelector('[data-employee-leave-date]');
    const statusCard = form.querySelector('[data-employee-status-card]');
    const statusText = form.querySelector('[data-employee-status-text]');
    const tabButtons = form.querySelectorAll('.employee-detail-tabs__item[data-tab-panel]');
    const tabContents = form.querySelectorAll('[data-tab-content]');
    const accountPanel = document.getElementById('accountPermissionPanel');
    const employeeId = accountPanel?.dataset.employeeId || '0';
    const trainingExperiencePanel = document.getElementById('trainingExperiencePanel');
    const trainingExperienceEmployeeId = trainingExperiencePanel?.dataset.employeeId || '0';
    const taskFollowUpPanel = document.getElementById('taskFollowUpPanel');
    const taskFollowUpEntityId = taskFollowUpPanel?.dataset.entityId || '0';
    const taskFollowUpFeatureCode = taskFollowUpPanel?.dataset.featureCode || form.dataset.taskFollowUpFeatureCode || '';
    // 任务跟进档案文档功能编码（对应单条任务跟进记录的通用文档集合）。
    const taskFollowUpDocumentFeatureCode = taskFollowUpPanel?.dataset.documentFeatureCode || 'HR_TASK_FOLLOW_UP';
    const employeePhotoFrame = form.querySelector('[data-employee-photo-frame]');
    const employeePhotoPathInput = form.querySelector("[name='PhotoPath']");
    // 员工档案文档功能编码（对应员工主档案的通用文档集合）。
    const employeeDocumentFeatureCode = form.dataset.employeeDocumentFeatureCode || 'HR_EMPLOYEE';
    // 员工图片文档功能编码（对应员工图片管理页上传的图片集合）。
    const employeePhotoFeatureCode = form.dataset.employeePhotoFeatureCode || 'HR_EMPLOYEE_PHOTO';
    // 员工主图保存地址（将选中的图片文档写入员工 PhotoPath）。
    const employeePrimaryPhotoUrl = form.dataset.employeePrimaryPhotoUrl || '';
    // 培训历程档案文档功能编码（对应单条培训历程记录的通用文档集合）。
    const trainingExperienceDocumentFeatureCode = trainingExperiencePanel?.dataset.documentFeatureCode || 'HR_EMPLOYEE_TRAINING_EXPERIENCE';
    const MASKED_PASSWORD_SENTINEL = '__KEEP_EXISTING_PASSWORD__';
    let accountPanelLoaded = false;
    let trainingExperiencePanelLoaded = false;
    let taskFollowUpPanelLoaded = false;
    let isSubmittingWithAccountSync = false;
    const trainingExperienceState = {
        pageNumber: 1,
        pageSize: 10,
        totalCount: 0,
        keyword: '',
        experienceTypeCode: '',
        experienceTypeOptions: [],
        items: []
    };
    const taskFollowUpState = {
        pageNumber: 1,
        pageSize: 10,
        totalCount: 0,
        keyword: '',
        statusCode: '',
        statusOptions: [],
        taskTypeOptions: [],
        priorityOptions: [],
        items: []
    };

    const getAccountApiUrl = () => `${accountApiBaseUrl}/${employeeId}`;
    const getTrainingExperienceApiUrl = () => `${trainingExperienceApiBaseUrl}?employeeId=${encodeURIComponent(trainingExperienceEmployeeId)}&keyword=${encodeURIComponent(trainingExperienceState.keyword)}&experienceTypeCode=${encodeURIComponent(trainingExperienceState.experienceTypeCode)}&pageNumber=${encodeURIComponent(trainingExperienceState.pageNumber)}&pageSize=${encodeURIComponent(trainingExperienceState.pageSize)}`;
    const getTaskFollowUpApiUrl = () => `${taskFollowUpApiBaseUrl}?featureCode=${encodeURIComponent(taskFollowUpFeatureCode)}&entityId=${encodeURIComponent(taskFollowUpEntityId)}&keyword=${encodeURIComponent(taskFollowUpState.keyword)}&statusCode=${encodeURIComponent(taskFollowUpState.statusCode)}&pageNumber=${encodeURIComponent(taskFollowUpState.pageNumber)}&pageSize=${encodeURIComponent(taskFollowUpState.pageSize)}`;

    const getRequestVerificationToken = () =>
        form.querySelector("input[name='__RequestVerificationToken']")?.value
        || document.querySelector("meta[name='request-verification-token']")?.content
        || '';

    const renderEmployeePhoto = photoPath => {
        if (!employeePhotoFrame) return;
        employeePhotoFrame.replaceChildren();
        if (photoPath) {
            const image = document.createElement('img');
            image.src = photoPath;
            image.alt = '员工照片';
            image.className = 'employee-detail-photo__image';
            employeePhotoFrame.appendChild(image);
            return;
        }

        const placeholder = document.createElement('span');
        placeholder.className = 'employee-detail-photo__placeholder';
        placeholder.textContent = '显示员工相片';
        employeePhotoFrame.appendChild(placeholder);
    };

    const setPrimaryEmployeePhoto = async documentItem => {
        if (form.classList.contains('is-readonly')) {
            window.alert('查看模式不能设置员工主图，请先点击编辑。');
            return false;
        }

        if (!employeePrimaryPhotoUrl) {
            window.alert('员工主图保存地址未配置，请刷新页面后重试。');
            return false;
        }

        const token = getRequestVerificationToken();
        if (!token) {
            window.alert('页面缺少防伪令牌，请刷新后重试。');
            return false;
        }

        const body = new URLSearchParams();
        body.set('employeeId', employeeId);
        if (documentItem?.id) {
            body.set('documentId', String(documentItem.id));
        }

        try {
            const response = await fetch(employeePrimaryPhotoUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token
                },
                body: body.toString()
            });
            const result = await response.json().catch(() => ({}));
            if (!response.ok) {
                window.alert(result.message || '设置员工主图失败，请稍后重试。');
                return false;
            }

            const photoPath = result.photoPath || '';
            if (employeePhotoPathInput instanceof HTMLInputElement) {
                employeePhotoPathInput.value = photoPath;
            }
            renderEmployeePhoto(photoPath);
            return photoPath;
        } catch {
            window.alert('网络请求失败，请稍后重试。');
            return false;
        }
    };

    // 打开通用文档管理弹窗（按业务功能编码与记录ID定位文档集合）。
    const openDocumentManager = options => {
        if (!window.openErpDocumentManager?.open) {
            window.alert('档案管理功能尚未载入，请刷新页面后重试。');
            return;
        }

        window.openErpDocumentManager.open({
            ...options,
            canEdit: !form.classList.contains('is-readonly')
        });
    };

    const escapeHtml = value => String(value ?? '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/\"/g, '&quot;')
        .replace(/'/g, '&#39;');

    const refreshCustomOptionSelect = async (sourceKey, selectedId = null) => {
        const trigger = customOptionTriggers.find(button => button.dataset.customOptionSource === sourceKey);
        if (!trigger) return;
        const endpoint = trigger.dataset.customOptionEndpoint;
        const targetName = trigger.dataset.customOptionTarget;
        const targetSelect = targetName ? form.querySelector(`[name='${targetName}']`) : null;
        if (!endpoint || !targetSelect) return;

        const previousValue = targetSelect.value;
        const response = await fetch(endpoint, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
        if (!response.ok) return;

        const options = await response.json();
        targetSelect.innerHTML = '';
        const placeholderOption = document.createElement('option');
        placeholderOption.value = '';
        placeholderOption.textContent = '请选择';
        targetSelect.appendChild(placeholderOption);
        options.forEach(option => {
            const optionElement = document.createElement('option');
            optionElement.value = String(option.value);
            optionElement.textContent = option.text;
            targetSelect.appendChild(optionElement);
        });

        if (selectedId && [...targetSelect.options].some(option => option.value === String(selectedId))) {
            targetSelect.value = String(selectedId);
            return;
        }
        targetSelect.value = [...targetSelect.options].some(option => option.value === previousValue) ? previousValue : '';
    };

    const refreshLocationSelect = async (selectElement, level, parentId, placeholderText, keepSelectedValue = true) => {
        if (!selectElement) return;
        const previousValue = keepSelectedValue ? selectElement.value : '';
        selectElement.innerHTML = '';
        const placeholderOption = document.createElement('option');
        placeholderOption.value = '';
        placeholderOption.textContent = placeholderText;
        selectElement.appendChild(placeholderOption);
        if (!locationOptionsUrl || !parentId) {
            selectElement.value = '';
            return;
        }

        const requestUrl = new URL(locationOptionsUrl, window.location.origin);
        requestUrl.searchParams.set('level', level);
        requestUrl.searchParams.set('parentId', parentId);
        const response = await fetch(requestUrl.toString(), { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
        if (!response.ok) return;

        const options = await response.json();
        options.forEach(option => {
            const optionElement = document.createElement('option');
            optionElement.value = String(option.value);
            optionElement.textContent = option.text;
            selectElement.appendChild(optionElement);
        });
        selectElement.value = [...selectElement.options].some(option => option.value === previousValue) ? previousValue : '';
    };

    const syncLocationCascade = async () => {
        await refreshLocationSelect(citySelect, 'CITY', countryRegionSelect?.value || '', countryRegionSelect?.value ? '请选择' : '请先选择国家/地区');
        await refreshLocationSelect(countySelect, 'COUNTY', citySelect?.value || '', citySelect?.value ? '请选择' : '请先选择城市');
    };

    countryRegionSelect?.addEventListener('change', async () => {
        await refreshLocationSelect(citySelect, 'CITY', countryRegionSelect.value, countryRegionSelect.value ? '请选择' : '请先选择国家/地区', false);
        await refreshLocationSelect(countySelect, 'COUNTY', '', '请先选择城市', false);
    });

    citySelect?.addEventListener('change', async () => {
        await refreshLocationSelect(countySelect, 'COUNTY', citySelect.value, citySelect.value ? '请选择' : '请先选择城市', false);
    });

    btnEdit?.addEventListener('click', async () => {
        form.querySelectorAll("input:not([type='hidden']), textarea").forEach(element => element.removeAttribute('readonly'));
        form.querySelectorAll('select').forEach(element => element.removeAttribute('disabled'));
        const lookupButton = form.querySelector("[data-employee-tool='lookup']");
        if (lookupButton instanceof HTMLElement) lookupButton.style.display = '';

        if (toolbar) {
            form.querySelectorAll('[data-custom-option-trigger]').forEach(button => {
                if (button instanceof HTMLElement) button.style.display = '';
            });
            toolbar.querySelectorAll('[data-employee-tool]').forEach(button => button.remove());
            btnEdit.remove();
            const saveButton = document.createElement('button');
            saveButton.type = 'submit';
            saveButton.className = 'employee-detail-toolbar__btn';
            saveButton.id = 'btnSave';
            saveButton.textContent = '保存';
            toolbar.insertBefore(saveButton, toolbar.firstChild);
        }

        form.classList.remove('is-readonly');
        syncTrainingExperienceEditorAccess();
        syncTaskFollowUpEditorAccess();
        await syncLocationCascade();
        if (accountPanelLoaded) {
            accountPanelLoaded = false;
            await loadAccountPanel();
        }
    });

    const syncStatus = () => {
        if (!statusCard || !statusText || !leaveDateInput) return;
        const isLeave = leaveDateInput.value.trim().length > 0;
        statusCard.classList.toggle('is-leave', isLeave);
        statusCard.classList.toggle('is-active', !isLeave);
        statusText.textContent = isLeave ? '离职' : '在职';
    };

    leaveDateInput?.addEventListener('change', syncStatus);
    syncStatus();
    void syncLocationCascade();

    window.addEventListener('message', event => {
        if (event.origin !== window.location.origin) return;
        if (event.data?.type === 'open-erp:custom-options-updated' && event.data?.sourceKey) {
            refreshCustomOptionSelect(event.data.sourceKey, event.data.selectedId ?? null);
        }
    });

    document.addEventListener('click', event => {
        const trigger = event.target.closest('[data-employee-tool]');
        if (!trigger || !form.contains(trigger)) return;
        const toolType = trigger.getAttribute('data-employee-tool');
        if (toolType === 'new') {
            const targetUrl = trigger.getAttribute('data-target-url');
            if (targetUrl) window.location.href = targetUrl;
            return;
        }
        if (toolType === 'lookup') return void window.alert('员工编号查询功能后续可继续接入编号规则与快速检索。');
        if (toolType === 'copy') {
            const idInput = form.querySelector("input[name='Id']");
            const currentEmployeeId = idInput ? idInput.value : '';
            if (currentEmployeeId) {
                const createUrl = form.getAttribute('data-create-url') || '';
                window.location.href = `${createUrl}&copyFromId=${encodeURIComponent(currentEmployeeId)}`;
            }
            return;
        }
        if (toolType === 'mail') return void window.alert('邮件功能待接入企业邮箱或通知中心。');
        if (toolType === 'message') return void window.alert('信息功能待接入内部消息或备注流转。');
        if (toolType === 'tab') return void window.alert('其余页签将继续扩展考勤工资、培训历程和任务派送。');
        if (toolType === 'annualLeaveHistory') return void window.alert('年假调整及历史功能后续可继续接入考勤记录与异动日志。');
        if (toolType === 'photo') {
            openDocumentManager({
                featureCode: employeePhotoFeatureCode,
                entityId: employeeId,
                title: '员工图片管理',
                mode: 'images',
                primaryUrl: employeePhotoPathInput?.value || '',
                emptyText: '当前没有员工图片，请先上传图片文档。',
                emptyMessage: '请先保存员工基本资料，再管理员工图片。',
                onPrimarySelected: setPrimaryEmployeePhoto
            });
            return;
        }
        if (toolType === 'archive') {
            openDocumentManager({
                featureCode: employeeDocumentFeatureCode,
                entityId: employeeId,
                title: '员工档案管理',
                emptyMessage: '请先保存员工基本资料，再管理员工档案。'
            });
        }
    });

    const switchTab = async targetPanel => {
        tabButtons.forEach(button => button.classList.remove('is-active'));
        const activeButton = [...tabButtons].find(button => button.dataset.tabPanel === targetPanel);
        activeButton?.classList.add('is-active');
        tabContents.forEach(panel => {
            panel.style.display = panel.dataset.tabContent === targetPanel ? '' : 'none';
        });
        if (targetPanel === 'accountPermission' && !accountPanelLoaded) {
            await loadAccountPanel();
        }
        if (targetPanel === 'trainingExperience' && !trainingExperiencePanelLoaded) {
            await loadTrainingExperiencePanel();
        }
        if (targetPanel === 'taskFollowUp' && !taskFollowUpPanelLoaded) {
            await loadTaskFollowUpPanel();
        }
    };

    tabButtons.forEach(button => {
        button.addEventListener('click', () => {
            const targetPanel = button.dataset.tabPanel;
            if (targetPanel) void switchTab(targetPanel);
        });
    });

    const buildPermissionGroups = permissions => {
        const categoryMap = new Map();
        permissions.slice().sort((left, right) => (left.sortOrder || 0) - (right.sortOrder || 0) || (left.id || 0) - (right.id || 0)).forEach(permission => {
            const categoryTitle = permission.category || '其他功能';
            if (!categoryMap.has(categoryTitle)) categoryMap.set(categoryTitle, []);
            categoryMap.get(categoryTitle).push(permission);
        });
        return [...categoryMap.entries()].map(([title, items], index) => ({ key: `permission-group-${index + 1}`, title, items }));
    };

    const syncPermissionGroupStates = () => {
        if (!accountPanel) return;
        accountPanel.querySelectorAll('.account-permission-group').forEach(groupElement => {
            const groupToggle = groupElement.querySelector('.account-group-toggle');
            const permissionCheckboxes = [...groupElement.querySelectorAll("[name='permissionIds']")];
            if (!(groupToggle instanceof HTMLInputElement) || permissionCheckboxes.length === 0) return;
            const checkedCount = permissionCheckboxes.filter(checkbox => checkbox.checked).length;
            groupToggle.checked = checkedCount > 0 && checkedCount === permissionCheckboxes.length;
            groupToggle.indeterminate = checkedCount > 0 && checkedCount < permissionCheckboxes.length;
        });
    };

    const loadAccountPanel = async () => {
        if (!accountPanel || employeeId === '0') {
            if (accountPanel) accountPanel.innerHTML = '<div class="account-panel__notice">请先保存员工基本信息，再设置账号及权限。</div>';
            return;
        }
        try {
            const response = await fetch(getAccountApiUrl(), { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
            if (!response.ok) {
                accountPanel.innerHTML = '<div class="account-panel__notice">加载账号及权限数据失败，请稍后重试。</div>';
                return;
            }
            renderAccountPanel(await response.json());
            accountPanelLoaded = true;
        } catch {
            accountPanel.innerHTML = '<div class="account-panel__notice">网络请求失败，请稍后重试。</div>';
        }
    };

    const bindAccountPanelEvents = hasPassword => {
        if (!accountPanel) return;
        const passwordInput = accountPanel.querySelector("[name='accountNewPassword']");
        const passwordToggleButton = accountPanel.querySelector('.account-field__toggle-pwd');
        const clearMaskedPassword = () => {
            if (!(passwordInput instanceof HTMLInputElement)) return;
            if (passwordInput.value === MASKED_PASSWORD_SENTINEL) {
                passwordInput.value = '';
                passwordInput.placeholder = '请输入新密码';
            }
        };

        if (passwordInput instanceof HTMLInputElement && hasPassword) {
            passwordInput.addEventListener('keydown', event => {
                if (passwordInput.value !== MASKED_PASSWORD_SENTINEL) return;
                if (event.key.length === 1 || event.key === 'Backspace' || event.key === 'Delete') {
                    clearMaskedPassword();
                }
            });
            passwordInput.addEventListener('paste', clearMaskedPassword);
        }

        if (passwordInput instanceof HTMLInputElement && passwordToggleButton instanceof HTMLButtonElement) {
            passwordToggleButton.addEventListener('click', () => {
                if (passwordInput.value === MASKED_PASSWORD_SENTINEL) clearMaskedPassword();
                const isPasswordVisible = passwordInput.type === 'text';
                passwordInput.type = isPasswordVisible ? 'password' : 'text';
                passwordToggleButton.setAttribute('aria-pressed', String(!isPasswordVisible).toLowerCase());
                passwordToggleButton.setAttribute('title', isPasswordVisible ? '显示密码' : '隐藏密码');
                passwordToggleButton.innerHTML = isPasswordVisible
                    ? '<i class="bi bi-eye" aria-hidden="true"></i>'
                    : '<i class="bi bi-eye-slash" aria-hidden="true"></i>';
            });
        }

        const saveButton = accountPanel.querySelector('#btnSaveAccount');
        if (saveButton instanceof HTMLButtonElement) saveButton.addEventListener('click', saveAccountSettings);

        accountPanel.querySelectorAll('.account-perm-action').forEach(button => {
            button.addEventListener('click', () => {
                const shouldCheck = button.getAttribute('data-action') === 'selectAllPermissions';
                accountPanel.querySelectorAll("[name='permissionIds']").forEach(checkbox => {
                    checkbox.checked = shouldCheck;
                });
                syncPermissionGroupStates();
            });
        });

        accountPanel.querySelectorAll('.account-company-action').forEach(button => {
            button.addEventListener('click', () => {
                const shouldCheck = button.getAttribute('data-action') === 'selectAllCompanies';
                accountPanel.querySelectorAll("[name='companyIds']").forEach(checkbox => {
                    checkbox.checked = shouldCheck;
                });
            });
        });

        accountPanel.querySelectorAll('.account-group-toggle').forEach(toggle => {
            toggle.addEventListener('change', () => {
                const groupKey = toggle.getAttribute('data-group-key');
                if (!groupKey) return;
                accountPanel.querySelectorAll(`[name='permissionIds'][data-group-key='${groupKey}']`).forEach(checkbox => {
                    checkbox.checked = toggle.checked;
                });
                syncPermissionGroupStates();
            });
        });

        accountPanel.querySelectorAll("[name='permissionIds']").forEach(checkbox => {
            checkbox.addEventListener('change', syncPermissionGroupStates);
        });
        syncPermissionGroupStates();
    };

    const renderAccountPanel = data => {
        if (!accountPanel) return;
        const isReadOnly = form.classList.contains('is-readonly');
        const disabledAttr = isReadOnly ? 'disabled' : '';
        const permissionIdSet = new Set((data.userPermissionIds || []).map(id => Number(id)));
        const companyIdSet = new Set((data.userCompanyIds || []).map(id => Number(id)));
        const permissionGroups = buildPermissionGroups(data.permissions || []);
        const forceViewRecordDaysValue = Number.isInteger(data.forceViewRecordDays) ? data.forceViewRecordDays : '';
        const passwordValue = data.hasPassword ? MASKED_PASSWORD_SENTINEL : '';
        const passwordPlaceholder = data.hasPassword ? '保留当前密码请直接保存，如需修改请直接输入新密码' : '请输入登录密码';
        const rolesHtml = ['<option value="">请选择角色</option>', ...(data.roles || []).map(role => `<option value="${role.id}" ${Number(data.roleId) === Number(role.id) ? 'selected' : ''}>${escapeHtml(role.roleName)}</option>`)].join('');

        const permissionsHtml = permissionGroups.length === 0
            ? '<div class="account-empty-state">当前没有可分配的功能权限。</div>'
            : permissionGroups.map(group => {
                const checkedCount = group.items.filter(item => permissionIdSet.has(Number(item.id))).length;
                const groupChecked = checkedCount > 0 && checkedCount === group.items.length;
                return `<section class="account-permission-group"><div class="account-permission-group__header"><label class="account-checkbox"><input type="checkbox" class="account-group-toggle" data-group-key="${group.key}" ${groupChecked ? 'checked' : ''} ${disabledAttr} /><span>${escapeHtml(group.title)}</span></label><span class="account-permission-group__count">${group.items.length} 项功能</span></div><div class="account-table-shell"><table class="account-table account-table--permissions"><thead><tr><th scope="col">序号</th><th scope="col">功能</th><th scope="col">访问权限</th></tr></thead><tbody>${group.items.map((permission, index) => `<tr><td>${index + 1}</td><td><div class="account-table__title">${escapeHtml(permission.permissionName)}</div><div class="account-table__code">${escapeHtml(permission.permissionCode)}</div></td><td><label class="account-checkbox"><input type="checkbox" name="permissionIds" value="${permission.id}" data-group-key="${group.key}" ${permissionIdSet.has(Number(permission.id)) ? 'checked' : ''} ${disabledAttr} /><span>启用</span></label></td></tr>`).join('')}</tbody></table></div></section>`;
            }).join('');

        const companiesHtml = (data.companies || []).length === 0
            ? '<tr><td colspan="4" class="account-table__empty">当前没有公司组织数据。</td></tr>'
            : (data.companies || []).map((company, index) => `<tr><td>${index + 1}</td><td><label class="account-checkbox account-checkbox--center"><input type="checkbox" name="companyIds" value="${company.id}" ${companyIdSet.has(Number(company.id)) ? 'checked' : ''} ${disabledAttr} /><span>管辖</span></label></td><td>${escapeHtml(company.organizationCode)}</td><td>${escapeHtml(company.organizationName)}</td></tr>`).join('');

        accountPanel.innerHTML = `<div class="account-form"><section class="account-section account-section--profile"><div class="account-section__header"><div><h4 class="account-section__title">账号设置</h4><p class="account-section__desc">密码仅以不可逆加密方式保存，至少 8 位，需包含字母、数字和特殊字符。</p></div></div><div class="account-profile-grid"><div class="account-field"><label for="accountLoginAccount">登录账号</label><input id="accountLoginAccount" type="text" name="accountLoginAccount" class="form-control" value="${escapeHtml(data.loginAccount || '')}" placeholder="请输入登录账号" ${disabledAttr} /></div><div class="account-field"><label for="accountNewPassword">登录密码</label><div class="account-field__password"><input id="accountNewPassword" type="password" name="accountNewPassword" class="form-control" value="${escapeHtml(passwordValue)}" placeholder="${passwordPlaceholder}" autocomplete="new-password" ${disabledAttr} /><button type="button" class="account-field__toggle-pwd" title="显示密码" aria-pressed="false" ${disabledAttr}><i class="bi bi-eye" aria-hidden="true"></i></button></div><span class="account-field__hint">${data.hasPassword ? '留空并直接保存可保持当前密码不变。' : '首次设置后系统仅保存加密结果。'}</span></div><div class="account-field"><label for="accountForceViewRecordDays">强制查看记录日数</label><input id="accountForceViewRecordDays" type="number" min="0" name="accountForceViewRecordDays" class="form-control" value="${forceViewRecordDaysValue}" placeholder="留空表示不限制" ${disabledAttr} /><span class="account-field__hint">留空表示不限制账号可查看的历史记录天数。</span></div><div class="account-field"><label for="accountRoleId">角色</label><select id="accountRoleId" name="accountRoleId" class="form-select" ${disabledAttr}>${rolesHtml}</select></div><div class="account-field"><label for="accountValidUntil">有效日期</label><input id="accountValidUntil" type="date" name="accountValidUntil" class="form-control" value="${escapeHtml(data.accountValidUntil || '')}" ${disabledAttr} /><span class="account-field__hint">留空表示永久有效，到期后账号不可登录系统。</span></div><div class="account-field account-field--checkbox"><label class="account-field__checkbox" for="accountIsFrozen"><input id="accountIsFrozen" type="checkbox" name="accountIsFrozen" ${data.isAccountFrozen ? 'checked' : ''} ${disabledAttr} /><span>冻结账号后，该账号将不可登录系统。</span></label></div></div></section><div class="account-workspace"><section class="account-section account-section--companies"><div class="account-section__header account-section__header--inline"><div><h4 class="account-section__title">管辖公司</h4><p class="account-section__desc">未勾选的公司组织不会出现在该账号的登录公司列表中。</p></div><div class="account-section__toolbar"><button type="button" class="account-company-action" data-action="selectAllCompanies" ${disabledAttr}>全选</button><button type="button" class="account-company-action" data-action="clearAllCompanies" ${disabledAttr}>清空</button></div></div><div class="account-table-shell"><table class="account-table account-table--companies"><thead><tr><th scope="col">序号</th><th scope="col">管辖</th><th scope="col">组织编号</th><th scope="col">组织名称</th></tr></thead><tbody>${companiesHtml}</tbody></table></div></section><section class="account-section account-section--permissions"><div class="account-section__header account-section__header--inline"><div><h4 class="account-section__title">功能权限</h4><p class="account-section__desc">先按系统导航现有功能分配账号可访问范围。</p></div><div class="account-section__toolbar"><button type="button" class="account-perm-action" data-action="selectAllPermissions" ${disabledAttr}>全选</button><button type="button" class="account-perm-action" data-action="clearAllPermissions" ${disabledAttr}>清空</button></div></div><div class="account-permission-list">${permissionsHtml}</div></section></div>${!isReadOnly ? '<div class="account-actions"><button type="button" class="employee-detail-toolbar__btn" id="btnSaveAccount">保存账号及权限</button></div>' : ''}</div>`;
        bindAccountPanelEvents(Boolean(data.hasPassword));
    };

    const renderTrainingExperienceOptionItems = (selectElement, options, includeAll = false) => {
        if (!selectElement) return;
        const previousValue = selectElement.value;
        selectElement.innerHTML = includeAll ? '<option value="">全部</option>' : '<option value="">请选择</option>';
        options.forEach(option => {
            const optionElement = document.createElement('option');
            optionElement.value = option.code;
            optionElement.textContent = option.label;
            selectElement.appendChild(optionElement);
        });
        if ([...selectElement.options].some(option => option.value === previousValue)) {
            selectElement.value = previousValue;
        }
    };

    const getTrainingExperienceEditorElements = () => ({
        editor: document.getElementById('trainingExperienceEditor'),
        id: document.getElementById('trainingExperienceId'),
        startDate: document.getElementById('trainingExperienceStartDate'),
        endDate: document.getElementById('trainingExperienceEndDate'),
        experienceTypeCode: document.getElementById('trainingExperienceTypeCode'),
        organizationName: document.getElementById('trainingExperienceOrganizationName'),
        certificateName: document.getElementById('trainingExperienceCertificateName'),
        archiveButton: document.getElementById('trainingExperienceArchiveButton'),
        description: document.getElementById('trainingExperienceDescription')
    });

    function syncTrainingExperienceEditorAccess() {
        if (!trainingExperiencePanel) return;
        const isReadOnly = form.classList.contains('is-readonly');
        const editorElements = getTrainingExperienceEditorElements();
        ['trainingExperienceAddButton', 'trainingExperienceDeleteButton', 'trainingExperienceSaveButton'].forEach(id => {
            const button = document.getElementById(id);
            if (button instanceof HTMLButtonElement) {
                button.disabled = isReadOnly;
            }
        });
        Object.values(editorElements).forEach(element => {
            if (!(element instanceof HTMLElement)) return;
            if (element instanceof HTMLInputElement || element instanceof HTMLTextAreaElement || element instanceof HTMLSelectElement) {
                element.disabled = isReadOnly;
            }
        });
        if (editorElements.archiveButton instanceof HTMLButtonElement) {
            const targetEntityId = Number.parseInt(editorElements.archiveButton.dataset.entityId || '0', 10);
            editorElements.archiveButton.disabled = !Number.isInteger(targetEntityId) || targetEntityId <= 0;
            editorElements.archiveButton.title = editorElements.archiveButton.disabled ? '请先保存培训历程记录，再管理档案。' : '打开档案管理';
        }
        const selectAll = document.getElementById('trainingExperienceSelectAll');
        if (selectAll instanceof HTMLInputElement) {
            selectAll.disabled = isReadOnly;
            if (isReadOnly) {
                selectAll.checked = false;
            }
        }

        document.querySelectorAll('.training-experience-row-selector').forEach(checkbox => {
            if (!(checkbox instanceof HTMLInputElement)) return;
            checkbox.disabled = isReadOnly;
            if (isReadOnly) {
                checkbox.checked = false;
            }
        });
    }

    const hideTrainingExperienceEditor = () => {
        const { editor } = getTrainingExperienceEditorElements();
        if (editor instanceof HTMLElement) {
            editor.hidden = true;
        }
    };

    const showTrainingExperienceEditor = item => {
        const editorElements = getTrainingExperienceEditorElements();
        if (!(editorElements.editor instanceof HTMLElement)) return;
        editorElements.editor.hidden = false;
        editorElements.id.value = item?.id ? String(item.id) : '';
        editorElements.startDate.value = item?.startDate || '';
        editorElements.endDate.value = item?.endDate || '';
        editorElements.experienceTypeCode.value = item?.experienceTypeCode || 'TRAINING';
        editorElements.organizationName.value = item?.organizationName || '';
        editorElements.certificateName.value = item?.certificateName || '';
        editorElements.description.value = item?.description || '';
        if (editorElements.archiveButton instanceof HTMLButtonElement) {
            editorElements.archiveButton.dataset.entityId = item?.id ? String(item.id) : '';
        }
        syncTrainingExperienceEditorAccess();
        editorElements.description.focus();
        editorElements.editor.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    };

    const renderTrainingExperiencePagination = () => {
        const pageNumbers = document.getElementById('trainingExperiencePageNumbers');
        const prevButton = document.getElementById('trainingExperiencePrevPage');
        const nextButton = document.getElementById('trainingExperienceNextPage');
        const summary = document.getElementById('trainingExperienceSummary');
        if (!pageNumbers || !prevButton || !nextButton || !summary) return;

        const totalPages = Math.max(1, Math.ceil(trainingExperienceState.totalCount / trainingExperienceState.pageSize));
        const currentPage = Math.min(trainingExperienceState.pageNumber, totalPages);
        trainingExperienceState.pageNumber = currentPage;
        summary.textContent = `共 ${trainingExperienceState.totalCount} 条，第 ${currentPage} / ${totalPages} 页`;
        prevButton.disabled = currentPage <= 1;
        nextButton.disabled = currentPage >= totalPages;

        pageNumbers.innerHTML = '';
        const startPage = Math.max(1, currentPage - 2);
        const endPage = Math.min(totalPages, startPage + 4);
        for (let page = startPage; page <= endPage; page += 1) {
            const button = document.createElement('button');
            button.type = 'button';
            button.className = `training-experience-pagebtn${page === currentPage ? ' is-active' : ''}`;
            button.textContent = String(page);
            button.dataset.pageNumber = String(page);
            pageNumbers.appendChild(button);
        }
    };

    const renderTrainingExperienceTable = () => {
        const tbody = document.getElementById('trainingExperienceTableBody');
        const selectAll = document.getElementById('trainingExperienceSelectAll');
        if (!tbody) return;

        if (trainingExperienceState.items.length === 0) {
            tbody.innerHTML = '<tr><td colspan="11" class="training-experience-table__empty">当前没有培训历程记录。</td></tr>';
            if (selectAll instanceof HTMLInputElement) {
                selectAll.checked = false;
            }
            renderTrainingExperiencePagination();
            return;
        }

        tbody.innerHTML = trainingExperienceState.items.map(item => `
            <tr data-training-experience-id="${item.id}">
                <td class="training-experience-table__seq">${item.sequenceNo}</td>
                <td class="training-experience-table__checkbox"><input type="checkbox" class="training-experience-row-selector" value="${item.id}" ${form.classList.contains('is-readonly') ? 'disabled' : ''} /></td>
                <td><button type="button" class="training-experience-link" data-action="editTrainingExperience" data-training-experience-id="${item.id}">${escapeHtml(item.experienceTypeLabel || '')}</button></td>
                <td>${escapeHtml(item.startDate || '')}</td>
                <td>${escapeHtml(item.endDate || '')}</td>
                <td class="training-experience-table__desc">${escapeHtml(item.description || '')}</td>
                <td>${escapeHtml(item.certificateName || '')}</td>
                <td>${escapeHtml(item.organizationName || '')}</td>
                <td><button type="button" class="training-experience-archive-btn" data-action="viewTrainingExperienceArchive" data-training-experience-id="${item.id}">${escapeHtml(item.archiveLabel || '档案')}</button></td>
                <td>${escapeHtml(item.createdBy || '')}</td>
                <td>${escapeHtml(item.createdAt || '')}</td>
            </tr>
        `).join('');

        if (selectAll instanceof HTMLInputElement) {
            selectAll.checked = false;
        }
        renderTrainingExperiencePagination();
    };

    const loadTrainingExperiencePanel = async () => {
        if (!trainingExperiencePanel) return;
        if (trainingExperienceEmployeeId === '0') {
            trainingExperiencePanel.innerHTML = '<div class="account-panel__notice">请先保存员工基本资料，再新增培训历程记录。</div>';
            trainingExperiencePanelLoaded = true;
            return;
        }

        try {
            const response = await fetch(getTrainingExperienceApiUrl(), { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
            const result = await response.json().catch(() => ({}));
            if (!response.ok) {
                trainingExperiencePanel.innerHTML = `<div class="account-panel__notice">${escapeHtml(result.message || '加载培训历程数据失败，请稍后重试。')}</div>`;
                return;
            }

            trainingExperienceState.totalCount = Number(result.totalCount) || 0;
            trainingExperienceState.pageNumber = Number(result.pageNumber) || 1;
            trainingExperienceState.pageSize = Number(result.pageSize) || 10;
            trainingExperienceState.experienceTypeOptions = result.experienceTypeOptions || [];
            trainingExperienceState.items = result.items || [];

            renderTrainingExperienceOptionItems(document.getElementById('trainingExperienceTypeFilter'), trainingExperienceState.experienceTypeOptions, true);
            renderTrainingExperienceOptionItems(document.getElementById('trainingExperienceTypeCode'), trainingExperienceState.experienceTypeOptions);

            const pageSizeSelect = document.getElementById('trainingExperiencePageSize');
            if (pageSizeSelect instanceof HTMLSelectElement) {
                pageSizeSelect.value = String(trainingExperienceState.pageSize);
            }

            renderTrainingExperienceTable();
            syncTrainingExperienceEditorAccess();
            trainingExperiencePanelLoaded = true;

            const totalPages = Math.max(1, Math.ceil(trainingExperienceState.totalCount / trainingExperienceState.pageSize));
            if (trainingExperienceState.items.length === 0 && trainingExperienceState.totalCount > 0 && trainingExperienceState.pageNumber > totalPages) {
                trainingExperienceState.pageNumber = totalPages;
                await loadTrainingExperiencePanel();
            }
        } catch {
            trainingExperiencePanel.innerHTML = '<div class="account-panel__notice">网络请求失败，请稍后重试。</div>';
        }
    };

    const collectTrainingExperiencePayload = () => {
        const editorElements = getTrainingExperienceEditorElements();
        if (!editorElements.startDate.value) {
            window.alert('开始日期不能为空。');
            editorElements.startDate.focus();
            return null;
        }
        if (!editorElements.description.value.trim()) {
            window.alert('历程描述不能为空。');
            editorElements.description.focus();
            return null;
        }
        if (editorElements.endDate.value && editorElements.endDate.value < editorElements.startDate.value) {
            window.alert('结束日期不能早于开始日期。');
            editorElements.endDate.focus();
            return null;
        }

        return {
            id: editorElements.id.value ? Number.parseInt(editorElements.id.value, 10) : null,
            employeeId: Number.parseInt(trainingExperienceEmployeeId, 10),
            experienceTypeCode: editorElements.experienceTypeCode.value || null,
            startDate: editorElements.startDate.value || null,
            endDate: editorElements.endDate.value || null,
            description: editorElements.description.value.trim(),
            certificateName: editorElements.certificateName.value.trim() || null,
            organizationName: editorElements.organizationName.value.trim() || null
        };
    };

    const saveTrainingExperience = async () => {
        const payload = collectTrainingExperiencePayload();
        if (!payload) return;

        try {
            const response = await fetch(trainingExperienceApiBaseUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest'
                },
                body: JSON.stringify(payload)
            });
            const result = await response.json().catch(() => ({}));
            if (!response.ok) {
                window.alert(result.message || '保存培训历程失败，请稍后重试。');
                return;
            }

            hideTrainingExperienceEditor();
            await loadTrainingExperiencePanel();
            window.alert(result.message || '培训历程已保存。');
        } catch {
            window.alert('网络请求失败，请稍后重试。');
        }
    };

    const deleteTrainingExperiences = async () => {
        const selectedIds = [...document.querySelectorAll('.training-experience-row-selector:checked')]
            .map(checkbox => Number.parseInt(checkbox.value, 10))
            .filter(Number.isInteger);

        if (selectedIds.length === 0) {
            window.alert('请先勾选要删除的培训历程记录。');
            return;
        }

        if (!window.confirm(`确定删除已选中的 ${selectedIds.length} 条培训历程记录吗？`)) {
            return;
        }

        try {
            const response = await fetch(`${trainingExperienceApiBaseUrl}/delete`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest'
                },
                body: JSON.stringify({
                    employeeId: Number.parseInt(trainingExperienceEmployeeId, 10),
                    ids: selectedIds
                })
            });
            const result = await response.json().catch(() => ({}));
            if (!response.ok) {
                window.alert(result.message || '删除培训历程失败，请稍后重试。');
                return;
            }

            const totalPagesAfterDelete = Math.max(1, Math.ceil(Math.max(0, trainingExperienceState.totalCount - selectedIds.length) / trainingExperienceState.pageSize));
            trainingExperienceState.pageNumber = Math.min(trainingExperienceState.pageNumber, totalPagesAfterDelete);
            await loadTrainingExperiencePanel();
            hideTrainingExperienceEditor();
            window.alert(result.message || '培训历程已删除。');
        } catch {
            window.alert('网络请求失败，请稍后重试。');
        }
    };

    const findTrainingExperienceItem = trainingExperienceId => trainingExperienceState.items.find(item => Number(item.id) === Number(trainingExperienceId));

    if (trainingExperiencePanel) {
        document.getElementById('trainingExperienceSearchButton')?.addEventListener('click', async () => {
            trainingExperienceState.keyword = document.getElementById('trainingExperienceKeyword')?.value?.trim() || '';
            trainingExperienceState.experienceTypeCode = document.getElementById('trainingExperienceTypeFilter')?.value || '';
            trainingExperienceState.pageNumber = 1;
            await loadTrainingExperiencePanel();
        });

        document.getElementById('trainingExperienceRefreshButton')?.addEventListener('click', async () => {
            const keywordInput = document.getElementById('trainingExperienceKeyword');
            const typeFilter = document.getElementById('trainingExperienceTypeFilter');
            if (keywordInput instanceof HTMLInputElement) keywordInput.value = '';
            if (typeFilter instanceof HTMLSelectElement) typeFilter.value = '';
            trainingExperienceState.keyword = '';
            trainingExperienceState.experienceTypeCode = '';
            trainingExperienceState.pageNumber = 1;
            hideTrainingExperienceEditor();
            await loadTrainingExperiencePanel();
        });

        document.getElementById('trainingExperienceAddButton')?.addEventListener('click', () => {
            showTrainingExperienceEditor(null);
        });

        document.getElementById('trainingExperienceDeleteButton')?.addEventListener('click', deleteTrainingExperiences);
        document.getElementById('trainingExperienceSaveButton')?.addEventListener('click', saveTrainingExperience);
        document.getElementById('trainingExperienceCancelButton')?.addEventListener('click', hideTrainingExperienceEditor);

        document.getElementById('trainingExperienceKeyword')?.addEventListener('keydown', event => {
            if (event.key === 'Enter') {
                event.preventDefault();
                document.getElementById('trainingExperienceSearchButton')?.click();
            }
        });

        document.getElementById('trainingExperiencePageSize')?.addEventListener('change', async event => {
            const nextPageSize = Number.parseInt(event.target.value, 10);
            trainingExperienceState.pageSize = Number.isInteger(nextPageSize) ? nextPageSize : 10;
            trainingExperienceState.pageNumber = 1;
            await loadTrainingExperiencePanel();
        });

        document.getElementById('trainingExperiencePrevPage')?.addEventListener('click', async () => {
            if (trainingExperienceState.pageNumber <= 1) return;
            trainingExperienceState.pageNumber -= 1;
            await loadTrainingExperiencePanel();
        });

        document.getElementById('trainingExperienceNextPage')?.addEventListener('click', async () => {
            const totalPages = Math.max(1, Math.ceil(trainingExperienceState.totalCount / trainingExperienceState.pageSize));
            if (trainingExperienceState.pageNumber >= totalPages) return;
            trainingExperienceState.pageNumber += 1;
            await loadTrainingExperiencePanel();
        });

        document.getElementById('trainingExperiencePageNumbers')?.addEventListener('click', async event => {
            const button = event.target.closest('[data-page-number]');
            if (!button) return;
            trainingExperienceState.pageNumber = Number.parseInt(button.dataset.pageNumber, 10) || 1;
            await loadTrainingExperiencePanel();
        });

        document.getElementById('trainingExperienceSelectAll')?.addEventListener('change', event => {
            document.querySelectorAll('.training-experience-row-selector').forEach(checkbox => {
                checkbox.checked = event.target.checked;
            });
        });

        document.getElementById('trainingExperienceTableBody')?.addEventListener('click', event => {
            const actionTrigger = event.target.closest('[data-action]');
            if (!actionTrigger) return;
            const action = actionTrigger.dataset.action;
            if (action === 'editTrainingExperience') {
                const trainingItem = findTrainingExperienceItem(actionTrigger.dataset.trainingExperienceId);
                if (trainingItem) {
                    showTrainingExperienceEditor(trainingItem);
                }
            }
            if (action === 'viewTrainingExperienceArchive') {
                openDocumentManager({
                    featureCode: trainingExperienceDocumentFeatureCode,
                    entityId: actionTrigger.dataset.trainingExperienceId || '0',
                    title: '培训历程档案管理',
                    emptyMessage: '请先保存培训历程记录，再管理档案。'
                });
            }
        });

        document.getElementById('trainingExperienceArchiveButton')?.addEventListener('click', event => {
            const button = event.currentTarget;
            openDocumentManager({
                featureCode: trainingExperienceDocumentFeatureCode,
                entityId: button?.dataset?.entityId || '0',
                title: '培训历程档案管理',
                emptyMessage: '请先保存培训历程记录，再管理档案。'
            });
        });
    }

    const getTaskFollowUpLabel = (options, code) => options.find(option => option.code === code)?.label || '';

    const renderTaskFollowUpOptionItems = (selectElement, options, includeAll = false) => {
        if (!selectElement) return;
        const previousValue = selectElement.value;
        selectElement.innerHTML = includeAll ? '<option value="">全部</option>' : '<option value="">请选择</option>';
        options.forEach(option => {
            const optionElement = document.createElement('option');
            optionElement.value = option.code;
            optionElement.textContent = option.label;
            selectElement.appendChild(optionElement);
        });
        if ([...selectElement.options].some(option => option.value === previousValue)) {
            selectElement.value = previousValue;
        }
    };

    const getTaskFollowUpEditorElements = () => ({
        editor: document.getElementById('taskFollowUpEditor'),
        id: document.getElementById('taskFollowUpId'),
        taskCode: document.getElementById('taskFollowUpTaskCode'),
        plannedDate: document.getElementById('taskFollowUpPlannedDate'),
        taskTypeCode: document.getElementById('taskFollowUpTaskTypeCode'),
        executorName: document.getElementById('taskFollowUpExecutorName'),
        statusCode: document.getElementById('taskFollowUpStatusCode'),
        priorityCode: document.getElementById('taskFollowUpPriorityCode'),
        progressPercent: document.getElementById('taskFollowUpProgressPercent'),
        completedDate: document.getElementById('taskFollowUpCompletedDate'),
        projectCode: document.getElementById('taskFollowUpProjectCode'),
        initiatorName: document.getElementById('taskFollowUpInitiatorName'),
        description: document.getElementById('taskFollowUpDescription'),
        archiveButton: document.getElementById('taskFollowUpArchiveButton')
    });

    function syncTaskFollowUpEditorAccess() {
        if (!taskFollowUpPanel) return;
        const isReadOnly = form.classList.contains('is-readonly');
        const editorElements = getTaskFollowUpEditorElements();
        ['taskFollowUpAddButton', 'taskFollowUpDeleteButton', 'taskFollowUpSaveButton'].forEach(id => {
            const button = document.getElementById(id);
            if (button instanceof HTMLButtonElement) {
                button.disabled = isReadOnly;
            }
        });
        Object.values(editorElements).forEach(element => {
            if (!(element instanceof HTMLElement)) return;
            if (element instanceof HTMLInputElement || element instanceof HTMLTextAreaElement || element instanceof HTMLSelectElement) {
                element.disabled = isReadOnly;
            }
        });
        if (editorElements.archiveButton instanceof HTMLButtonElement) {
            const targetEntityId = Number.parseInt(editorElements.archiveButton.dataset.entityId || '0', 10);
            editorElements.archiveButton.disabled = !Number.isInteger(targetEntityId) || targetEntityId <= 0;
            editorElements.archiveButton.title = editorElements.archiveButton.disabled ? '请先保存任务跟进记录，再管理档案。' : '打开档案管理';
        }
        const selectAll = document.getElementById('taskFollowUpSelectAll');
        if (selectAll instanceof HTMLInputElement) {
            selectAll.disabled = isReadOnly;
            if (isReadOnly) {
                selectAll.checked = false;
            }
        }

        document.querySelectorAll('.task-follow-up-row-selector').forEach(checkbox => {
            if (!(checkbox instanceof HTMLInputElement)) return;
            checkbox.disabled = isReadOnly;
            if (isReadOnly) {
                checkbox.checked = false;
            }
        });
    }

    const hideTaskFollowUpEditor = () => {
        const { editor } = getTaskFollowUpEditorElements();
        if (editor instanceof HTMLElement) {
            editor.hidden = true;
        }
    };

    const showTaskFollowUpEditor = item => {
        const editorElements = getTaskFollowUpEditorElements();
        if (!(editorElements.editor instanceof HTMLElement)) return;
        editorElements.editor.hidden = false;
        editorElements.id.value = item?.id ? String(item.id) : '';
        editorElements.taskCode.value = item?.taskCode || '';
        editorElements.plannedDate.value = item?.plannedDate || '';
        editorElements.taskTypeCode.value = item?.taskTypeCode || 'TASK';
        editorElements.executorName.value = item?.executorName || '';
        editorElements.statusCode.value = item?.statusCode || 'PENDING';
        editorElements.priorityCode.value = item?.priorityCode || 'NORMAL';
        editorElements.progressPercent.value = String(Number.isInteger(item?.progressPercent) ? item.progressPercent : 0);
        editorElements.completedDate.value = item?.completedDate || '';
        editorElements.projectCode.value = item?.projectCode || '';
        editorElements.initiatorName.value = item?.initiatorName || '';
        editorElements.description.value = item?.description || '';
        if (editorElements.archiveButton instanceof HTMLButtonElement) {
            editorElements.archiveButton.dataset.entityId = item?.id ? String(item.id) : '';
        }
        syncTaskFollowUpEditorAccess();
        editorElements.description.focus();
        editorElements.editor.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    };

    const renderTaskFollowUpPagination = () => {
        const pageNumbers = document.getElementById('taskFollowUpPageNumbers');
        const prevButton = document.getElementById('taskFollowUpPrevPage');
        const nextButton = document.getElementById('taskFollowUpNextPage');
        const summary = document.getElementById('taskFollowUpSummary');
        if (!pageNumbers || !prevButton || !nextButton || !summary) return;

        const totalPages = Math.max(1, Math.ceil(taskFollowUpState.totalCount / taskFollowUpState.pageSize));
        const currentPage = Math.min(taskFollowUpState.pageNumber, totalPages);
        taskFollowUpState.pageNumber = currentPage;
        summary.textContent = `共 ${taskFollowUpState.totalCount} 条，第 ${currentPage} / ${totalPages} 页`;
        prevButton.disabled = currentPage <= 1;
        nextButton.disabled = currentPage >= totalPages;

        pageNumbers.innerHTML = '';
        const startPage = Math.max(1, currentPage - 2);
        const endPage = Math.min(totalPages, startPage + 4);
        for (let page = startPage; page <= endPage; page += 1) {
            const button = document.createElement('button');
            button.type = 'button';
            button.className = `task-follow-up-pagebtn${page === currentPage ? ' is-active' : ''}`;
            button.textContent = String(page);
            button.dataset.pageNumber = String(page);
            pageNumbers.appendChild(button);
        }
    };

    const renderTaskFollowUpTable = () => {
        const tbody = document.getElementById('taskFollowUpTableBody');
        const selectAll = document.getElementById('taskFollowUpSelectAll');
        if (!tbody) return;

        if (taskFollowUpState.items.length === 0) {
            tbody.innerHTML = '<tr><td colspan="15" class="task-follow-up-table__empty">当前没有任务跟进记录。</td></tr>';
            if (selectAll instanceof HTMLInputElement) {
                selectAll.checked = false;
            }
            renderTaskFollowUpPagination();
            return;
        }

        tbody.innerHTML = taskFollowUpState.items.map(item => `
            <tr data-task-follow-up-id="${item.id}">
                <td class="task-follow-up-table__seq">${item.sequenceNo}</td>
                <td class="task-follow-up-table__checkbox"><input type="checkbox" class="task-follow-up-row-selector" value="${item.id}" ${form.classList.contains('is-readonly') ? 'disabled' : ''} /></td>
                <td><button type="button" class="task-follow-up-link" data-action="editTaskFollowUp" data-task-follow-up-id="${item.id}">${escapeHtml(item.taskCode)}</button></td>
                <td><button type="button" class="task-follow-up-archive-btn" data-action="viewTaskArchive" data-task-follow-up-id="${item.id}">档案</button></td>
                <td>${escapeHtml(item.statusLabel || '')}</td>
                <td>${escapeHtml(item.plannedDate || '')}</td>
                <td>${escapeHtml(item.taskTypeLabel || '')}</td>
                <td>${escapeHtml(item.executorName || '')}</td>
                <td class="task-follow-up-table__desc">${escapeHtml(item.description || '')}</td>
                <td>${escapeHtml(item.priorityLabel || '')}</td>
                <td>${escapeHtml(item.progressPercent ?? 0)}%</td>
                <td>${escapeHtml(item.completedDate || '')}</td>
                <td>${escapeHtml(item.projectCode || '')}</td>
                <td>${escapeHtml(item.initiatorName || '')}</td>
                <td>${escapeHtml(item.createdAt || '')}</td>
            </tr>
        `).join('');

        if (selectAll instanceof HTMLInputElement) {
            selectAll.checked = false;
        }
        renderTaskFollowUpPagination();
    };

    const loadTaskFollowUpPanel = async () => {
        if (!taskFollowUpPanel) return;
        if (taskFollowUpEntityId === '0') {
            taskFollowUpPanel.innerHTML = '<div class="account-panel__notice">请先保存员工基本资料，再新增任务跟进记录。</div>';
            taskFollowUpPanelLoaded = true;
            return;
        }

        try {
            const response = await fetch(getTaskFollowUpApiUrl(), { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
            const result = await response.json().catch(() => ({}));
            if (!response.ok) {
                taskFollowUpPanel.innerHTML = `<div class="account-panel__notice">${escapeHtml(result.message || '加载任务跟进数据失败，请稍后重试。')}</div>`;
                return;
            }

            taskFollowUpState.totalCount = Number(result.totalCount) || 0;
            taskFollowUpState.pageNumber = Number(result.pageNumber) || 1;
            taskFollowUpState.pageSize = Number(result.pageSize) || 10;
            taskFollowUpState.statusOptions = result.statusOptions || [];
            taskFollowUpState.taskTypeOptions = result.taskTypeOptions || [];
            taskFollowUpState.priorityOptions = result.priorityOptions || [];
            taskFollowUpState.items = result.items || [];

            renderTaskFollowUpOptionItems(document.getElementById('taskFollowUpStatusFilter'), taskFollowUpState.statusOptions, true);
            renderTaskFollowUpOptionItems(document.getElementById('taskFollowUpTaskTypeCode'), taskFollowUpState.taskTypeOptions);
            renderTaskFollowUpOptionItems(document.getElementById('taskFollowUpStatusCode'), taskFollowUpState.statusOptions);
            renderTaskFollowUpOptionItems(document.getElementById('taskFollowUpPriorityCode'), taskFollowUpState.priorityOptions);

            const pageSizeSelect = document.getElementById('taskFollowUpPageSize');
            if (pageSizeSelect instanceof HTMLSelectElement) {
                pageSizeSelect.value = String(taskFollowUpState.pageSize);
            }

            renderTaskFollowUpTable();
            syncTaskFollowUpEditorAccess();
            taskFollowUpPanelLoaded = true;

            const totalPages = Math.max(1, Math.ceil(taskFollowUpState.totalCount / taskFollowUpState.pageSize));
            if (taskFollowUpState.items.length === 0 && taskFollowUpState.totalCount > 0 && taskFollowUpState.pageNumber > totalPages) {
                taskFollowUpState.pageNumber = totalPages;
                await loadTaskFollowUpPanel();
            }
        } catch {
            taskFollowUpPanel.innerHTML = '<div class="account-panel__notice">网络请求失败，请稍后重试。</div>';
        }
    };

    const collectTaskFollowUpPayload = () => {
        const editorElements = getTaskFollowUpEditorElements();
        const progressPercent = Number.parseInt(editorElements.progressPercent.value || '0', 10);
        if (!editorElements.description.value.trim()) {
            window.alert('任务描述不能为空。');
            editorElements.description.focus();
            return null;
        }
        if (!Number.isInteger(progressPercent) || progressPercent < 0 || progressPercent > 100) {
            window.alert('执行进程必须输入 0 到 100 的整数。');
            editorElements.progressPercent.focus();
            return null;
        }

        return {
            id: editorElements.id.value ? Number.parseInt(editorElements.id.value, 10) : null,
            featureCode: taskFollowUpFeatureCode,
            entityId: Number.parseInt(taskFollowUpEntityId, 10),
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

    const saveTaskFollowUp = async () => {
        const payload = collectTaskFollowUpPayload();
        if (!payload) return;

        try {
            const response = await fetch(taskFollowUpApiBaseUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest'
                },
                body: JSON.stringify(payload)
            });
            const result = await response.json().catch(() => ({}));
            if (!response.ok) {
                window.alert(result.message || '保存任务跟进失败，请稍后重试。');
                return;
            }

            hideTaskFollowUpEditor();
            await loadTaskFollowUpPanel();
            window.alert(result.message || '任务跟进已保存。');
        } catch {
            window.alert('网络请求失败，请稍后重试。');
        }
    };

    const deleteTaskFollowUps = async () => {
        const selectedIds = [...document.querySelectorAll('.task-follow-up-row-selector:checked')]
            .map(checkbox => Number.parseInt(checkbox.value, 10))
            .filter(Number.isInteger);

        if (selectedIds.length === 0) {
            window.alert('请先勾选要删除的任务跟进记录。');
            return;
        }

        if (!window.confirm(`确定删除已选中的 ${selectedIds.length} 条任务跟进记录吗？`)) {
            return;
        }

        try {
            const response = await fetch(`${taskFollowUpApiBaseUrl}/delete`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest'
                },
                body: JSON.stringify({
                    featureCode: taskFollowUpFeatureCode,
                    entityId: Number.parseInt(taskFollowUpEntityId, 10),
                    ids: selectedIds
                })
            });
            const result = await response.json().catch(() => ({}));
            if (!response.ok) {
                window.alert(result.message || '删除任务跟进失败，请稍后重试。');
                return;
            }

            const totalPagesAfterDelete = Math.max(1, Math.ceil(Math.max(0, taskFollowUpState.totalCount - selectedIds.length) / taskFollowUpState.pageSize));
            taskFollowUpState.pageNumber = Math.min(taskFollowUpState.pageNumber, totalPagesAfterDelete);
            await loadTaskFollowUpPanel();
            hideTaskFollowUpEditor();
            window.alert(result.message || '任务跟进已删除。');
        } catch {
            window.alert('网络请求失败，请稍后重试。');
        }
    };

    const findTaskFollowUpItem = taskFollowUpId => taskFollowUpState.items.find(item => Number(item.id) === Number(taskFollowUpId));

    if (taskFollowUpPanel) {
        document.getElementById('taskFollowUpSearchButton')?.addEventListener('click', async () => {
            taskFollowUpState.keyword = document.getElementById('taskFollowUpKeyword')?.value?.trim() || '';
            taskFollowUpState.statusCode = document.getElementById('taskFollowUpStatusFilter')?.value || '';
            taskFollowUpState.pageNumber = 1;
            await loadTaskFollowUpPanel();
        });

        document.getElementById('taskFollowUpRefreshButton')?.addEventListener('click', async () => {
            const keywordInput = document.getElementById('taskFollowUpKeyword');
            const statusFilter = document.getElementById('taskFollowUpStatusFilter');
            if (keywordInput instanceof HTMLInputElement) keywordInput.value = '';
            if (statusFilter instanceof HTMLSelectElement) statusFilter.value = '';
            taskFollowUpState.keyword = '';
            taskFollowUpState.statusCode = '';
            taskFollowUpState.pageNumber = 1;
            hideTaskFollowUpEditor();
            await loadTaskFollowUpPanel();
        });

        document.getElementById('taskFollowUpAddButton')?.addEventListener('click', () => {
            showTaskFollowUpEditor(null);
        });

        document.getElementById('taskFollowUpDeleteButton')?.addEventListener('click', deleteTaskFollowUps);
        document.getElementById('taskFollowUpSaveButton')?.addEventListener('click', saveTaskFollowUp);
        document.getElementById('taskFollowUpCancelButton')?.addEventListener('click', hideTaskFollowUpEditor);

        document.getElementById('taskFollowUpKeyword')?.addEventListener('keydown', event => {
            if (event.key === 'Enter') {
                event.preventDefault();
                document.getElementById('taskFollowUpSearchButton')?.click();
            }
        });

        document.getElementById('taskFollowUpPageSize')?.addEventListener('change', async event => {
            const nextPageSize = Number.parseInt(event.target.value, 10);
            taskFollowUpState.pageSize = Number.isInteger(nextPageSize) ? nextPageSize : 10;
            taskFollowUpState.pageNumber = 1;
            await loadTaskFollowUpPanel();
        });

        document.getElementById('taskFollowUpPrevPage')?.addEventListener('click', async () => {
            if (taskFollowUpState.pageNumber <= 1) return;
            taskFollowUpState.pageNumber -= 1;
            await loadTaskFollowUpPanel();
        });

        document.getElementById('taskFollowUpNextPage')?.addEventListener('click', async () => {
            const totalPages = Math.max(1, Math.ceil(taskFollowUpState.totalCount / taskFollowUpState.pageSize));
            if (taskFollowUpState.pageNumber >= totalPages) return;
            taskFollowUpState.pageNumber += 1;
            await loadTaskFollowUpPanel();
        });

        document.getElementById('taskFollowUpPageNumbers')?.addEventListener('click', async event => {
            const button = event.target.closest('[data-page-number]');
            if (!button) return;
            taskFollowUpState.pageNumber = Number.parseInt(button.dataset.pageNumber, 10) || 1;
            await loadTaskFollowUpPanel();
        });

        document.getElementById('taskFollowUpSelectAll')?.addEventListener('change', event => {
            document.querySelectorAll('.task-follow-up-row-selector').forEach(checkbox => {
                checkbox.checked = event.target.checked;
            });
        });

        document.getElementById('taskFollowUpTableBody')?.addEventListener('click', event => {
            const actionTrigger = event.target.closest('[data-action]');
            if (!actionTrigger) return;
            const action = actionTrigger.dataset.action;
            if (action === 'editTaskFollowUp') {
                const taskItem = findTaskFollowUpItem(actionTrigger.dataset.taskFollowUpId);
                if (taskItem) {
                    showTaskFollowUpEditor(taskItem);
                }
            }
            if (action === 'viewTaskArchive') {
                openDocumentManager({
                    featureCode: taskFollowUpDocumentFeatureCode,
                    entityId: actionTrigger.dataset.taskFollowUpId || '0',
                    title: '任务跟进档案管理',
                    emptyMessage: '请先保存任务跟进记录，再管理档案。'
                });
            }
        });

        document.getElementById('taskFollowUpArchiveButton')?.addEventListener('click', event => {
            const button = event.currentTarget;
            openDocumentManager({
                featureCode: taskFollowUpDocumentFeatureCode,
                entityId: button?.dataset?.entityId || '0',
                title: '任务跟进档案管理',
                emptyMessage: '请先保存任务跟进记录，再管理档案。'
            });
        });
    }

    const saveAccountSettings = async ({ silentSuccess = false } = {}) => {
        if (!accountPanel) return;
        const loginAccountInput = accountPanel.querySelector("[name='accountLoginAccount']");
        const passwordInput = accountPanel.querySelector("[name='accountNewPassword']");
        const roleSelect = accountPanel.querySelector("[name='accountRoleId']");
        const validUntilInput = accountPanel.querySelector("[name='accountValidUntil']");
        const frozenInput = accountPanel.querySelector("[name='accountIsFrozen']");
        const forceViewRecordDaysInput = accountPanel.querySelector("[name='accountForceViewRecordDays']");
        const loginAccount = loginAccountInput?.value?.trim() || null;
        const newPassword = passwordInput?.value && passwordInput.value !== MASKED_PASSWORD_SENTINEL ? passwordInput.value : null;
        const roleIdValue = roleSelect?.value || '';
        const roleId = roleIdValue ? Number.parseInt(roleIdValue, 10) : null;
        const accountValidUntil = validUntilInput?.value || null;
        const isAccountFrozen = Boolean(frozenInput?.checked);
        const forceViewRecordDaysValue = forceViewRecordDaysInput?.value?.trim() || '';
        let forceViewRecordDays = null;
        if (forceViewRecordDaysValue) {
            forceViewRecordDays = Number.parseInt(forceViewRecordDaysValue, 10);
            if (Number.isNaN(forceViewRecordDays) || forceViewRecordDays < 0) {
                window.alert('强制查看记录日数必须是大于或等于 0 的整数。');
                forceViewRecordDaysInput?.focus();
                return false;
            }
        }
        const permissionIds = [...accountPanel.querySelectorAll("[name='permissionIds']:checked")].map(checkbox => Number.parseInt(checkbox.value, 10)).filter(Number.isInteger);
        const companyIds = [...accountPanel.querySelectorAll("[name='companyIds']:checked")].map(checkbox => Number.parseInt(checkbox.value, 10)).filter(Number.isInteger);
        try {
            const response = await fetch(getAccountApiUrl(), { method: 'POST', headers: { 'Content-Type': 'application/json', 'X-Requested-With': 'XMLHttpRequest' }, body: JSON.stringify({ loginAccount, newPassword, roleId, accountValidUntil, isAccountFrozen, forceViewRecordDays, permissionIds, companyIds }) });
            const result = await response.json().catch(() => ({}));
            if (!response.ok) {
                window.alert(result.message || '保存失败，请稍后重试。');
                return false;
            }
            accountPanelLoaded = false;
            await loadAccountPanel();
            if (!silentSuccess) {
                window.alert(result.message || '账号及权限保存成功。');
            }
            return true;
        } catch {
            window.alert('网络请求失败，请稍后重试。');
            return false;
        }
    };

    form.addEventListener('submit', async event => {
        if (isSubmittingWithAccountSync) return;
        if (form.classList.contains('is-readonly')) return;
        if (!accountPanelLoaded) return;

        event.preventDefault();
        const accountSaved = await saveAccountSettings({ silentSuccess: true });
        if (!accountSaved) return;

        isSubmittingWithAccountSync = true;
        form.submit();
    });
})();
