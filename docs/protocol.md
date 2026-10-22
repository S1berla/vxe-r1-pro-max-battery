# VXE R1 Pro Max 电量读取协议

## 1. 设备身份

| 项目 | 值 |
| --- | --- |
| 鼠标型号 | VXE Dragonfly R1 Pro Max |
| USB VID | `0x3554` |
| 接收器 PID | `0xF58A` |
| Windows 产品名 | `VXE NordicMouse 1K Dongle` |
| 设备接口 | `MI_01` |
| 数据集合 | `Col05` |
| HID Usage Page | `0xFF02` |
| HID Usage | `2` |
| Input Report Length | 17 字节 |
| Output Report Length | 17 字节 |
| HID Report ID | `0x08` |

设备路径通常类似：

```text
\\?\HID#VID_3554&PID_F58A&MI_01&Col05#...#{4d1e55b2-f16f-11cf-88cb-001111000030}
```

同一个复合设备还暴露了键盘、鼠标、Consumer Control 和多个厂商自定义集合。电量协议使用的是 `0xFF02 / usage 2` 数据集合，不是 `0xFF04` 信息集合。

## 2. HID 报文格式

Windows 中实际写入的是 17 字节：

```text
[Report ID][16 字节 COMPX 命令]
```

Report ID 固定为 `0x08`。

## 3. COMPX 命令格式

逻辑命令固定为 16 字节：

| 偏移 | 长度 | 含义 |
| --- | --- | --- |
| 0 | 1 | Command ID |
| 1 | 1 | Command Status |
| 2 | 2 | EEPROM Address，小端序 |
| 4 | 1 | Valid Data Length |
| 5 | 10 | Command Data |
| 15 | 1 | Checksum |

校验和：

```text
checksum = 85 - ((8 + sum(command[0..14])) & 0xFF)
```

其中 `8` 是 HID Report ID。

## 4. 读取电量流程

推荐顺序：

```text
0x03 GetWirelessMouseOnline
0x01 DownLoadData / 会话读取
0x04 GetBatteryLevel
```

电池查询在鼠标休眠、接收器尚未就绪或官方驱动占用输入句柄时可能超时。托盘程序应使用短间隔重试，并将暂时无响应映射为“设备休眠/不可用”，而不是显示 0%。

## 5. 关键命令

| ID | 名称 | 用途 |
| --- | --- | --- |
| `0x01` | DownLoadData | 读取加密会话数据，返回 CID、MID、设备类型 |
| `0x03` | GetWirelessMouseOnline | 查询鼠标是否在线 |
| `0x04` | GetBatteryLevel | 查询电池 |
| `0x0E` | GetCurrentConfig | 查询当前配置 |
| `0x12` | GetMouseVersion | 查询鼠标版本 |
| `0x19` | GetDongleLightMode | 查询接收器灯光模式 |
| `0x1D` | GetDongleVersion | 查询接收器版本 |

## 6. 实际报文示例

### 查询在线状态

```text
08 03 00 00 00 00 00 00 00 00 00 00 00 00 00 00 4A
```

返回示例：

```text
08 03 00 00 00 01 01 C5 1B 07 00 00 00 00 00 00 61
```

### 会话读取

请求数据为 `01 02 03 04 05 06 07 08`：

```text
08 01 00 00 00 08 01 02 03 04 05 06 07 08 00 00 20
```

返回示例：

```text
08 01 00 00 00 08 05 29 0D 11 02 22 00 08 00 00 CC
```

### 查询电量

```text
08 04 00 00 00 00 00 00 00 00 00 00 00 00 00 00 49
```

返回示例：

```text
08 04 00 00 00 02 5F 00 10 1A 00 00 00 00 00 00 BA
```

## 7. 电量响应解析

`node-hid` 收到的输入缓冲区包含 Report ID。以下索引均基于完整缓冲区：

| 字段 | 索引 | 说明 |
| --- | --- | --- |
| Report ID | 0 | 固定 `0x08` |
| Command ID | 1 | 电量响应为 `0x04` |
| Command Status | 2 | `0` 表示正常 |
| Valid Data Length | 5 | 电量响应通常为 `2` |
| Battery Percentage | 6 | `0..100` |
| Charging State | 7 | `1` 充电，`0` 放电 |
| Battery Voltage | 8..9 | 小端序，单位 mV |

示例：

```text
08 04 00 00 00 02 5F 00 10 1A ...
                  ^  ^  ^-----^
                  |  |     |
                  |  |     +-- 0x1A10 = 6672 mV
                  |  +-------- 0x00 = 放电
                  +----------- 0x5F = 95%
```

## 8. WebHID 与 node-hid 的索引差异

官方 Electron 界面使用 WebHID。`HIDInputReportEvent.data` 不包含 Report ID，因此其字段索引整体比 `node-hid` 少 1。

WebHID：

```text
percentage = data[5]
charging   = data[6]
voltage    = data[7] | (data[8] << 8)
```

node-hid：

```text
percentage = data[6]
charging   = data[7]
voltage    = data[8] | (data[9] << 8)
```

## 9. 已知限制

- 不同接收器 PID 或不同固件版本可能使用不同命令或字段布局。
- 当前只验证了 `PID_F58A` 的 1K 接收器。
- ATK HUB 与第三方读取程序可能争用 HID 输入报告。
- 鼠标休眠时，接收器可能仍报告在线，但鼠标相关命令暂时无响应。

