// ==UserScript==
// @name         Bilibili 自动开房间（脚本1-开标签）
// @match        http://localhost:8080/index.html
// @grant        GM_openInTab
// ==/UserScript==

(function () {
    'use strict';

    const OPEN_INTERVAL = 15000; // 15秒开一个
    const MAX_OPEN = 20;

    let opened = 0;
    let index = 0;
    let rooms = [];
    let timer = null;
    let isRunning = false;

    function showStatus(text) {
        let div = document.getElementById('script1-status');
        if (!div) {
            div = document.createElement('div');
            div.id = 'script1-status';
            div.style.cssText = 'position:fixed; top:120px; right:20px; padding:10px; background:#333; color:#0f0; z-index:99999; border-radius:4px; min-width:200px;';
            document.body.appendChild(div);
        }
        div.innerHTML = text;
    }

    function getRooms() {
        const rows = document.querySelectorAll('table tbody tr');
        const list = [];
        for (let row of rows) {
            const cells = row.querySelectorAll('td');
            if (cells.length < 7) continue;
            const roomId = cells[3]?.textContent?.trim();
            const uidText = cells[6]?.textContent?.trim();
            const hasUid = uidText && uidText !== '-' && uidText !== '';
            if (roomId && !isNaN(Number(roomId)) && !hasUid) {
                list.push(roomId);
            }
        }
        return list;
    }

    function start() {
        if (isRunning) return;

        rooms = getRooms();
        if (rooms.length === 0) {
            showStatus('没有找到待处理的房间！');
            alert('没有找到待处理的房间');
            return;
        }

        isRunning = true;
        opened = 0;
        index = 0;

        document.getElementById('start-btn').style.display = 'none';
        document.getElementById('stop-btn').style.display = 'block';

        timer = setInterval(() => {
            if (!isRunning || opened >= MAX_OPEN || index >= rooms.length) {
                stop();
                return;
            }

            const roomId = rooms[index++];
            GM_openInTab(`https://live.bilibili.com/${roomId}`, false);
            opened++;

            showStatus(`已开 ${opened}/${rooms.length} 个<br>当前: ${roomId}<br>剩余: ${rooms.length - index}`);
            console.log(`[Script1] 打开房间 ${roomId}`);

        }, OPEN_INTERVAL);

        showStatus('运行中...');
    }

    function stop() {
        isRunning = false;
        if (timer) {
            clearInterval(timer);
            timer = null;
        }
        document.getElementById('start-btn').style.display = 'block';
        document.getElementById('stop-btn').style.display = 'none';
        showStatus(`已停止！开了 ${opened} 个`);
    }

    // 开始按钮
    const startBtn = document.createElement('button');
    startBtn.id = 'start-btn';
    startBtn.textContent = '🚀 开始自动开房间';
    startBtn.style.cssText = 'position:fixed; top:20px; right:20px; padding:12px 20px; background:#ff4081; color:white; border:none; border-radius:6px; cursor:pointer; z-index:99999; font-size:14px;';
    startBtn.onclick = start;
    document.body.appendChild(startBtn);

    // 停止按钮
    const stopBtn = document.createElement('button');
    stopBtn.id = 'stop-btn';
    stopBtn.textContent = '⏹ 停止';
    stopBtn.style.cssText = 'position:fixed; top:20px; right:20px; padding:12px 20px; background:#666; color:white; border:none; border-radius:6px; cursor:pointer; z-index:99999; font-size:14px; display:none;';
    stopBtn.onclick = stop;
    document.body.appendChild(stopBtn);

    showStatus('就绪');
})();