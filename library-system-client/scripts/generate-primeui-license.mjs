import {
  mkdir,
  writeFile
} from 'node:fs/promises';

import {
  dirname,
  resolve
} from 'node:path';

import {
  fileURLToPath
} from 'node:url';

const currentDirectory =
  dirname(
    fileURLToPath(import.meta.url)
  );

const projectRoot =
  resolve(
    currentDirectory,
    '..'
  );

const outputPath =
  resolve(
    projectRoot,
    'src',
    'app',
    'core',
    'config',
    'primeui-license.generated.ts'
  );

const licenseKey =
  process.env.PRIMEUI_LICENSE_KEY?.trim() ?? '';

if (!licenseKey) {
  console.warn(
    'PrimeUI license key bulunamadı. ' +
    'PRIMEUI_LICENSE_KEY ortam değişkenini tanımlayın.'
  );
}

const fileContent =
`// AUTO-GENERATED FILE.
// Do not edit or commit this file.

export const primeUiLicenseKey =
  ${JSON.stringify(licenseKey)};
`;

await mkdir(
  dirname(outputPath),
  {
    recursive: true
  }
);

await writeFile(
  outputPath,
  fileContent,
  'utf8'
);

console.log(
  'PrimeUI license configuration generated.'
);