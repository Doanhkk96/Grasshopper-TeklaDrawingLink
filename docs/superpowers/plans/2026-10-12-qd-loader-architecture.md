# QD TeklaLink Loader & Multi-Version Architecture Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Xây dựng cơ chế nạp động đa phiên bản độc lập (Multi-Version Dynamic Loader) thông qua `QD_TeklaLink.Loader.gha`, cho phép Grasshopper tự động nạp đúng file `QD_G-TDrawingLink.<tekla_version>.dll` tương ứng với phiên bản Tekla Structures đang chạy, loại bỏ hoàn toàn lỗi xung đột Duplicate GUID khi cài đặt song song nhiều phiên bản.

**Architecture:**
- Các file component được đóng gói dưới dạng thư viện liên kết `.dll` (ví dụ: `QD_G-TDrawingLink.2021.dll`, `QD_G-TDrawingLink.2024.dll`) và lưu trữ trong thư mục riêng biệt `%APPDATA%\Grasshopper\QD_TeklaLink\`. Vị trí này ngăn chặn Grasshopper tự động quét nạp thô bạo lúc khởi động và tránh bị Loader của Trimble quét nhầm.
- Xây dựng một loader chuyên dụng siêu nhẹ `QD_TeklaLink.Loader.gha` kế thừa `GH_AssemblyPriority` của Grasshopper.
- Khi Grasshopper khởi động, `QD_TeklaLink.Loader.gha` xác định phiên bản Tekla đang kết nối (thông qua `Tekla.Structures` assembly trong AppDomain, tiến trình `TeklaStructures`, hoặc sự kiện `AppDomain.AssemblyLoad`), sau đó gọi phương thức nội bộ `GH_ComponentServer.LoadGHA` để nạp duy nhất file `.dll` tương thích.

**Tech Stack:** C# .NET Framework 4.8, Grasshopper SDK (v6.0.18016.23451), Reflection, MSBuild (Visual Studio Community 18).

**Spec:** [Tài liệu kiến trúc phân tích cơ chế Loader trong cuộc hội thoại ngày 2026-10-12]

---

## Global Constraints

- Tuyệt đối không can thiệp, sửa đổi mã nguồn hoặc file `.csproj` trong `src/GrasshopperTeklaDrawingLink/`.
- Mọi thành phần mới phải nằm hoàn toàn trong thư mục `custom/`.
- File `.gha` duy nhất đặt trong `%APPDATA%\Grasshopper\Libraries\` là `QD_TeklaLink.Loader.gha`.
- Các file assembly của từng phiên bản Tekla có đuôi `.dll` và được lưu tại `%APPDATA%\Grasshopper\QD_TeklaLink\QD_G-TDrawingLink.<tekla_version>.dll`.
- Tương thích song song và độc lập với `GrasshopperTeklaLink.Loader.gha` của Trimble.

---

## Review Focus

1. **Khởi động Grasshopper khi chưa bật Tekla:** Loader không được văng Exception làm dừng Grasshopper; phải có cơ chế chờ (AssemblyLoad hook) hoặc fallback an toàn.
2. **Khởi động cùng Tekla 2021:** Chỉ nạp duy nhất `QD_G-TDrawingLink.2021.dll`, không nạp bản 2024.
3. **Khởi động cùng Tekla 2024:** Chỉ nạp duy nhất `QD_G-TDrawingLink.2024.dll`, không nạp bản 2021.
4. **Xung đột tên file & thư mục:** Tránh lưu các file `.dll` trực tiếp tại gốc `Libraries` vì Loader của Trimble sẽ quét và nạp nhầm.
5. **Cơ chế nạp LoadGHA:** Sử dụng Reflection gọi an toàn phương thức `LoadGHA(GH_ExternalFile, bool)` của Grasshopper Engine.

---

## Task Breakdown

### Task 1: Cấu hình Output DLL cho `QD_GrasshopperTeklaDrawingLink.csproj`

**Files:**
- Modify: `custom/QD_GrasshopperTeklaDrawingLink/QD_GrasshopperTeklaDrawingLink.csproj`

**Interfaces:**
- Consumes: Cấu hình MSBuild `$(Configuration)` (2021, 2024...)
- Produces: File `QD_G-TDrawingLink.$(Configuration).dll` tại `custom/QD_GrasshopperTeklaDrawingLink/bin/$(Configuration)/` và tự động copy sang `%APPDATA%\Grasshopper\QD_TeklaLink\QD_G-TDrawingLink.$(Configuration).dll`.

- [ ] **Step 1:** Cập nhật mục `PostBuild` trong `QD_GrasshopperTeklaDrawingLink.csproj`:
  - Sao chép file DLL đầu ra thành `$(TargetDir)QD_G-TDrawingLink_$(Configuration).dll`.
  - Tạo thư mục `%APPDATA%\Grasshopper\QD_TeklaLink` nếu chưa tồn tại.
  - Sao chép `QD_G-TDrawingLink_$(Configuration).dll` vào `%APPDATA%\Grasshopper\QD_TeklaLink\`.
  - Xóa bỏ câu lệnh sao chép file `.gha` cũ vào `%APPDATA%\Grasshopper\Libraries\` để tránh xung đột.
- [ ] **Step 2:** Chạy lệnh build kiểm thử cho cấu hình 2021 và 2024.
  Run: `& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" custom\QD_GrasshopperTeklaDrawingLink\QD_GrasshopperTeklaDrawingLink.csproj -restore /p:Configuration=2021 /p:BuildProjectReferences=false`
  Expected: Build thành công (0 Error), sinh ra file `QD_G-TDrawingLink_2021.dll` trong `%APPDATA%\Grasshopper\QD_TeklaLink\`.

---

### Task 2: Xây dựng Project `QD_TeklaLink.Loader`

**Files:**
- Create: `custom/QD_TeklaLink.Loader/QD_TeklaLink.Loader.csproj`
- Create: `custom/QD_TeklaLink.Loader/QD_LoaderPriority.cs`
- Create: `custom/QD_TeklaLink.Loader/QD_LoaderEngine.cs`

**Interfaces:**
- Consumes: `Grasshopper.Kernel.GH_AssemblyPriority`, `GH_ComponentServer`
- Produces: File assembly `QD_TeklaLink.Loader.gha` tại `%APPDATA%\Grasshopper\Libraries\`.

- [ ] **Step 1:** Tạo file project `custom/QD_TeklaLink.Loader/QD_TeklaLink.Loader.csproj`:
  - Target Framework `net48`.
  - Tham chiếu package `Grasshopper` (v6.0.18016.23451).
  - PostBuild: Copy DLL thành `QD_TeklaLink.Loader.gha` và chuyển vào `%APPDATA%\Grasshopper\Libraries\`.
- [ ] **Step 2:** Tạo file `custom/QD_TeklaLink.Loader/QD_LoaderPriority.cs`:
  - Kế thừa `GH_AssemblyPriority`.
  - Override `PriorityLoad()`: Gọi `QD_LoaderEngine.Initialize()` và trả về `GH_LoadingInstruction.Proceed`.
- [ ] **Step 3:** Tạo file `custom/QD_TeklaLink.Loader/QD_LoaderEngine.cs`:
  - Cài đặt phương thức `Initialize()`:
    1. Kiểm tra phiên bản Tekla đang hoạt động trong `AppDomain` (tìm `Tekla.Structures` hoặc `GrasshopperTeklaDrawingLink.*`).
    2. Nếu chưa tìm thấy, kiểm tra danh sách tiến trình đang chạy (`TeklaStructures`).
    3. Nếu xác định được phiên bản (vd: `"2021"` hoặc `"2024"`): Gọi hàm `LoadTargetAssembly(version)`.
    4. Nếu chưa xác định được: Đăng ký sự kiện `AppDomain.CurrentDomain.AssemblyLoad` để tự động nạp ngay khi Trimble Loader nạp Tekla vào domain.
  - Cài đặt phương thức `LoadTargetAssembly(string version)`:
    - Tìm file `%APPDATA%\Grasshopper\QD_TeklaLink\QD_G-TDrawingLink.<version>.dll`.
    - Dùng Reflection gọi `GH_ComponentServer.LoadGHA` để nạp file DLL vào Grasshopper Canvas.

---

### Task 3: Biên dịch và Xác minh `QD_TeklaLink.Loader`

**Files:**
- Verify build: `custom/QD_TeklaLink.Loader/bin/Release/QD_TeklaLink.Loader.gha`
- Verify deployment: `%APPDATA%\Grasshopper\Libraries\QD_TeklaLink.Loader.gha`

- [ ] **Step 1:** Biên dịch project `QD_TeklaLink.Loader.csproj`.
  Run: `& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" custom\QD_TeklaLink.Loader\QD_TeklaLink.Loader.csproj -restore /p:Configuration=Release`
  Expected: Build thành công (0 Error), file `QD_TeklaLink.Loader.gha` được sinh ra và tự động copy vào `%APPDATA%\Grasshopper\Libraries\`.
- [ ] **Step 2:** Dọn dẹp các file `.gha` custom cũ khỏi thư mục `%APPDATA%\Grasshopper\Libraries\`:
  - Xóa `QD_G-TDrawingLink_2021.gha` và `QD_G-TDrawingLink_2024.gha` (nếu còn).
  - Đảm bảo trong thư mục `Libraries` chỉ còn `QD_TeklaLink.Loader.gha`.

---

### Task 4: Cập nhật Toàn bộ Tài liệu Hướng dẫn

**Files:**
- Modify: `docs/BUILD_AND_TESTING.md`
- Modify: `docs/CUSTOM_COMPONENTS.md`
- Modify: `docs/PROJECT_MAP.md`
- Modify: `.agents/rules/tekladrawinglink-upstream-safe.md`

- [ ] **Step 1:** Cập nhật `docs/CUSTOM_COMPONENTS.md`: Ghi nhận kiến trúc phân phối mới với Loader và thư mục `QD_TeklaLink\`.
- [ ] **Step 2:** Cập nhật `docs/BUILD_AND_TESTING.md`: Hướng dẫn lệnh build Loader và các phiên bản DLL.
- [ ] **Step 3:** Cập nhật `docs/PROJECT_MAP.md`: Mô tả chi tiết sơ đồ Loader -> QD_Assemblies -> Grasshopper ComponentServer.
- [ ] **Step 4:** Cập nhật `.agents/rules/tekladrawinglink-upstream-safe.md`: Ghi nhận quy tắc đóng gói `.dll` phiên bản kết hợp `.gha` loader.
