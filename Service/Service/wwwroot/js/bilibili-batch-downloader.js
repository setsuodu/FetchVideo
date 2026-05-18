// js/bilibili-batch-downloader.js
export function initBilibiliBatchDownloader() {

    const fileInput = document.getElementById('biliJsonFile');
    const jsonText = document.getElementById('biliJsonText');
    const startBtn = document.getElementById('biliBatchStartBtn');
    const progressArea = document.getElementById('biliBatchProgressArea');
    const progressText = document.getElementById('biliBatchProgressText');
    const progressBar = document.getElementById('biliBatchProgressBar');
    const currentVideo = document.getElementById('biliBatchCurrentVideo');

    if (!startBtn)  return;

    let parsedVideos = [];

    // 监听文件选择
    if (fileInput) {
        fileInput.addEventListener('change', async (e) => {
            const file = e.target.files[0];
            if (!file) return;

            try {
                const text = await file.text();
                parsedVideos = JSON.parse(text);
                alert(`成功解析 ${parsedVideos.length} 条视频`);
            } catch (err) {
                alert('JSON 文件解析失败，请检查格式');
                console.error(err);
            }
        });
    }

    // 点击开始下载
    startBtn.addEventListener('click', async () => {
        // 解析 JSON（文件或文本框）
        if (parsedVideos.length === 0 && jsonText && jsonText.value.trim()) {
            try {
                parsedVideos = JSON.parse(jsonText.value.trim());
            } catch (err) {
                alert('文本框 JSON 格式错误');
                return;
            }
        }

        if (!parsedVideos || parsedVideos.length === 0) {
            alert('请上传 JSON 文件或在文本框粘贴内容');
            return;
        }

        // 显示进度区
        progressArea.style.display = 'block';
        startBtn.disabled = true;
        progressText.textContent = `0/${parsedVideos.length}`;
        currentVideo.textContent = '任务已提交，后台正在下载中...（可关闭此页面）';

        try {
            const res = await fetch('/api/bilibili/batch-download', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    upName: "小利萝",                    // ← 根据实际 UP 修改
                    mid: "3546606573979889",             // ← 根据实际 mid 修改
                    videos: parsedVideos
                })
            });

            if (res.ok) {
                const data = await res.json();
                currentVideo.textContent = `任务已提交！后台正在下载（共 ${data.total} 个），可关闭此页面查看日志`;
                // 不再启动任何轮询
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
}