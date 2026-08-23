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

## Migrate

1. Delete
```
dotnet ef migrations remove
```

2. Add
```
dotnet ef migrations add Init
```

3. Update
```
dotnet ef database update
```

## Run

```
// 👇默认只能用 http://localhost:8080访问
dotnet run

// 👇如果需要用IP，局域网内访问
dotnet run --urls "http://*:8080"
```

## Build

```
dotnet build -c Release
```

## Test
1. Bilibili video
	- https://b23.tv/fC5i764
	- https://www.bilibili.com/video/BV~
2. Bilibili live
	- https://b23.tv/uKettYB（手机分享生成。浏览器带手机标签，会产生/h5/）
	- https://live.bilibili.com/room_id
3. Youtube video
	- full url: https://www.youtube.com/watch?v=TsWzmbvGIsY
	- short url: https://youtu.be/TsWzmbvGIsY
	- Shorts: https://www.youtube.com/shorts/jVbkzaKBSFM

# Deploy

Ctrl + `
D:\GitHub\[Workspace]\FetchVideo>

💻从项目根目录构建（推荐）：
```
docker build -f Service/Service/Dockerfile -t fetch-service .
```
⚙️构建后运行（Docker Desktop，映射C盘）
```
docker run -d --name downloader -p 8080:8080  -v C:/users/33913/downloads:/app/downloads fetch-service
```
💻（运行没问题）推送远程
```
# 1. 先给本地镜像打上 1.4.1 标签
docker tag fetch-service setsuodu/fetch-service:1.4.1

# 2. 再给本地镜像打上 latest 标签（如果你本地还没有 latest，可以再打一次）
docker tag fetch-service setsuodu/fetch-service:latest

# 3. 推送 1.4.1 标签
docker push setsuodu/fetch-service:1.4.1

# 4. 推送 latest 标签
docker push setsuodu/fetch-service:latest
```
🐳部署机器上更新
```
docker pull setsuodu/fetch-service:latest
```
🐳挂载app.db映射
```
docker run -d \
  --name downloader \
  -p 8080:8080 \
  -v /vol1/1000/download:/app/downloads \
  -v /vol1/1000/docker/fetch-service:/app/data \
  setsuodu/fetch-service:1.4.1
  
docker run -d --name downloader -p 8080:8080 -v /vol1/1000/download:/app/downloads -v /vol1/1000/docker/fetch-service:/app/data setsuodu/fetch-service:1.4.1
```

🐳删除旧的镜像，出现<none>是latest标签被取代了
```
docker ps
docker stop <Container_ID>
docker rm <Container_ID>

docker images -a
docker rmi <Image_ID>
```

## Ubuntu / Synology / fnOS Common

- Windows: c:\users\你的用户名\downloads
- Ubuntu: ~/downloads
- Synology: /volume1/download
- fnOS: /vol1/1000/download


## Push DockerHub

1. create a new repository on hub.docker.com, named "setsuodu/fetch-service"
2. docker build -t fetch-service .
3. docker tag fetch-service setsuodu/fetch-service:latest
4. docker login(if needed, use Credential Storage in your OS)
5. docker push setsuodu/fetch-service:latest

## WebView

WebView：http://Your IP:8080 → jump to index.html
How to get 404 logs：on index click → http://your IP:8080/downloads/download_404.txt