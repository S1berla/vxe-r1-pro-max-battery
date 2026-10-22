# Windows 托盘程序

## 已实现功能

- WinForms `NotifyIcon` 托盘入口
- 图标内部显示当前电量百分比
- 悬停显示型号、百分比、充电状态和电压
- 双击打开电池详情窗口
- 右键“立即刷新 / 开机启动 / 退出”
- 首次运行默认注册当前用户开机启动
- 用户可在右键菜单关闭开机启动
- 正常状态每 60 秒刷新
- 读取失败后保留上一次有效值并显示灰色“未更新”状态
- 单实例运行
- Per-Monitor V2 DPI 感知
- 日志写入 `%LOCALAPPDATA%\VxeMouseBatteryTray\app.log`
- 第一版不发送低电量通知

## 启动

```powershell
.\build\MouseBatteryTray.exe
```

托盘程序启动后不会显示主窗口，只会出现在 Windows 通知区域。

## 自检

不启动托盘界面，只验证采集链路：

```powershell
pwsh -File .\scripts\run-tray.ps1 -Test

# 或使用 Windows PowerShell
powershell -ExecutionPolicy Bypass -File .\scripts\run-tray.ps1 -Test
```

运行自检前应先退出正在运行的托盘程序，避免两个 HID 读取器争用输入报告。

正常输出：

```json
{"power":95,"battery":"discharging","voltageMv":6672,"commandStatus":0}
```

## 检查运行状态

```powershell
pwsh -File .\scripts\verify-tray.ps1
```

该脚本会检查：

- 托盘进程数量和路径
- 当前用户开机启动命令
- 首次启动配置标记
- 最近的运行日志

## 开机启动

注册表位置：

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run
```

值名：

```text
VxeMouseBatteryTray
```

首次启动时自动注册。用户关闭右键菜单里的“开机启动”后，后续启动不会强制重新开启。

## 状态规则

| 状态 | 图标/文字 |
| --- | --- |
| 60% 以上 | 绿色电量图标 |
| 20% 到 59% | 橙色电量图标 |
| 低于 20% | 红色电量图标 |
| 充电中 | 蓝色电量图标 |
| 读取失败但存在旧值 | 灰色旧百分比，悬停提示“未更新” |
| 从未读取成功 | 灰色问号 |

读取失败不会被解释为 0% 电量。

## 已知限制

- `NotifyIcon` 不支持在图标旁长期显示独立文字。
- 鼠标休眠时，接收器仍可能在线，但电量查询会超时。
- ATK HUB 与托盘程序可能争用 HID 输入句柄。
- 第一版没有低电量通知。

## 实机验证

- 托盘进程成功单实例运行。
- 首次运行已写入当前用户开机启动项。
- 运行日志中已记录成功读取 `95% / discharging`。
- 鼠标休眠时按预期超时，并保留上一次有效读数。

