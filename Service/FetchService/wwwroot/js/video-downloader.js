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

    // 锁定表单（禁用交互）
    const lockForm = () => {
        console.log("lock");
        submitBtn.disabled = true;
        submitBtn.textContent = '下载中...';
        videoInput.disabled = true;
        videoInput.classList.add('disabled');
    };

    // 解锁表单（恢复交互）
    const unlockForm = () => {
        console.log("unlock");
        submitBtn.disabled = false;
        submitBtn.textContent = '开始下载';
        videoInput.disabled = false;
        videoInput.classList.remove('disabled');
    };

    form.addEventListener('submit', async (e) => {
        e.preventDefault();

        // 1. 锁定 UI
        lockForm();
        resultDiv.classList.remove('d-none');
        progressBar.style.width = '0%';
        progressBar.textContent = '0%';
        status.textContent = '正在检查 URL...';
        log.textContent = '';
        logLink.classList.add('d-none');

        const videoUrl = encodeURIComponent(videoInput.value.trim());
        const apiUrl = `http://localhost:8080/api/route/check?url=${videoUrl}`;

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
            clearInterval(fakeInterval);

            if (!response.ok) throw new Error(data.message || data.error || '请求失败');

            // 成功
            progressBar.style.width = '100%';
            progressBar.textContent = '100%';
            progressBar.classList.remove('progress-bar-animated');

            status.innerHTML = `
                <strong class="text-success">下载完成！</strong><br>
                文件: <code>${data.file || data.filePath || '—'}</code><br>
                状态: ${data.status || '成功'}
            `;

            log.textContent = `视频已下载至服务器。`;

            if (data.downloadUrl == "Merge") {
                console.log("是视频，不变");
            } else if (data.downloadUrl == "Convert") {
                console.log("是直播，变成停止按钮");
            }

            if (data.logPath || data.downloadUrl) {
                logLink.href = data.logPath || data.downloadUrl;
                logLink.textContent = `下载日志 (${data.fileName || 'download_log.txt'})`;
                logLink.classList.remove('d-none');
            }

        } catch (err) {
            progressBar.style.width = '100%';
            progressBar.textContent = '错误';
            progressBar.classList.add('bg-danger');
            status.innerHTML = `<span class="text-danger">错误: ${err.message}</span>`;
            log.textContent = '请检查 URL 或服务状态。';
        } finally {
            // 2. 无论成功失败，都解锁
            unlockForm();
        }
    });
}