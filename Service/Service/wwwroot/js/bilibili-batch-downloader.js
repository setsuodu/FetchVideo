// js/bilibili-batch-downloader.js
export function initBilibiliBatchDownloader() {
    const fileInput = document.getElementById('biliJsonFile');
    const startBtn = document.getElementById('biliBatchStartBtn');
    const checkBtn = document.getElementById('biliBatchCheckBtn');   // 新增
    const progressArea = document.getElementById('biliBatchProgressArea');
    const progressText = document.getElementById('biliBatchProgressText');
    const currentVideo = document.getElementById('biliBatchCurrentVideo');

    let parsedVideos = [];
    let currentUpName = "";
    let currentMid = "";

    if (fileInput) {
        fileInput.addEventListener('change', async (e) => {
            const file = e.target.files[0];
            if (!file) return;

            const fileName = file.name.replace(/\.json$/i, '');
            const parts = fileName.split('_');
            if (parts.length >= 2) {
                currentUpName = parts[0];
                currentMid = parts[1];
            }

            try {
                const text = await file.text();
                parsedVideos = JSON.parse(text);
                alert(`✅ 解析成功！\nUP: ${currentUpName}\nMID: ${currentMid}\n共 ${parsedVideos.length} 条`);
            } catch (err) {
                alert('❌ JSON 解析失败');
                console.error(err);
            }
        });
    }

    // 开始下载（保持不变）
    if (startBtn) {
        startBtn.addEventListener('click', async () => { /* ... 你之前的代码 ... */ });
    }

    // 新增：检查缺失按钮
    if (checkBtn) {
        checkBtn.addEventListener('click', async () => {
            if (!parsedVideos || parsedVideos.length === 0) {
                alert('请先上传 JSON 文件');
                return;
            }
            if (!currentUpName || !currentMid) {
                alert('无法识别 UP 名和 MID，请确认文件名格式');
                return;
            }

            currentVideo.textContent = '正在对比文件夹...';
            try {
                const res = await fetch('/api/bilibili/check-missing', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        upName: currentUpName,
                        mid: currentMid,
                        videos: parsedVideos
                    })
                });

                const data = await res.json();
                let msg = `📊 对比完成\nJSON: ${data.totalInJson} 个\n已下载: ${data.downloaded} 个\n缺失: ${data.missingCount} 个\n\n`;

                if (data.missingCount > 0) {
                    msg += "缺失视频：\n";
                    data.missing.forEach(m => {
                        msg += `- ${m.title} (${m.bvid})\n`;
                    });
                } else {
                    msg += "✅ 全部下载完成！";
                }

                alert(msg);
                currentVideo.textContent = `对比完成 → 缺失 ${data.missingCount} 个`;
            } catch (err) {
                alert('检查失败: ' + err.message);
            }
        });
    }
}