// schedule.js
export function initScheduleManager() {
    const API_GET = 'http://localhost:8080/api/schedule/current';
    const API_POST = 'http://localhost:8080/api/schedule/update';

    const scheduleForm = document.getElementById('scheduleForm');
    const scheduleJsonInput = document.getElementById('scheduleJson');
    const scheduleTextLabel = document.querySelector('#scheduleText'); // 当前计划 显示的 label

    if (!scheduleForm || !scheduleJsonInput || !scheduleTextLabel) {
        console.warn('Schedule 模块未找到对应元素，跳过初始化');
        return;
    }

    /**
     * GET 当前计划
     */
    async function fetchCurrentSchedule() {
        console.log('GET 当前计划');
        return;
        try {
            const response = await fetch(API_GET, {
                method: 'GET',
                credentials: 'include', // 如果后端用了 session/cookie 认证的话需要带上
                headers: {
                    'Accept': 'application/json',
                },
            });

            if (!response.ok) throw new Error(`HTTP ${response.status}`);

            const data = await response.json(); // 期望返回 ["00:00", "04:00",...]
            updateDisplayAndInput(data);
        } catch (err) {
            console.error('获取当前计划失败:', err);
            scheduleTextLabel.textContent = '当前计划：获取失败';
        }
    }

    /**
     * POST 更新计划
     */
    async function updateSchedule(timesArray) {
        console.log('POST 更新计划');
        return;
        try {
            const response = await fetch(API_POST, {
                method: 'POST',
                credentials: 'include',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(timesArray),
            });

            if (!response.ok) {
                const text = await response.text();
                throw new Error(`更新失败: ${response.status} ${text}`);
            }

            const data = await response.json();
            updateDisplayAndInput(data); // 成功后用返回的数据刷新显示
            alert('计划更新成功！');
        } catch (err) {
            console.error('更新计划失败:', err);
            alert('更新失败：' + err.message);
        }
    }

    /**
     * 同时更新 label 显示 和 input 框内容
     */
    function updateDisplayAndInput(timesArray) {
        if (Array.isArray(timesArray) && timesArray.length > 0) {
            const displayText = timesArray.join('，');
            scheduleTextLabel.textContent = `当前计划：${displayText}`;
            scheduleJsonInput.value = JSON.stringify(timesArray);
        } else {
            scheduleTextLabel.textContent = '当前计划：暂无';
            scheduleJsonInput.value = '';
        }
    }

    /**
     * 表单提交事件
     */
    scheduleForm.addEventListener('submit', (e) => {
        e.preventDefault();

        let inputValue = scheduleJsonInput.value.trim();

        if (!inputValue) {
            alert('请输入时间数组');
            return;
        }

        let timesArray;
        try {
            // 支持直接粘贴 ["00:00","04:00"] 或 ["00:00", "04:00"] 两种格式
            timesArray = JSON.parse(inputValue);
            if (!Array.isArray(timesArray)) throw new Error();
        } catch (err) {
            alert('请输入正确的 JSON 数组格式，例如：["00:00", "04:00", "10:00"]');
            return;
        }

        // 简单校验时间格式（可选）
        const timeRegex = /^([0-1][0-9]|2[0-3]):[0-5][0-9]$/;
        const valid = timesArray.every(t => typeof t === 'string' && timeRegex.test(t));
        if (!valid) {
            if (!confirm('检测到有非标准时间格式，仍然提交？')) return;
        }

        updateSchedule(timesArray);
    });

    // 页面加载完立即获取一次
    fetchCurrentSchedule();
    console.log('页面加载完立即获取一次:');
}