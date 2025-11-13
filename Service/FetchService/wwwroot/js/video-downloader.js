// js/video-downloader.js
export function initVideoDownloader() {
    const form = document.getElementById('videoDownloadForm');
    const resultDiv = document.getElementById('videoResult');
    const progressBar = document.getElementById('videoProgressBar');
    const status = document.getElementById('videoStatus');
    const log = document.getElementById('log-video');
    const logLink = document.getElementById('videoLogLink');

    form.addEventListener('submit', async (e) => {
        e.preventDefault();

        resultDiv.classList.remove('d-none');
        progressBar.style.width = '0%';
        progressBar.textContent = '0%';
        status.textContent = '正在检查 URL...';
        log.textContent = '';
        logLink.classList.add('d-none');

        const videoUrl = encodeURIComponent(document.getElementById('videoUrl').value.trim());
        const apiUrl = `http://localhost:8080/api/route/check?url=${videoUrl}`;

        try {
            // 改用 GET 请求
            const responsePromise = fetch(apiUrl, {
                method: 'GET',
                headers: {
                    'Accept': 'application/json'
                }
            });

            // 假进度（和之前一致）
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

            if (!response.ok) {
                throw new Error(data.message || data.error || '请求失败');
            }

            // 成功：跳到 100%
            progressBar.style.width = '100%';
            progressBar.textContent = '100%';
            progressBar.classList.remove('progress-bar-animated');

            // 根据你的后端返回结构调整字段（常见字段示例）
            status.innerHTML = `
                <strong class="text-success">下载完成！</strong><br>
                文件: <code>${data.file || data.filePath || '—'}</code><br>
                大小: ${formatBytes(data.size) || '—'}<br>
                状态: ${data.status || '成功'}
            `;

            log.textContent = `视频已下载至服务器。`;

            // 如果后端返回日志或下载链接
            if (data.logPath || data.downloadUrl) {
                const linkUrl = data.logPath || data.downloadUrl;
                const fileName = data.fileName || 'download_log.txt';
                logLink.href = linkUrl;
                logLink.textContent = `下载日志 (${fileName})`;
                logLink.classList.remove('d-none');
            }

        } catch (err) {
            progressBar.style.width = '100%';
            progressBar.textContent = '错误';
            progressBar.classList.add('bg-danger');
            status.innerHTML = `<span class="text-danger">错误: ${err.message}</span>`;
            log.textContent = '请检查 URL 是否正确或服务是否可用。';
        }
    });
}

// 辅助函数：格式化字节
function formatBytes(bytes, decimals = 2) {
    if (!bytes) return null;
    const k = 1024;
    const dm = decimals < 0 ? 0 : decimals;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i];
}