🌍 **Language / 语言 / 言語**
[English](./README.md) | [中文](./README.zh.md)

[![en](https://img.shields.io/badge/lang-English-blue.svg)](README.md)
[![zh](https://img.shields.io/badge/语言-中文-red.svg)](README.zh.md)

# FetchVideo
Docker Services & CrossPlatfom App

# Client

A Windows console application, to download a video you should follow these steps:
1. double click FetchVideo.exe;
2. paste video URL;
3. press 'enter' on your keyboard;
4. check your desktop, .mp4 file is there;

## Build

```
dotnet build -c Release
```

## Support
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

上下文构建
```
cd FetchVideo/Service/FetchService
docker build -t fetch-service .
```
从项目根目录构建（推荐）：
```
docker build -f Service/FetchService/Dockerfile -t fetch-service .
```
构建后运行（Docker Desktop，映射C盘）
```
docker run -d --name downloader -p 8080:8080  -v C:/users/33913/downloads:/app/downloads fetch-service
```
（运行没问题）推送远程
```

```

## Docker Desktop for Windows
```
mkdir -p C:\downloads  ##-p : make sure folder exist

docker run -d \
  --name downloader \
  -p 8080:8080 \
  -v C:/users/33913/downloads:/app/downloads \  ## use C:/ the Host is Windows
  fetch-service
```

## Ubuntu / Synology / fnOS Common

- Windows: c:\users\你的用户名\downloads
- Ubuntu: ~/downloads
- Synology: /volume1/download
- fnOS: /vol1/1000/download

```
mkdir -p /download  ##create Host folder

docker run -d \
  --name downloader \
  -p 8080:8080 \
  -v /download:/app/downloads \
  fetch-service
```

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

## Push DockerHub

1. create a new repository on hub.docker.com, named "setsuodu/fetch-service"
2. docker build -t fetch-service .
3. docker tag fetch-service setsuodu/fetch-service:latest
4. docker login(if needed, use Credential Storage in your OS)
5. docker push setsuodu/fetch-service:latest

## WebView

WebView：http://Your IP:8080 → jump to index.html
How to get 404 logs：on index click → http://your IP:8080/downloads/download_404.txt