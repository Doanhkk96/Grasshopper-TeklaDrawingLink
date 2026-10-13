namespace GTDrawingLink.Tools
{
    /// <summary>
    /// Định nghĩa các tên SubCategory (Panel Headings) chuẩn cho Custom Components trong Category "QD".
    /// Giúp thống nhất tên gọi các nhóm chức năng trên Grasshopper Canvas và đảm bảo RibbonStyler
    /// tính toán mở rộng kích thước hiển thị hài hòa, thẩm mỹ.
    /// </summary>
    public static class QD_PanelHeadings
    {
        /// <summary>
        /// Nhóm hình học: điểm, đường cong, mặt, trích xuất & xử lý tọa độ.
        /// </summary>
        public const string Geometry = "Geometry";

        /// <summary>
        /// Nhóm đối tượng mô hình Tekla (Part, Bolt, Rebar, Assembly...).
        /// </summary>
        public const string Model = "Model";

        /// <summary>
        /// Nhóm bản vẽ: Drawing, Sheet, View, Layout.
        /// </summary>
        public const string Drawing = "Drawing";

        /// <summary>
        /// Nhóm kích thước: Linear Dimension, Curved Dimension, Dimension Line.
        /// </summary>
        public const string Dimensions = "Dimensions";

        /// <summary>
        /// Nhóm chú thích: Text, Mark, Leader, Symbol.
        /// </summary>
        public const string Annotations = "Annotations";

        /// <summary>
        /// Nhóm công cụ tiện ích tổng hợp.
        /// </summary>
        public const string Tools = "Tools";

        /// <summary>
        /// Tên SubCategory kế thừa cho các component custom tổng hợp ban đầu.
        /// </summary>
        public const string QD_Components = "QD_Components";
    }
}
