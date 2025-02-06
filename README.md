# Quantum: Zjuers 桌面端一站式工具

一款基于 .NET 8 , Blazor 和 Electron.NET 开发的，面向浙大学子的桌面端一站式工具。

## 目前已实现功能

- 选课系统：
   - 课程信息抓取与整合
   - 教师评分系统集成
   - 选课概率计算
   - 毕业要求检查
   - 自动课表优化

## 项目结构

- **Quantum.Core**: 包含核心业务模型和接口定义、枚举、常量等
- **Quantum.Infrastructure**: 实现核心接口，处理外部交互，包含 Exception、 Utilities 等
- **Quantum.UI**: 基于 Blazor 和 Electron.NET 的用户界面，包含States等。

## 开发环境要求

- .NET 8 SDK
- Node.js（用于 Electron.NET）
- Visual Studio 2022 或更高版本（推荐）

electronize.exe start /PublishSingleFile false
electronize.exe build

## 参与贡献

本项目正在积极开发中。欢迎各位同学前来贡献代码。

1. Fork 本项目
2. 创建新的 Feature branch
3. 将修改 Push 到自己的 branch
4. 创建 Pull Request

## 开源协议

[MIT 开源协议](LICENSE)
