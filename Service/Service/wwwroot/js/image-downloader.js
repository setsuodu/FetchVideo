// js/image-downloader.js
export function initImageDownloader() {
    const form = document.getElementById('imageDownloadForm');
    const resultDiv = document.getElementById('imageResult');
    const progressBar = document.getElementById('imageProgressBar');
    const status = document.getElementById('imageStatus');
    const log = document.getElementById('log-image');
    const logLink = document.getElementById('imageLogLink');
    const firstInput = document.getElementById('firstUrl');
    const lastInput = document.getElementById('lastUrl');
    const clearFirst = document.getElementById('clearFirstBtn');
    const clearLast = document.getElementById('clearLastBtn');

    // 新增元素获取
    const txtInput = document.getElementById('imageTxtFile');
    const clearTxtBtn = document.getElementById('clearTxtBtn');

    // 智能互斥控制：输入连号时禁用TXT，上传TXT时禁用连号
    function toggleInputs() {
        if (firstInput.value.length > 0 || lastInput.value.length > 0) {
            txtInput.disabled = true;
        } else {
            txtInput.disabled = false;
        }
    }
    firstInput.addEventListener('input', toggleInputs);
    lastInput.addEventListener('input', toggleInputs);

    txtInput.addEventListener('change', function () {
        if (txtInput.files.length > 0) {
            clearTxtBtn.classList.remove('d-none');
            firstInput.disabled = true;
            lastInput.disabled = true;
        } else {
            clearTxtBtn.classList.add('d-none');
            firstInput.disabled = false;
            lastInput.disabled = false;
        }
    });

    clearTxtBtn.addEventListener('click', function () {
        txtInput.value = '';
        clearTxtBtn.classList.add('d-none');
        firstInput.disabled = false;
        lastInput.disabled = false;
    });

    // 表单提交事件
    form.addEventListener('submit', async (e) => {
        e.preventDefault();

        const txtFile = txtInput.files[0];
        const firstUrlVal = firstInput.value.trim();
        const lastUrlVal = lastInput.value.trim();
        const concurrencyVal = parseInt(document.getElementById('concurrency').value) || 5;

        // 表单合法性验证
        if (!txtFile && (!firstUrlVal || !lastUrlVal)) {
            alert('请填写连号 URL 或上传包含图片链接的 TXT 文件！');
            return;
        }

        resultDiv.classList.remove('d-none');
        progressBar.style.width = '0%';
        progressBar.textContent = '0%';
        progressBar.classList.add('progress-bar-animated');
        progressBar.classList.remove('bg-danger');
        status.textContent = '正在初始化...';
        log.textContent = '';
        logLink.classList.add('d-none');

        // 进度条伪动画
        let fakeProgress = 0;
        const fakeInterval = setInterval(() => {
            fakeProgress += Math.random() * 8 + 2;
            if (fakeProgress >= 90) { fakeProgress = 90; clearInterval(fakeInterval); }
            progressBar.style.width = fakeProgress + '%';
            progressBar.textContent = Math.round(fakeProgress) + '%';
        }, 300);

        try {
            let response;

            if (txtFile) {
                // 【模式二：处理上传的 TXT 文件】
                status.textContent = '正在读取并解析 TXT 文件...';
                const fileText = await readTextFile(txtFile);

                // 按行分割并清洗数据，只保留标准 http/https 链接
                const urls = fileText.split(/\r?\n/)
                    .map(line => line.trim())
                    .filter(line => line.length > 0 && line.startsWith('http'));

                if (urls.length === 0) {
                    throw new Error('TXT 文件中没有找到有效的图片 URL 链接（须以 http/https 开头）');
                }

                status.textContent = `正向服务器发送 ${urls.length} 个自定义链接...`;

                // 去掉 .txt 扩展名作为保存的文件夹名字
                const folderName = txtFile.name.replace(/\.[^/.]+$/, "");

                response = await fetch('/api/download/download-list', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        Urls: urls,
                        Concurrency: concurrencyVal,
                        FolderName: folderName
                    })
                });
            } else {
                // 【模式一：原有的连号下载】
                response = await fetch('/api/download/download-batch', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        FirstUrl: firstUrlVal,
                        LastUrl: lastUrlVal,
                        Concurrency: concurrencyVal
                    })
                });
            }

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
            clearInterval(fakeInterval);
            progressBar.style.width = '100%';
            progressBar.textContent = '错误';
            progressBar.classList.add('bg-danger');
            status.innerHTML = `<span class="text-danger">错误: ${err.message}</span>`;
            log.textContent = '';
        }
    });

    // 辅助函数：使用 Promise 读取文本
    function readTextFile(file) {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = () => resolve(reader.result);
            reader.onerror = () => reject(new Error('TXT 文件读取失败'));
            reader.readAsText(file, 'utf-8');
        });
    }

    // 原有连号清除按钮逻辑保持不变
    firstInput.addEventListener('input', function () {
        if (firstInput.value.length > 0) clearFirst.classList.remove('d-none');
        else clearFirst.classList.add('d-none');
    });
    lastInput.addEventListener('input', function () {
        if (lastInput.value.length > 0) clearLast.classList.remove('d-none');
        else clearLast.classList.add('d-none');
    });
    clearFirst.addEventListener('click', function () {
        firstInput.value = '';
        clearFirst.classList.add('d-none');
        toggleInputs();
    });
    clearLast.addEventListener('click', function () {
        lastInput.value = '';
        clearLast.classList.add('d-none');
        toggleInputs();
    });
}