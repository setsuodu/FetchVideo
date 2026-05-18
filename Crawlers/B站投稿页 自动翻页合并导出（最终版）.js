// ==UserScript==
// @name         B站投稿页 自动翻页合并导出（最终版）
// @namespace    https://github.com/
// @version      2.4
// @description  自动翻页 + 内存合并 + 最后只导出一个文件
// @author       Grok
// @match        https://space.bilibili.com/*/upload/video*
// @grant        GM_download
// @run-at       document-end
// ==/UserScript==

(async function () {
    'use strict';

    await new Promise(r => setTimeout(r, 3000));

    const midMatch = location.pathname.match(/\/(\d+)\/upload\/video/);
    const mid = midMatch ? midMatch[1] : 'unknown';

    let upName = document.querySelector('.nickname')?.textContent.trim() ||
                 document.querySelector('.user-name')?.textContent.trim() ||
                 '未知UP';

    const allVideos = [];
    const seen = new Set();

    const btn = document.createElement('button');
    btn.innerHTML = '🚀 开始自动翻页合并导出';
    btn.style.cssText = `position:fixed;top:80px;right:20px;z-index:99999;padding:16px 28px;background:#fb7299;color:white;border:none;border-radius:8px;cursor:pointer;font-size:17px;font-weight:bold;`;
    document.body.appendChild(btn);

    btn.onclick = async () => {
        btn.disabled = true;
        btn.textContent = '⏳ 正在翻页合并... 第1页';

        let page = 1;

        while (true) {
            // 抓当前页
            let added = 0;
            document.querySelectorAll('.bili-video-card').forEach(card => {
                const titleEl = card.querySelector('.bili-video-card__title a');
                const linkEl = card.querySelector('a[href*="BV"]');
                if (titleEl && linkEl) {
                    const bvidMatch = linkEl.href.match(/BV[A-Za-z0-9]{10}/);
                    if (bvidMatch) {
                        const bvid = bvidMatch[0];
                        if (!seen.has(bvid)) {
                            seen.add(bvid);
                            allVideos.push({
                                title: titleEl.textContent.trim(),
                                bvid: bvid
                            });
                            added++;
                        }
                    }
                }
            });

            console.log(`第${page}页 新增 ${added} 条，累计 ${allVideos.length} 条`);

            // 找下一页按钮
            const nextBtn = document.querySelector('.vui_pagenation--btn-side:last-child');

            if (!nextBtn || nextBtn.disabled || page >= 30) {
                break;
            }

            nextBtn.click();
            await new Promise(r => setTimeout(r, 2200)); // 等待加载
            page++;
            btn.textContent = `⏳ 正在翻页合并... 第${page}页`;
        }

        // ================== 全部翻完后一次性导出 ==================
        if (allVideos.length === 0) {
            alert('没抓到视频');
            return;
        }

        const fileName = `${upName}_${mid}_${allVideos.length}条.json`;

        const jsonStr = JSON.stringify(allVideos, null, 2);
        const blob = new Blob([jsonStr], { type: 'application/json' });

        GM_download({
            url: URL.createObjectURL(blob),
            saveAs: true,
            name: fileName
        });

        btn.textContent = '✅ 全部完成！';
        alert(`🎉 导出完成！\n总共 ${allVideos.length} 条视频\n文件名：${fileName}`);
    };
})();