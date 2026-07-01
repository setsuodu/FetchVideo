// ==UserScript==
// @name         Bilibili 自动采集（脚本2-调试版）
// @match        https://live.bilibili.com/*
// @grant        GM_xmlhttpRequest
// @connect      localhost
// ==/UserScript==

(function () {
    'use strict';

    if (window.__SCRIPT2_EXECUTED__) return;
    window.__SCRIPT2_EXECUTED__ = true;

    const API_URL = 'http://localhost:8080/api/Bilibili/save_uid';

    function log(text, color = '#0f0') {
        console.log(text);
        let div = document.getElementById('script2-log');
        if (!div) {
            div = document.createElement('div');
            div.id = 'script2-log';
            div.style.cssText = 'position:fixed; top:10px; left:10px; padding:15px; background:rgba(0,0,0,0.9); z-index:99999; font-size:13px; max-width:400px; border-radius:8px; max-height:300px; overflow:auto; font-family:monospace;';
            document.body.appendChild(div);
        }
        div.innerHTML += `<div style="color:${color};margin:2px 0;">${text}</div>`;
        div.scrollTop = div.scrollHeight;
    }

    function extractUid() {
        log('[Script2] 🔄 正在从 __NEPTUNE_IS_MY_WAIFU__ 提取 UID...', '#ff0');

        const neptune = window.__NEPTUNE_IS_MY_WAIFU__;
        if (neptune) {
            if (neptune.roomInitRes?.data?.uid) {
                const uid = parseInt(neptune.roomInitRes.data.uid, 10);
                log(`从 roomInitRes 获取到 UID: ${uid}`, '#0f0');
                return uid;
            }

            if (neptune.roomInfoRes?.data?.room_info?.uid) {
                const uid = parseInt(neptune.roomInfoRes.data.room_info.uid, 10);
                log(`从 roomInfoRes 获取到 UID: ${uid}`, '#0f0');
                return uid;
            }
        }

        log('__NEPTUNE_IS_MY_WAIFU__ 未找到UID，尝试DOM提取...', '#ff0');

        const anchorElements = document.querySelectorAll('[data-anchor-id]');
        for (let el of anchorElements) {
            const uid = el.getAttribute('data-anchor-id');
            if (uid && /^\d{5,}$/.test(uid)) {
                return parseInt(uid, 10);
            }
        }

        const scripts = document.querySelectorAll('script');
        for (let script of scripts) {
            const match = script.textContent.match(/"uid"\s*:\s*(\d+)/);
            if (match) return parseInt(match[1], 10);
        }

        return 0;
    }

    setTimeout(() => {
        const roomId = location.pathname.match(/\/(\d+)/)?.[1];
        if (!roomId) {
            log('[Script2] ❌ 未找到房间号', '#f00');
            return;
        }

        log(`[Script2] 开始处理房间 ${roomId}`, '#0f0');

        let uid = extractUid();

        const neptune = window.__NEPTUNE_IS_MY_WAIFU__;
        const blockInfo = neptune?.roomInfoRes?.data?.block_info;
        const isBlocked = blockInfo?.block === true;
        const hasForbidden = document.querySelector('.user-forbidden') !== null;
        const hasErrorText = document.body.innerText.includes('主播账号异常');

        // 🔴 新增：检测"房间已被封禁"
        const isRoomBlocked = document.querySelector('.room-blocked') !== null ||
                              document.body.innerText.includes('这个房间已经被封禁');

        // 🔴 修改：加入 isRoomBlocked
        const isError = isBlocked || hasForbidden || hasErrorText || isRoomBlocked;

        log(`检测: block=${isBlocked}, forbidden=${hasForbidden}, text=${hasErrorText}, roomBlocked=${isRoomBlocked}`, '#ff0');
        log(`结果: ${isError ? '⚠️ 异常' : '✅ 正常'}`, isError ? '#f80' : '#0f0');
        log(`UID: ${uid || '未找到'}`, uid ? '#0f0' : '#f00');

        const status = isError ? 'error' : (uid > 0 ? 'success' : 'error');

        log(`上报: RoomId=${roomId}, Uid=${uid}, Status=${status}`, '#0ff');

        GM_xmlhttpRequest({
            method: 'POST',
            url: API_URL,
            headers: { 'Content-Type': 'application/json' },
            data: JSON.stringify({
                RoomId: roomId,
                Uid: uid,
                Status: status
            }),
            onload: (r) => {
                log(`HTTP ${r.status}: ${r.responseText}`, '#ff0');

                try {
                    const res = JSON.parse(r.responseText);
                    if (res.success === false) {
                        log(`❌ 服务端拒绝: ${res.message || '未知错误'}`, '#f00');
                        log(`检查: 1.RoomId ${roomId} 是否在数据库 2.Uid=${uid} 是否合法`, '#f80');
                    } else if (res.success === true) {
                        log(`✅ 真正成功`, '#0f0');
                    } else {
                        log(`⚠️ 返回格式异常: ${r.responseText}`, '#f80');
                    }
                } catch (e) {
                    log(`✅ 上报完成: ${r.responseText}`, '#0f0');
                }
            },
            onerror: (r) => {
                log(`❌ HTTP 错误 ${r.status}: ${r.responseText}`, '#f00');
            }
        });

    }, 3000);
})();