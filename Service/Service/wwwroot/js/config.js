// 动态计算应用 Base Path，支持子路径部署（例如 /fetchvideo）
// 页面在 https://aa.bb.cc/fetchvideo 或 /fetchvideo/ 或 /fetchvideo/index.html 时，BASE 会是 "/fetchvideo"
export function getBasePath() {
    let path = window.location.pathname || '';
    // 去掉末尾的 index.html 和多余斜杠
    path = path.replace(/\/index\.html$/i, '').replace(/\/+$/, '');
    // 根路径返回空字符串
    if (!path || path === '/') return '';
    return path;
}

export const BASE = getBasePath();
