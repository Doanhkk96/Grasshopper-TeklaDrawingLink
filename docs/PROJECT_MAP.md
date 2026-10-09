# Project Architecture Map

## Solution & Project Structure

- **Solution**: `GrasshopperTeklaDrawingLink.sln`
- **Core Project**: `src/GrasshopperTeklaDrawingLink/GrasshopperTeklaDrawingLink.csproj` (Target: .NET 4.8)
- **Cơ chế nạp Component (Grasshopper Loader)**: 
  - Grasshopper tự động quét các file `.gha` (được build ra từ `.dll`) trong thư mục `Libraries`.
  - Các component được kế thừa từ `GH_Component`. Dự án này sử dụng lớp cơ sở `TeklaComponentBase` và `TeklaComponentBaseNew<T>` để chuẩn hóa việc xử lý tham số đầu vào/đầu ra (thông qua `CommandBase`).
- **Quản lý ComponentGuid**:
  - Theo thiết kế gốc, thuộc tính `ComponentGuid` được nạp tập trung từ một Dictionary trong file `src/GrasshopperTeklaDrawingLink/Tools/VersionSpecificConstants.cs`.
  - Bất kỳ class component nào kế thừa `TeklaComponentBase` mà không override `ComponentGuid` sẽ bị phụ thuộc vào Dictionary này.

## Dependencies chính
- `Grasshopper` (v6.0.18016.23451)
- `Tekla.Structures.Model`
- `Tekla.Structures.Drawing`
- `Rhino.Geometry`

## Quy trình Build
- Build project sẽ tạo ra file `GrasshopperTeklaDrawingLink.dll`.
- Post-build event sử dụng câu lệnh `copy` để đổi đuôi file thành `.gha` và tự động chuyển vào `%APPDATA%\Grasshopper\Libraries\`.
