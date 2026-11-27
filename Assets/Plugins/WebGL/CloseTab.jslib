mergeInto(LibraryManager.library, {
  // 供 C# 调用：[DllImport("__Internal")] static extern void CloseTab();
  // 浏览器只允许"由脚本打开的窗口"被 window.close() 关闭，
  // 用户手动输入的地址（比如从简历点开链接）通常关不掉，这时给个视觉提示。
  CloseTab: function () {
    try { window.close(); } catch (e) { }
    setTimeout(function () {
      if (!document.hidden) {
        var tip = document.createElement('div');
        tip.textContent = '浏览器不允许直接关闭此标签页，请按 Ctrl+W 手动关闭';
        tip.style.cssText =
          'position:fixed;left:0;right:0;top:0;padding:12px;text-align:center;' +
          'background:rgba(20,24,32,.92);color:#e6e9ef;font:14px/1.6 system-ui,sans-serif;z-index:99999';
        document.body.appendChild(tip);
        setTimeout(function () { tip.remove(); }, 3000);
      }
    }, 120);
  }
});
