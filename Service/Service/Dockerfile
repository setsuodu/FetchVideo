# 阶段1: 构建
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
#COPY . .
#RUN dotnet restore


# 1. 复制所有必需的项目文件夹和解决方案文件
# 路径相对于项目根目录 (即构建上下文)
COPY Shared/ Shared/ 
COPY Service/ Service/ 
# 注意：我们不需要复制 Client/ 目录，因为它不是 Service 的依赖。

# 2. 还原依赖项
# 将工作目录切换到包含 .sln 文件的目录
# 这确保了 dotnet restore 能正确解析所有项目间的相对路径
WORKDIR /src/Service 
RUN dotnet restore

# 3. 编译和发布主项目
# 将工作目录切换到主项目文件所在的目录
WORKDIR /src/Service/Service

# 使用 ARG 接收 GitHub Actions 传过来的版本
ARG APP_VERSION=dev
ENV APP_VERSION=${APP_VERSION}

RUN dotnet publish -c Release -o /app/publish \
    /p:Version=${APP_VERSION} \
    /p:AssemblyVersion=${APP_VERSION} \
    /p:FileVersion=${APP_VERSION}

# 阶段2: 运行
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine
RUN sed -i 's|http://dl-cdn.alpinelinux.org|https://mirrors.tuna.tsinghua.edu.cn|g' /etc/apk/repositories && \
    apk add --no-cache ffmpeg
WORKDIR /app
COPY --from=build /app/publish .

# API 端口
EXPOSE 8080

# 容器内 /app/downloads 映射到宿主机 /download
VOLUME ["/app/downloads"]
ENTRYPOINT ["dotnet", "FetchVideo.Service.dll"]