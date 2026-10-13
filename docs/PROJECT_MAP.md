# Project Architecture Map

## Solution & Project Structure

- **Solution**: `GrasshopperTeklaDrawingLink.sln`
- **Core Upstream Project**: `src/GrasshopperTeklaDrawingLink/GrasshopperTeklaDrawingLink.csproj` (Target: .NET 4.8)
  - Thư viện gốc của tác giả Grzegorz, biên dịch ra `GrasshopperTeklaDrawingLink.<Configuration>.dll` cho Live Link loader gốc.
- **Custom Projects (Cô lập hoàn toàn trong thư mục `custom/`)**:
  1. `custom/QD_TeklaLink.Loader/QD_TeklaLink.Loader.csproj` (Target: .NET 4.8):
     - Dynamic Loader siêu nhẹ.
     - Kế thừa `GH_AssemblyPriority`, chịu trách nhiệm phát hiện phiên bản Tekla Structures đang hoạt động và nạp động file DLL component tương thích tương ứng.
     - Biên dịch ra `QD_TeklaLink.Loader.gha` và tự động deploy vào `%APPDATA%\Grasshopper\Libraries\`.
  2. `custom/QD_GrasshopperTeklaDrawingLink/QD_GrasshopperTeklaDrawingLink.csproj` (Target: .NET 4.8):
     - Chứa toàn bộ các Custom Component phát triển riêng cho QD.
     - Hỗ trợ đa cấu hình Tekla (2021, 2024...) thông qua `<Choose>` trong csproj.
     - Biên dịch ra `QD_G-TDrawingLink_<version>.dll` và tự động deploy vào `%APPDATA%\Grasshopper\QD_TeklaLink\`.

## Sơ đồ Luồng Khởi động và Nạp Động (Startup Flow)

```
Grasshopper Startup
        │
        ▼
Quét thư mục Libraries (%APPDATA%\Grasshopper\Libraries)
        │
        ▼
Nạp QD_TeklaLink.Loader.gha (kế thừa GH_AssemblyPriority)
        │
        ▼
PriorityLoad() ──► QD_LoaderEngine.Initialize()
        │
        ▼
Nhận diện Tekla Structures Runtime (Kiểm tra AppDomain / Running Process / Event)
        │
        ▼
Xác định phiên bản Tekla (ví dụ: "2021" hoặc "2024")
        │
        ▼
Định vị DLL tại %APPDATA%\Grasshopper\QD_TeklaLink\QD_G-TDrawingLink_<version>.dll
        │
        ▼
Reflection gọi GH_ComponentServer.LoadGHA(dllPath, loadType)
        │
        ▼
Grasshopper nạp toàn bộ component Custom (Category: "QD")
        │
        ▼
Cập nhật Canvas Ribbon UI & QD_RibbonStyler tự động cân đối kích thước SubCategory panel
```

## Quản lý ComponentGuid & Category cho Custom Project
- **ComponentGuid**:
  - Mỗi custom component bắt buộc khai báo override trực tiếp: `public override Guid ComponentGuid => new Guid("...");` để cô lập hoàn toàn, không đụng vào file `VersionSpecificConstants.cs` của core upstream.
  - GUID là duy nhất vĩnh viễn, không sinh lại sau khi phát hành.
- **Category & SubCategory**:
  - Mọi custom component bắt buộc khai báo `Category = "QD"` để tập trung trên một Tab riêng biệt trên Grasshopper Canvas.
  - SubCategory phân nhóm theo miền chức năng chuẩn thông qua `GTDrawingLink.Tools.QD_PanelHeadings` (`Geometry`, `Model`, `Annotations`, `Dimensions`, `Tools`...).
  - **Harmonious Ribbon Layout**: Module `QD_RibbonStyler` trong `custom/QD_TeklaLink.Loader` tự động đo đạc độ dài tên SubCategory và áp dụng padding đối xứng (tối thiểu 78px hoặc lớn hơn theo độ dài text) để chống lỗi cắt cụt chữ (`Geo...`, `QD_...`), đảm bảo thẩm mỹ khi phát triển thêm component/subcategory mới.

## Dependencies chính
- `Grasshopper` (v6.0.18016.23451)
- `Tekla.Structures.Model`
- `Tekla.Structures.Drawing`
- `Rhino.Geometry`

## Quy trình Build & Phân phối
- **Project core**: Biên dịch ra `GrasshopperTeklaDrawingLink.<Configuration>.dll` phục vụ cơ chế nạp LiveLink loader gốc.
- **Project Dynamic Loader (`custom/QD_TeklaLink.Loader/`)**:
  - Biên dịch ra `QD_TeklaLink.Loader.gha`.
  - Post-build event copy file vào `%APPDATA%\Grasshopper\Libraries\`.
- **Project custom components (`custom/QD_GrasshopperTeklaDrawingLink/`)**: 
  - Biên dịch ra DLL phiên bản theo chuẩn: `QD_G-TDrawingLink_<tekla_version>.dll` (ví dụ `QD_G-TDrawingLink_2021.dll`, `QD_G-TDrawingLink_2024.dll`).
  - Post-build event tự động copy file `.dll` vào `%APPDATA%\Grasshopper\QD_TeklaLink\`.
  - **Lợi ích**: Giải quyết triệt để lỗi Duplicate GUID khi cài đặt đa phiên bản Tekla trên cùng một hệ thống.
