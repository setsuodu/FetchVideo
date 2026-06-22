// js/video-downloader.js
export function initVideoDownloader() {
    const form = document.getElementById('videoDownloadForm');
    const submitBtn = form.querySelector('button[type="submit"]');
    const videoInput = document.getElementById('videoUrl');
    const DEFAULT_LENGTH = 10; // 定义默认值常量（placeholder只作提示，没有值）
    const resultDiv = document.getElementById('videoResult');
    const progressBar = document.getElementById('videoProgressBar');
    const status = document.getElementById('videoStatus');
    const log = document.getElementById('log-video');
    const logLink = document.getElementById('videoLogLink');
    const clearBtn = document.getElementById('clearBtn');
    // ====================== 新增：获取封面 ======================
    const getCoverBtn = document.getElementById('getCoverBtn');

    if (getCoverBtn) {
        getCoverBtn.addEventListener('click', async () => {
            const videoUrl = extractCleanUrl(videoInput.value);
            if (!videoUrl) {
                alert('请输入直播间 URL');
                return;
            }

            getCoverBtn.disabled = true;
            getCoverBtn.textContent = '获取中...';

            // 显示结果区域（像视频下载那样）
            resultDiv.classList.remove('d-none');
            progressBar.style.width = '100%';
            progressBar.textContent = '100%';
            status.textContent = '正在获取封面...';
            log.textContent = '';
            logLink.classList.add('d-none');

            try {
                const encodedUrl = encodeURIComponent(videoUrl);
                const response = await fetch(`/api/bilibili/get_cover?url=${encodedUrl}`, {
                    method: 'GET',
                    headers: { 'Accept': 'application/json' }
                });

                const data = await response.json();
                if (!response.ok) throw new Error(data.message || '获取失败');

                // ✅ 像视频下载完成那样显示UI
                status.innerHTML = `
                <strong class="text-success">封面获取成功！</strong><br>
                文件: <code>${data.coverPath}</code><br>
                UP: <code>${data.upName || 'N/A'}</code><br>
                标题: <code>${data.title || 'N/A'}</code>
            `;

                log.textContent = `封面已下载至服务器。`;

                // 显示下载按钮（如果路径可访问）
                if (data.coverPath) {
                    logLink.href = data.coverPath;
                    logLink.textContent = '下载封面';
                    logLink.classList.remove('d-none');
                }

            } catch (err) {
                progressBar.style.width = '100%';
                progressBar.classList.add('bg-danger');
                status.innerHTML = `<span class="text-danger">获取失败: ${err.message}</span>`;
                log.textContent = '请检查 URL 或服务状态。';
            } finally {
                getCoverBtn.disabled = false;
                getCoverBtn.textContent = '🖼️ 获取封面';
            }
        });
    }

    // ============================================================

    // ====================== 新增：订阅 Toggle ======================
    const subscribeToggle = document.getElementById('subscribeToggle');
    // ====================== 新增：默认关闭订阅 ======================
    if (subscribeToggle) {
        subscribeToggle.checked = false;   // 默认关闭（未订阅）
        console.log('[订阅 Toggle] 已设置为默认关闭');
    }
    // ============================================================

    let currentTaskId = null; // 记录当前录制任务 ID
    let isRecording = false;

    //console.log(`录制时长: ${getRecordLength()}`);
    let recordLength = 10;
    function getRecordLength() {
        const inputElement = document.getElementById('recordLength');

        // 1. 使用 .value 获取输入的值 (返回字符串)
        const inputValue = inputElement.value;

        let finalLength;

        if (inputValue === "" || inputValue === null) {
            // 2. 如果值是空字符串 (用户未输入)
            finalLength = DEFAULT_LENGTH;
            console.log(`输入为空，使用默认值: ${finalLength}`);

        } else {
            // 3. 如果用户输入了值，则将其转换为数字。
            finalLength = Number(inputValue);

            if (isNaN(finalLength)) {
                console.warn("输入值无效，回退到默认值。");
                finalLength = DEFAULT_LENGTH;
            }
        }

        console.log(`最终长度为: ${finalLength} (类型: ${typeof finalLength})`);

        return finalLength;
    }

    ///////////////////
    /* 时钟功能start */
    let timer = null;    // 保存 setInterval 的 id
    let seconds = 0;     // 记录经过的秒数
    function formatTime(sec) {
        const m = String(Math.floor(sec / 60)).padStart(2, '0');
        const s = String(sec % 60).padStart(2, '0');
        return `${m}:${s}`;
    }
    function startTimer() {
        if (timer) return; // 防止重复开始
        let recordLengthSec = recordLength * 60;
        timer = setInterval(() => {
            seconds++;
            progressBar.textContent = formatTime(seconds);

            if (seconds > recordLengthSec) {
                console.log(`计时器到了: ${seconds}`);
                if (isRecording && currentTaskId) {
                    console.log(`计时器满足条件，重置 taskId=${currentTaskId}`);
                    stopTimer();
                    status.innerHTML = `
                        <strong class="text-info">已停止录制</strong><br>
                        任务 ID: <code>${currentTaskId}</code><br>
                        文件已保存
                    `;
                    log.textContent = `录制已终止，文件已保存。`;

                    setStartButton();
                    unlockForm();
                    progressBar.style.width = '0%';
                    progressBar.textContent = '—';
                    resetTimer();
                }
            }
        }, 1000);
    }
    function stopTimer() {
        clearInterval(timer);
        timer = null;
    }
    function resetTimer() {
        stopTimer();
        seconds = 0;
        progressBar.textContent = '00:00';
    }
    /* 时钟功能end */
    /////////////////

    // 锁定表单
    const lockForm = () => {
        submitBtn.disabled = true;
        videoInput.disabled = true;
        videoInput.classList.add('disabled');
    };

    // 解锁表单
    const unlockForm = () => {
        submitBtn.disabled = false;
        videoInput.disabled = false;
        videoInput.classList.remove('disabled');
    };

    // 设置按钮为“停止录制”（红色）
    const setStopButton = () => {
        submitBtn.textContent = '停止录制';
        submitBtn.classList.remove('btn-success');
        submitBtn.classList.add('btn-danger');
        isRecording = true;
    };

    // 恢复为“开始下载”（绿色）
    const setStartButton = () => {
        console.log('开始下载');
        submitBtn.textContent = '开始下载';
        submitBtn.classList.remove('btn-danger');
        submitBtn.classList.add('btn-success');
        isRecording = false;
        currentTaskId = null;
    };

    // 按下按钮
    form.addEventListener('submit', async (e) => {
        e.preventDefault();

        // 如果正在录制，点击即为“停止”
        if (isRecording && currentTaskId) {
            stopTimer();
            await stopRecording();
            resetTimer();
            return;
        }

        // —— 开始下载 / 录制 ——
        lockForm();
        resultDiv.classList.remove('d-none');
        progressBar.style.width = '0%';
        progressBar.textContent = '0%';
        status.textContent = '正在检查 URL...';
        log.textContent = '';
        logLink.classList.add('d-none');

        const videoUrl = encodeURIComponent(extractCleanUrl(videoInput.value));
        console.log(`videoUrl: ${videoUrl}`);

        // ====================== 新增订阅逻辑 ======================
        let subscribe = false;
        if (subscribeToggle) {
            subscribe = subscribeToggle.checked;
        }
        console.log(`[订阅] 状态: ${subscribe}`);
        // =========================================================

        recordLength = getRecordLength();
        console.log(`录制时长: ${recordLength} min`);

        // ====================== 关键修改：加上 subscribe ======================
        const apiUrl = `/api/route/check?url=${videoUrl}&length=${recordLength}&subscribe=${subscribe}`;
        // =====================================================================

        try {
            const responsePromise = fetch(apiUrl, {
                method: 'GET',
                headers: { 'Accept': 'application/json' }
            });

            let fakeProgress = 0;
            const fakeInterval = setInterval(() => {
                fakeProgress += Math.random() * 8 + 2;
                if (fakeProgress >= 90) { fakeProgress = 90; clearInterval(fakeInterval); }
                progressBar.style.width = fakeProgress + '%';
                progressBar.textContent = Math.round(fakeProgress) + '%';
            }, 300);

            const response = await responsePromise;
            const data = await response.json();
            console.log(`收到响应:`, data);
            clearInterval(fakeInterval);

            if (!response.ok) throw new Error(data.message || data.error || '请求失败');

            progressBar.style.width = '100%';
            progressBar.textContent = '100%';
            progressBar.classList.remove('progress-bar-animated');

            const isLiveRecording = data.downloadUrl == "Convert";

            if (isLiveRecording) {
                currentTaskId = data.file;
                setStopButton();
                unlockForm();
                resetTimer();
                startTimer();
                progressBar.classList.add('progress-bar-animated');

                status.innerHTML = `
                    <strong class="text-warning">录制中...</strong><br>
                    任务 ID: <code>${currentTaskId}</code><br>
                    点击 <strong>停止录制</strong> 终止
                `;

                log.textContent = `直播录制已启动，任务 ID: ${currentTaskId}，点击按钮可停止。`;

            } else {
                setStartButton();
                status.innerHTML = `
                    <strong class="text-success">下载完成！</strong><br>
                    文件: <code>${data.output}</code><br>
                    时长: <code>${data.duration}</code> 分钟<br>
                `;

                log.textContent = `视频已下载至服务器。`;

                if (data.logPath || data.downloadUrl) {
                    logLink.href = data.logPath || data.downloadUrl;
                    logLink.textContent = `下载文件/日志`;
                    logLink.classList.remove('d-none');
                }
            }

        } catch (err) {
            progressBar.style.width = '100%';
            progressBar.textContent = '错误';
            progressBar.classList.add('bg-danger');
            status.innerHTML = `<span class="text-danger">错误: ${err.message}</span>`;
            log.textContent = '请检查 URL 或服务状态。';
            setStartButton();
        } finally {
            if (!isRecording) {
                unlockForm();
            }
        }
    });

    // 清理 URL
    function extractCleanUrl(str) {
        const match = str.match(/(https?:\/\/[^\s"。』」】）》]+)/i);
        return match ? match[1].trim() : '';
    }

    // 停止录制函数
    async function stopRecording() {
        if (!currentTaskId) return;

        lockForm();
        submitBtn.textContent = '停止中...';

        try {
            const stopResponse = await fetch(`/api/route/stop?taskId=${currentTaskId}`, {
                method: 'GET',
                headers: { 'Accept': 'application/json' }
            });

            const stopData = await stopResponse.json();
            console.log('收到响应' + stopData);

            if (!stopResponse.ok) throw new Error(stopData.message || '停止失败');

            status.innerHTML = `
                <strong class="text-info">已停止录制</strong><br>
                任务 ID: <code>${currentTaskId}</code><br>
                文件已保存
            `;

            log.textContent = `录制已终止，文件已保存。`;

            if (stopData.filePath || stopData.downloadUrl) {
                logLink.href = stopData.filePath || stopData.downloadUrl;
                logLink.textContent = `下载录制文件`;
                logLink.classList.remove('d-none');
            }

        } catch (err) {
            status.innerHTML = `<span class="text-danger">停止失败: ${err.message}</span>`;
        } finally {
            setStartButton();
            unlockForm();
            progressBar.style.width = '0%';
            progressBar.textContent = '—';
        }
    }

    // Show/hide button based on input value
    videoInput.addEventListener('input', function () {
        if (videoInput.value.length > 0) {
            clearBtn.classList.remove('d-none');
        } else {
            clearBtn.classList.add('d-none');
        }
    });

    // Clear input on button click
    clearBtn.addEventListener('click', function () {
        videoInput.value = '';
        clearBtn.classList.add('d-none');
        videoInput.focus();
    });
}