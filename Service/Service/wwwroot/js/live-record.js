// live-record.js
export function initLiveRecordManager() {
    const API_GET_ROOMS = '/api/linkItem/get_rooms';
    const API_SET_ROOMS = '/api/linkItem/set_rooms';
    const API_SUBSCRIBE = '/api/linkItem/toggle_subscribe';
    const API_PROCESS = '/api/bilibili/running_tasks';
    const API_STOP = '/api/bilibili/stop_tasks';

    const upListTextLabel = document.querySelector('#upListText');
    //const processTextLabel = document.querySelector('#processText');
    const processStopBtn = document.getElementById('processStop');
    if (!upListTextLabel || !processStopBtn) {
        console.warn('LiveRecord 模块未找到对应元素，跳过初始化');
        return;
    }
    // 关键：监听 Bootstrap Tab 切换事件
    const liveRecordTab = document.querySelector('a[data-bs-target="#live-record-content"], a[href="#live-record-content"]');
    // 兼容两种常见写法：data-bs-target 或 href
    if (liveRecordTab) {
        liveRecordTab.addEventListener('shown.bs.tab', () => {
            // 每次切到 liveRecord Tab 都刷新一次（即使已经请求过，也拿最新）
            console.log('每次切到 liveRecord Tab 都刷新一次（即使已经请求过，也拿最新）')
            fetchGetRooms();
            //fetchCurrentProcess();
        });
    } else {
        console.warn('未找到 liveRecord 的 Tab 按钮，降级为页面加载时请求一次');
        fetchGetRooms();
        //fetchCurrentProcess(); // 降级方案
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

            let html = `
            <table class="process-table">
                <thead>
                    <tr>
                        <th style="width:60px;">序号</th>
                        <th style="width:140px;">状态</th>
                        <th>主播</th>
                        <th style="width:120px;text-align:center;">是否订阅</th>
                    </tr>
                </thead>
                <tbody>`;

            if (data.length === 0) {
                html += `<tr><td colspan="4" style="text-align:center;color:#999;padding:30px;">暂无订阅主播</td></tr>`;
            } else {
                data.forEach((item, index) => {
                    const isRecording = item.CurrentStatus === "录制中" &&
                        item.StartTime &&
                        item.DurationSeconds > 0;

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

                    html += `
                    <tr>
                        <td>${index + 1}</td>
                        <td class="status-cell">${statusHtml}</td>
                        <td class="upname" title="${escapeHtml(item.Name || '-')}">
                            ${escapeHtml(item.Name || '-')}
                        </td>
                        <td style="text-align:center;">
                            <label class="toggle-switch">
                                <input type="checkbox"
                                       data-taskid="${item.Id}"
                                       ${item.IsSubscribed ? 'checked' : ''}>
                                <span class="slider"></span>
                            </label>
                        </td>
                    </tr>`;
                });
            }

            html += `
                </tbody>
            </table>`;

            upListTextLabel.innerHTML = html;

            // 绑定订阅开关事件
            document.querySelectorAll('input[type="checkbox"][data-taskid]').forEach(checkbox => {
                checkbox.onchange = null;
                checkbox.addEventListener('change', async function () {
                    const taskId = this.dataset.taskid;
                    const newState = this.checked;

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
                    } catch (err) {
                        console.error('订阅切换失败:', err);
                        this.checked = !newState;
                        alert(err.message || '操作失败');
                    }
                });
            });

            // 启动倒计时
            updateAllCountdowns();                    // 立即更新一次
            setInterval(updateAllCountdowns, 1000);   // 每秒更新

        } catch (err) {
            console.error('获取列表失败:', err);
            upListTextLabel.innerHTML = '<p style="color:red;text-align:center;">加载失败，请刷新重试</p>';
        }
    }

    function escapeHtml(str) {
        if (!str) return '-';
        const div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    }

    // 倒计时更新 + 详细时间打印
    function updateAllCountdowns() {
        document.querySelectorAll('.badge.recording .countdown-text').forEach(span => {
            const startTimeIso = span.dataset.startTime;  // 后端传的原始 ISO 字符串
            const durationSec = parseInt(span.dataset.duration) || 0;
            const itemId = span.id.replace('countdown-', '');
            const upName = span.closest('tr')?.querySelector('.upname')?.textContent.trim() || '未知主播';

            if (!startTimeIso || durationSec <= 0) {
                span.textContent = '00:00';
                return;
            }

            // ========== 时间计算 ==========
            const startTime = new Date(startTimeIso);                  // 解析为 Date 对象
            const startTimestamp = startTime.getTime();                // 毫秒时间戳
            const nowTimestamp = Date.now();                           // 当前毫秒时间戳
            const now = new Date(nowTimestamp);                        // 当前 Date 对象

            const elapsedSec = Math.floor((nowTimestamp - startTimestamp) / 1000);
            const remainingSec = Math.max(0, durationSec - elapsedSec);

            const plannedEndTime = new Date(startTimestamp + durationSec * 1000);

            // ========== 详细打印 ==========
            console.log(`%c[时间详情] 主播: ${upName} (ID: ${itemId})`, 'font-weight: bold; color: #3498db;');
            console.log(`   开始时间（原始）: ${startTimeIso}`);
            console.log(`   开始时间（本地）: ${startTime.toLocaleString()}`);
            console.log(`   当前时间（本地）: ${now.toLocaleString()}`);
            console.log(`   计划结束时间    : ${plannedEndTime.toLocaleString()}`);
            console.log(`   已过去秒数      : ${elapsedSec} 秒`);
            console.log(`   计划时长        : ${durationSec} 秒`);
            console.log(`   剩余秒数        : ${remainingSec} 秒`);
            console.log(`   倒计时显示      : ${remainingSec > 0 ?
                Math.floor(remainingSec / 60).toString().padStart(2, '0') + ':' + (remainingSec % 60).toString().padStart(2, '0') :
                '00:00'}`);

            // ========== 原有每秒更新打印 ==========
            if (remainingSec > 0) {
                const m = Math.floor(remainingSec / 60).toString().padStart(2, '0');
                const s = (remainingSec % 60).toString().padStart(2, '0');
                console.log(`[倒计时更新] 主播: ${upName} (ID: ${itemId})  剩余: ${m}:${s}`);
            }

            // ========== UI 更新 ==========
            if (remainingSec > 0) {
                const m = Math.floor(remainingSec / 60).toString().padStart(2, '0');
                const s = (remainingSec % 60).toString().padStart(2, '0');
                span.textContent = `${m}:${s}`;
                span.dataset.lastRemaining = remainingSec.toString();
            } else {
                span.textContent = '00:00';

                // 只在刚结束时打印一次结束日志
                if (span.dataset.lastRemaining && parseInt(span.dataset.lastRemaining) > 0) {
                    console.log(`%c[录制结束] 主播: ${upName} (ID: ${itemId})  已完成录制，时长 ${durationSec} 秒`,
                        'color: #27ae60; font-weight: bold; font-size: 14px;');
                }
                span.dataset.lastRemaining = '0';

                // 自动变回“空闲”
                const badge = span.closest('.badge.recording');
                if (badge && !badge.dataset.ended) {
                    badge.outerHTML = '<span class="badge other">空闲</span>';
                    badge.dataset.ended = 'true';
                    console.log(`[状态更新] 主播: ${upName} 已变回“空闲”`);
                }
            }
        });
    }

    //👆合并👇//

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