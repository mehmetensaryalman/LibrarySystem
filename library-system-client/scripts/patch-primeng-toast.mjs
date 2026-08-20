import {
  readFile,
  writeFile
} from 'node:fs/promises';

import {
  fileURLToPath
} from 'node:url';

import {
  dirname,
  join
} from 'node:path';

const currentFile =
  fileURLToPath(
    import.meta.url
  );

const currentDirectory =
  dirname(currentFile);

const projectRoot =
  join(
    currentDirectory,
    '..'
  );

const toastFile =
  join(
    projectRoot,
    'node_modules',
    'primeng',
    'fesm2022',
    'primeng-toast.mjs'
  );

const originalRight =
  "right: (position === 'top-right' || position === 'bottom-right') && '20px',";

const patchedRight =
  "right: position === 'top-right' || position === 'bottom-right' ? '20px' : null,";

const originalBottom =
  "bottom: (position === 'bottom-left' || position === 'bottom-right' || position === 'bottom-center') && '20px',";

const patchedBottom =
  "bottom: position === 'bottom-left' || position === 'bottom-right' || position === 'bottom-center' ? '20px' : null,";

let content =
  await readFile(
    toastFile,
    'utf8'
  );

let changed = false;

if (
  content.includes(
    originalRight
  )
) {
  content =
    content.replace(
      originalRight,
      patchedRight
    );

  changed = true;
}

if (
  content.includes(
    originalBottom
  )
) {
  content =
    content.replace(
      originalBottom,
      patchedBottom
    );

  changed = true;
}

const rightAlreadyPatched =
  content.includes(
    patchedRight
  );

const bottomAlreadyPatched =
  content.includes(
    patchedBottom
  );

if (
  !rightAlreadyPatched ||
  !bottomAlreadyPatched
) {
  throw new Error(
    'PrimeNG Toast patch uygulanamadı. ' +
    'primeng-toast.mjs içeriği beklenen sürümle eşleşmiyor.'
  );
}

if (changed) {
  await writeFile(
    toastFile,
    content,
    'utf8'
  );

  console.log(
    'PrimeNG Toast Angular NG0318 patch uygulandı.'
  );
} else {
  console.log(
    'PrimeNG Toast patch zaten uygulanmış.'
  );
}