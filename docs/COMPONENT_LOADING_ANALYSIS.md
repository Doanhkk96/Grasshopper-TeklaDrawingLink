# Báo cáo Phân tích Cơ chế Loader và GUID

Tài liệu này cung cấp bằng chứng từ mã nguồn thực tế về cơ chế tải component và quản lý GUID, chứng minh tính khả thi của việc tạo assembly riêng lẻ.

## 1. Khai báo `ComponentGuid`
- **Nguồn gốc**: Trong Grasshopper SDK, `ComponentGuid` được khai báo tại class `GH_Component` dưới dạng một thuộc tính trừu tượng (`public abstract Guid ComponentGuid { get; }`).
- **Implement ở project gốc**: Tại file `src/GrasshopperTeklaDrawingLink/Components/TeklaComponentBase.cs` (Dòng 12), thuộc tính này được nạp bằng từ khóa `override`:
  ```csharp
  public override Guid ComponentGuid => VersionSpecificConstants.GetGuid(GetType());
  ```
- **Kết luận**: Vì thuộc tính này **không có từ khóa `sealed`**, bất kỳ class nào kế thừa từ `TeklaComponentBase` (hoặc `TeklaComponentBaseNew<T>`) đều có quyền tiếp tục `override` nó.

## 2. Cơ chế loader của Grasshopper
Grasshopper Engine nạp component bằng cách quét toàn bộ file assembly (`.gha`), dùng Reflection để tìm các class public kế thừa `GH_Component`, khởi tạo chúng và truy cập vào property `ComponentGuid`. Nó không quan tâm GUID này được lấy từ Dictionary, Database hay được hardcode.

## 3. Vai trò của `_typeGuids` và `VersionSpecificConstants.cs`
- `_typeGuids` là một Dictionary tĩnh. Mã nguồn gốc sử dụng nó như một nơi tập trung quản lý ID nhằm tiện tra cứu cho dự án lớn.
- **Có bắt buộc không?** KHÔNG BẮT BUỘC. Nếu custom component tự `override ComponentGuid`, hàm `VersionSpecificConstants.GetGuid(GetType())` sẽ không bao giờ được Grasshopper gọi đến. Do đó, ta hoàn toàn có thể bỏ qua việc khai báo vào `_typeGuids`.

## 4. Khả năng tái sử dụng các Class và Tham số (API Accessibility)
Các thành phần hạ tầng của project gốc cần thiết cho custom component có khả dụng không?
- `TeklaComponentBaseNew<T>`: `public abstract class` -> Hoàn toàn truy cập được.
- `CommandBase`: `public abstract class` -> Hoàn toàn truy cập được. Các hàm `GetInputParameters()`, `GetOutputParameters()` đều public. Hàm `SetOutput(IGH_DataAccess DA)` là `protected` nên class con truy cập được.
- Các class tham số (`InputStructParam`, `OutputTreeParam`, v.v...): đều là `public class` trong `ParameterManager.cs`.
- Tham chiếu hằng số `ParamInfos`: là `public static class`.

## 5. Các rào cản Internal Type
Qua phân tích source code custom hiện tại `FilterBoltsByDirectionComponent.cs`, nó có gọi đến:
```csharp
boltGroup = ModelInteractor.GetModelObject(drawingModelObject.ModelIdentifier) as TSM.BoltGroup;
```
- Bằng chứng tại `src/GrasshopperTeklaDrawingLink/Tools/ModelInteractor.cs`: Class này được đánh dấu là `internal class ModelInteractor`.
- **Hệ quả**: Nếu tách assembly, dòng code này sẽ báo lỗi biên dịch do khác assembly không thể gọi type `internal`.
- **Giải pháp**: Bản thân `ModelInteractor.GetModelObject` gọi API chuẩn của Tekla. Ta chỉ cần thay thế bằng:
  ```csharp
  boltGroup = new Tekla.Structures.Model.Model().SelectModelObject(drawingModelObject.ModelIdentifier) as TSM.BoltGroup;
  ```
Như vậy, không có bất cứ giới hạn kỹ thuật nào ngăn cản việc tách Assembly độc lập.
