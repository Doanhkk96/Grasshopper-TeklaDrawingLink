# Filter Extreme Points Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Triển khai Grasshopper component `Filter Extreme Points` trong project custom `QD_GrasshopperTeklaDrawingLink` và cập nhật tài liệu chuẩn hóa Category `QD`.

**Architecture:** Tạo class `FilterExtremePointsComponent` kế thừa `TeklaComponentBase` trong `custom/QD_GrasshopperTeklaDrawingLink/`, cô lập hoàn toàn khỏi core repo. Xử lý gom cụm tọa độ bằng grid key `(long)Math.Round(coord / tolerance)` kèm điều kiện lọc cực trị Min/Max. Cập nhật các tài liệu hướng dẫn (`CUSTOM_COMPONENTS.md`, `PROJECT_MAP.md`, `tekladrawinglink-upstream-safe.md`) để chuẩn hóa quy ước Category `QD`.

**Tech Stack:** C# .NET Framework 4.8, Grasshopper SDK (RhinoCommon `Point3d`), Tekla Open API (2021 & 2024 targets via MSBuild).

---

## Global Constraints

- Không chỉnh sửa bất kỳ tệp nào trong `src/GrasshopperTeklaDrawingLink/`.
- Component mới phải nằm trong `custom/QD_GrasshopperTeklaDrawingLink/`.
- Category hiển thị trên Grasshopper Canvas: `QD`.
- SubCategory: `Geometry`.
- `ComponentGuid` duy nhất, cố định: `7f4f603c-8eb3-4791-8848-18e38adcdb98`.
- Biên dịch thành công với cả Configuration `2021` và `2024`.

---

## Task Breakdown

### Task 1: Cập nhật tài liệu hướng dẫn quy chuẩn Category `QD`

**Files:**
- Modify: `.agents/rules/tekladrawinglink-upstream-safe.md`
- Modify: `docs/CUSTOM_COMPONENTS.md`
- Modify: `docs/PROJECT_MAP.md`

- [ ] **Step 1:** Cập nhật `.agents/rules/tekladrawinglink-upstream-safe.md` bổ sung quy định: Mọi component cá nhân mới bắt buộc sử dụng `Category = "QD"`.
- [ ] **Step 2:** Cập nhật `docs/CUSTOM_COMPONENTS.md` ghi nhận quy chuẩn Category `QD` và thông tin về component `FilterExtremePointsComponent`.
- [ ] **Step 3:** Cập nhật `docs/PROJECT_MAP.md` về cấu trúc phân loại component custom.

### Task 2: Cài đặt Component `FilterExtremePointsComponent`

**Files:**
- Create: `custom/QD_GrasshopperTeklaDrawingLink/FilterExtremePointsComponent.cs`

- [ ] **Step 1:** Viết mã nguồn `FilterExtremePointsComponent.cs` kế thừa `TeklaComponentBase`, override `ComponentGuid`, thiết lập `Category = "QD"`, `SubCategory = "Geometry"`.
- [ ] **Step 2:** Khai báo Input: `Points` (List), `Keep Max Y` (bool, default true), `Keep Max X` (bool, default true), `Tolerance` (double, default 1.0).
- [ ] **Step 3:** Khai báo Output: `Points by X` (List), `Points by Y` (List).
- [ ] **Step 4:** Cài đặt thuật toán lọc gom cụm theo `long ToGridKey(double val) => (long)Math.Round(val / tolerance)` và sắp xếp tăng dần. Bỏ qua các điểm không hợp lệ (`!pt.IsValid`).
- [ ] **Step 5:** Thêm Icon 24x24 dạng Bitmap Base64.

### Task 3: Build và xác minh cho cả Tekla 2021 và 2024

**Files:**
- Verify build: `custom/QD_GrasshopperTeklaDrawingLink/bin/2021/QD_GrasshopperTeklaDrawingLink.gha`
- Verify build: `custom/QD_GrasshopperTeklaDrawingLink/bin/2024/QD_GrasshopperTeklaDrawingLink.gha`

- [ ] **Step 1:** Chạy lệnh build MSBuild Configuration 2021.
- [ ] **Step 2:** Kiểm tra kết quả build 2021 (0 Error, file `.gha` tạo thành công).
- [ ] **Step 3:** Chạy lệnh build MSBuild Configuration 2024.
- [ ] **Step 4:** Kiểm tra kết quả build 2024 (0 Error, file `.gha` tạo thành công).
- [ ] **Step 5:** Cập nhật `docs/BUILD_AND_TESTING.md` với kết quả xác minh mới nhất.
