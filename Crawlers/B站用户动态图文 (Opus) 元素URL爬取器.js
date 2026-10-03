// ==UserScript==
// @name         B站用户动态/图文 (Opus) 元素URL爬取器
// @namespace    http://tampermonkey.net/
// @version      1.0
// @description  自动爬取 B站 指定用户主页动态/图文(Opus)页面的所有元素链接并导出 JSON/CSV
// @author       You
// @match        https://space.bilibili.com/*/upload/opus*
// @match        https://space.bilibili.com/*/dynamic*
// @grant        none
// ==/UserScript==

(function() {
    'use strict';

    let collectedData = [];
    let isAutoScrolling = false;

    // 创建悬浮控制面板
    function createPanel() {
        if (document.getElementById('bili-opus-crawler-panel')) return;

        const panel = document.createElement('div');
        panel.id = 'bili-opus-crawler-panel';
        panel.style.cssText = `
            position: fixed;
            top: 100px;
            right: 20px;
            z-index: 99999;
            background: #ffffff;
            border: 1px solid #e3e6e8;
            border-radius: 8px;
            padding: 15px;
            box-shadow: 0 4px 12px rgba(0,0,0,0.15);
            font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
            font-size: 14px;
            color: #18191c;
            display: flex;
            flex-direction: column;
            gap: 10px;
            min-width: 180px;
        `;

        panel.innerHTML = `
            <div style="font-weight: bold; border-bottom: 1px solid #eee; padding-bottom: 5px;">Opus 爬取工具</div>
            <div>已采集数据: <span id="opus-count" style="color: #00aeec; font-weight: bold;">0</span> 条</div>
            <button id="btn-collect" style="${btnStyle('#00aeec')}">提取当前已加载</button>
            <button id="btn-autoscroll" style="${btnStyle('#27c24c')}">自动滚动加载并提取</button>
            <button id="btn-export-json" style="${btnStyle('#7266ba')}">导出 JSON</button>
            <button id="btn-export-csv" style="${btnStyle('#f60')}">导出 CSV (Excel)</button>
        `;

        document.body.appendChild(panel);

        // 绑定事件
        document.getElementById('btn-collect').onclick = parsePageElements;
        document.getElementById('btn-autoscroll').onclick = toggleAutoScroll;
        document.getElementById('btn-export-json').onclick = () => exportData('json');
        document.getElementById('btn-export-csv').onclick = () => exportData('csv');
    }

    function btnStyle(bg) {
        return `
            background: ${bg};
            color: white;
            border: none;
            padding: 6px 12px;
            border-radius: 4px;
            cursor: pointer;
            font-size: 12px;
            transition: opacity 0.2s;
        `;
    }

    // 获取用户昵称（提取 class="nickname" 的元素）
    function getUserNickname() {
        const nickElem = document.querySelector('.nickname, #h-name');
        if (nickElem) {
            // 清理非法文件名字符（如 \ / : * ? " < > |）
            return nickElem.innerText.trim().replace(/[\\/:*?"<>|]/g, '_');
        }

        // 如果找不到昵称，回退提取 UID
        const userIdMatch = window.location.href.match(/space\.bilibili\.com\/(\d+)/);
        return userIdMatch ? userIdMatch[1] : 'user';
    }

    // 解析当前页面上的动态/图文元素
    function parsePageElements() {
        // 匹配新版 Opus 卡片与旧版动态卡片
        const cards = document.querySelectorAll('.opus-card, .bili-dyn-list__item, .bili-opus-card');

        cards.forEach(card => {
            // 获取图文/动态卡片链接
            const linkElem = card.querySelector('a.opus-card__cover, a.opus-card__title, a[href*="/opus/"], a[href*="//t.bilibili.com/"]');
            if (!linkElem) return;

            let opusUrl = linkElem.getAttribute('href') || '';
            if (!opusUrl) return;

            // 补充完整 URL
            if (opusUrl.startsWith('//')) {
                opusUrl = 'https:' + opusUrl;
            } else if (opusUrl.startsWith('/')) {
                opusUrl = 'https://www.bilibili.com' + opusUrl;
            }

            // 去除 URL 参数
            opusUrl = opusUrl.split('?')[0];

            // 提取 Opus ID
            const opusIdMatch = opusUrl.match(/(?:opus\/|t\.bilibili\.com\/)(\d+)/);
            const opusId = opusIdMatch ? opusIdMatch[1] : '';

            // 查重
            if (collectedData.some(d => d.opusUrl === opusUrl)) return;

            // 获取标题/文本内容
            let title = '';
            const titleElem = card.querySelector('.opus-card__title, .bili-dyn-title, .bili-rich-text__content');
            if (titleElem) {
                title = titleElem.innerText.trim().replace(/\s+/g, ' ');
            }
            if (!title) {
                const imgAlt = card.querySelector('img[alt]');
                if (imgAlt) title = imgAlt.getAttribute('alt').trim();
            }

            // 仅保留 opusId, title, opusUrl（去掉了 likes 和 imageUrls）
            collectedData.push({
                opusId,
                title: title || '无标题',
                opusUrl
            });
        });

        const countElem = document.getElementById('opus-count');
        if (countElem) countElem.innerText = collectedData.length;
    }

    // 自动滚动到底部加载更多内容
    function toggleAutoScroll() {
        const btn = document.getElementById('btn-autoscroll');
        if (isAutoScrolling) {
            isAutoScrolling = false;
            btn.innerText = '自动滚动加载并提取';
            btn.style.background = '#27c24c';
        } else {
            isAutoScrolling = true;
            btn.innerText = '停止自动滚动';
            btn.style.background = '#f05050';

            const timer = setInterval(() => {
                if (!isAutoScrolling) {
                    clearInterval(timer);
                    return;
                }

                parsePageElements();
                window.scrollTo(0, document.body.scrollHeight);
            }, 1500);
        }
    }

    // 导出文件
    function exportData(format) {
        if (collectedData.length === 0) {
            parsePageElements();
            if (collectedData.length === 0) {
                alert('未采集到任何数据！');
                return;
            }
        }

        // 提取昵称并构造文件名
        const nickname = getUserNickname();
        const dateStr = new Date().toISOString().slice(0, 10);
        const fileName = `bilibili_opus_${nickname}_${dateStr}`;

        if (format === 'json') {
            // JSON 导出：仅包含 opusId, title, opusUrl
            const dataStr = "data:text/json;charset=utf-8," + encodeURIComponent(JSON.stringify(collectedData, null, 2));
            downloadFile(dataStr, `${fileName}.json`);
        } else if (format === 'csv') {
            let csvContent = "\uFEFF"; // 加上 BOM 防止 Excel 打开乱码
            csvContent += "Opus ID,标题/文本,图文URL\n";

            collectedData.forEach(row => {
                const escapedTitle = `"${row.title.replace(/"/g, '""')}"`;
                csvContent += `${row.opusId},${escapedTitle},${row.opusUrl}\n`;
            });

            const dataStr = "data:text/csv;charset=utf-8," + encodeURIComponent(csvContent);
            downloadFile(dataStr, `${fileName}.csv`);
        }
    }

    function downloadFile(content, fileName) {
        const downloadAnchor = document.createElement('a');
        downloadAnchor.setAttribute("href", content);
        downloadAnchor.setAttribute("download", fileName);
        document.body.appendChild(downloadAnchor);
        downloadAnchor.click();
        downloadAnchor.remove();
    }

    // 初始化
    window.addEventListener('load', () => {
        setTimeout(createPanel, 1500);
    });
})();