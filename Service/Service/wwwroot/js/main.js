// js/main.js
import { initImageDownloader } from './image-downloader.js';
import { initVideoDownloader } from './video-downloader.js';
import { initScheduleManager } from './schedule.js';
import { initLiveRecordManager } from './live-record.js';
// 新增：B站UP批量下载模块
import { initBilibiliBatchDownloader } from './bilibili-batch-downloader.js';

// --- 新增：初始化 vConsole ---
function initVConsole() {
    if (typeof VConsole !== 'undefined') {
        window.vConsole = new VConsole({
            theme: 'dark',
            onReady: function () {
                const vcBtn = document.querySelector('.vc-switch');
                if (vcBtn) {
                    vcBtn.style.right = 'auto';
                    vcBtn.style.left = '20px';
                    vcBtn.style.bottom = '20px';
                }
            }
        });
    }
}

// 获取版本号并显示
async function loadAppVersion() {
    //alert('✅ 版本号加载...');
    try {
        const response = await fetch('/api/version');
        if (!response.ok) throw new Error('Failed to fetch version');

        const data = await response.json();
        const version = data.version || 'dev';

        // 更新卡片头部
        const headerVersion = document.getElementById('header-version');
        if (headerVersion) {
            headerVersion.textContent = `v${version}`;
        }

        // 可选：同时更新页面 title（纯文本）
        document.title = `下载工具 v${version}`;

        console.log('✅ 版本号加载成功:', version);
    } catch (err) {
        console.log('⚠️ 获取版本号失败，使用默认值', err);
        document.getElementById('header-version').textContent = 'vdev';
    }
}

// 在页面加载完成后执行
//document.addEventListener('DOMContentLoaded', loadAppVersion);

// --- 新增：回到顶部按钮逻辑 ---
function initBackToTop() {
    const backToTopBtn = document.getElementById('backToTop');
    if (!backToTopBtn) return;

    window.addEventListener('scroll', () => {
        // 使用 window.scrollY 更现代
        if (window.scrollY > 300) {
            backToTopBtn.style.display = "block";
        } else {
            backToTopBtn.style.display = "none";
        }
    });

    backToTopBtn.addEventListener('click', () => {
        //window.scrollTo({ top: 0, behavior: 'smooth' });
        const html = document.documentElement;

        // 强制禁用 smooth
        html.style.setProperty('scroll-behavior', 'auto', 'important');

        // 滚动到顶
        html.scrollTop = 0;

        // 恢复（让页面其他锚点跳转保持平滑）
        html.style.scrollBehavior = '';
    });
}

document.addEventListener('DOMContentLoaded', () => {
    initVConsole();     // 初始化调试面板
    loadAppVersion();   // 加载并显示版本号
    initBackToTop();    // 初始化回到顶部
    initImageDownloader();
    initVideoDownloader();
    initScheduleManager();
    initLiveRecordManager();
    // 新增这一行
    initBilibiliBatchDownloader();
});