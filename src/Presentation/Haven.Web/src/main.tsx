import './index.css';

import React from 'react';
import ReactDOM from 'react-dom/client';

import { App } from './App.tsx';
import { serviceStatusHub } from './lib/signalr/hubs.ts';
async function bootstrap() {
  await Promise.all([serviceStatusHub.start()]);

  ReactDOM.createRoot(document.getElementById('root')!).render(
    <React.StrictMode>
      <App />
    </React.StrictMode>
  );
}

bootstrap();
