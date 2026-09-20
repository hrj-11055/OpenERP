(() => {
    const apiBaseUrl = '/api/documents';
    const imageContentTypes = new Set(['image/jpeg', 'image/png', 'image/webp', 'image/gif']);
    const imageExtensions = new Set(['.jpg', '.jpeg', '.png', '.webp', '.gif']);
    // 文档管理弹窗状态（保存当前业务记录、编辑权限、显示模式和文档列表）。
    const state = {
        featureCode: '',
        entityId: 0,
        title: '档案管理',
        canEdit: true,
        mode: 'documents',
        primaryUrl: '',
        emptyText: '当前没有关联文档。',
        onPrimarySelected: null,
        items: []
    };

    const escapeHtml = value => String(value ?? '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/\"/g, '&quot;')
        .replace(/'/g, '&#39;');

    const normalizeUrl = value => {
        if (!value) return '';
        try {
            const url = new URL(value, window.location.origin);
            return `${url.pathname}${url.search}`.toLowerCase();
        } catch {
            return String(value).trim().toLowerCase();
        }
    };

    const isImageDocument = item => {
        const contentType = String(item?.contentType || '').toLowerCase();
        const extension = String(item?.fileExtension || '').toLowerCase();
        return imageContentTypes.has(contentType) || imageExtensions.has(extension);
    };

    const getRequestVerificationToken = () =>
        document.querySelector("input[name='__RequestVerificationToken']")?.value
        || document.querySelector("meta[name='request-verification-token']")?.content
        || '';

    const buildUnsafeRequestHeaders = () => {
        const token = getRequestVerificationToken();
        if (!token) {
            return null;
        }

        return {
            'X-Requested-With': 'XMLHttpRequest',
            'RequestVerificationToken': token
        };
    };

    // 获取或创建文档管理弹窗外壳。
    const getShell = () => {
        let shell = document.getElementById('commonDocumentManager');
        if (shell) return shell;

        shell = document.createElement('div');
        shell.id = 'commonDocumentManager';
        shell.className = 'document-manager-lock';
        shell.hidden = true;
        shell.innerHTML = `
            <div class="document-manager-dialog" role="dialog" aria-modal="true" aria-labelledby="documentManagerTitle">
                <div class="document-manager-dialog__header">
                    <h2 class="document-manager-dialog__title" id="documentManagerTitle">档案管理</h2>
                    <button type="button" class="document-manager-icon-btn" data-document-action="close" aria-label="关闭档案管理">
                        <i class="bi bi-x-lg" aria-hidden="true"></i>
                    </button>
                </div>
                <div class="document-manager-dialog__toolbar">
                    <div class="document-manager-dialog__summary" id="documentManagerSummary">共 0 个文档</div>
                    <div class="document-manager-dialog__actions">
                        <input id="documentManagerFileInput" type="file" multiple hidden />
                        <button type="button" class="document-manager-btn" data-document-action="upload">
                            <i class="bi bi-upload" aria-hidden="true"></i><span>追加上传</span>
                        </button>
                        <button type="button" class="document-manager-btn" data-document-action="refresh">
                            <i class="bi bi-arrow-clockwise" aria-hidden="true"></i><span>刷新</span>
                        </button>
                    </div>
                </div>
                <div class="document-manager-status" id="documentManagerStatus" hidden></div>
                <div class="document-manager-content" id="documentManagerContent"></div>
            </div>
        `;
        document.body.appendChild(shell);
        bindShellEvents(shell);
        return shell;
    };

    // 设置文档管理弹窗状态消息。
    const setStatus = (message, isError = false) => {
        const status = document.getElementById('documentManagerStatus');
        if (!status) return;
        status.hidden = !message;
        status.textContent = message || '';
        status.classList.toggle('is-error', isError);
    };

    // 设置文档管理弹窗忙碌状态。
    const setBusy = isBusy => {
        const shell = getShell();
        shell.classList.toggle('is-busy', isBusy);
        shell.querySelectorAll('button').forEach(button => {
            const action = button.dataset.documentAction;
            button.disabled = isBusy || (!state.canEdit && (action === 'upload' || action === 'delete' || action === 'setPrimary'));
        });
    };

    const renderTable = content => {
        if (state.items.length === 0) {
            content.innerHTML = `<div class="document-manager-empty">${escapeHtml(state.emptyText)}</div>`;
            return;
        }

        content.innerHTML = `
            <div class="document-manager-table-shell">
                <table class="document-manager-table">
                    <thead>
                        <tr>
                            <th>文档名</th>
                            <th>文档类型</th>
                            <th>文档大小</th>
                            <th>上传时间</th>
                            <th>上传人</th>
                            <th>操作</th>
                        </tr>
                    </thead>
                    <tbody id="documentManagerTableBody">
                        ${state.items.map(item => `
                            <tr data-document-id="${item.id}">
                                <td class="document-manager-table__name" title="${escapeHtml(item.originalFileName)}">${escapeHtml(item.originalFileName)}</td>
                                <td>${escapeHtml(item.contentType || item.fileExtension || '')}</td>
                                <td>${escapeHtml(item.fileSizeLabel || '')}</td>
                                <td>${escapeHtml(item.uploadedAt || '')}</td>
                                <td>${escapeHtml(item.uploadedBy || '')}</td>
                                <td class="document-manager-table__actions">
                                    <button type="button" class="document-manager-row-btn" data-document-action="open" data-document-id="${item.id}" title="打开文档">
                                        <i class="bi bi-box-arrow-up-right" aria-hidden="true"></i><span>打开</span>
                                    </button>
                                    <button type="button" class="document-manager-row-btn" data-document-action="download" data-document-id="${item.id}" title="下载文档">
                                        <i class="bi bi-download" aria-hidden="true"></i><span>下载</span>
                                    </button>
                                    <button type="button" class="document-manager-row-btn document-manager-row-btn--danger" data-document-action="delete" data-document-id="${item.id}" title="删除文档" ${state.canEdit ? '' : 'disabled'}>
                                        <i class="bi bi-trash" aria-hidden="true"></i><span>删除</span>
                                    </button>
                                </td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            </div>
        `;
    };

    const renderImageGrid = content => {
        const imageItems = state.items.filter(isImageDocument);
        if (imageItems.length === 0) {
            content.innerHTML = `<div class="document-manager-empty">${escapeHtml(state.emptyText || '当前没有员工图片。')}</div>`;
            return;
        }

        const primaryUrl = normalizeUrl(state.primaryUrl);
        content.innerHTML = `
            <div class="document-manager-image-grid">
                ${imageItems.map(item => {
                    const isPrimary = primaryUrl && normalizeUrl(item.openUrl) === primaryUrl;
                    return `
                        <article class="document-manager-image-card${isPrimary ? ' is-primary' : ''}" data-document-id="${item.id}">
                            <div class="document-manager-image-card__preview">
                                <img src="${escapeHtml(item.openUrl)}" alt="${escapeHtml(item.originalFileName)}" loading="lazy" />
                                ${isPrimary ? '<span class="document-manager-image-card__badge">主图</span>' : ''}
                            </div>
                            <div class="document-manager-image-card__body">
                                <div class="document-manager-image-card__name" title="${escapeHtml(item.originalFileName)}">${escapeHtml(item.originalFileName)}</div>
                                <div class="document-manager-image-card__meta">${escapeHtml(item.fileSizeLabel || '')} · ${escapeHtml(item.uploadedAt || '')}</div>
                            </div>
                            <div class="document-manager-image-card__actions">
                                <button type="button" class="document-manager-row-btn" data-document-action="open" data-document-id="${item.id}" title="预览图片">
                                    <i class="bi bi-box-arrow-up-right" aria-hidden="true"></i><span>预览</span>
                                </button>
                                <button type="button" class="document-manager-row-btn" data-document-action="setPrimary" data-document-id="${item.id}" title="设为员工主图" ${state.canEdit && !isPrimary ? '' : 'disabled'}>
                                    <i class="bi bi-person-bounding-box" aria-hidden="true"></i><span>${isPrimary ? '当前主图' : '设为主图'}</span>
                                </button>
                                <button type="button" class="document-manager-row-btn document-manager-row-btn--danger" data-document-action="delete" data-document-id="${item.id}" title="删除图片" ${state.canEdit ? '' : 'disabled'}>
                                    <i class="bi bi-trash" aria-hidden="true"></i><span>删除</span>
                                </button>
                            </div>
                        </article>
                    `;
                }).join('')}
            </div>
        `;
    };

    // 渲染当前业务记录的文档或图片列表。
    const render = () => {
        const title = document.getElementById('documentManagerTitle');
        const summary = document.getElementById('documentManagerSummary');
        const content = document.getElementById('documentManagerContent');
        const uploadButton = document.querySelector("[data-document-action='upload']");
        const fileInput = document.getElementById('documentManagerFileInput');
        const visibleItems = state.mode === 'images' ? state.items.filter(isImageDocument) : state.items;
        const unitText = state.mode === 'images' ? '张图片' : '个文档';
        if (title) title.textContent = state.title || '档案管理';
        if (summary) summary.textContent = `共 ${visibleItems.length} ${unitText}`;
        if (uploadButton instanceof HTMLButtonElement) {
            uploadButton.disabled = !state.canEdit;
            uploadButton.title = state.canEdit ? '追加上传新文档' : '查看模式不能上传文档';
        }
        if (fileInput instanceof HTMLInputElement) {
            fileInput.accept = state.mode === 'images'
                ? 'image/jpeg,image/png,image/webp,image/gif'
                : '';
        }
        if (!content) return;

        if (state.mode === 'images') {
            renderImageGrid(content);
        } else {
            renderTable(content);
        }
    };

    // 加载当前业务记录关联的文档列表。
    const loadDocuments = async () => {
        setBusy(true);
        setStatus(state.mode === 'images' ? '正在加载图片...' : '正在加载文档...');
        try {
            const url = `${apiBaseUrl}?featureCode=${encodeURIComponent(state.featureCode)}&entityId=${encodeURIComponent(state.entityId)}`;
            const response = await fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
            const result = await response.json().catch(() => ({}));
            if (!response.ok) {
                setStatus(result.message || '加载文档失败，请稍后重试。', true);
                return;
            }

            state.items = result.items || [];
            setStatus('');
            render();
        } catch {
            setStatus('网络请求失败，请稍后重试。', true);
        } finally {
            setBusy(false);
        }
    };

    // 上传文档并追加到当前业务记录。
    const uploadDocuments = async files => {
        if (!files || files.length === 0) return;
        if (!state.canEdit) return;

        if (state.mode === 'images') {
            const invalidFiles = [...files].filter(file => !imageContentTypes.has(String(file.type || '').toLowerCase()));
            if (invalidFiles.length > 0) {
                setStatus('员工图片仅支持 JPG、PNG、WEBP 或 GIF 格式。', true);
                return;
            }
        }

        const headers = buildUnsafeRequestHeaders();
        if (!headers) {
            setStatus('页面缺少防伪令牌，请刷新后重试。', true);
            return;
        }

        const formData = new FormData();
        formData.append('featureCode', state.featureCode);
        formData.append('entityId', String(state.entityId));
        [...files].forEach(file => formData.append('files', file));

        setBusy(true);
        setStatus(state.mode === 'images' ? '正在上传图片...' : '正在上传文档...');
        try {
            const response = await fetch(`${apiBaseUrl}/upload`, {
                method: 'POST',
                headers,
                body: formData
            });
            const result = await response.json().catch(() => ({}));
            if (!response.ok) {
                setStatus(result.message || '上传文档失败，请稍后重试。', true);
                return;
            }

            setStatus(result.message || '文档已上传。');
            await loadDocuments();
        } catch {
            setStatus('网络请求失败，请稍后重试。', true);
        } finally {
            setBusy(false);
        }
    };

    // 删除当前文档集合中的指定文档。
    const deleteDocument = async documentId => {
        if (!state.canEdit) return;
        const documentItem = state.items.find(item => Number(item.id) === Number(documentId));
        const confirmText = state.mode === 'images' ? '确定删除这张图片吗？' : '确定删除这个文档吗？';
        if (!window.confirm(confirmText)) return;

        const headers = buildUnsafeRequestHeaders();
        if (!headers) {
            setStatus('页面缺少防伪令牌，请刷新后重试。', true);
            return;
        }

        const wasPrimary = state.mode === 'images'
            && documentItem?.openUrl
            && normalizeUrl(documentItem.openUrl) === normalizeUrl(state.primaryUrl);

        setBusy(true);
        setStatus(state.mode === 'images' ? '正在删除图片...' : '正在删除文档...');
        try {
            const response = await fetch(`${apiBaseUrl}/${encodeURIComponent(documentId)}`, {
                method: 'DELETE',
                headers
            });
            const result = await response.json().catch(() => ({}));
            if (!response.ok) {
                setStatus(result.message || '删除文档失败，请稍后重试。', true);
                return;
            }

            if (wasPrimary && typeof state.onPrimarySelected === 'function') {
                const callbackResult = await state.onPrimarySelected(null);
                if (callbackResult !== false) {
                    state.primaryUrl = '';
                }
            }

            setStatus(result.message || '文档已删除。');
            await loadDocuments();
        } catch {
            setStatus('网络请求失败，请稍后重试。', true);
        } finally {
            setBusy(false);
        }
    };

    // 将图片文档设置为业务主图。
    const setPrimaryImage = async documentId => {
        if (!state.canEdit || state.mode !== 'images') return;
        const documentItem = state.items.find(item => Number(item.id) === Number(documentId));
        if (!documentItem || !isImageDocument(documentItem)) {
            setStatus('请选择有效的图片文档。', true);
            return;
        }

        if (typeof state.onPrimarySelected !== 'function') {
            state.primaryUrl = documentItem.openUrl || '';
            render();
            return;
        }

        setBusy(true);
        setStatus('正在设置主图...');
        try {
            const callbackResult = await state.onPrimarySelected(documentItem);
            if (callbackResult === false) {
                return;
            }

            state.primaryUrl = typeof callbackResult === 'string' ? callbackResult : (documentItem.openUrl || '');
            setStatus('主图已更新。');
            render();
        } catch {
            setStatus('设置主图失败，请稍后重试。', true);
        } finally {
            setBusy(false);
        }
    };

    // 关闭文档管理弹窗。
    const close = () => {
        const shell = getShell();
        shell.hidden = true;
    };

    // 绑定文档管理弹窗内的按钮、文件选择与快捷键事件。
    const bindShellEvents = shell => {
        const fileInput = shell.querySelector('#documentManagerFileInput');
        shell.addEventListener('click', event => {
            if (event.target === shell) {
                close();
                return;
            }

            const trigger = event.target.closest('[data-document-action]');
            if (!trigger) return;

            const action = trigger.dataset.documentAction;
            const documentId = Number.parseInt(trigger.dataset.documentId || '0', 10);
            const documentItem = state.items.find(item => Number(item.id) === documentId);
            if (action === 'close') {
                close();
                return;
            }
            if (action === 'refresh') {
                void loadDocuments();
                return;
            }
            if (action === 'upload' && fileInput instanceof HTMLInputElement) {
                fileInput.click();
                return;
            }
            if (action === 'open' && documentItem?.openUrl) {
                window.open(documentItem.openUrl, '_blank', 'noopener');
                return;
            }
            if (action === 'download' && documentItem?.downloadUrl) {
                window.location.href = documentItem.downloadUrl;
                return;
            }
            if (action === 'setPrimary' && documentId > 0) {
                void setPrimaryImage(documentId);
                return;
            }
            if (action === 'delete' && documentId > 0) {
                void deleteDocument(documentId);
            }
        });

        fileInput?.addEventListener('change', event => {
            const input = event.target;
            if (!(input instanceof HTMLInputElement)) return;
            void uploadDocuments(input.files);
            input.value = '';
        });

        document.addEventListener('keydown', event => {
            if (event.key === 'Escape' && !shell.hidden) {
                close();
            }
        });
    };

    // 暴露给各业务页面复用的文档管理入口。
    window.openErpDocumentManager = {
        open(options) {
            const nextFeatureCode = (options?.featureCode || '').trim();
            const nextEntityId = Number.parseInt(options?.entityId || '0', 10);
            if (!nextFeatureCode || !Number.isInteger(nextEntityId) || nextEntityId <= 0) {
                window.alert(options?.emptyMessage || '请先保存当前业务记录，再管理档案。');
                return;
            }

            state.featureCode = nextFeatureCode;
            state.entityId = nextEntityId;
            state.title = options?.title || '档案管理';
            state.canEdit = options?.canEdit !== false;
            state.mode = options?.mode === 'images' ? 'images' : 'documents';
            state.primaryUrl = options?.primaryUrl || '';
            state.emptyText = options?.emptyText || (state.mode === 'images' ? '当前没有员工图片。' : '当前没有关联文档。');
            state.onPrimarySelected = typeof options?.onPrimarySelected === 'function' ? options.onPrimarySelected : null;
            state.items = [];

            const shell = getShell();
            shell.hidden = false;
            render();
            void loadDocuments();
        },
        close
    };
})();
