const HID = require("node-hid");

const VENDOR_ID = 0x3554;
const PRODUCT_ID = 0xf58a;
const REPORT_ID = 0x08;

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

async function main() {
  const vendorDevices = HID.devices().filter(
    (device) =>
      device.vendorId === VENDOR_ID &&
      device.productId === PRODUCT_ID &&
      [0xff03, 0xff05, 0xff02].includes(device.usagePage),
  );

  const opened = [];

  for (const info of vendorDevices) {
    try {
      const device = new HID.HID(info.path);
      opened.push({ info, device });

      device.on("data", (data) => {
        console.log(
          `data usagePage=0x${info.usagePage.toString(16)}`,
          JSON.stringify(Array.from(data)),
        );
      });

      device.on("error", (error) => {
        console.log(
          `error usagePage=0x${info.usagePage.toString(16)}`,
          error.message,
        );
      });

      console.log(
        "opened",
        `usagePage=0x${info.usagePage.toString(16)}`,
        info.path,
      );
    } catch (error) {
      console.log(
        "open failed",
        `usagePage=0x${info.usagePage.toString(16)}`,
        error.message,
      );
    }
  }

  const writer = opened.find(({ info }) => info.usagePage === 0xff02);
  if (!writer) {
    throw new Error("0xFF02 data transport collection not found");
  }

  const reports = [
    buildCommand(0x03),
    buildCommand(0x01, [1, 2, 3, 4, 5, 6, 7, 8]),
    ...Array.from({ length: 10 }, () => buildCommand(0x04)),
  ];

  for (const report of reports) {
    console.log("write", JSON.stringify(report));
    console.log("written", writer.device.write(report));
    await new Promise((resolve) => setTimeout(resolve, 500));
  }

  await new Promise((resolve) => setTimeout(resolve, 1000));

  for (const { device } of opened) {
    try {
      device.close();
    } catch {
      // The diagnostic can continue after a device disappears.
    }
  }
}

main().catch((error) => {
  console.error(error.stack || error.message || String(error));
  process.exitCode = 1;
});

