// js/bilibili-batch-downloader.js
// 支持上传 1 个 JSON（全量下载）或 2 个 JSON（计算差包，只下载新增视频）
// 修改：旧 JSON 默认灰色禁用，需先传新 JSON 才可上传；删除检查缺失；新增重置按钮
export function initBilibiliBatchDownloader() {
    const fileInputOld = document.getElementById('biliJsonFileOld');
    const fileInputNew = document.getElementById('biliJsonFile');
    const startBtn = document.getElementById('biliBatchStartBtn');
    const progressArea = document.getElementById('biliBatchProgressArea');
    const progressText = document.getElementById('biliBatchProgressText');
    const currentVideo = document.getElementById('biliBatchCurrentVideo');
    const statusHint = document.getElementById('biliBatchStatusHint');
    const resetBtn = document.getElementById('biliBatchResetBtn');
    const oldJsonWrapper = document.getElementById('biliOldJsonWrapper');

    let oldVideos = [];      // 旧 JSON（可选）
    let newVideos = [];      // 新 JSON（主）
    let currentUpName = "";
    let currentMid = "";
    let isDiffMode = false;  // 是否处于差包模式

    // 初始：禁用旧 JSON 上传（灰色）
    function setOldInputEnabled(enabled) {
        if (fileInputOld) {
            fileInputOld.disabled = !enabled;
        }
        if (oldJsonWrapper) {
            if (enabled) {
                oldJsonWrapper.classList.remove('opacity-50', 'pe-none');
                oldJsonWrapper.style.opacity = '1';
                oldJsonWrapper.style.pointerEvents = 'auto';
            } else {
                oldJsonWrapper.classList.add('opacity-50');
                oldJsonWrapper.style.opacity = '0.5';
                oldJsonWrapper.style.pointerEvents = 'none';
            }
        }
    }

    // 初始禁用
    setOldInputEnabled(false);

    function parseFileName(fileName) {
        const base = fileName.replace(/\.json$/i, '');
        const parts = base.split('_');
        if (parts.length >= 2) {
            return { upName: parts[0], mid: parts[1] };
        }
        return { upName: '', mid: '' };
    }

    function getBvid(item) {
        return (item.bvid || item.Bvid || '').toString().trim();
    }

    // 计算差集：new 中有、old 中没有的视频（按 bvid）
    function computeDiff(oldList, newList) {
        const oldSet = new Set(oldList.map(getBvid).filter(Boolean));
        return newList.filter(v => {
            const bv = getBvid(v);
            return bv && !oldSet.has(bv);
        });
    }

    function updateHint() {
        if (!statusHint) return;
        if (newVideos.length === 0) {
            statusHint.textContent = '请上传 JSON 文件';
            statusHint.className = 'form-text text-muted';
            setOldInputEnabled(false);
            return;
        }
        // 有新 JSON 后才允许上传旧 JSON
        setOldInputEnabled(true);
        if (oldVideos.length === 0) {
            statusHint.innerHTML = `📦 <strong>全量模式</strong>：将下载全部 <strong>${newVideos.length}</strong> 个视频`;
            statusHint.className = 'form-text text-primary';
            isDiffMode = false;
        } else {
            const diff = computeDiff(oldVideos, newVideos);
            statusHint.innerHTML = `📦 <strong>差包模式</strong>：旧 ${oldVideos.length} 条 → 新 ${newVideos.length} 条 → 新增 <strong>${diff.length}</strong> 条`;
            statusHint.className = 'form-text text-success';
            isDiffMode = true;
        }
    }

    // 旧 JSON（可选）
    if (fileInputOld) {
        fileInputOld.addEventListener('change', async (e) => {
            const file = e.target.files[0];
            if (!file) {
                oldVideos = [];
                updateHint();
                return;
            }
            try {
                const text = await file.text();
                oldVideos = JSON.parse(text);
                if (!Array.isArray(oldVideos)) throw new Error('不是数组');
                alert(`✅ 旧 JSON 解析成功！共 ${oldVideos.length} 条`);
            } catch (err) {
                oldVideos = [];
                alert('❌ 旧 JSON 解析失败');
                console.error(err);
            }
            updateHint();
        });
    }

    // 新 JSON（主文件）
    if (fileInputNew) {
        fileInputNew.addEventListener('change', async (e) => {
            const file = e.target.files[0];
            if (!file) {
                newVideos = [];
                currentUpName = '';
                currentMid = '';
                // 清空新时，也清旧并禁用旧输入
                oldVideos = [];
                if (fileInputOld) fileInputOld.value = '';
                updateHint();
                return;
            }

            const meta = parseFileName(file.name);
            currentUpName = meta.upName;
            currentMid = meta.mid;

            try {
                const text = await file.text();
                newVideos = JSON.parse(text);
                if (!Array.isArray(newVideos)) throw new Error('不是数组');
                alert(`✅ 新 JSON 解析成功！\nUP: ${currentUpName || '未知'}\nMID: ${currentMid || '未知'}\n共 ${newVideos.length} 条`);
            } catch (err) {
                newVideos = [];
                alert('❌ 新 JSON 解析失败');
                console.error(err);
            }
            updateHint();
        });
    }

    // 点击开始下载
    if (startBtn) {
        startBtn.addEventListener('click', async () => {
            if (!newVideos || newVideos.length === 0) {
                alert('请先上传【新 JSON】文件');
                return;
            }

            let videosToDownload = newVideos;
            let modeLabel = '全量';

            if (oldVideos && oldVideos.length > 0) {
                videosToDownload = computeDiff(oldVideos, newVideos);
                modeLabel = '差包';
                if (videosToDownload.length === 0) {
                    alert('差包结果为空：新 JSON 中没有比旧 JSON 多出的视频，无需下载');
                    return;
                }
            }

            // 显示进度区
            if (progressArea) progressArea.style.display = 'block';
            startBtn.disabled = true;
            if (progressText) progressText.textContent = `0/${videosToDownload.length}`;
            if (currentVideo) currentVideo.textContent = `【${modeLabel}】任务已提交，后台正在下载中...（可关闭此页面）`;

            try {
                const res = await fetch('/api/bilibili/batch-download', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        upName: currentUpName || '未知UP',
                        mid: currentMid || '',
                        videos: videosToDownload
                    })
                });

                if (res.ok) {
                    const data = await res.json();
                    if (currentVideo) currentVideo.textContent = `【${modeLabel}】任务已提交！后台正在下载（共 ${data.total} 个），可关闭此页面查看日志`;
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

    // 【重置】按钮：清空新旧 JSON、状态、进度
    if (resetBtn) {
        resetBtn.addEventListener('click', () => {
            oldVideos = [];
            newVideos = [];
            currentUpName = '';
            currentMid = '';
            isDiffMode = false;

            if (fileInputOld) fileInputOld.value = '';
            if (fileInputNew) fileInputNew.value = '';
            if (progressArea) progressArea.style.display = 'none';
            if (progressText) progressText.textContent = '0/0';
            if (currentVideo) currentVideo.textContent = '';
            if (startBtn) startBtn.disabled = false;

            setOldInputEnabled(false);
            updateHint();
            alert('已重置，请重新上传 JSON');
        });
    }
}
