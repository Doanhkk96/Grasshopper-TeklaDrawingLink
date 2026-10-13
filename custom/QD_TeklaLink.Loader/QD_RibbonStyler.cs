using System;
using System.Collections;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Grasshopper.Kernel;

namespace QD_TeklaLink.Loader
{
    /// <summary>
    /// Tự động định dạng và mở rộng kích thước các Ribbon Panel (SubCategories) trong Tab "QD".
    /// Giúp tiêu đề không bao giờ bị cắt ngắn ("Geo...", "QD_...") và các icon được bố trí hài hòa.
    /// </summary>
    public static class QD_RibbonStyler
    {
        private static bool _isHooked;
        private static bool _isFormatting;
        private static Timer _timer;
        private static int _timerTicks;

        public static void HookRibbon()
        {
            if (_isHooked)
                return;

            _isHooked = true;

            try
            {
                // 1. Hook RhinoApp.Idle (chạy liên tục trên UI thread khi Rhino idle)
                Rhino.RhinoApp.Idle += OnRhinoIdle;

                // 2. Hook Grasshopper CanvasCreated
                Grasshopper.Instances.CanvasCreated += OnCanvasCreated;

                // 3. Khởi tạo Timer chạy kiểm tra liên tục khi khởi động
                StartPollingTimer();

                QD_LoaderEngine.Log("[QD RibbonStyler] Hooked Ribbon styler listeners (RhinoApp.Idle, CanvasCreated, Timer).");
            }
            catch (Exception ex)
            {
                QD_LoaderEngine.Log($"[QD RibbonStyler] HookRibbon error: {ex.Message}");
            }
        }

        private static void StartPollingTimer()
        {
            try
            {
                if (_timer != null)
                    return;

                _timer = new Timer();
                _timer.Interval = 300;
                _timer.Tick += OnTimerTick;
                _timer.Start();
            }
            catch (Exception ex)
            {
                QD_LoaderEngine.Log($"[QD RibbonStyler] StartPollingTimer error: {ex.Message}");
            }
        }

        private static void OnRhinoIdle(object sender, EventArgs e)
        {
            try
            {
                FormatQdRibbonPanels();
            }
            catch { }
        }

        private static void OnTimerTick(object sender, EventArgs e)
        {
            try
            {
                _timerTicks++;
                FormatQdRibbonPanels();

                // Sau 100 lần tick (~30s), nếu đã format xong hoặc quá lâu, giảm tần suất tick xuống 2s
                if (_timerTicks > 100 && _timer != null)
                {
                    _timer.Interval = 2000;
                }
            }
            catch { }
        }

        private static void OnCanvasCreated(Grasshopper.GUI.Canvas.GH_Canvas canvas)
        {
            try
            {
                AttachToDocumentEditor();
                FormatQdRibbonPanels();
            }
            catch (Exception ex)
            {
                QD_LoaderEngine.Log($"[QD RibbonStyler] OnCanvasCreated error: {ex.Message}");
            }
        }

        private static void AttachToDocumentEditor()
        {
            try
            {
                var docEditor = Grasshopper.Instances.DocumentEditor;
                if (docEditor == null)
                    return;

                var ribbonProp = docEditor.GetType().GetProperty("Ribbon", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                var ribbon = ribbonProp?.GetValue(docEditor, null) as Control;
                if (ribbon != null)
                {
                    ribbon.MouseClick -= OnRibbonClick;
                    ribbon.MouseClick += OnRibbonClick;
                }
            }
            catch { }
        }

        private static void OnRibbonClick(object sender, MouseEventArgs e)
        {
            try
            {
                FormatQdRibbonPanels();
            }
            catch { }
        }

        public static void FormatQdRibbonPanels()
        {
            if (_isFormatting)
                return;

            try
            {
                var docEditor = Grasshopper.Instances.DocumentEditor;
                if (docEditor == null)
                    return;

                if (docEditor.InvokeRequired)
                {
                    docEditor.BeginInvoke(new Action(FormatQdRibbonPanels));
                    return;
                }

                _isFormatting = true;

                // Gắn mouse listener lên ribbon nếu chưa gắn
                AttachToDocumentEditor();

                var ribbonProp = docEditor.GetType().GetProperty("Ribbon", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                var ribbon = ribbonProp?.GetValue(docEditor, null);
                if (ribbon == null)
                    return;

                var tabsProp = ribbon.GetType().GetProperty("Tabs", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                var tabs = tabsProp?.GetValue(ribbon, null) as IEnumerable;
                if (tabs == null)
                    return;

                object qdTab = null;
                foreach (var tab in tabs)
                {
                    var nameProp = tab.GetType().GetProperty("NameFull", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                                   ?? tab.GetType().GetProperty("Name", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                    var name = nameProp?.GetValue(tab, null) as string;
                    if (string.Equals(name?.Trim(), "QD", StringComparison.OrdinalIgnoreCase))
                    {
                        qdTab = tab;
                        break;
                    }
                }

                if (qdTab == null)
                    return;

                var panelsProp = qdTab.GetType().GetProperty("Panels", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                var panels = panelsProp?.GetValue(qdTab, null) as IEnumerable;
                if (panels == null)
                    return;

                bool modified = false;

                foreach (var panel in panels)
                {
                    var panelNameProp = panel.GetType().GetProperty("Name", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    var panelName = panelNameProp?.GetValue(panel, null) as string;
                    if (string.IsNullOrEmpty(panelName))
                        continue;

                    string displayName = panelName.Trim();

                    // 1. Đo chiều rộng chữ theo font thực tế của Ribbon
                    int textWidth = 0;
                    try
                    {
                        var fontMethod = panel.GetType().GetMethod("RibbonFont", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        using (var font = fontMethod?.Invoke(panel, null) as Font)
                        {
                            if (font != null)
                                textWidth = TextRenderer.MeasureText(displayName, font).Width;
                        }
                    }
                    catch { }

                    if (textWidth <= 0)
                    {
                        using (var fallbackFont = new Font("Segoe UI", 9f))
                        {
                            textWidth = TextRenderer.MeasureText(displayName, fallbackFont).Width;
                        }
                    }

                    // Grasshopper kiểm tra textWidth < barRegion.Width - UiAdjust(15) trong RenderPanelBorder.
                    // Cần cộng thêm 22px để chữ hiển thị trọn vẹn, không bao giờ bị cắt thành dấu "..."
                    int minWidthForText = textWidth + 22;
                    const int standardMinWidth = 84; // Chiều rộng tiêu chuẩn đẹp mắt (~3 cột icon)
                    int targetWidth = Math.Max(minWidthForText, standardMinWidth);

                    // 2. Xác định số cột icon hiện tại để tính chiều rộng gốc của các icon
                    var visibleItemsProp = panel.GetType().GetProperty("VisibleItems", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                                           ?? panel.GetType().GetProperty("AllItems", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    var itemsList = visibleItemsProp?.GetValue(panel, null) as IList;
                    int itemCount = itemsList != null ? itemsList.Count : 1;
                    int numColumns = (int)Math.Ceiling(itemCount / 3.0);
                    if (numColumns < 1) numColumns = 1;
                    int itemBaseWidth = numColumns * 28;

                    int finalWidth = Math.Max(targetWidth, itemBaseWidth + 8);

                    var paddingProp = panel.GetType().GetProperty("Padding", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    var currentPadding = (Padding)paddingProp.GetValue(panel, null);

                    int extra = finalWidth - itemBaseWidth;
                    if (extra < 0) extra = 0;

                    int padLeft = 2 + (extra / 2);
                    int padRight = 2 + (extra - (extra / 2));

                    // Nếu Padding chưa được mở rộng (hoặc bị reset), thực hiện cập nhật
                    if (currentPadding.Left != padLeft || currentPadding.Right != padRight)
                    {
                        paddingProp.SetValue(panel, new Padding(padLeft, currentPadding.Top, padRight, currentPadding.Bottom), null);
                        modified = true;
                        QD_LoaderEngine.Log($"[QD RibbonStyler] Resized panel '{displayName}': {itemCount} item(s), itemBaseWidth={itemBaseWidth}px, textWidth={textWidth}px -> targetWidth={finalWidth}px (padLeft={padLeft}, padRight={padRight}).");
                    }
                }

                if (modified)
                {
                    // Gọi PerformLayout trên Tab "QD" để áp dụng ngay Padding và xếp lại thứ tự panel
                    var performLayoutMethod = qdTab.GetType().GetMethod("PerformLayout", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    performLayoutMethod?.Invoke(qdTab, null);

                    var control = ribbon as Control;
                    control?.Invalidate();
                    QD_LoaderEngine.Log("[QD RibbonStyler] Successfully resized Tab QD panels and refreshed Ribbon layout.");
                }
            }
            catch (Exception ex)
            {
                QD_LoaderEngine.Log($"[QD RibbonStyler] FormatQdRibbonPanels error: {ex.Message}");
            }
            finally
            {
                _isFormatting = false;
            }
        }
    }
}
