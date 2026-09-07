import '@testing-library/jest-dom/vitest';
import '@/i18n';

import { cleanup } from '@testing-library/react';
import { afterEach } from 'vitest';

afterEach(() => {
  cleanup();
});
