// js/main.js
import { initImageDownloader } from './image-downloader.js';
import { initVideoDownloader } from './video-downloader.js';

document.addEventListener('DOMContentLoaded', () => {
    initImageDownloader();
    initVideoDownloader();
});