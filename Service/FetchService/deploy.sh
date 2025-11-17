#!/bin/bash
# deploy.sh - 一键构建 + 运行

cd FetchVideo/Service/FetchService

echo "构建镜像..."
docker build -t fetch-service .

echo "打标签..."
docker tag fetch-service setsuodu/fetch-service:latest

echo "推送..."
docker push setsuodu/fetch-service:latest


# 👇以下在docker里执行
echo "拉取新镜像..."
docker pull setsuodu/fetch-service:latest

echo "运行..."
docker run -d --name downloader -p 8080:8080 -v /vol1/1000/download:/app/downloads setsuodu/fetch-service:latest
