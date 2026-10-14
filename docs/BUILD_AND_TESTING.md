# Chẩn đoán Build và Kiểm thử

Tài liệu này ghi lại quá trình chẩn đoán lỗi môi trường khi biên dịch project gốc, cấu hình build project mở rộng (custom assembly), và trạng thái kiểm thử hiện tại.

## 1. Môi trường Build và Nguyên nhân lỗi biên dịch Project gốc (MSB3823 / MSB3822)
- **Môi trường**: .NET SDK 10, MSBuild 18.4 (từ Visual Studio).
- **Lỗi gặp phải**: Khi chạy `dotnet build` thông thường, project gốc báo lỗi thiếu thuộc tính `GenerateResourceUsePreserializedResources` và package `System.Resources.Extensions`.
- **Nguyên nhân cốt lõi**: Từ .NET 8 trở lên, MSBuild đã thay đổi cách xử lý các file resource không chứa chuỗi (như `Resources.resx` chứa các file ảnh Base64/Icon). Khi biên dịch project `.NET Framework` (net48) sử dụng SDK mới, nó sẽ báo lỗi này nếu không được cấu hình `System.Resources.Extensions`. Việc đưa cấu hình "lách lỗi" vào file `.csproj` của upstream là sai nguyên tắc bảo toàn mã nguồn.

## 2. Phương án Build được khuyến nghị (Không sửa file core)
Để bảo toàn mã nguồn gốc, chúng ta sử dụng trực tiếp **MSBuild.exe** (hỗ trợ chuẩn .NET Framework). 

### A. Lệnh build dự án gốc (Core Upstream):
- **Cho Tekla 2024:**
  ```powershell
  & "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" src\GrasshopperTeklaDrawingLink\GrasshopperTeklaDrawingLink.csproj -restore /p:Configuration=2024
  ```
- **Cho Tekla 2021:**
  ```powershell
  & "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" src\GrasshopperTeklaDrawingLink\GrasshopperTeklaDrawingLink.csproj -restore /p:Configuration=2021
  ```
*Lưu ý: Lệnh này biên dịch thành công file `.dll` vào thư mục `bin\2024\` hoặc `bin\2021\`. Bước `PostBuild` sẽ văng mã lỗi 1 do cố gắng copy file vào thư mục máy tính của tác giả gốc (`C:\Users\grzeg\...`). Có thể an tâm bỏ qua lỗi này vì `.dll` đã được tạo.*

### B. Lệnh build Dynamic Loader (`QD_TeklaLink.Loader`):
Dự án loader siêu nhẹ đặt tại `custom/QD_TeklaLink.Loader/`, đóng vai trò nhận diện Tekla runtime và nạp động component tương thích:
```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" custom\QD_TeklaLink.Loader\QD_TeklaLink.Loader.csproj -restore /p:Configuration=Release
```
*Cơ chế output:* Post-build event tự động copy file `QD_TeklaLink.Loader.gha` vào thư mục `%APPDATA%\Grasshopper\Libraries\`. Đây là file `.gha` duy nhất của dự án custom nằm trong `Libraries\`.

### C. Lệnh build dự án Custom Components (`QD_GrasshopperTeklaDrawingLink`):
Sử dụng cờ `/p:BuildProjectReferences=false` để MSBuild dùng trực tiếp `.dll` của core đã tạo, tránh bị dừng do lỗi PostBuild của dự án gốc.
- **Cho Tekla 2024:**
  ```powershell
  & "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" custom\QD_GrasshopperTeklaDrawingLink\QD_GrasshopperTeklaDrawingLink.csproj -restore /p:Configuration=2024 /p:BuildProjectReferences=false
  ```
- **Cho Tekla 2021:**
  ```powershell
  & "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" custom\QD_GrasshopperTeklaDrawingLink\QD_GrasshopperTeklaDrawingLink.csproj -restore /p:Configuration=2021 /p:BuildProjectReferences=false
  ```
*Cơ chế output:* Post-build event tự động copy file `QD_G-TDrawingLink_<version>.dll` vào thư mục `%APPDATA%\Grasshopper\QD_TeklaLink\`. Tuyệt đối không copy file `.dll` này vào thư mục `Libraries\` để tránh lỗi Duplicate GUID.

## 3. Kết quả Build và Đường dẫn Assembly
- **Project core**: Đã compile thành công DLL cho các phiên bản Tekla.
- **Project Dynamic Loader (`custom/QD_TeklaLink.Loader/`)**:
  - Output build: `custom/QD_TeklaLink.Loader/bin/Release/QD_TeklaLink.Loader.gha`
  - Vị trí triển khai: `%APPDATA%\Grasshopper\Libraries\QD_TeklaLink.Loader.gha` (Tự động copy qua Post-Build).
- **Project custom component (`custom/QD_GrasshopperTeklaDrawingLink/`)**:
  - Cấu hình động `<Choose>` tự động tham chiếu Tekla SDK tương ứng theo `Configuration` (2021, 2024...).
  - Quy ước tên file DLL: `QD_G-TDrawingLink_<tekla_version>.dll`
  - Output build 2021: `custom/QD_GrasshopperTeklaDrawingLink/bin/2021/QD_G-TDrawingLink_2021.dll`
  - Output build 2024: `custom/QD_GrasshopperTeklaDrawingLink/bin/2024/QD_G-TDrawingLink_2024.dll`
  - Vị trí triển khai: `%APPDATA%\Grasshopper\QD_TeklaLink\` (Tự động copy qua Post-Build).
  - Tình trạng: Biên dịch thành công 0 Error cho cả 2021 và 2024 (`FilterBoltsByDirectionComponent`, `FilterExtremePointsComponent`, `SortCurvesByMidpointComponent`, `FitViewScaleComponent`).

## 4. Kết quả kiểm thử (Runtime Testing)
- **Kiến trúc Dynamic Loader & Tự động Cân đối Kích thước Subcategory**:
  - Khi Grasshopper khởi động, nạp `QD_TeklaLink.Loader.gha` qua `GH_AssemblyPriority`.
  - Loader nhận diện Tekla runtime đang mở, gọi Reflection `GH_ComponentServer.LoadGHA` nạp đúng `QD_G-TDrawingLink_<version>.dll` từ `%APPDATA%\Grasshopper\QD_TeklaLink\` và cập nhật Ribbon.
  - Ngăn chặn hoàn toàn lỗi trùng lặp định danh (Duplicate GUID) khi trên máy người dùng có sẵn nhiều phiên bản Tekla.
  - Tích hợp module `QD_RibbonStyler` tự động đo đạc độ rộng tiêu đề SubCategory và mở rộng padding panel thẩm mỹ (tối thiểu ~78px hoặc lớn hơn theo text), loại bỏ hoàn toàn lỗi chữ bị cắt cụt (`Geo...`, `QD_...`) và căn giữa các icon cân đối trên thanh Ribbon.
- **Tình trạng hoạt động của component trên Tab "QD"**:
  - `FilterBoltsByDirectionComponent`: Hoạt động thành công trên môi trường thực tế với Tekla Structures.
  - `FilterExtremePointsComponent`: Đã hoàn thiện mã nguồn và kiểm chứng biên dịch thành công qua MSBuild; sẵn sàng hoạt động trên Grasshopper Canvas (Category: `QD`, SubCategory: `Geometry`).
  - `SortCurvesByMidpointComponent`: Khôi phục thành công từ nhánh `sort_curves_by_midpoint`, giữ nguyên GUID gốc và hoạt động trên Grasshopper Canvas (Category: `QD`, SubCategory: `Geometry`).
  - `FitViewScaleComponent`: Đã hoàn thiện và kiểm thử thực tế thành công trên môi trường Tekla Structures & Grasshopper (Category: `QD`, SubCategory: `Drawing`). Tự động điều chỉnh Scale theo bước chia hết cho 5 để tối ưu hóa kích thước View frame geometry.

## 5. Các vấn đề còn lưu ý
- **Triển khai tự động (Deployment)**: Các Post-build event của dự án custom đã tự động đồng bộ file vào đúng các thư mục `%APPDATA%\Grasshopper\Libraries\` và `%APPDATA%\Grasshopper\QD_TeklaLink\`. Người dùng chỉ cần build là môi trường Grasshopper đã sẵn sàng.
- **Hỗ trợ thêm phiên bản Tekla**: Khi cần hỗ trợ phiên bản Tekla mới (như 2022, 2023, 2025, 2026), chỉ cần khai báo thêm `PropertyGroup` với Condition tương ứng trong `QD_GrasshopperTeklaDrawingLink.csproj` và biên dịch với `/p:Configuration=<version>`.
