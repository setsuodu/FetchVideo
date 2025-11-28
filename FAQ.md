一、只改这里:
```
# 阶段2: 运行
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine
RUN apk add --no-cache ffmpeg
```

二、测试图片
https://yourdoll.jp/wp-content/uploads/2025/06/mmd213-00.jpg

三、测试视频
（视频）
https://www.bilibili.com/video/BV16p1QBtEsR  31:00
https://www.bilibili.com/video/BV1jYkyBoEoo  01:00
https://m.bilibili.com/video/BV1RSpszvE8h
https://b23.tv/fC5i764
（直播）
https://live.bilibili.com/1848767744
https://live.bilibili.com/h5/1848767744
https://b23.tv/gWcSTNw
（视频）
https://www.youtube.com/watch?v=TsWzmbvGIsY  06:34
https://youtu.be/TsWzmbvGIsY
https://www.youtube.com/shorts/jVbkzaKBSFM

四、curl 短链 → 长链
curl -i https://b23.tv/gWcSTNw
Location: https://live.bilibili.com/1864385910?。。。。。。


image:latest更新
setsuodu/fetch-service   latest    c2cb0481a077   19 hours ago    225MB
-----------------------------------------------------------------------
👇👇👇👇👇👇👇👇👇👇👇👇👇👇👇👇👇👇👇👇👇👇👇👇👇👇👇👇👇👇👇
-----------------------------------------------------------------------
setsuodu/fetch-service   <none>    c2cb0481a077   19 hours ago    225MB 👈Docker 不会自动删除无标签镜像（<none>），除非你手动清理
setsuodu/fetch-service   latest    1827b502d82e   11 minutes ago  245MB


## MORE
- VOLUME 是“声明意图”（好习惯）
	- VOLUME ["/app/downloads"]
	- 作用：
		- 声明容器内的 /app/downloads 是一个“卷”（volume）目录。
		- 告诉 Docker：这个目录里的数据应该持久化，不要随容器删除而丢失。
		- 如果你没有手动映射这个目录，Docker 会自动创建一个匿名的 Docker 卷（anonymous volume），挂载到 /app/downloads。
- -v 是“实际映射”（你能看到文件）
	- 作用：
		- 宿主机目录:容器内目录 ``-v /download:/app/downloads``
- 效果：
	- 位置路径宿主机/download/pic.jpg（C:/downloads/pic.jpg）
	- 容器内/app/downloads/pic.jpg


# 汇总
1. VS调试：✅全部正常
2. Docker Desktop调试：✅全部正常
3. 飞牛云调试：图片✅，B站✅，YT❌

# 2025/11/25 解决特殊情况
个别视频用 https://api.bilibili.com/x/player/pagelist 接口，part乱码：lv_0_20250329183802
如：https://www.bilibili.com/video/BV1P8ZAYPENT
👆基于以上，更换view接口