# 逆向证据与来源

## 1. 调查对象

官方软件：

```text
D:\Program Files\ATK HUB\ATK HUB.exe
```

已安装版本：

```text
ATK HUB 3.0.18
```

主要资源：

```text
D:\Program Files\ATK HUB\resources\app.asar
D:\Program Files\ATK HUB\resources\main\app.asar
D:\Program Files\ATK HUB\resources\app.asar.unpacked\node_modules\node-hid
```

## 2. 设备枚举证据

Windows 对目标设备枚举出以下关键集合：

| Usage Page | Usage | Input | Output | Feature | 用途 |
| --- | --- | --- | --- | --- | --- |
| `0xFF05` | - | 8 | 0 | 0 | 厂商输入集合 |
| `0xFF03` | - | 8 | 0 | 0 | 厂商输入集合 |
| `0xFF02` | 2 | 17 | 17 | 0 | 电量数据通道 |
| `0xFF04` | 2 | 0 | 0 | 8 | 设备信息/识别集合 |

`0xFF04` 只用于设备识别，不能作为电量命令的 Output Report 通道。

## 3. ASAR 中的关键代码

官方前端中可定位到以下逻辑：

```js
class BatteryCommand extends Command {
  static get() {
    return new BatteryCommand(BatteryCommand.getData(), CommandId.GetBatteryLevel);
  }

  get batteryLevel() {
    return this.raw.getUint8(this.baseOffset);
  }

  get batteryCharge() {
    return this.raw.getUint8(this.baseOffset + 1);
  }

  get batteryVoltage() {
    return this.raw.getUint16(this.baseOffset + 2);
  }
}
```

命令 ID：

```text
GetWirelessMouseOnline = 3
GetBatteryLevel = 4
DownLoadData = 1
```

传输层：

```js
async transferForResult(command) {
  return command.raw = await this.transferForResultAsync(
    command.toUint8Array(),
    8
  );
}
```

这确认了 HID Report ID 为 `8`。

命令基类：

```js
toUint8Array() {
  const bytes = new Uint8Array(this.raw.buffer);
  this.checkSum =
    85 - (sum([8, ...bytes.slice(0, 15)]) & 0xFF);
  return new Uint8Array(this.raw.buffer);
}
```

电池命令解析基址：

```text
baseOffset = 5
```

因此电池字段相对于命令数据位于：

```text
batteryLevel  = data[5]
batteryCharge = data[6]
batteryVoltage = littleEndian16(data[7..8])
```

## 4. 已排除的错误线索

- `D:\Program Files\iot_driver\iot_driver.exe` 是键盘驱动，不属于 R1 Pro Max 协议链。
- 旧版 `V HUB` 的 key-linker worker 不是 R1 Pro Max 当前使用的 COMPX 协议。
- `0xFF04 / usage 2` Feature Report 不是电量数据传输通道。
- `PID_F58A` 的键盘和鼠标集合不应作为配置命令入口。

## 5. 实机验证

使用最小 `node-hid` 脚本向 `0xFF02` 集合发送：

```text
0x03 在线检查
0x01 会话读取
0x04 电量查询
```

获得的电量响应示例：

```text
08 04 00 00 00 02 5F 00 10 1A 00 00 00 00 00 00 BA
```

解析结果：

```text
电量：95%
状态：放电
电压：6672 mV
命令状态：0
```

## 6. USBPcap 的地位

USBPcap 可用于进一步比较官方驱动和自研程序的 USB 流量，但当前协议已经通过以下证据闭环：

1. 官方前端代码中的命令 ID、校验和和字段偏移。
2. Windows HID 描述符中的集合和报告长度。
3. 实机发送读取命令后返回的真实电量数据。

因此托盘程序的第一版不依赖 USBPcap。

## 7. 复现提取过程

```powershell
node .\scripts\extract-asar.js `
  'D:\Program Files\ATK HUB\resources\app.asar' `
  '.\research\atk-hub-asar' `
  '^(dist-electron/.*\.js|node_modules/atk-mouse-system-api/.*|package\.json)$'
```

调查时应只读取官方安装目录，不运行固件升级工具，不发送写入或恢复出厂命令。

