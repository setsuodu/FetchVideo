# FAQ

1. 本项目纯Dockerfile构建，没有docker-compose，新增 Shared 引起 Dockerfile 构建错误。
	- 修正：在根目录执行 docker build -t fetch-service -f Service/Service/Dockerfile .