# 阶段1: 构建
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# 安装 ffmpeg 和 yt-dlp
RUN apk add --no-cache ffmpeg python3 py3-pip && \
    pip3 install --no-cache-dir --index-url https://pypi.tuna.tsinghua.edu.cn/simple yt-dlp

WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# 阶段2: 运行
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .

# API 端口
EXPOSE 8080

# 容器内 /app/downloads 映射到宿主机 /download
VOLUME ["/app/downloads"]
ENTRYPOINT ["dotnet", "FetchService.dll"]