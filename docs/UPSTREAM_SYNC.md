# Upstream Synchronization & Branching Strategy

## Cấu trúc Git hiện tại
- **Remotes**:
  - `origin`: Fork cá nhân (`https://github.com/Doanhkk96/Grasshopper-TeklaDrawingLink.git`)
  - `upstream`: Repository gốc (`https://github.com/LetsConstructIT/Grasshopper-TeklaDrawingLink.git`)
- **Branches**:
  - `main`: Branch chính, hiện đang đồng bộ ở commit `91b8c78 Fixing bug for arcs #144`.
  - `QDcustom`: Branch cá nhân được tạo từ `main`, nhưng hiện tại mã nguồn custom chưa được commit (vẫn đang ở trạng thái untracked/unstaged trong worktree chính).
  - `survey_custom_components_architecture`: Branch khảo sát hiện tại (worktree phụ).

## Phương án phát triển được khuyến nghị: Tách Assembly Riêng

Thay vì nhúng custom component vào project gốc, bạn nên tạo một project C# mới (`QD_GrasshopperTeklaDrawingLink.csproj`).

**Ưu điểm:**
- Tránh hoàn toàn xung đột (merge conflicts) khi kéo bản cập nhật từ `upstream/main`.
- Độc lập vòng đời build: file `.gha` của upstream và file `.gha` của QDcustom hoàn toàn tách biệt trong Grasshopper.

**Cách thực hiện (Tránh lỗi loader):**
1. Project mới sẽ tham chiếu (reference) tới file `GrasshopperTeklaDrawingLink.dll` gốc.
2. Custom component vẫn kế thừa `TeklaComponentBaseNew<T>`.
3. Để tránh phụ thuộc vào `VersionSpecificConstants.cs` (gây lỗi khi không khai báo), trong class `FilterBoltsByDirectionComponent`, hãy **override thuộc tính ComponentGuid**:
   ```csharp
   public override Guid ComponentGuid => new Guid("6d8fd5c4-1572-4983-be71-1d50b334f6d4");
   ```
4. Grasshopper sẽ tự động nạp component này từ file `.gha` mới của bạn.

## Quy trình cập nhật (Upstream Sync)
1. Commit toàn bộ custom code hiện tại vào branch `QDcustom` (đã tách riêng project).
2. Khi upstream có bản mới, tải về: `git fetch upstream`
3. Cập nhật nhánh main: `git checkout main` và `git merge upstream/main`
4. Rebase hoặc merge nhánh `main` vào `QDcustom`: `git checkout QDcustom` và `git rebase main` (Sẽ không có conflict vì code custom đã nằm ở project riêng).
