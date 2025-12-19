// live-record.js
export function initLiveRecordManager() {
    const API_GET_ROOMS = '/api/linkItem/get_rooms';
    const API_PROCESS = '/api/bilibili/running_tasks';
    const API_STOP = '/api/bilibili/stop_tasks';

    const upListTextLabel = document.querySelector('#upListText');
    const processTextLabel = document.querySelector('#processText');
    const processStopBtn = document.getElementById('processStop');

    if (!upListTextLabel || !processTextLabel || !processStopBtn) {
        console.warn('LiveRecord 模块未找到对应元素，跳过初始化');
        return;
    }

    // 关键：监听 Bootstrap Tab 切换事件
    const liveRecordTab = document.querySelector('a[data-bs-target="#live-record-content"], a[href="#live-record-content"]');
    // 兼容两种常见写法：data-bs-target 或 href
    if (liveRecordTab) {
        liveRecordTab.addEventListener('shown.bs.tab', () => {
            // 每次切到 liveRecord Tab 都刷新一次（即使已经请求过，也拿最新）
            fetchGetRooms();
            fetchCurrentProcess();
        });

        // 可选：第一次手动点开时如果还没请求过，也请求一次
        // 如果你希望第一次进入页面就显示（即使没点 Tab），可以加下面这行：
        // if (liveRecordTab.parentElement.classList.contains('active')) fetchCurrentSchedule();
    } else {
        console.warn('未找到 liveRecord 的 Tab 按钮，降级为页面加载时请求一次');
        fetchGetRooms();
        fetchCurrentProcess(); // 降级方案
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
            console.log(data); // 订阅的主播

            let html = `
            <table class="process-table">
                <thead>
                    <tr>
                        <th style="width:60px;">序号</th>
                        <th style="width:100px;">状态</th>
                        <th>主播</th>
                        <th>开始时间</th>
                        <th style="width:120px;text-align:center;">是否订阅</th>
                    </tr>
                </thead>
                <tbody>`;

            if (data.length === 0) {
                html += `<tr><td colspan="4" style="text-align:center;color:#999;padding:30px;">暂无运行中的任务</td></tr>`;
            } else {
                data.forEach((linkItem, index) => {
                    const statusClass = 'other';
                    const statusText = '空闲中';

                    // 假设任务对象里有 IsSubscribed 字段（true/false），没有就默认 false
                    const isSubscribed = linkItem.Active === true;

                    html += `
                        <tr>
                            <td>${index + 1}</td>
                            <td><span class="badge ${statusClass}">${statusText}</span></td>
                            <td class="upname" title="${linkItem.Name || '-'}">${linkItem.Name || '-'}</td>
                            <td>${linkItem.StartTimeDisplay || '-'}</td>
                            <td style="text-align:center;">
                                <label class="toggle-switch">
                                    <input type="checkbox" 
                                           data-taskid="${linkItem.Id || index}" 
                                           ${isSubscribed ? 'checked' : ''}>
                                    <span class="slider"></span>
                                </label>
                            </td>
                        </tr>`;
                });
            }

            html += `
                </tbody>
            </table>`;

            // 关键就这一行：改用 innerHTML，而不是 textContent
            upListTextLabel.innerHTML = html;

            // ============ 新增：动态绑定所有订阅开关的事件 ============
            document.querySelectorAll('input[type="checkbox"][data-taskid]').forEach(checkbox => {
                // 先移除旧的监听器（防止重复绑定，比如多次刷新表格）
                checkbox.onchange = null;

                checkbox.addEventListener('change', async function () {
                    const taskId = this.dataset.taskid;
                    const newState = this.checked;

                    console.log(`任务 ${taskId} 订阅状态切换为: ${newState ? '订阅' : '取消订阅'}`);

                    // 如果你已经有后端接口，取消注释下面这段：
                    /*
                    try {
                        const response = await fetch('/api/toggle-subscribe', {  // ← 改成你的真实接口
                            method: 'POST',
                            credentials: 'include',
                            headers: { 'Content-Type': 'application/json' },
                            body: JSON.stringify({ taskId: taskId, subscribe: newState })
                        });
            
                        if (!response.ok) throw new Error('更新失败');
            
                        // 可选：成功提示
                        // alert(newState ? '订阅成功' : '取消订阅成功');
            
                    } catch (err) {
                        console.error('订阅切换失败:', err);
                        this.checked = !newState;  // 失败时回滚开关状态（超级重要！）
                        alert('操作失败，请重试或检查网络');
                    }
                    */

                    // 暂时没接口？就用上面 console.log 测试，开关也能正常点
                });
            });

        } catch (err) {
            console.error('当前任务数失败:', err);
        }
    }

    //👆合并👇//

    /**
     * GET 当前任务数
     */
    async function fetchCurrentProcess() {
        try {
            const response = await fetch(API_PROCESS, {
                method: 'GET',
                credentials: 'include',
                headers: { 'Accept': 'application/json' },
            });
            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            const data = await response.json();

            console.log('↓运行的任务↓');
            console.log(data);

            let html = `
            <table class="process-table">
                <thead>
                    <tr>
                        <th style="width:60px;">序号</th>
                        <th style="width:100px;">状态</th>
                        <th>主播</th>
                        <th>开始时间</th>
                        <th style="width:120px;text-align:center;">是否订阅</th>
                    </tr>
                </thead>
                <tbody>`;

            if (data.length === 0) {
                html += `<tr><td colspan="5" style="text-align:center;color:#999;padding:30px;">暂无运行中的任务</td></tr>`;
            } else {
                data.forEach((task, index) => {
                    const statusClass =
                        task.Status === 'Running' ? 'running' :
                            task.Status === 'Completed' ? 'completed' :
                                task.Status === 'Failed' ? 'failed' : 'other';

                    const statusText =
                        task.Status === 'Running' ? '运行中' :
                            task.Status === 'Completed' ? '已完成' :
                                task.Status === 'Failed' ? '失败' : task.Status;

                    // 假设任务对象里有 IsSubscribed 字段（true/false），没有就默认 false
                    const isSubscribed = task.IsSubscribed === true;
                    const checkedAttr = isSubscribed ? 'checked' : '';

                    // 给每个 toggle 一个唯一 ID，方便后续操作（用 TaskId 最稳）
                    const toggleId = `subscribe-toggle-${task.TaskId || index}`;

                    html += `
                    <tr>
                        <td>${index + 1}</td>
                        <td><span class="badge ${statusClass}">${statusText}</span></td>
                        <td class="upname" title="${task.UpName || '-'}">${task.UpName || '-'}</td>
                        <td>${task.StartTimeDisplay || '-'}</td>
                        <td style="text-align:center;">
                            <label class="toggle-switch">
                                <input type="checkbox" id="${toggleId}" ${checkedAttr} 
                                       data-taskid="${task.TaskId}" 
                                       onchange="toggleSubscribe(this)">
                                <span class="slider"></span>
                            </label>
                        </td>
                    </tr>`;
                });
            }

            html += `
                </tbody>
            </table>`;

            processTextLabel.innerHTML = html;

        } catch (err) {
            console.error('获取当前任务失败:', err);
            processTextLabel.innerHTML = `<div style="color:#e74c3c;text-align:center;padding:20px;">加载失败：${err.message}</div>`;
        }
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