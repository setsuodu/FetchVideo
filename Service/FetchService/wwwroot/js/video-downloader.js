// js/video-downloader.js
export function initVideoDownloader() {
    const form = document.getElementById('videoDownloadForm');
    const submitBtn = form.querySelector('button[type="submit"]');
    const videoInput = document.getElementById('videoUrl');
    const resultDiv = document.getElementById('videoResult');
    const progressBar = document.getElementById('videoProgressBar');
    const status = document.getElementById('videoStatus');
    const log = document.getElementById('log-video');
    const logLink = document.getElementById('videoLogLink');

    let currentTaskId = null; // 记录当前录制任务 ID
    let isRecording = false;

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
        console.log('停止录制');
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

    form.addEventListener('submit', async (e) => {
        e.preventDefault();

        // 如果正在录制，点击即为“停止”
        if (isRecording && currentTaskId) {
            await stopRecording();
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

        const videoUrl = encodeURIComponent(videoInput.value.trim());
        const apiUrl = `/api/route/check?url=${videoUrl}`;

        try {
            const responsePromise = fetch(apiUrl, {
                method: 'GET',
                headers: { 'Accept': 'application/json' }
            });

            // 假进度
            let fakeProgress = 0;
            const fakeInterval = setInterval(() => {
                fakeProgress += Math.random() * 8 + 2;
                if (fakeProgress >= 90) { fakeProgress = 90; clearInterval(fakeInterval); }
                progressBar.style.width = fakeProgress + '%';
                progressBar.textContent = Math.round(fakeProgress) + '%';
            }, 300);

            const response = await responsePromise;
            const data = await response.json();
            console.log('收到响应');
            console.log(data);
            clearInterval(fakeInterval);

            if (!response.ok) throw new Error(data.message || data.error || '请求失败');

            // —— 成功响应处理 ——
            progressBar.style.width = '100%';
            progressBar.textContent = '100%';
            progressBar.classList.remove('progress-bar-animated');

            // 判断是否为【直播录制任务】
            const isLiveRecording = data.downloadUrl == "Convert";

            if (isLiveRecording) {
                // 提取 taskId
                currentTaskId = data.file;
                setStopButton();
                unlockForm();

                status.innerHTML = `
                    <strong class="text-warning">录制中...</strong><br>
                    任务 ID: <code>${currentTaskId}</code><br>
                    点击 <strong>停止录制</strong> 终止
                `;

                log.textContent = `直播录制已启动，任务 ID: ${currentTaskId}，点击按钮可停止。`;

            } else {
                // 普通下载完成
                setStartButton();
                status.innerHTML = `
                    <strong class="text-success">下载完成！</strong><br>
                    文件: <code>${data.file || data.filePath || '—'}</code>
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
            setStartButton(); // 错误也恢复按钮
        } finally {
            if (!isRecording) {
                unlockForm(); // 只有非录制状态才解锁输入框
            }
        }
    });

    // —— 停止录制函数 ——
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

            // 停止成功
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
}