# Custom Components (QD)

Tài liệu này ghi nhận các Grasshopper component được tùy biến và phát triển độc lập trong project `custom/QD_GrasshopperTeklaDrawingLink/`.

---

## 1. Quy chuẩn phát triển Custom Component

Để đồng bộ trải nghiệm người dùng trên Grasshopper Canvas và không lẫn lộn với các component của upstream:
- **Category:** Bắt buộc sử dụng `"QD"`.
- **SubCategory:** Đặt theo miền nghiệp vụ tương ứng:
  - `"Geometry"`: Các công cụ tính toán, phân loại, trích xuất hình học điểm/đường/mặt.
  - `"Model"`: Các công cụ tương tác đối tượng Tekla Structures Model (Part, Bolt, Rebar...).
  - `"Drawing"` / `"Dimensions"`: Các công cụ phục vụ tự động hóa bản vẽ.
  - `"Annotations"`: Các công cụ chú thích, text, mark, ghi chú.
  - `"Tools"` / `"QD_Components"`: Các tiện ích xử lý đa năng/tổng hợp.
- **ComponentGuid:** Mỗi component phải có một GUID cố định, sinh một lần duy nhất và ghi nhận vào tài liệu này. Tuyệt đối không thay đổi GUID khi đã đưa vào definition của người dùng.

### Kiến trúc Phân phối Đa phiên bản Động (Dynamic Loader Architecture)
Để hỗ trợ song song nhiều phiên bản Tekla Structures (2021, 2024...) mà không gặp lỗi Duplicate GUID của Grasshopper:
- **Thư viện Component (.dll):** Dự án `custom/QD_GrasshopperTeklaDrawingLink/` được biên dịch thành thư viện động dạng `.dll` theo từng phiên bản Tekla:
  - Cấu trúc tên: `QD_G-TDrawingLink_<tekla_version>.dll` (ví dụ: `QD_G-TDrawingLink_2021.dll`, `QD_G-TDrawingLink_2024.dll`).
  - Thư mục lưu trữ: Tự động copy vào `%APPDATA%\Grasshopper\QD_TeklaLink\`.
  - Tuyệt đối **không** đặt các file `.dll` này trực tiếp trong `%APPDATA%\Grasshopper\Libraries\` để tránh việc Grasshopper tự động nạp đồng thời gây trùng lặp GUID.
- **Dynamic Loader (.gha):** Dự án siêu nhẹ `custom/QD_TeklaLink.Loader/` được biên dịch thành file `.gha` duy nhất:
  - Cấu trúc tên: `QD_TeklaLink.Loader.gha`.
  - Vị trí: Đặt trực tiếp trong `%APPDATA%\Grasshopper\Libraries\`.
  - Nguyên lý hoạt động: Kế thừa `GH_AssemblyPriority`, tự động dò tìm phiên bản Tekla Structures đang kết nối (thông qua AppDomain, process hoặc sự kiện kết nối) và gọi nạp động đúng file `QD_G-TDrawingLink_<version>.dll` tương ứng qua Reflection `GH_ComponentServer.LoadGHA`, sau đó cập nhật lại Ribbon của Grasshopper.
- **Ưu điểm:** Giải quyết triệt để lỗi xung đột Duplicate GUID khi người dùng cài đặt nhiều phiên bản Tekla trên cùng một máy, người dùng không cần đổi tên hay copy đè thủ công mỗi khi chuyển đổi phiên bản.

### Cơ chế Tự động Cân đối Kích thước Subcategory (Harmonious Ribbon Layout)
- **Vấn đề mặc định của Grasshopper:** Khi một SubCategory chỉ có 1 hoặc 2 component (1 cột icon ~ 28px), Grasshopper sẽ vẽ thanh tiêu đề của nhóm rất hẹp. Hậu quả là tên nhóm dài hơn 28px sẽ bị cắt cụt bằng dấu ba chấm (`Geo...`, `QD_...`), gây mất thẩm mỹ và khó nhận diện chức năng.
- **Giải pháp tự động (`QD_RibbonStyler`):**
  - Được triển khai bên trong `custom/QD_TeklaLink.Loader/QD_RibbonStyler.cs`.
  - Tự động bắt sự kiện Ribbon của Grasshopper (`CanvasCreated`, `Shown`, `Layout`).
  - Tìm Tab `"QD"` và duyệt qua tất cả các panel (SubCategory) thuộc tab này.
  - Sử dụng font chữ thực tế của Ribbon để đo đạc chính xác chiều dài chuỗi tên (`panel.Name`).
  - Thiết lập chiều rộng mục tiêu: `targetWidth = Math.Max(textSize.Width + 18, 78)`. Trong đó 78px là chiều rộng tối thiểu thẩm mỹ (tương đương khoảng 3 cột icon), và `+ 18px` đảm bảo chữ luôn có khoảng đệm an toàn, không bao giờ bị cắt cụt.
  - Tự động tính toán và điều chỉnh `panel.Padding` đối xứng (`padLeft` và `padRight`), giúp:
    1. Tiêu đề hiển thị trọn vẹn 100%, căn giữa đều đặn dưới đáy panel.
    2. Các icon được căn giữa cân đối bên trong khung panel, không bị dạt về một phía.
    3. Khoảng cách giữa các SubCategory liền kề tự động dời sang phải một cách chuẩn xác, không bị đè lên nhau.
- **Quy chuẩn cho các Component / SubCategory mới:**
  - Định nghĩa tên nhóm trong file chuẩn: `custom/QD_GrasshopperTeklaDrawingLink/Tools/QD_PanelHeadings.cs`.
  - Khi thêm component mới hoặc tạo thêm SubCategory mới: chỉ cần gán `Category = "QD"` và `SubCategory = QD_PanelHeadings.<TênNhóm>`. Cơ chế `QD_RibbonStyler` sẽ tự động nhận diện và tính toán kích thước mở rộng tương ứng trên thanh Ribbon, nhà phát triển hoàn toàn không cần can thiệp giao diện thủ công.

---

## 2. Danh sách Component

### A. `FilterExtremePointsComponent`
- **Tên hiển thị Canvas:** `Filter Extreme Points` (NickName: `FilterExtPts`)
- **Category / SubCategory:** `QD` / `Geometry`
- **Class / File:** `FilterExtremePointsComponent.cs` (Kế thừa `TeklaComponentBase`)
- **ComponentGuid:** `7f4f603c-8eb3-4791-8848-18e38adcdb98`
- **Mục đích:** Lọc các điểm cực biên (Min/Max) theo từng đường gióng tọa độ X hoặc Y dựa trên dung sai gom cụm (Tolerance). Thường dùng để lấy các điểm ngoài cùng của dầm, cột, bản mã phục vụ đặt kích thước tự động.
- **Inputs:**
  1. `Points` (`P`, List of Point3d): Danh sách điểm đầu vào.
  2. `Keep Max Y` (`MaxY`, bool, mặc định: `true`): Lọc theo X, giữ điểm có Y lớn nhất (`true`) hoặc nhỏ nhất (`false`).
  3. `Keep Max X` (`MaxX`, bool, mặc định: `true`): Lọc theo Y, giữ điểm có X lớn nhất (`true`) hoặc nhỏ nhất (`false`).
  4. `Tolerance` (`T`, double, mặc định: `1.0` mm): Dung sai gom cụm tọa độ để tránh sai số số thực.
- **Outputs:**
  1. `Points by X` (`PtsX`, List of Point3d): Danh sách điểm lọc theo X, sắp xếp tăng dần theo X.
  2. `Points by Y` (`PtsY`, List of Point3d): Danh sách điểm lọc theo Y, sắp xếp tăng dần theo Y.

---

### B. `FilterBoltsByDirectionComponent`
- **Tên hiển thị Canvas:** `Filter Bolts by Direction` (NickName: `FilterBolts`)
- **Category / SubCategory:** `QD` / `QD_Components`
- **Class / File:** `FilterBoltsByDirectionComponent.cs` (Kế thừa `TeklaComponentBaseNew<FilterBoltsByDirectionCommand>`)
- **ComponentGuid:** `6d8fd5c4-1572-4983-be71-1d50b334f6d4`
- **Mục đích:** Phân loại vị trí các bu lông theo hướng trục X, Y, Z hoặc xiên (Skew) dựa trên hệ tọa độ tham chiếu.

---

### C. `SortCurvesByMidpointComponent`
- **Tên hiển thị Canvas:** `Sort Curves by Midpoint` (NickName: `SortCurvesMid`)
- **Category / SubCategory:** `QD` / `Geometry`
- **Class / File:** `SortCurvesByMidpointComponent.cs` (Kế thừa `TeklaComponentBaseNew<SortCurvesByMidpointCommand>`)
- **ComponentGuid:** `e224e70e-e30d-45db-b27b-204b73b64c23`
- **Mục đích:** Sắp xếp danh sách đường cong (Curves) dựa trên tọa độ X và Y của điểm chính giữa (Midpoint) của chúng.
- **Inputs:**
  1. `Curves` (`C`, List of Curve): Danh sách các đường cong đầu vào cần sắp xếp.
- **Outputs:**
  1. `Sorted X` (`X`, List of Curve): Danh sách đường cong sắp xếp tăng dần theo tọa độ X của trung điểm.
  2. `Sorted Y` (`Y`, List of Curve): Danh sách đường cong sắp xếp tăng dần theo tọa độ Y của trung điểm.

---

## 4. Quy chuẩn phân nhánh Git khi phát triển Component mới

Để bảo vệ an toàn cho nhánh chính và lịch sử Git rõ ràng:
1. **Nhánh gốc tích hợp:** `QDcustom` là nhánh chính thức duy nhất chứa toàn bộ custom code đã hoàn thiện.
2. **Quy trình khi thêm Component mới:**
   - Tạo branch mới tách từ `QDcustom`: `git checkout QDcustom && git checkout -b feature/<ten-component>` (hoặc tạo git worktree riêng).
   - Phát triển component mới, cấu hình Category `QD`, chọn SubCategory theo `QD_PanelHeadings`.
   - Biên dịch và kiểm thử thực tế trên Grasshopper & Tekla.
   - Khi hoàn thành và kiểm thử đạt yêu cầu: Merge branch tính năng vào `QDcustom`, xóa branch tính năng tạm.
3. **Không phát triển trực tiếp trên `main` hoặc `QDcustom`** mà chưa qua nhánh tính năng.

