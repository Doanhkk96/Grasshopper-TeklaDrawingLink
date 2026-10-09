# Chẩn đoán Build và Kiểm thử

Tài liệu này ghi lại quá trình chẩn đoán lỗi môi trường khi biên dịch project gốc, cấu hình build project mở rộng (custom assembly), và trạng thái kiểm thử hiện tại.

## 1. Môi trường Build và Nguyên nhân lỗi biên dịch Project gốc (MSB3823 / MSB3822)
- **Môi trường**: .NET SDK 10, MSBuild 18.4 (từ Visual Studio).
- **Lỗi gặp phải**: Khi chạy `dotnet build` thông thường, project gốc báo lỗi thiếu thuộc tính `GenerateResourceUsePreserializedResources` và package `System.Resources.Extensions`.
- **Nguyên nhân cốt lõi**: Từ .NET 8 trở lên, MSBuild đã thay đổi cách xử lý các file resource không chứa chuỗi (như `Resources.resx` chứa các file ảnh Base64/Icon). Khi biên dịch project `.NET Framework` (net48) sử dụng SDK mới, nó sẽ báo lỗi này nếu không được cấu hình `System.Resources.Extensions`. Việc đưa cấu hình "lách lỗi" vào file `.csproj` của upstream là sai nguyên tắc bảo toàn mã nguồn.

## 2. Phương án Build được khuyến nghị (Không sửa file core)
Để bảo toàn mã nguồn gốc, chúng ta sử dụng trực tiếp **MSBuild.exe** (hỗ trợ chuẩn .NET Framework). 

**Lệnh build dự án gốc (Core):**
- **Cho Tekla 2024:**
  ```powershell
  & "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" src\GrasshopperTeklaDrawingLink\GrasshopperTeklaDrawingLink.csproj -restore /p:Configuration=2024
  ```
- **Cho Tekla 2021:**
  ```powershell
  & "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" src\GrasshopperTeklaDrawingLink\GrasshopperTeklaDrawingLink.csproj -restore /p:Configuration=2021
  ```
*Lưu ý: Lệnh này biên dịch thành công file `.dll` vào thư mục `bin\2024\` hoặc `bin\2021\`. Bước `PostBuild` sẽ văng mã lỗi 1 do cố gắng copy file vào thư mục máy tính của tác giả gốc (`C:\Users\grzeg\...`). Có thể an tâm bỏ qua lỗi này vì `.dll` đã được tạo.*

**Lệnh build dự án Custom:**
Sử dụng cờ `/p:BuildProjectReferences=false` để MSBuild dùng trực tiếp `.dll` của core đã tạo, tránh bị dừng do lỗi PostBuild nói trên.
- **Cho Tekla 2024:**
  ```powershell
  & "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" custom\QD_GrasshopperTeklaDrawingLink\QD_GrasshopperTeklaDrawingLink.csproj -restore /p:Configuration=2024 /p:BuildProjectReferences=false
  ```
- **Cho Tekla 2021:**
  ```powershell
  & "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" custom\QD_GrasshopperTeklaDrawingLink\QD_GrasshopperTeklaDrawingLink.csproj -restore /p:Configuration=2021 /p:BuildProjectReferences=false
  ```

## 3. Kết quả Build và Đường dẫn Assembly
- **Project core**: Đã compile thành công DLL. 
- **Project custom**: 
  - File `.csproj` đã được cấu hình động (dùng `<Choose>`) để tự động tham chiếu đúng phiên bản Tekla SDK (`2021.0.*`, `2024.0.*`, v.v.) dựa theo `Configuration`.
  - **Output 2024**: `custom/QD_GrasshopperTeklaDrawingLink/bin/2024/QD_GrasshopperTeklaDrawingLink.gha`
  - **Output 2021**: `custom/QD_GrasshopperTeklaDrawingLink/bin/2021/QD_GrasshopperTeklaDrawingLink.gha`
  - Warning trùng lặp code (Duplicate type) đã biến mất nhờ việc xóa dứt điểm file custom cũ khỏi project lõi.

## 4. Kết quả kiểm thử (Runtime Testing)
- **Xác nhận từ người dùng**: Người dùng đã trực tiếp nạp file `QD_GrasshopperTeklaDrawingLink.gha` (bản build 2021) vào Grasshopper và chạy cùng Tekla Structures 2021.
- **Tình trạng hoạt động**: Tính năng `FilterBoltsByDirection` (với thay đổi dùng API `new Tekla.Structures.Model.Model().SelectModelObject()`) hoạt động thành công trên môi trường thực tế, không gặp sự cố COM threading hay phiên bản nào.

## 5. Các vấn đề còn chưa kiểm chứng
- Việc tự động cài đặt (Deployment): File `.gha` hiện cần copy thủ công vào `Libraries` của Grasshopper. Chưa thiết lập hệ thống triển khai tự động (PostBuild event riêng cho dự án custom) hướng đến máy tính nội bộ của SEICOGROUP.
- Sự ổn định trên các phiên bản Tekla khác (2022, 2023, 2025) chưa được kiểm chứng runtime, dù đã hỗ trợ compile thành công về mặt cấu hình.
