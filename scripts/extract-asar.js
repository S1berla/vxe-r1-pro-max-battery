const fs = require("fs");
const path = require("path");

const archive = process.argv[2];
const outputDir = process.argv[3];

if (!archive || !outputDir) {
  console.error(
    "Usage: node extract-asar.js <app.asar> <output-dir> [include-regex]",
  );
  process.exit(1);
}

const buffer = fs.readFileSync(archive);
const pickleSize = buffer.readUInt32LE(4);
const jsonSize = buffer.readUInt32LE(12);
const header = JSON.parse(buffer.subarray(16, 16 + jsonSize).toString("utf8"));
const contentBase = 8 + pickleSize;
const unpackedBase = archive.replace(/\.asar$/i, ".asar.unpacked");
const includePattern = process.argv[4]
  ? new RegExp(process.argv[4])
  : /.*/;

function walk(node, currentPath, output) {
  for (const [name, child] of Object.entries(node.files || {})) {
    const childPath = currentPath ? `${currentPath}/${name}` : name;
    if (child.files) {
      walk(child, childPath, output);
    } else {
      output.push({ path: childPath, node: child });
    }
  }
}

const entries = [];
walk(header, "", entries);

for (const { path: filePath, node } of entries) {
  if (!includePattern.test(filePath)) {
    continue;
  }

  const target = path.join(outputDir, ...filePath.split("/"));
  fs.mkdirSync(path.dirname(target), { recursive: true });

  if (node.unpacked) {
    fs.copyFileSync(path.join(unpackedBase, ...filePath.split("/")), target);
  } else {
    const offset = contentBase + Number(node.offset);
    fs.writeFileSync(
      target,
      buffer.subarray(offset, offset + Number(node.size)),
    );
  }

  console.log(filePath);
}
