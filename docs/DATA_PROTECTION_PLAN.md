# Kế hoạch Bảo vệ Dữ liệu (Data Protection Plan)

Hiện tại, toàn bộ mã nguồn của custom component và các file mẫu đang nằm ở trạng thái nguy hiểm (Untracked và Unstaged) tại worktree chính (`C:\Users\QUOC_DOANH\OneDrive - SEICOGROUP\Repo\Grasshopper-TeklaDrawingLink`), nhánh `QDcustom`.

## 1. Danh sách các thay đổi cần bảo vệ

**A. Các file Untracked (mã nguồn custom và tài liệu):**
- `src/GrasshopperTeklaDrawingLink/Components/QD_Components/FilterBoltsByDirectionComponent.cs`
- `samples/QDScript/QD_Purlin Geometry.ghx`
- `docs/PROJECT_RESEARCH.md`
- (Rule file: `.agents/rules/tekladrawinglink-upstream-safe.md`)

**B. Các file Staged (đã git add nhưng chưa commit):**
- `samples/Move by anchoring to Bottom Left.ghcluster`
- `samples/QDScript/A_MultipleSectionDimensions.ghx`

**C. Các file Unstaged (đã chỉnh sửa từ mã nguồn gốc):**
- `src/GrasshopperTeklaDrawingLink/GrasshopperTeklaDrawingLink.csproj` (Bị đổi từ `.dll` sang `.gha` và thêm cấu hình Resources).
- `src/GrasshopperTeklaDrawingLink/Tools/VersionSpecificConstants.cs` (Bị thêm code đăng ký GUID cho `FilterBoltsByDirectionComponent`).

## 2. Kế hoạch Sao lưu và Khôi phục

**Quy tắc:** Tuyệt đối không chạy lệnh `git clean`, `git reset --hard` hoặc `git checkout` các file đang bị modified trước khi chúng được sao lưu an toàn.

**Các bước thực thi (Phase 3):**

1. **Commit bảo toàn trạng thái nguyên bản:**
   - Tại worktree chính (`C:\Users\QUOC_DOANH\OneDrive - SEICOGROUP\Repo\Grasshopper-TeklaDrawingLink`), thêm tất cả thay đổi (cả untracked, staged và unstaged) vào bộ đệm:
     `git add .`
   - Tạo một commit snapshot:
     `git commit -m "backup: save custom components and upstream modifications before architecture refactoring"`
   - Tạo một nhánh dự phòng để không bao giờ mất dữ liệu này:
     `git branch backup-qdcustom-v1`

2. **Khôi phục Upstream Code (Hoàn tác các sửa đổi sai nguyên tắc):**
   - Đảm bảo đang đứng ở nhánh `QDcustom`.
   - Phục hồi file `.csproj` và `VersionSpecificConstants.cs` về giống y hệt nhánh `main`:
     `git checkout main -- src/GrasshopperTeklaDrawingLink/GrasshopperTeklaDrawingLink.csproj`
     `git checkout main -- src/GrasshopperTeklaDrawingLink/Tools/VersionSpecificConstants.cs`
   - Commit việc khôi phục này:
     `git commit -m "chore: revert core files modification to prepare for separate assembly"`

3. **Di chuyển Custom Code sang Project Mới:**
   - Khởi tạo project `QD_GrasshopperTeklaDrawingLink.csproj`.
   - Di chuyển an toàn thư mục `QD_Components` sang project mới.
   - Sửa đổi nội dung `FilterBoltsByDirectionComponent.cs` để ghi đè `ComponentGuid` và loại bỏ tham chiếu `internal` (ModelInteractor).
   - Tiến hành build thử và kiểm thử.
