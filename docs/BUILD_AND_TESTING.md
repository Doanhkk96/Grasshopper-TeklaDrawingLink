# Chẩn đoán Build và Kiểm thử

Tài liệu này ghi lại quá trình chẩn đoán lỗi môi trường khi biên dịch project gốc và hướng dẫn build custom assembly độc lập.

## 1. Nguyên nhân lỗi biên dịch Project gốc (MSB3823 / MSB3822)
- **Lỗi gặp phải**: Khi chạy `dotnet build` với .NET SDK 10, project gốc báo lỗi thiếu thuộc tính `GenerateResourceUsePreserializedResources` và package `System.Resources.Extensions`.
- **Nguyên nhân cốt lõi**: Từ .NET 8 trở lên, MSBuild đã thay đổi cách xử lý các file resource không chứa chuỗi (như `Resources.resx` chứa các file ảnh Base64/Icon). Khi biên dịch một project `.NET Framework` (net48) sử dụng SDK mới, nó sẽ báo lỗi này nếu không được cấu hình `System.Resources.Extensions`.
- **Kết luận**: Đây không phải là lỗi code của thư viện, mà là do sự không tương thích giữa môi trường `dotnet CLI` phiên bản mới và cấu trúc `.resx` cũ. Việc bạn (hoặc IDE) tự động thêm thuộc tính này vào file `.csproj` gốc trước đây chính là "workaround" (cách lách lỗi) để build được trên máy của bạn. Tuy nhiên, việc đưa cấu hình lách lỗi cá nhân này vào mã nguồn upstream là sai nguyên tắc.

## 2. Phương án Build được khuyến nghị (Không sửa file core)
Thay vì dùng `dotnet build`, ta có thể sử dụng trực tiếp **MSBuild.exe** đi kèm với Visual Studio (hỗ trợ chuẩn .NET Framework). 

**Lệnh build dự án gốc:**
```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" src\GrasshopperTeklaDrawingLink\GrasshopperTeklaDrawingLink.csproj -restore /p:Configuration=2021
```
*Lưu ý: Lệnh này sẽ biên dịch thành công file `GrasshopperTeklaDrawingLink.2021.dll` vào thư mục `bin\2021\`. Ở bước cuối `PostBuild` sẽ văng thông báo lỗi đỏ (Mã lỗi 1) vì nó cố gắng copy file vào thư mục `C:\Users\grzeg\...` của tác giả gốc. Bạn có thể bỏ qua lỗi đỏ ở cuối này vì file `.dll` đã được tạo ra thành công.*

**Lệnh build dự án Custom:**
```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" custom\QD_GrasshopperTeklaDrawingLink\QD_GrasshopperTeklaDrawingLink.csproj -restore /p:Configuration=2021 /p:BuildProjectReferences=false
```
*Lưu ý: Flag `/p:BuildProjectReferences=false` giúp MSBuild không tự động build lại project core (tránh bị dừng do lỗi PostBuild của tác giả), mà chỉ dùng thẳng `.dll` của core đã tạo.*

## 3. Kết quả Build
- **Project core**: Thành công biên dịch mã nguồn C# và tạo ra DLL (bị lỗi ở tác vụ copy file cuối cùng như đã giải thích).
- **Project custom**: Build thành công 100% ra file `custom\QD_GrasshopperTeklaDrawingLink\bin\2024\QD_GrasshopperTeklaDrawingLink.dll`, và tự động copy thành file `.gha` kế bên. Cảnh báo trùng lặp code (Duplicate type) cũng đã biến mất hoàn toàn do bản custom cũ trong thư mục lõi đã được dọn dẹp.

## 4. Những điều chưa được kiểm tra (Runtime)
- **Chưa thử nghiệm Grasshopper**: Chúng ta chưa nạp file `QD_GrasshopperTeklaDrawingLink.gha` vào Grasshopper cùng với `GrasshopperTeklaLink.Loader.gha` để kiểm tra Assembly Resolver có nhận đúng phiên bản `.dll` của core không.
- **Rủi ro còn lại**: Tuy API Tekla `new Tekla.Structures.Model.Model().SelectModelObject()` là tương đương với hàm internal cũ, nhưng vẫn cần test thực tế để chắc chắn nó không gặp vấn đề về COM threading khi chạy từ Grasshopper component.

## 5. Hướng dẫn tiếp theo (Runtime Testing)
1. Tắt hoàn toàn Grasshopper và Rhino.
2. Sao chép file `QD_GrasshopperTeklaDrawingLink.gha` từ thư mục `custom/QD_GrasshopperTeklaDrawingLink/bin/2024/` vào thư mục `%APPDATA%\Grasshopper\Libraries\`.
3. Khởi động lại Rhino, mở Grasshopper và Tekla Structures.
4. Kéo component `FilterBoltsByDirection` ra Canvas và cung cấp tham số để kiểm tra nó có phân loại bu lông bình thường không.
