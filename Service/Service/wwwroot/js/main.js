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
    initBackToTop();    // 初始化回到顶部
    initImageDownloader();
    initVideoDownloader();
    initScheduleManager();
    initLiveRecordManager();
    // 新增这一行
    initBilibiliBatchDownloader();
});