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
            //console.log('↓订阅的主播↓');
            console.log(data);
            // ['https://live.bilibili.com/1904551806', 'https://live.bilibili.com/1868870262']


            let html = `
            <table class="process-table">
                <thead>
                    <tr>
                        <th style="width:60px;">序号</th>
                        <th style="width:100px;">状态</th>
                        <th>主播</th>
                        <th>开始时间</th>
                        <th>订阅</th>
                    </tr>
                </thead>
                <tbody>`;

            if (data.length === 0) {
                html += `<tr><td colspan="4" style="text-align:center;color:#999;padding:30px;">暂无运行中的任务</td></tr>`;
            } else {
                data.forEach((linkItem, index) => {
                    const statusClass = 'other';
                    const statusText = '空闲中';
                    html += `
                    <tr>
                        <td>${index + 1}</td>
                        <td><span class="badge ${statusClass}">${statusText}</span></td>
                        <td class="upname">${linkItem.Name || '-'}</td>
                        <td>${linkItem.StartTimeDisplay || '-'}</td>
                        <td>${linkItem.Active ? "☑️" : "🔲"}</td>
                    </tr>`;
                });
            }

            html += `
                </tbody>
            </table>`;

            // 关键就这一行：改用 innerHTML，而不是 textContent
            upListTextLabel.innerHTML = html;

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
                        <th>订阅</th>
                    </tr>
                </thead>
                <tbody>`;

            if (data.length === 0) {
                html += `<tr><td colspan="4" style="text-align:center;color:#999;padding:30px;">暂无运行中的任务</td></tr>`;
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

                    html += `
                    <tr>
                        <td>${index + 1}</td>
                        <td><span class="badge ${statusClass}">${statusText}</span></td>
                        <td class="upname">${task.UpName || '-'}</td>
                        <td>${task.StartTimeDisplay || '-'}</td>
                        <td>☑️🟪 ⬜ ⏹️✔️❌🟦🔵 🟩✅❎🔲</td>
                    </tr>`;
                });
            }

            html += `
                </tbody>
            </table>`;

            // 关键就这一行：改用 innerHTML，而不是 textContent
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
}