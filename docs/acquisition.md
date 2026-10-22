# C# 电量采集程序

> 以下命令均假设当前终端已切换到项目根目录，并使用 PowerShell 7（`pwsh`）。

## 目标

把已经验证的 HID 读取流程实现成可复用组件，后续 WinForms 托盘程序不重复编写协议代码。

## 项目结构

```text
src/
├─ MouseBattery/
│  ├─ HidNative.cs          # SetupAPI、HID 和异步 I/O P/Invoke
│  └─ VxeBatteryReader.cs   # 设备发现、命令、重试和结果解析
└─ MouseBattery.Console/
   └─ Program.cs            # 单次/循环读取的演示入口
```

## 核心接口

```csharp
var reader = new VxeBatteryReader();
MouseBatteryReading reading = reader.ReadOnce();

Console.WriteLine(reading.Power);
Console.WriteLine(reading.IsCharging);
Console.WriteLine(reading.VoltageMillivolts);
```

`MouseBatteryReading` 字段：

| 字段 | 类型 | 含义 |
| --- | --- | --- |
| `Power` | `int` | 电量百分比 |
| `IsCharging` | `bool` | 是否正在充电 |
| `VoltageMillivolts` | `int` | 电压，单位 mV |
| `CommandStatus` | `byte` | 设备命令状态 |
| `RetrievedAtUtc` | `DateTime` | 采集时间 |

## 设备发现

`HidNative.EnumerateInterfaces()` 使用 SetupAPI 枚举 HID 接口，并通过 Preparsed Data 读取每个集合的：

- Usage Page
- Usage
- Input/Output/Feature Report 长度

采集器只选择：

```text
VID_3554
PID_F58A
usagePage 0xFF02
usage 2
```

这样可以避免误用键盘、鼠标或 `0xFF04` Feature 信息集合。

## 命令顺序

```text
0x03  查询无线鼠标在线状态
0x01  建立会话并读取加密状态
0x04  查询电量，最多重试 20 次
```

每次重试都会重新等待 500 ms。超时不会被解释为 0%。

## 构建

项目使用 Windows 自带的 C# 编译器：

```powershell
pwsh -File .\build.ps1
```

输出：

```text
build\MouseBatteryConsole.exe
build\MouseBatteryTray.exe
```

## 使用

单次读取：

```powershell
.\build\MouseBatteryConsole.exe
```

持续读取：

```powershell
.\build\MouseBatteryConsole.exe --watch --interval 60
```

JSON：

```powershell
.\build\MouseBatteryConsole.exe --json
```

## 实机验证

C# 控制台程序已经直接从 `PID_F58A` 接收器读取成功：

```json
{
  "power": 95,
  "battery": "discharging",
  "voltageMv": 7696,
  "commandStatus": 0
}
```

这次验证使用的就是后续托盘层要引用的
`VxeBatteryReader.ReadOnce()`，没有调用官方驱动界面。

## 托盘复用方式

WinForms 托盘项目可以直接引用：

```text
src\MouseBattery\HidNative.cs
src\MouseBattery\VxeBatteryReader.cs
```

托盘计时器调用 `ReadOnce()`，根据返回结果更新 `NotifyIcon.Icon`、`Text` 和悬停详情，不需要经过控制台程序。

## 已知行为

- 鼠标休眠时，在线状态仍可能为真，但电量查询会超时。
- 官方 ATK HUB 可能占用 HID 输入句柄。
- 可以退出 ATK HUB 客户端以减少 HID 争用；不要卸载接收器或 HID 驱动。
- 托盘层应保留上一次有效读数，并在超时时显示“设备休眠/未更新”。

