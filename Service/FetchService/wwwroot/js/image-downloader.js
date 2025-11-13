// js/image-downloader.js
export function initImageDownloader() {
    const form = document.getElementById('imageDownloadForm');
    const resultDiv = document.getElementById('imageResult');
    const progressBar = document.getElementById('imageProgressBar');
    const status = document.getElementById('imageStatus');
    const log = document.getElementById('log-image');
    const logLink = document.getElementById('imageLogLink');

    form.addEventListener('submit', async (e) => {
        e.preventDefault();

        resultDiv.classList.remove('d-none');
        progressBar.style.width = '0%';
        progressBar.textContent = '0%';
        status.textContent = '正在解析 URL...';
        log.textContent = '';
        logLink.classList.add('d-none');

        const payload = {
            FirstUrl: document.getElementById('firstUrl').value.trim(),
            LastUrl: document.getElementById('lastUrl').value.trim(),
            Concurrency: parseInt(document.getElementById('concurrency').value) || 5
        };

        try {
            const responsePromise = fetch('/api/download/download-batch', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
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
            clearInterval(fakeInterval);

            if (!response.ok) throw new Error(data.message || data.title || '请求失败');

            progressBar.style.width = '100%';
            progressBar.textContent = '100%';
            progressBar.classList.remove('progress-bar-animated');

            status.innerHTML = `
                <strong class="text-success">下载完成！</strong><br>
                文件夹: <code>${data.Folder || '—'}</code><br>
                总数: ${data.Total}，成功: ${data.Downloaded}，失败: ${data.Failed}
            `;

            log.textContent = `预计下载 ${data.Total} 张图片，已全部处理完毕。`;

            if (data.LogPath) {
                logLink.href = data.LogPath;
                logLink.textContent = `下载 404 日志 (${data.Folder}/download_404.txt)`;
                logLink.classList.remove('d-none');
            }
        } catch (err) {
            progressBar.style.width = '100%';
            progressBar.textContent = '错误';
            progressBar.classList.add('bg-danger');
            status.innerHTML = `<span class="text-danger">错误: ${err.message}</span>`;
            log.textContent = '';
        }
    });
}