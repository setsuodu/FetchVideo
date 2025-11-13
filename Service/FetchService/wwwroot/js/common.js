// wwwroot/js/common.js
function switchPage(to) {
    document.querySelectorAll('.page').forEach(p => p.classList.add('hidden'));
    document.getElementById(to).classList.remove('hidden');
}

// 虚假进度条（0~100% 随机波动）
function runFakeProgress(barId, btnId, duration = 5000) {
    const bar = document.getElementById(barId);
    const btn = document.getElementById(btnId);
    let progress = 0;
    bar.style.width = '0%';
    btn.disabled = true;

    const interval = setInterval(() => {
        progress += Math.random() * 15;
        if (progress >= 100) {
            progress = 100;
            clearInterval(interval);
            btn.disabled = false;
        }
        bar.style.width = progress + '%';
        bar.textContent = Math.round(progress) + '%';
    }, duration / 30);
}