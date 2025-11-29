// js/main.js
import { initImageDownloader } from './image-downloader.js';
import { initVideoDownloader } from './video-downloader.js';
import { initScheduleManager } from './schedule.js';

document.addEventListener('DOMContentLoaded', () => {
    initImageDownloader();
    initVideoDownloader();
    initScheduleManager();
});