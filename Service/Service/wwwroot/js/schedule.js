// schedule.js
export function initScheduleManager() {
    const API_PROCESS = '/api/bilibili/running_tasks';
    const API_STOP = '/api/bilibili/stop_tasks';
    const API_GET = '/api/schedule/current';
    const API_POST = '/api/schedule/update';

    const scheduleForm = document.getElementById('scheduleForm');
    const scheduleJsonInput = document.getElementById('scheduleJson');
    const scheduleTextLabel = document.querySelector('#scheduleText');
    const processTextLabel = document.querySelector('#processText');
    const processStopBtn = document.getElementById('processStop');



    if (!scheduleForm || !scheduleJsonInput || !scheduleTextLabel || !processTextLabel) {
        console.warn('Schedule 模块未找到对应元素，跳过初始化');
        return;
    }

    let hasFetched = false; // 标记是否已经请求过，避免重复请求

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

            const data = await response.json(); // 这里 data 就是你上面那个数组
            console.log(data);

            // 例如取第一个任务的 TaskId
            if (data.length > 0) {
                console.log('当前运行中的任务ID:', data[0].TaskId);
                console.log('状态:', data[0].Status);
            }

            let text = `序号 \t状态 \t主播 \t开始时间 \n`;
            //text += `────┼──────┼──────────────────┼─────────\n`;
            data.forEach((task, index) => {
                console.log(`在遍历：${index}`);
                const up_name = task.UpName;
                const time = task.StartTimeDisplay;

                // 根据状态加颜色标记（只是文本标记，textarea不认HTML）
                const statusMark = task.Status === 'Running' ? 'RUNNING' :
                    task.Status === 'Completed' ? 'COMPLETED' :
                        task.Status === 'Failed' ? 'FAILED' : task.Status;

                text += `${String(index + 1).padStart(3)} │ ${statusMark} │ ${up_name} │ ${time}\n`;
            });
            processTextLabel.textContent = text;

        } catch (err) {
            console.error('当前任务数失败:', err);
        }
    }

    /**
     * GET 当前计划并更新显示
     */
    async function fetchCurrentSchedule() {
        console.log('GET 当前计划并更新显示');
        try {
            const response = await fetch(API_GET, {
                method: 'GET',
                credentials: 'include',
                headers: { 'Accept': 'application/json' },
            });

            if (!response.ok) throw new Error(`HTTP ${response.status}`);

            const data = await response.json();
            //console.log(data);
            //console.log(data['currentTimes']);
            updateDisplayAndInput(data['currentTimes']);
            hasFetched = true;
        } catch (err) {
            console.error('获取当前计划失败:', err);
            scheduleTextLabel.textContent = '当前计划：获取失败';
        }
    }

    /**
     * 更新 label 和 input
     */
    function updateDisplayAndInput(timesArray) {
        if (Array.isArray(timesArray) && timesArray.length > 0) {
            scheduleTextLabel.textContent = `当前计划：${timesArray.join('，')}`;
            scheduleJsonInput.value = JSON.stringify(timesArray);
        } else {
            scheduleTextLabel.textContent = '当前计划：暂无';
            scheduleJsonInput.value = '';
        }
    }

    /**
     * POST 更新计划（保持不变）
     */
    async function updateSchedule(timesArray) {
        try {
            const response = await fetch(API_POST, {
                method: 'POST',
                credentials: 'include',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(timesArray),
            });

            if (!response.ok) {
                const text = await response.text();
                throw new Error(`更新失败: ${response.status} ${text}`);
                console.log('更新失败');
                return;
            }
            const data = await response.json();
            updateDisplayAndInput(data['current']);
            //alert('计划更新成功！');
        } catch (err) {
            console.error('更新计划失败:', err);
            alert('更新失败：' + err.message);
        }
    }

    // 表单提交
    scheduleForm.addEventListener('submit', (e) => {
        e.preventDefault();

        let inputValue = scheduleJsonInput.value.trim();
        if (!inputValue) return alert('请输入时间数组');

        let timesArray;
        try {
            timesArray = JSON.parse(inputValue);
            if (!Array.isArray(timesArray)) throw new Error();
        } catch {
            return alert('请输入正确的 JSON 数组格式，例如：["00:00", "04:00"]');
        }

        // 客户端当地时间 转成 Docker的UTC
        updateSchedule(convertLocalTimesToUTC(timesArray));
    });
    /**
     * 将本地时间字符串数组转换为 UTC 时间字符串数组
     * @param {string[]} localTimes - 本地时间数组，例如 ["08:00", "12:00"]
     * @returns {string[]} UTC 时间数组，例如 ["00:00", "04:00"]（假设本地为 UTC+8）
     */
    function convertLocalTimesToUTC(localTimes) {
        // 获取当前时区的偏移分钟数（本地时间减 UTC 的分钟数，例如 UTC+8 为 480）
        const offsetMinutes = new Date().getTimezoneOffset(); // 注意：返回值为负数或正数
        // 转换为小时（正值表示 UTC 到本地的偏移小时）
        const offsetHours = -offsetMinutes / 60;

        return localTimes.map(time => {
            // 解析时间字符串 "HH:MM"
            const [hours, minutes] = time.split(':').map(Number);

            // 本地时间转换为分钟
            let localMinutes = hours * 60 + minutes;

            // 减去偏移（本地到 UTC）
            let utcMinutes = localMinutes - offsetHours * 60;

            // 处理跨天（模 1440）
            utcMinutes = (utcMinutes + 1440) % 1440;

            // 转换回小时和分钟
            const utcHours = Math.floor(utcMinutes / 60);
            const utcMins = utcMinutes % 60;

            // 格式化为 "HH:MM"
            return `${utcHours.toString().padStart(2, '0')}:${utcMins.toString().padStart(2, '0')}`;
        });
    }
    // 示例使用（假设当前时区为 UTC+8，如中国标准时间）
    //console.log(convertLocalTimesToUTC(["08:00", "12:00"]));
    // 输出: ["00:00", "04:00"]
    // 另一个示例（假设时区为 UTC-5，如美国东部标准时间）
    //console.log(convertLocalTimesToUTC(["08:00", "12:00"]));
    // 输出: ["13:00", "17:00"]


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
    const dashboardTab = document.querySelector('a[data-bs-target="#dashboard-content"], a[href="#dashboard-content"]');
    // 兼容两种常见写法：data-bs-target 或 href

    if (dashboardTab) {
        dashboardTab.addEventListener('shown.bs.tab', () => {
            // 每次切到 dashboard Tab 都刷新一次（即使已经请求过，也拿最新）
            fetchCurrentSchedule();
            fetchCurrentProcess();
        });

        // 可选：第一次手动点开时如果还没请求过，也请求一次
        // 如果你希望第一次进入页面就显示（即使没点 Tab），可以加下面这行：
        // if (dashboardTab.parentElement.classList.contains('active')) fetchCurrentSchedule();
    } else {
        console.warn('未找到 dashboard 的 Tab 按钮，降级为页面加载时请求一次');
        fetchCurrentSchedule(); // 降级方案
        fetchCurrentProcess();
    }
}