🌍 **Language / 语言 / 言語**
[English](./README.md) | [中文](./README.zh.md)

[![en](https://img.shields.io/badge/lang-English-blue.svg)](README.md)
[![zh](https://img.shields.io/badge/语言-中文-red.svg)](README.zh.md)

# FetchVideo
Docker Services &amp; CrossPlatfom App

# Client

A Windows console application, to download a video you should follow these steps:
1. double click FetchVideo.exe;
2. paste video URL;
3. press 'enter' on your keyboard;
4. check your desktop, .mp4 file is there;

## Support:
1. Bilibili video
	- https://www.bilibili.com/video/BV~
2. Bilibili live
	- https://b23.tv/uKettYB
	- https://live.bilibili.com/room_id
3. Youtube video
	- basic: 
	- short url: 
	- short video: 

# Service
构建
```
docker build -t fetch-service .
```

运行
```
docker run -d --name downloader -p 8080:8080 -v /download:/app/downloads -e DOWNLOAD_PATH=/app/downloads fetch-service
```

停止旧容器&构建&运行
```
# 先停止并删除旧容器（避免端口冲突）
docker rm -f downloader

# 构建 + 运行（一条命令搞定）
docker build -t fetch-service . && \
docker run -d \
  --name downloader \
  -p 8080:8080 \
  -v /download:/app/downloads \
  fetch-service
```