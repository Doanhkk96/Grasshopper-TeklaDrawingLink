# Custom Components

## Component: `FilterBoltsByDirectionComponent`

**1. Vị trí & Trạng thái hiện tại:**
- **Thư mục**: `src/GrasshopperTeklaDrawingLink/Components/QD_Components/`
- **Branch**: Component này được phát hiện đang ở trạng thái *untracked* và sửa đổi (unstaged) trên branch `QDcustom` trong worktree chính của máy tính, chưa được commit vào history.
- **Project**: Đang được thêm trực tiếp vào project gốc `GrasshopperTeklaDrawingLink.csproj`.

**2. Thông tin kỹ thuật:**
- **Class**: `FilterBoltsByDirectionComponent` kế thừa `TeklaComponentBaseNew<FilterBoltsByDirectionCommand>`.
- **Command**: `FilterBoltsByDirectionCommand` kế thừa `CommandBase`.
- **Input**: `ModelObject`, `DisplayCoordinateSystem` (Plane).
- **Output**: `X Positions`, `Y Positions`, `Z Positions`, `Skew Positions`.
- **Chức năng**: Lấy danh sách BoltGroup và phân loại tọa độ các bu lông theo hướng song song với các trục X, Y, Z hoặc Skew dựa trên mặt phẳng tham chiếu.

**3. Mối liên hệ & Rủi ro khi nạp vào Grasshopper:**
- **Đăng ký GUID**: Component đang được gán GUID `"6d8fd5c4-1572-4983-be71-1d50b334f6d4"` bằng cách sửa trực tiếp file `VersionSpecificConstants.cs` của core repo.
- **Rủi ro**: 
  - Việc sửa trực tiếp `VersionSpecificConstants.cs` và `.csproj` của upstream sẽ gây **merge conflict** mỗi khi cập nhật phiên bản mới từ repository gốc.
  - Mã nguồn gốc phụ thuộc chặt chẽ, nếu upstream thay đổi `TeklaComponentBaseNew` hoặc `VersionSpecificConstants`, component tùy chỉnh có thể bị lỗi biên dịch.

**4. Điểm cần bảo vệ:**
- Logic phân loại hướng Bolt.
- Các file tài nguyên `.ghcluster`, `.ghx` trong thư mục `samples/QDScript/`.
