# 托盘实现与后续计划

## 目标

构建一个 Windows 托盘程序，在不打开 ATK HUB 界面的情况下周期性读取 VXE R1 Pro Max 电量，并持续显示在通知区域。

## 推荐技术栈

- C# / .NET Framework 4.8
- WinForms `NotifyIcon`
- Windows HID P/Invoke
- 单实例 Mutex
- 默认：当前用户开机启动注册表项

采集层已经完成：

- `src\MouseBattery\HidNative.cs`
- `src\MouseBattery\VxeBatteryReader.cs`
- `build\MouseBatteryConsole.exe`

托盘层已经完成：

- `src\MouseBattery.Tray\Program.cs`
- `src\MouseBattery.Tray\TrayApplicationContext.cs`
- `src\MouseBattery.Tray\BatteryIconRenderer.cs`
- `build\MouseBatteryTray.exe`

## 第一版功能

1. 启动后定位 `VID_3554 / PID_F58A` 的 `0xFF02 / usage 2` 集合。
2. 发送 `0x03` 在线检查。
3. 发送 `0x01` 会话读取。
4. 发送 `0x04` 电量查询。
5. 在托盘中显示电量图标和百分比。
6. 悬停显示：
   - 鼠标型号
   - 电量百分比
   - 充电/放电状态
   - 电压
   - 最近更新时间
7. 处理设备断开、重新连接、读取超时和程序退出。

## 托盘接入点

托盘计时器直接调用：

```csharp
MouseBatteryReading reading = new VxeBatteryReader().ReadOnce();
```

读取失败时保留上一次成功值。不要因为超时把电量更新为 0%。

## 状态机

```text
Starting
  -> DeviceFound
  -> Reading
  -> Ready

Ready
  -> Reading (timer)
  -> Sleeping (timeout)
  -> DeviceMissing (receiver removed)

Sleeping
  -> Reading (retry or next timer)

DeviceMissing
  -> DeviceFound (WM_DEVICECHANGE or polling)
```

## 托盘显示

- 鼠标轮廓图标，绿色填充高度等于电量比例
- 图标内不显示具体数字
- 充电中：悬停文字标明充电状态
- 无响应：灰色轮廓或灰色旧电量填充

不要把无响应显示成 `0%`。

## 错误处理

- 单次读取超时：保留上一次成功读数，并标记为“未更新”。
- 连续多次超时：显示“设备休眠或驱动占用”。
- 接收器不存在：显示“未连接”。
- 程序退出或系统休眠：取消未完成读取，关闭 HID 句柄。

## 建议刷新策略

```text
正常：60 秒
低电量：30 秒
读取失败：5 秒、15 秒、30 秒退避
系统唤醒：立即重试
```

鼠标电量变化缓慢，不需要高频轮询。

## 验证清单

- [x] C# 控制台读取成功
- [x] 托盘程序编译成功
- [x] 托盘程序成功读取 95% 电量
- [x] 托盘进程单实例启动
- [x] 开机启动项写入当前用户注册表
- [x] 鼠标休眠时保留上次有效值并显示未更新
- [x] Per-Monitor V2 DPI 清单
- [ ] ATK HUB 同时打开时的完整回归
- [ ] 接收器拔出/插回的自动恢复验证
- [ ] 长时间运行稳定性验证

## 后续扩展

- 低电量 Windows 通知
- 电池历史曲线
- 多设备配置
- 日志导出
- 其他已验证 PID 的显式支持

