# 油猴脚本说明

1. Bilibili 直播脚本1-开房间.js
	- 【下载工具/订阅录制】下：15秒开一个，只管开
	- 要求 localhost，不要在 remote容器爬
2. Bilibili 直播脚本2-采集.js
	- 主播的 UID，直播间状态（正常或异常）
	- 要求 localhost，不要在 remote容器爬
3. B站_主页_投稿_视频_自动翻页合并导出.js
	- 用户主页_投稿_视频，自动翻页导出json，用于【下载工具/UP投稿打包】上传，下载所有视频
4. B站_主页_投稿_图文_自动爬取器.js
	- 用户主页_投稿_图文，自动翻页导出json，用于【新工具】，下载所有子页照片
	- GET https://api.bilibili.com/x/polymer/web-dynamic/v1/opus/detail?id={opusId}