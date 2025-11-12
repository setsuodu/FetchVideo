#!/bin/bash
# deploy.sh - 一键构建 + 运行

echo "停止旧容器..."
docker rm -f downloader || true

echo "构建镜像..."
docker build -t fetch-service .

echo "启动新容器..."
docker run -d \
  --name downloader \
  -p 8080:8080 \
  -v /download:/app/downloads \
fetch-service

echo "部署完成！访问 http://localhost:8080/swagger"