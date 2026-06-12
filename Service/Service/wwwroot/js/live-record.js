// live-record.js
export function initLiveRecordManager() {
    const API_GET_ROOMS = '/api/linkItem/get_rooms';
    const API_SET_ROOMS = '/api/linkItem/set_rooms';
    const API_SUBSCRIBE = '/api/linkItem/toggle_subscribe';
    const API_PROCESS = '/api/bilibili/running_tasks';
    const API_STOP = '/api/bilibili/stop_tasks';

    let allRoomsData = [];  // 保存完整数据用于过滤
    let countdownIntervalId = null; // 用于管理全局定时器，防止多次渲染后定时器叠加叠加变快

    const upListTextLabel = document.querySelector('#upListText');
    const processStopBtn = document.getElementById('processStop');

    if (!upListTextLabel || !processStopBtn) {
        console.warn('LiveRecord 模块未找到对应元素，跳过初始化');
        return;
    }

    // 关键：监听 Bootstrap Tab 切换事件
    const liveRecordTab = document.querySelector('a[data-bs-target="#live-record-content"], a[href="#live-record-content"]');
    if (liveRecordTab) {
        liveRecordTab.addEventListener('shown.bs.tab', () => {
            console.log('每次切到 liveRecord Tab 都刷新一次（即使已经请求过，也拿最新）');
            fetchGetRooms();
        });
    } else {
        console.warn('未找到 liveRecord 的 Tab 按钮，降级为页面加载时请求一次');
        fetchGetRooms();
    }

    /**
     * GET 订阅的主播列表
     */
    async function fetchGetRooms() {
        try {
            const response = await fetch(API_GET_ROOMS, {
                method: 'GET',
                credentials: 'include',
                headers: { 'Accept': 'application/json' },
            });

            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            const data = await response.json();
            console.log('订阅主播列表:', data);

            allRoomsData = data;  // 保存原始数据
            renderRoomsTable(data);  // 首次渲染

            // 绑定搜索事件
            bindSearchEvents();

        } catch (err) {
            console.error('获取列表失败:', err);
            upListTextLabel.innerHTML = '<p style="color:red;text-align:center;">加载失败，请刷新重试</p>';
        }
    }

    /**
     * 渲染表格函数
     */
    function renderRoomsTable(data) {
        let html = `
    <table class="process-table">
        <thead>
            <tr>
                <th style="width:60px;">序号</th>
                <th style="width:140px;">状态</th>
                <th>主播</th>
                <th>房间号</th>
                <th style="width:120px;text-align:center;">是否订阅</th>
                <th style="width:160px;">最后录制</th>
            </tr>
        </thead>
        <tbody>`;

        if (data.length === 0) {
            html += `<tr><td colspan="6" style="text-align:center;color:#999;padding:30px;">未找到匹配的主播</td></tr>`;
        } else {
            data.forEach((item, index) => {
                const isRecording = item.CurrentStatus === "录制中" &&
                    item.StartTime && item.DurationSeconds > 0;

                let statusHtml = '';
                if (isRecording) {
                    statusHtml = `
                <span class="badge recording">
                    <span class="stripe-overlay"></span>
                    <span id="countdown-${item.Id}"
                          data-start-time="${item.StartTime}"
                          data-duration="${item.DurationSeconds}"
                          class="countdown-text">
                        计算中...
                    </span>
                </span>`;
                } else {
                    statusHtml = `<span class="badge other">空闲</span>`;
                }

                const lastRecordedHtml = formatLastRecorded(item.LastRecordedAt);

                html += `
            <tr>
                <td>${index + 1}</td>
                <td class="status-cell">${statusHtml}</td>
                <td class="upname" title="${escapeHtml(item.Name || '-')}">
                    ${escapeHtml(item.Name || '-')}
                </td>
                <td>${escapeHtml(item.RoomId || '-')}</td>
                <td style="text-align:center;">
                    <label class="toggle-switch">
                        <input type="checkbox"
                               data-taskid="${item.Id}"
                               ${item.IsSubscribed ? 'checked' : ''}>
                        <span class="slider"></span>
                    </label>
                </td>
                <td class="last-recorded">${lastRecordedHtml}</td>
            </tr>`;
            });
        }

        html += `</tbody></table>`;
        upListTextLabel.innerHTML = html;

        // 重新绑定开关事件（每次渲染后都需要）
        bindToggleEvents();

        // 重置并拉起倒计时
        updateAllCountdowns(); // 立即更新一次
        if (countdownIntervalId) clearInterval(countdownIntervalId); // 清除旧的，防止多重定时器叠加
        countdownIntervalId = setInterval(updateAllCountdowns, 1000); // 解决问题①：重新注入每秒更新
    }

    /**
     * 搜索事件绑定
     */
    function bindSearchEvents() {
        const searchInput = document.getElementById('liveSearchInput');
        const clearBtn = document.getElementById('liveSearchClear');

        if (!searchInput) return;

        // 移除旧监听防止重复触发
        searchInput.oninput = null;
        const doSearch = () => {
            const keyword = searchInput.value.trim().toLowerCase();
            if (!keyword) {
                renderRoomsTable(allRoomsData);
                return;
            }

            const filtered = allRoomsData.filter(item =>
                (item.Name && item.Name.toLowerCase().includes(keyword)) ||
                (item.RoomId && String(item.RoomId).toLowerCase().includes(keyword))
            );
            renderRoomsTable(filtered);
        };

        searchInput.addEventListener('input', doSearch);

        if (clearBtn) {
            clearBtn.onclick = null;
            clearBtn.addEventListener('click', () => {
                searchInput.value = '';
                renderRoomsTable(allRoomsData);
            });
        }
    }

    /**
     * 解决问题②&③：复活并绑定原版 Toggle 切换逻辑
     */
    function bindToggleEvents() {
        document.querySelectorAll('input[type="checkbox"][data-taskid]').forEach(checkbox => {
            checkbox.onchange = null;  // 清理旧事件
            checkbox.addEventListener('change', async function () {
                const taskId = this.dataset.taskid;
                const newState = this.checked;
                console.log(`任务 ${taskId} 订阅状态切换为: ${newState ? '订阅' : '取消订阅'}`);

                try {
                    const resp = await fetch(API_SUBSCRIBE, {
                        method: 'POST',
                        credentials: 'include',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ id: parseInt(taskId) })
                    });

                    if (!resp.ok) {
                        const err = await resp.json().catch(() => ({}));
                        throw new Error(err.message || '更新失败');
                    }

                    // 同步修改本地内存中的数据状态，确保搜索筛选时状态是对的
                    const localItem = allRoomsData.find(item => item.Id === parseInt(taskId));
                    if (localItem) {
                        localItem.IsSubscribed = newState;
                    }

                } catch (err) {
                    console.error('订阅切换失败:', err);
                    this.checked = !newState; // 失败时回滚开关状态
                    alert(err.message || '操作失败');
                }
            });
        });
    }

    function escapeHtml(str) {
        if (!str) return '-';
        const div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    }

    /**
     * 倒计时核心计算
     */
    function updateAllCountdowns() {
        const remainingElements = document.querySelectorAll('.badge.recording .countdown-text');

        // 如果当前页面没有任何录制中的节点，且定时器还在，可以考虑不执行
        if (remainingElements.length === 0) return;

        remainingElements.forEach(span => {
            const startTimeIso = span.dataset.startTime;
            const durationSec = parseInt(span.dataset.duration) || 0;
            const itemId = span.id.replace('countdown-', '');
            const upName = span.closest('tr')?.querySelector('.upname')?.textContent.trim() || '未知主播';

            if (!startTimeIso || durationSec <= 0) {
                span.textContent = '00:00';
                return;
            }

            const startTime = new Date(startTimeIso);
            const startTimestamp = startTime.getTime();
            const nowTimestamp = Date.now();

            const elapsedSec = Math.floor((nowTimestamp - startTimestamp) / 1000);
            const remainingSec = Math.max(0, durationSec - elapsedSec);

            if (remainingSec > 0) {
                const m = Math.floor(remainingSec / 60).toString().padStart(2, '0');
                const s = (remainingSec % 60).toString().padStart(2, '0');
                span.textContent = `${m}:${s}`;
                span.dataset.lastRemaining = remainingSec.toString();
            } else {
                span.textContent = '00:00';

                if (span.dataset.lastRemaining && parseInt(span.dataset.lastRemaining) > 0) {
                    console.log(`%c[录制结束] 主播: ${upName} (ID: ${itemId})  已完成录制，时长 ${durationSec} 秒`,
                        'color: #27ae60; font-weight: bold; font-size: 14px;');
                }
                span.dataset.lastRemaining = '0';

                const badge = span.closest('.badge.recording');
                if (badge && !badge.dataset.ended) {
                    badge.outerHTML = '<span class="badge other">空闲</span>';
                    badge.dataset.ended = 'true';
                    console.log(`[状态更新] 主播: ${upName} 已变回“空闲”`);
                }
            }
        });
    }

    // 格式化最后录制时间
    function formatLastRecorded(dateStr) {
        if (!dateStr) return '<span style="color:#999;">从未录制</span>';

        // 【核心就在这里】
        // 后端 Docker 存的是标准 UTC 并在 API 吐出了 ISO 格式，但少了个 'Z'。
        // 我们在这里手动补上 'Z'，告诉 JavaScript 它是 UTC。
        // 浏览器会自动把它 +8 小时，转换成真正的北京时间！
        const recordDate = new Date(dateStr.includes('Z') ? dateStr : dateStr + 'Z');

        const now = new Date();

        // 下面是原有的对齐 0 点计算天数的逻辑，保持不动
        const recordDateZero = new Date(recordDate.getFullYear(), recordDate.getMonth(), recordDate.getDate());
        const nowZero = new Date(now.getFullYear(), now.getMonth(), now.getDate());
        const diffDays = Math.floor((nowZero - recordDateZero) / (1000 * 60 * 60 * 24));

        const hours = recordDate.getHours().toString().padStart(2, '0');
        const minutes = recordDate.getMinutes().toString().padStart(2, '0');

        if (diffDays === 0) return `<span style="color:#4ade80;">今天 ${hours}:${minutes}</span>`;
        if (diffDays === 1) return `<span style="color:#fbbf24;">昨天 ${hours}:${minutes}</span>`;
        if (diffDays < 7) return `<span>${diffDays}天前</span>`;

        const yyyy = recordDate.getFullYear();
        const mm = (recordDate.getMonth() + 1).toString().padStart(2, '0');
        const dd = recordDate.getDate().toString().padStart(2, '0');
        return `<span style="color:#999;">${yyyy}-${mm}-${dd}</span>`;
    }

    /**
     * POST 停止所有任务
     */
    async function stopTasks(user) {
        try {
            const response = await fetch(API_STOP, {
                method: 'POST',
                credentials: 'include',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(user),
            });

            if (!response.ok) {
                console.log('停止失败');
                return;
            }
            const data = await response.json();
            console.log('👇停止成功👇');
            console.log(data);
            //alert('任务停止成功！');
        } catch (err) {
            console.error('更新计划失败:', err);
        }
    }

    processStopBtn.addEventListener('click', function (e) {
        // 阻止表单默认提交行为（因为按钮是 type="submit"）
        e.preventDefault();

        console.log('已点击【停止所有】按钮');
        alert('停止所有任务的逻辑在这里执行');

        // 在这里写你真正的“停止所有”逻辑
        //stopAllProcesses(); //POST
    });
}