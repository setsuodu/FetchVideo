// js/bilibili-opus-batch-downloader.js
// 图文独立区块，不与视频共用解析逻辑
export function initBilibiliOpusBatchDownloader() {
    const fileInput = document.getElementById('biliOpusJsonFile');
    const startBtn = document.getElementById('biliOpusBatchStartBtn');
    const checkBtn = document.getElementById('biliOpusBatchCheckBtn');
    const progressArea = document.getElementById('biliOpusBatchProgressArea');
    const progressText = document.getElementById('biliOpusBatchProgressText');
    const currentItem = document.getElementById('biliOpusBatchCurrentItem');

    let parsedItems = [];
    let currentUpName = "";
    let currentMid = "";

    if (!fileInput || !startBtn) {
        console.warn('[opus-batch] DOM elements not found');
        return;
    }

    fileInput.addEventListener('change', async (e) => {
        const file = e.target.files[0];
        if (!file) return;

        // 文件名约定：UP名_MID_xxx.json 或 bilibili_opus_UP名_日期.json
        const fileName = file.name.replace(/\.json$/i, '');
        const parts = fileName.split('_');
        if (parts.length >= 2) {
            // 兼容 bilibili_opus_aeri酱咩_2026-10-03 与 aeri酱咩_3546880038406535_xx
            if (parts[0] === 'bilibili' && parts[1] === 'opus' && parts.length >= 3) {
                currentUpName = parts[2];
                currentMid = parts[3] || '';
            } else {
                currentUpName = parts[0];
                currentMid = parts[1];
            }
        }

        try {
            const text = await file.text();
            const data = JSON.parse(text);
            if (!Array.isArray(data)) {
                alert('❌ JSON 必须是数组');
                parsedItems = [];
                return;
            }
            // 只接受带 opusId 的项
            parsedItems = data.filter(x => x && (x.opusId || x.OpusId));
            if (parsedItems.length === 0) {
                alert('❌ 未找到 opusId 字段，请确认是图文 JSON');
                return;
            }
            alert(`✅ 图文 JSON 解析成功！\nUP: ${currentUpName || '(未识别)'}\n共 ${parsedItems.length} 条`);
        } catch (err) {
            alert('❌ JSON 解析失败');
            console.error(err);
            parsedItems = [];
        }
    });

    startBtn.addEventListener('click', async () => {
        if (!parsedItems || parsedItems.length === 0) {
            alert('请先上传图文 JSON 文件');
            return;
        }

        progressArea.style.display = 'block';
        startBtn.disabled = true;
        progressText.textContent = `0/${parsedItems.length}`;
        currentItem.textContent = '任务已提交，后台正在下载中...（可关闭此页面）';

        try {
            const res = await fetch('/api/bilibili/opus-batch-download', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    upName: currentUpName || '未知UP',
                    mid: currentMid || '',
                    items: parsedItems.map(x => ({
                        title: x.title || x.Title || '',
                        opusId: String(x.opusId || x.OpusId || ''),
                        opusUrl: x.opusUrl || x.OpusUrl || ''
                    }))
                })
            });

            if (res.ok) {
                const data = await res.json();
                currentItem.textContent = `任务已提交！后台正在下载图文（共 ${data.total} 条），可关闭此页面查看日志`;
            } else {
                const errData = await res.json().catch(() => ({}));
                alert('提交失败: ' + (errData.error || res.statusText));
                startBtn.disabled = false;
            }
        } catch (err) {
            console.error(err);
            alert('请求失败: ' + err.message);
            startBtn.disabled = false;
        }
    });

    if (checkBtn) {
        checkBtn.addEventListener('click', async () => {
            if (!parsedItems || parsedItems.length === 0) {
                alert('请先上传图文 JSON 文件');
                return;
            }
            currentItem.textContent = '正在对比文件夹...';
            try {
                const res = await fetch('/api/bilibili/opus-check-missing', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        upName: currentUpName || '未知UP',
                        mid: currentMid || '',
                        items: parsedItems.map(x => ({
                            title: x.title || x.Title || '',
                            opusId: String(x.opusId || x.OpusId || ''),
                            opusUrl: x.opusUrl || x.OpusUrl || ''
                        }))
                    })
                });

                const data = await res.json();
                let msg = `📊 对比完成\nJSON: ${data.totalInJson} 条\n已下载: ${data.downloaded} 条\n缺失: ${data.missingCount} 条\n\n`;
                if (data.missingCount > 0) {
                    msg += "缺失图文：\n";
                    data.missing.forEach(m => {
                        msg += `- ${m.title} (${m.opusId})\n`;
                    });
                } else {
                    msg += "✅ 全部下载完成！";
                }
                alert(msg);
                currentItem.textContent = `对比完成 → 缺失 ${data.missingCount} 条`;
            } catch (err) {
                alert('检查失败: ' + err.message);
            }
        });
    }
}
