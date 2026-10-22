# VXE R1 Pro Max 鼠标电量读取

> Read the battery level of a VXE Dragonfly R1 Pro Max mouse over its 2.4G HID receiver on Windows.

一个面向 Windows 的鼠标电量读取项目。当前已完成 VXE Dragonfly R1 Pro Max 的 2.4G 接收器协议分析，并实机验证了电量读取。

## 当前状态

- 设备型号：VXE Dragonfly R1 Pro Max
- 接收器：`VID_3554 / PID_F58A`，产品名 `VXE NordicMouse 1K Dongle`
- 连接方式：2.4G
- 协议：COMPX HID 私有协议
- 验证日期：2026-10-22
- 已验证读数：95% 电量、放电状态、6.672 V
- C# 采集层：已实现并编译通过
- Windows 托盘界面：已实现并编译通过
- 首次运行：默认注册开机启动，可在右键菜单关闭
- 低电量通知：第一版不发送

## 快速运行

环境要求：Windows 10/11 x64。C# 部分使用系统自带的 .NET Framework 4.x 编译器，无需安装额外 SDK。

克隆并构建：

```powershell
git clone https://github.com/S1berla/vxe-r1-pro-max-battery.git
cd vxe-r1-pro-max-battery

# PowerShell 7
pwsh -File .\build.ps1

# 或者使用系统自带的 Windows PowerShell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

也可以直接从 Releases 下载预编译的 exe，跳过构建步骤。

启动托盘：

```powershell
.\build\MouseBatteryTray.exe
```

右键菜单：

- 立即刷新
- 开机启动开关
- 退出

双击托盘图标会打开电池详情窗口。图标内部显示百分比，悬停显示电量、充电状态和电压。

控制台单次读取：

```powershell
.\build\MouseBatteryConsole.exe
```

控制台持续读取：

```powershell
.\build\MouseBatteryConsole.exe --watch --interval 60
```

托盘自检：

```powershell
.\build\MouseBatteryTray.exe --test
```

如果读取超时，先关闭 ATK HUB，再移动鼠标保持唤醒后重试。官方驱动与诊断脚本可能争用同一个 HID 输入句柄。

### Node.js 诊断脚本（可选）

`scripts/` 下的协议诊断脚本需要 Node.js 18+，并需要先安装依赖：

```powershell
npm install
node scripts/read-battery.js   # 或 npm run read:battery
node scripts/probe-paths.js    # 或 npm run probe
```

## 项目结构

```text
.
├─ README.md
├─ LICENSE
├─ package.json
├─ build.ps1                # 使用系统 C# 编译器构建控制台与托盘程序
├─ build/                   # 构建产物，不提交；发布在 GitHub Releases
├─ src/
│  ├─ MouseBattery/         # 可复用的 HID 与电量采集层
│  ├─ MouseBattery.Console/ # 控制台入口
│  └─ MouseBattery.Tray/    # WinForms 托盘程序
├─ docs/
│  ├─ protocol.md           # HID 与 COMPX 协议细节
│  ├─ evidence.md           # 逆向证据与来源
│  ├─ decisions.md          # 已确定决策与待定事项
│  ├─ acquisition.md        # 采集程序结构与使用说明
│  ├─ tray.md               # 托盘程序和运行状态说明
│  └─ next-steps.md         # 后续实现计划
├─ scripts/
│  ├─ read-battery.js       # 读取一次电量
│  ├─ probe-paths.js        # 多集合协议诊断
│  ├─ extract-asar.js       # 从 app.asar 提取调查文件
│  ├─ run-tray.ps1          # 启动托盘或执行无界面自检
│  └─ verify-tray.ps1       # 检查进程、开机启动和日志
```

## 关键文档

- [协议说明](docs/protocol.md)
- [逆向证据](docs/evidence.md)
- [决策记录](docs/decisions.md)
- [采集程序说明](docs/acquisition.md)
- [托盘程序说明](docs/tray.md)
- [下一步实现计划](docs/next-steps.md)

## 安全边界

当前脚本只发送读取命令：

1. 查询鼠标是否在线。
2. 建立只读会话。
3. 查询电量。

不要在执行调查脚本时混入固件升级、恢复出厂、写入配置或配对命令。后续托盘程序也应坚持只读原则。

## 免责声明

- 本项目为非官方社区项目，与 VXE、ATK 官方无任何关联。
- 所有 HID 命令均为只读查询（查询在线状态、建立只读会话、读取电量），不会写入配置、触发固件升级或修改设备状态。
- 协议信息通过分析本机安装的官方驱动获得，仅用于互操作性目的。
- 使用本项目的风险由使用者自行承担。

## 许可证

[MIT](LICENSE)

