# Grasshopper-TeklaDrawingLink — ghi chú khảo sát trước khi phát triển fork

**Mốc khảo sát thông tin công khai:** 09/10/2026  
**Repository gốc:** https://github.com/LetsConstructIT/Grasshopper-TeklaDrawingLink

## 1. Kết luận chính

Đây là dự án Grasshopper dùng để tương tác với vùng bản vẽ Tekla Structures, không chỉ là một tập hợp component hình học độc lập. Cách đóng gói và nạp đúng DLL theo phiên bản Tekla là một điều kiện tương thích quan trọng. Với mục tiêu giữ fork có thể đồng bộ upstream, việc đầu tiên không nên là viết code mới ngay, mà là kiểm kê project/build/load pipeline trong đúng bản fork trên máy người dùng.

## 2. Những điều đã xác minh từ nguồn công khai

### Cấu trúc repository

Trang gốc hiện hiển thị các mục cấp cao `src/`, `samples/`, `tools/LinkInstaller/`, `.gitignore`, `LICENSE`, `README.md`. README tham chiếu icon trong `src/GrasshopperTeklaDrawingLink/Icons/`. Cần xác minh lại cây thực tế của fork/branch được mở trong Antigravity.

### Chức năng tổng thể

README liệt kê các nhóm component sau:

- **Params:** chọn drawing object/part/point, màu bản vẽ và kiểu view.
- **Drawing / Drawing List:** tạo, mở, đóng, lấy thông tin, liệt kê và xóa drawing; lấy drawing liên quan đến model object.
- **View:** tạo model/part/detail/section view, lấy đối tượng và view liên quan, lấy kích thước/thuộc tính, di chuyển và refresh view.
- **Parts / Geometry:** liên kết drawing part với model object, chọn/lấy/xóa/sửa drawing object, chuyển đổi tọa độ.
- **Attributes / Mark / UDA / Plugin / Misc:** thuộc tính đường nét/màu, level mark, user-defined attributes, plugin picker inputs, chạy macro và lấy center of gravity.

Những chức năng tạo, sửa hoặc xóa drawing/object có thể tác động lên dữ liệu Tekla thật. Custom component mới có side effect nên có chốt chạy rõ ràng và kiểm thử trên model thử nghiệm.

### Cơ chế cài đặt/nạp

README nói Drawing Link dùng cơ chế Grasshopper-Tekla Live Link để chọn phiên bản phù hợp khi chạy. File `GrasshopperTeklaLink.Loader.gha` chịu trách nhiệm cho việc nạp; Drawing Link nên được cài dạng `.dll` phù hợp với phiên bản Tekla vào `%AppData%\Grasshopper\Libraries`, theo hướng dẫn của README. README cảnh báo cách dùng `.gha` thủ công sẽ đòi hỏi xử lý cơ chế DLL Tekla khác.

Hệ quả: không được tự đổi đuôi DLL, đổi tên output, thêm assembly custom hoặc thay thư mục cài đặt khi chưa xác minh loader/dependency binding.

### Giấy phép và phiên bản

- README dẫn tới giấy phép MIT. Giữ lại license/copyright hiện có.
- Tại thời điểm khảo sát, trang Releases hiển thị `GTDrawingLink v.2.25.1`; notes `v.2.25.0` nêu hỗ trợ Tekla 2026, còn `v.2.25.1` nêu sửa lỗi liên quan đến chèn mark và Weld Marks.
- Trang releases cũng liệt kê các bản “Grasshopper Application” riêng. Vì vậy, cần xác minh xem workspace/branch thực tế có code companion application hay packaging liên quan nào ngoài nhóm component drawing được README tóm tắt.
- Tag/release mới nhất không nhất thiết trùng với HEAD của branch mặc định. Khi đồng bộ, hãy so sánh cả branch, commit và tags; không nâng version chỉ dựa vào số hiển thị trên trang web.

## 3. Các rủi ro kỹ thuật chính cần kiểm tra trong source local

1. **Loader & assembly resolution:** loader chọn DLL dựa vào điều kiện nào; có hỗ trợ assembly custom riêng không; assembly name, dependency, probing path và bản Tekla cụ thể được chọn ra sao.
2. **Ma trận build:** số solution/project, target framework, reference assemblies, conditional compilation, build configurations và output cho từng phiên bản Tekla.
3. **Định danh component:** cách component được đăng ký; GUID hiện tại; metadata assembly; cách icon/resource được đóng gói. Component GUID/port là vấn đề tương thích với file `.gh/.ghx` đã lưu.
4. **Đóng gói:** installer có copy/rename DLL hoặc tạo output đặc biệt không; file build nào được dùng khi cài; có CI/test hiện hữu không.
5. **API và threading:** pattern kết nối Tekla model/drawing, cách gọi Open API, thread/context, error handling và side effect khi Grasshopper recompute.
6. **Upstream history:** xác định các file upstream hay sửa thường xuyên; custom thay đổi có thể tách ra hay sẽ chạm cùng file; issues/releases có thay đổi API ảnh hưởng tới custom.

## 4. Chiến lược fork đề xuất

- Trước tiên giữ nguyên branch và lịch sử hiện tại; không tự động di chuyển commit hoặc rewrite history.
- Xác minh `origin` có trỏ về fork của người dùng và thêm remote `upstream` cho repository gốc chỉ khi chưa tồn tại/không bị xung đột.
- Sau khi khảo sát trạng thái hiện tại, ưu tiên một nhánh dùng để nhận thay đổi upstream và một nhánh `custom/*` hoặc `feature/*` giữ thay đổi cá nhân. Không mặc định tên branch cho đến khi kiểm tra repository.
- Giữ custom code/commit tách biệt; chỉ sửa điểm tích hợp nhỏ nhất có thể.
- Khi cập nhật, fetch tag/branch, đọc release notes, xem diff, backup branch trước merge, xử lý conflict theo ý nghĩa thay vì chọn “ours/theirs” toàn bộ; build và test các target liên quan trước khi coi bản cập nhật là dùng được.
- Không tự push, tạo release, reset hard, clean file chưa commit hoặc ghi đè DLL cài đặt.

## 5. Giới hạn của khảo sát này

Tôi đã xác minh README, trang repository, release notes và tài liệu chính thức về Live Link/Antigravity rules. Trong phiên khảo sát web này, các trang duyệt trực tiếp thư mục mã nguồn GitHub không được mở đầy đủ; vì vậy tôi **chưa xác nhận từng class `.cs`, nội dung `.sln/.csproj`, tên target framework, hay thuật toán nội bộ chọn DLL**. Không nên diễn đạt rằng đã thực hiện code review từng file.

Để khắc phục đúng cách, rule đi kèm yêu cầu Antigravity thực hiện bước source-level audit trên working copy của fork, ghi kết quả vào `docs/PROJECT_MAP.md` và `docs/UPSTREAM_SYNC.md`, rồi mới chỉnh code. Đây là phần bắt buộc để biến ghi chú công khai ở trên thành hiểu biết đầy đủ về đúng phiên bản bạn đang phát triển.

## 6. Nguồn

- Repository / README: https://github.com/LetsConstructIT/Grasshopper-TeklaDrawingLink
- README dạng raw text: https://raw.githubusercontent.com/LetsConstructIT/Grasshopper-TeklaDrawingLink/main/README.md
- Releases: https://github.com/LetsConstructIT/Grasshopper-TeklaDrawingLink/releases
- Issues: https://github.com/LetsConstructIT/Grasshopper-TeklaDrawingLink/issues
- Tekla official Live Link FAQ: https://support.tekla.com/help/tekla-structures/not-version-specific/grasshopperteklalink-faq
- Antigravity official Rules docs: https://www.antigravity.google/docs/rules/
