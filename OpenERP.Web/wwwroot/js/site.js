// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

(() => {
    // 员工资料模态状态（用于跟踪当前打开的详细页、遮罩层和刷新标记）。
    const employeeDetailState = {
        overlayElement: null,
        // 弹窗主体元素，用于按业务页面切换弹窗尺寸样式。
        dialogElement: null,
        iframeElement: null,
        titleElement: null,
        refreshRequested: false
    };

    // 判断当前页面是否运行在员工资料模态内嵌页中。
    const isEmbeddedDetailPage = () => window.parent && window.parent !== window;

    // 关闭员工资料详细页，并根据参数决定是否刷新父页面。
    const notifyDetailHostToClose = (refreshRequested) => {
        const message = {
            type: "open-erp:employee-modal-close",
            refreshRequested: !!refreshRequested
        };

        if (isEmbeddedDetailPage()) {
            window.parent.postMessage(message, window.location.origin);
            return true;
        }

        if (window.opener && !window.opener.closed) {
            window.opener.postMessage(message, window.location.origin);
            return true;
        }

        return false;
    };

    // 暴露给员工详细页按钮使用的关闭方法，兼容模态、独立窗口和普通页面。
    window.closeEmployeeDetailPage = (refreshRequested = false) => {
        const handledByHost = notifyDetailHostToClose(refreshRequested);

        if (window.opener && !window.opener.closed) {
            window.close();
            return;
        }

        if (!handledByHost && !isEmbeddedDetailPage()) {
            window.history.back();
        }
    };

    // 公司组织详细页复用同一套弹层关闭协议，提供独立别名便于页面脚本调用。
    window.closeCompanyOrganizationDetailPage = (refreshRequested = false) => {
        window.closeEmployeeDetailPage(refreshRequested);
    };

    // 构建员工资料模态外壳，父页面在模态关闭前保持锁定状态。
    const ensureEmployeeDetailModal = () => {
        if (employeeDetailState.overlayElement) {
            return employeeDetailState.overlayElement;
        }

        const overlayElement = document.createElement("div");
        overlayElement.className = "page-popup-lock page-popup-lock--modal";
        overlayElement.innerHTML = `
            <div class="page-popup-lock__dialog" role="dialog" aria-modal="true" aria-label="员工资料详细页">
                <div class="page-popup-lock__dialog-header">
                    <div class="page-popup-lock__dialog-title">员工资料</div>
                    <button type="button" class="page-popup-lock__dialog-close" aria-label="关闭员工资料详细页">
                        <i class="bi bi-x-lg"></i>
                    </button>
                </div>
                <div class="page-popup-lock__dialog-body">
                    <iframe class="page-popup-lock__iframe" title="员工资料详细页" loading="eager"></iframe>
                </div>
            </div>
        `;

        const closeButton = overlayElement.querySelector(".page-popup-lock__dialog-close");
        const dialogElement = overlayElement.querySelector(".page-popup-lock__dialog");
        const iframeElement = overlayElement.querySelector(".page-popup-lock__iframe");
        const titleElement = overlayElement.querySelector(".page-popup-lock__dialog-title");

        closeButton?.addEventListener("click", () => {
            closeEmployeeDetailModal(false);
        });

        document.body.appendChild(overlayElement);
        document.body.classList.add("page-popup-lock-open");

        employeeDetailState.overlayElement = overlayElement;
        employeeDetailState.dialogElement = dialogElement;
        employeeDetailState.iframeElement = iframeElement;
        employeeDetailState.titleElement = titleElement;

        return overlayElement;
    };

    // 关闭员工资料模态，并在需要时刷新当前列表页面。
    const closeEmployeeDetailModal = (refreshRequested) => {
        if (typeof refreshRequested === "boolean") {
            employeeDetailState.refreshRequested = refreshRequested;
        }

        if (employeeDetailState.iframeElement) {
            employeeDetailState.iframeElement.src = "about:blank";
        }

        employeeDetailState.overlayElement?.remove();
        employeeDetailState.overlayElement = null;
        employeeDetailState.dialogElement = null;
        employeeDetailState.iframeElement = null;
        employeeDetailState.titleElement = null;
        document.body.classList.remove("page-popup-lock-open");

        if (employeeDetailState.refreshRequested) {
            employeeDetailState.refreshRequested = false;
            window.location.reload();
            return;
        }

        employeeDetailState.refreshRequested = false;
    };

    // 统一打开员工资料模态，避免独立浏览器窗口显示地址栏。
    window.openBlockingPopup = (url, options = {}) => {
        if (!url || url === "#") {
            return null;
        }

        ensureEmployeeDetailModal();

        if (employeeDetailState.dialogElement) {
            employeeDetailState.dialogElement.className = "page-popup-lock__dialog";
            if (options.dialogClass) {
                employeeDetailState.dialogElement.classList.add(options.dialogClass);
            }
        }

        if (employeeDetailState.titleElement) {
            employeeDetailState.titleElement.textContent = options.title ?? "员工资料";
        }

        if (employeeDetailState.iframeElement) {
            employeeDetailState.iframeElement.src = url;
        }

        employeeDetailState.refreshRequested = false;
        return employeeDetailState.overlayElement;
    };

    // 接收员工资料页发回的关闭通知，关闭模态并按需刷新父页面。
    window.addEventListener("message", event => {
        if (event.origin !== window.location.origin) {
            return;
        }

        if (event.data?.type === "open-erp:employee-modal-close") {
            closeEmployeeDetailModal(!!event.data.refreshRequested);
        }
    });

    // 统一处理带 data-popup-url 的员工资料入口，改为页面内模态打开。
    document.addEventListener("click", event => {
        // 业务页面已接管的弹窗入口不再重复用默认尺寸打开。
        if (event.defaultPrevented) {
            return;
        }

        const trigger = event.target.closest("[data-popup-url]");
        if (!trigger) {
            return;
        }

        event.preventDefault();
        const popupUrl = trigger.getAttribute("data-popup-url");
        const popupTitle = trigger.getAttribute("data-popup-title") ?? "员工资料";
        window.openBlockingPopup(popupUrl, { title: popupTitle });
    });
})();
