const HID = require("node-hid");

const TARGET = {
  vendorId: 0x3554,
  productId: 0xf58a,
  usagePage: 0xff02,
  usage: 2,
};

const REPORT_ID = 0x08;
const BATTERY_COMMAND_ID = 0x04;
const MAX_BATTERY_ATTEMPTS = 20;

function findTarget() {
  return HID.devices().find(
    (device) =>
      device.vendorId === TARGET.vendorId &&
      device.productId === TARGET.productId &&
      device.usagePage === TARGET.usagePage &&
      device.usage === TARGET.usage,
  );
}

function buildCommand(commandId, data = []) {
  const command = [
    commandId,
    0,
    0,
    0,
    data.length,
    ...data,
    ...Array.from({ length: Math.max(0, 10 - data.length) }, () => 0),
  ];

  command[15] =
    85 -
    (command.slice(0, 15).reduce((sum, value) => sum + value, 8) & 0xff);

  return [REPORT_ID, ...command];
}

function transact(device, commandId, data, timeoutMs) {
  return new Promise((resolve, reject) => {
    const onData = (buffer) => {
      const bytes = Array.from(buffer);
      if (bytes[0] !== REPORT_ID || bytes[1] !== commandId) {
        return;
      }
      cleanup();
      resolve(bytes);
    };

    const cleanup = () => {
      clearTimeout(timeout);
      device.removeListener("data", onData);
    };

    const timeout = setTimeout(() => {
      cleanup();
      reject(
        new Error(
          `Command 0x${commandId.toString(16)} timed out after ${timeoutMs} ms`,
        ),
      );
    }, timeoutMs);

    device.on("data", onData);
    device.write(buildCommand(commandId, data));
  });
}

async function readBattery() {
  const target = findTarget();
  if (!target) {
    throw new Error(
      "VXE NordicMouse 1K Dongle data interface was not found",
    );
  }

  const device = new HID.HID(target.path);

  try {
    await transact(device, 0x03, [], 1000);
    await transact(device, 0x01, [1, 2, 3, 4, 5, 6, 7, 8], 1000);

    let response;
    let lastError;

    for (let attempt = 1; attempt <= MAX_BATTERY_ATTEMPTS; attempt += 1) {
      try {
        response = await transact(device, BATTERY_COMMAND_ID, [], 500);
        break;
      } catch (error) {
        lastError = error;
      }
    }

    if (!response) {
      throw lastError || new Error("Battery response was not received");
    }

    return {
      power: response[6],
      battery: response[7] === 1 ? "charging" : "discharging",
      voltageMv: response[8] | (response[9] << 8),
      commandStatus: response[2],
    };
  } finally {
    device.close();
  }
}

readBattery()
  .then((result) => {
    console.log(JSON.stringify(result, null, 2));
  })
  .catch((error) => {
    console.error(error.stack || error.message || String(error));
    process.exitCode = 1;
  });

