// schedule.js
export function initScheduleManager() {
    const API_GET = '/api/schedule/current';
    const API_POST = '/api/schedule/update';

    const scheduleForm = document.getElementById('scheduleForm');
    const scheduleJsonInput = document.getElementById('scheduleJson');
    const scheduleTextLabel = document.querySelector('#scheduleText');
    if (!scheduleForm || !scheduleJsonInput || !scheduleTextLabel) {
        console.warn('Schedule 模块未找到对应元素，跳过初始化');
        return;
    }
    // 关键：监听 Bootstrap Tab 切换事件
    const dashboardTab = document.querySelector('a[data-bs-target="#dashboard-content"], a[href="#dashboard-content"]');
    if (dashboardTab) {
        dashboardTab.addEventListener('shown.bs.tab', () => {
            fetchCurrentSchedule();
        });
    } else {
        console.warn('未找到 dashboard 的 Tab 按钮，降级为页面加载时请求一次');
        fetchCurrentSchedule();
    }


    let hasFetched = false; // 标记是否已经请求过，避免重复请求
    /**
     * GET 当前计划并更新显示
     */
    async function fetchCurrentSchedule() {
        console.log('GET 当前计划并更新显示'); //👈这时间是服务器时区。
        try {
            const response = await fetch(API_GET, {
                method: 'GET',
                credentials: 'include',
                headers: { 'Accept': 'application/json' },
            });

            if (!response.ok) throw new Error(`HTTP ${response.status}`);

            const data = await response.json();
            console.log(data);
            console.log(data['currentTimes']); //['00:00', '04:00', '08:00', '12:00', '14:00', '16:00', '20:00']
            console.log(data['serverTimeZone']); //{ianaId: 'UTC', offsetMinutes: 0}
            console.log('↓↓↓↓↓↓↓↓↓↓');
            console.log('↓转成客户端时区↓');
            const localTimes = convertServerTimesToLocal(
                data.currentTimes,
                data.serverTimeZone.ianaId  // 或 data.serverTimeZone.offsetMinutes（备用）
            );
            console.log(localTimes); //['16:00', '20:00', '00:00', '04:00', '06:00', '08:00', '12:00']👈？？

            updateDisplayAndInput(localTimes);
            hasFetched = true;
        } catch (err) {
            console.error('获取当前计划失败:', err);
            scheduleTextLabel.textContent = '当前计划：获取失败';
        }
    }
    /**
     * 将服务器时区的时间列表转换为客户端本地时区时间
     * @param {string[]} serverTimes - 如 ["00:00", "08:00", "12:00"]
     * @param {string} serverIanaId - 服务器时区 IANA ID（如 "Asia/Shanghai"、"China Standard Time" 在现代浏览器中也支持）
     * @returns {string[]} 本地时区的时间字符串数组（如 ["16:00", "00:00", "04:00"]）
     */
    function convertServerTimesToLocal(serverTimes, serverIanaId) {
        // 获取今天在服务器时区下的日期字符串（yyyy-mm-dd）
        const todayInServerTz = new Intl.DateTimeFormat('sv', {  // 'sv' 为 ISO 格式
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            timeZone: serverIanaId
        }).format(new Date());

        const converted = serverTimes.map(timeStr => {
            const [h, m] = timeStr.split(':').map(Number);
            const hh = String(h).padStart(2, '0');
            const mm = String(m).padStart(2, '0');

            // 构造服务器时区的日期时间字符串
            const serverDateTimeStr = `${todayInServerTz} ${hh}:${mm}:00`;

            // 用服务器时区格式化成可被 new Date() 正确解析的字符串
            const formattedInServerTz = new Intl.DateTimeFormat('en-US', {
                year: 'numeric',
                month: '2-digit',
                day: '2-digit',
                hour: '2-digit',
                minute: '2-digit',
                second: '2-digit',
                hour12: false,
                timeZone: serverIanaId
            }).formatToParts(new Date(serverDateTimeStr))
                .map(part => part.value)
                .join('');

            const date = new Date(formattedInServerTz);

            // 转为客户端固定时区（UTC+8）
            let localFormatted = new Intl.DateTimeFormat('en-US', {
                hour: '2-digit',
                minute: '2-digit',
                hour12: false,
                timeZone: 'Asia/Shanghai'
            }).format(date);

            localFormatted = localFormatted.replace(/AM|PM/i, '').trim();
            // 确保小时两位（部分 locale 可能输出单数字，如 9:00）
            return localFormatted.replace(/^(\d):/, '0$1:');
        });

        // ★★★ 前端排序：按时间从小到大 ★★★
        converted.sort((a, b) => a.localeCompare(b));

        return converted;
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
        console.log('↑↑↑↑↑↑↑↑POST');
        console.log(timesArray);
        console.log('↑POST客户端时区↑');
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
            console.log('↓服务器返回↓');
            console.log(data);
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

        updateSchedule(timesArray); //js也在客户端，这里还不知道服务器的时区
    });
}