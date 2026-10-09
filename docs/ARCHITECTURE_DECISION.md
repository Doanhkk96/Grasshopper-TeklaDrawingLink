# Quyết định Kiến trúc & Đánh giá Phương án

Mục tiêu của tài liệu này là so sánh các phương án phát triển custom component và đưa ra kiến trúc tối ưu nhất dựa trên phân tích source code thực tế.

## So sánh ba phương án

### Phương án A: Giữ custom component trong assembly gốc
- **Mô tả**: Đặt file vào thư mục `QD_Components` trong project lõi, sửa `.csproj` để build chung và sửa `VersionSpecificConstants.cs` để đăng ký GUID.
- **Số file upstream phải sửa**: Ít nhất 2 file cốt lõi (`GrasshopperTeklaDrawingLink.csproj` và `VersionSpecificConstants.cs`). Việc người dùng tự ý đổi hậu tố build từ `.dll` sang `.gha` trong post-build event cũng đang phá vỡ quy tắc nạp của Live Link.
- **Khả năng conflict**: **Rất cao**. Mỗi lần upstream thêm component mới, file `VersionSpecificConstants.cs` sẽ bị thay đổi và sinh ra xung đột trực tiếp.
- **Độ phức tạp build**: Thấp (chỉ cần ấn Build), nhưng rủi ro deployment cao vì làm thay đổi cơ chế phân phối chuẩn của Tekla.

### Phương án B: Tách custom component thành assembly riêng, tham chiếu core (Khuyến nghị)
- **Mô tả**: Tạo project `QD_GrasshopperTeklaDrawingLink.csproj` độc lập. Reference tới `GrasshopperTeklaDrawingLink.dll`. Component override trực tiếp thuộc tính `ComponentGuid`.
- **Số file upstream phải sửa**: **0 file**. Code gốc được giữ nguyên vẹn.
- **Khả năng conflict**: **Không có**. Folder mã nguồn custom hoàn toàn tách biệt.
- **Chi phí bảo trì**: Cực kỳ thấp. Trừ khi upstream thay đổi các API public cơ sở như `TeklaComponentBaseNew<T>`, bản custom sẽ không bao giờ bị ảnh hưởng. Nếu có thay đổi, chỉ việc chỉnh sửa ở project custom.
- **Phù hợp với người không chuyên**: Rất phù hợp vì quá trình sync upstream sẽ hoàn toàn tự động, không đòi hỏi giải quyết merge conflict phức tạp.

### Phương án C: Tách project nhưng đăng ký tích hợp với core
- **Mô tả**: Tạo project riêng nhưng vẫn dùng Reflection hoặc sửa một phần core để inject GUID vào dictionary.
- **Đánh giá**: Dư thừa và không cần thiết vì Grasshopper loader và kiến trúc C# cho phép override `ComponentGuid` một cách tự nhiên (đã được chứng minh ở `COMPONENT_LOADING_ANALYSIS.md`).

## Khuyến nghị và Chiến lược Git

**Lựa chọn**: Phương án B. Tuyệt đối không can thiệp vào các tệp lõi của project gốc. Trả lại Post-build event về nguyên trạng (copy đuôi `.dll` như upstream).

**Chiến lược Git (Upstream Sync):**
1. **Upstream Commit**: Mọi commit từ kho gốc sẽ được kéo về và gộp vào nhánh `main` nội bộ. Nhánh `main` phải luôn giữ trạng thái sạch (clean), y hệt bản gốc.
2. **Custom Commit**: Mọi thay đổi về custom component, project `QD_GrasshopperTeklaDrawingLink`, thư mục `samples/QDScript` sẽ được commit hoàn toàn trên nhánh `QDcustom` (được rẽ nhánh từ `main`).
3. **Cập nhật định kỳ**: 
   - `git checkout main` -> `git merge upstream/main`
   - `git checkout QDcustom` -> `git rebase main`
   - Lợi ích của `rebase` ở đây là nó sẽ đặt toàn bộ commit custom của bạn lên đầu lịch sử mới nhất của upstream. Vì không có tệp nào chung bị sửa, quá trình rebase sẽ diễn ra tự động 100% mà không gây conflict.

## Rủi ro còn lại và Kiểm thử cần thiết
- Khi chuyển sang sử dụng Project ngoài, custom component sẽ mất quyền truy cập vào các class bị đánh dấu là `internal` trong core (cụ thể là `ModelInteractor.cs`). Rủi ro này đã có hướng giải quyết: sử dụng trực tiếp API công khai của Tekla `new Tekla.Structures.Model.Model().SelectModelObject()`.
- Cần chạy Smoke test thực tế trên Grasshopper sau khi tách file `.gha` để đảm bảo Grasshopper AssemblyResolver nạp đúng file `GrasshopperTeklaDrawingLink.dll` mà không báo lỗi "Missing dependency".


## Trạng thái triển khai (Update Phase 4)
- **Đã triển khai**: Đã tạo project `custom/QD_GrasshopperTeklaDrawingLink/QD_GrasshopperTeklaDrawingLink.csproj` độc lập, tham chiếu `GrasshopperTeklaDrawingLink.csproj`.
- **Đã di dời mã nguồn**: Đã chép `FilterBoltsByDirectionComponent.cs` sang project mới, thực hiện override `ComponentGuid` và loại bỏ tham chiếu `internal ModelInteractor`.
- **Khôi phục Core**: Đã hoàn tác (`git checkout HEAD`) file `GrasshopperTeklaDrawingLink.csproj` và `VersionSpecificConstants.cs` về nguyên trạng upstream.
- **Vấn đề Build (SDK 10)**: Core project nguyên bản của upstream gặp lỗi khi build với MSBuild / .NET SDK 10 do chứa file `.resx` (đòi hỏi `System.Resources.Extensions`). Lỗi này thuộc về core, không phải do kiến trúc mới, nên được giữ nguyên không tự ý sửa core lách lỗi. Tạm thời giữ lại mã nguồn custom cũ trong core cho đến khi môi trường build được fix.
