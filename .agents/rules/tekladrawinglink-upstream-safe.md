---
trigger: always_on
description: "Quy tắc bắt buộc khi phát triển custom component, sửa code, build hoặc đồng bộ upstream trong fork Grasshopper-TeklaDrawingLink. Ưu tiên bảo toàn tương thích với dự án gốc và dữ liệu Grasshopper hiện có."
---

# Quy tắc phát triển an toàn — Grasshopper-TeklaDrawingLink

## 1. Mục tiêu không được đánh đổi

Bạn đang làm việc trên một fork của `LetsConstructIT/Grasshopper-TeklaDrawingLink`. Mục tiêu theo thứ tự ưu tiên:

1. Không làm hỏng component, file Grasshopper, quy trình build, cơ chế nạp DLL hoặc hành vi đã có của dự án gốc.
2. Giữ thay đổi custom nhỏ, có thể nhận diện, kiểm thử và tách khỏi thay đổi upstream.
3. Cho phép cập nhật từ repository gốc với ít xung đột nhất có thể.
4. Chỉ cải tiến code gốc khi có lý do rõ ràng, lợi ích cụ thể và kiểm thử tương xứng.
5. Giải thích bằng tiếng Việt dễ hiểu; người dùng không phải lập trình viên chuyên nghiệp.

**Không được coi việc build thành công là bằng chứng đủ để kết luận tương thích với Tekla hoặc Grasshopper.** Phải phân biệt rõ điều đã kiểm tra, điều suy luận và điều chưa thể kiểm tra.

## 2. Những thông tin dự án đã biết và điều phải xác minh

- Repository gốc: `https://github.com/LetsConstructIT/Grasshopper-TeklaDrawingLink`.
- README công khai mô tả các Grasshopper component thao tác vùng bản vẽ Tekla: Drawing, Drawing List, View, Parts, Geometry, Attributes, Mark, UDA, Plugin và các parameter chọn đối tượng bản vẽ.
- README ghi cơ chế cài đặt chuẩn dùng Grasshopper-Tekla Live Link; thành phần loader là `GrasshopperTeklaLink.Loader.gha`, còn Drawing Link được cung cấp dạng `.dll` phù hợp phiên bản Tekla. Không tự ý đổi sang `.gha`, đổi tên DLL hoặc thay cơ chế nạp.
- Các mục gốc nhìn thấy trên GitHub gồm `src/`, `samples/`, `tools/LinkInstaller/`, `.gitignore`, `LICENSE`, `README.md`. Hãy xác minh lại cấu trúc thực tế trong workspace vì fork và branch có thể khác.
- README công khai ghi giấy phép MIT. Giữ nguyên các thông báo bản quyền và giấy phép đang có; không tự xóa hoặc thay thế chúng.
- Trang Releases đã hiển thị `GTDrawingLink v.2.25.1`; ghi chú `v.2.25.0` nêu hỗ trợ Tekla 2026. Đây chỉ là mốc tham khảo trực tuyến, không phải bằng chứng branch đang checkout tương ứng với bản phát hành đó.

Các thông tin trên **không thay thế việc đọc mã nguồn thực tế**. Tuyệt đối không tự đoán tên solution/project, target framework, cách chọn phiên bản Tekla, tên assembly, cách đăng ký component, cấu hình build hoặc API wrapper. Hãy đọc và ghi nhận chúng từ workspace trước khi sửa.

## 3. Bắt buộc khảo sát trước lần sửa đầu tiên

Trước khi chỉnh sửa mã nguồn, hãy làm khảo sát chỉ đọc và lập bản đồ dự án. Nếu đã có tài liệu khảo sát còn đúng, hãy kiểm tra và cập nhật thay vì tạo bản trùng lặp.

### 3.1. Kiểm tra trạng thái Git an toàn

Kiểm tra tối thiểu:

- thư mục gốc workspace và branch hiện tại;
- `git status --short --branch`;
- `git remote -v`;
- `git log --oneline --decorate --graph --all -n 30`;
- tags/release liên quan và những thay đổi local chưa commit.

Không được ghi đè, bỏ đi hoặc đưa thay đổi chưa commit vào một thay đổi khác. Nếu working tree không sạch, xác định chính xác file nào đã đổi và bảo vệ các thay đổi đó trước khi tiếp tục. Không tự ý commit hoặc push.

### 3.2. Đọc cấu trúc và đường build

Tìm và đọc các tệp thực tế phù hợp, bao gồm nhưng không giới hạn:

- mọi `.sln`, `.csproj`, `Directory.Build.*`, `global.json`, `NuGet.config`, `packages.config` hoặc tệp quản lý package hiện có;
- cấu hình Debug/Release, conditional compilation, post-build event, script build/packaging và các tệp CI nếu có;
- `AssemblyInfo`, tên assembly, version, namespace, entry point/assembly metadata và phần đăng ký component;
- mọi `HintPath`, reference và package liên quan đến Grasshopper, Rhino, Tekla Structures, Tekla Open API hoặc Live Link;
- phần xử lý chọn/nạp phiên bản Tekla, đường dẫn output, tên DLL, installer và cơ chế phân phối;
- cách icon được nhúng/tải; cách các component hiện có đăng ký input/output và xử lý lỗi;
- `samples/`, các file Grasshopper mẫu và kiểm thử hiện có (nếu có).

Xác định chính xác: project nào tạo ra từng DLL; DLL nào được loader nạp; mỗi target dùng phiên bản Tekla nào; môi trường và công cụ nào cần để build; file nào được đóng gói; file nào là nguồn thật và file nào chỉ là output sinh ra.

### 3.3. Tạo bản đồ dự án

Tạo hoặc cập nhật `docs/PROJECT_MAP.md` và ghi lại:

- sơ đồ thư mục/solution/project;
- vai trò các project và điểm vào;
- danh sách target framework/configuration/Tekla version đã xác minh;
- luồng build → output → loader → Grasshopper;
- quy ước component, namespace, GUID, icon, xử lý dữ liệu và thông báo lỗi;
- lệnh build đã xác minh, điều kiện tiên quyết và các giới hạn chưa kiểm tra được.

Tạo hoặc cập nhật `docs/UPSTREAM_SYNC.md` với remote, branch mặc định đã xác minh, quy trình cập nhật và cách khôi phục. Không ghi thông tin giả định như thể đã kiểm tra.

Nếu thiếu SDK/Tekla/Visual Studio hoặc không thể build trong môi trường hiện tại, ghi rõ hạn chế đó. Không bịa kết quả build hay runtime.

## 4. Kiến trúc cho custom component: cô lập trước, sửa code gốc sau

Trước khi thêm component, hãy tìm component hiện có gần nhất về chức năng và làm theo mẫu thực tế của repository.

Ưu tiên theo thứ tự:

1. **Assembly/project custom riêng**, chỉ khi đã xác minh loader có thể nạp nó đúng cách, API cần dùng có thể truy cập và việc bind đúng các DLL Grasshopper/Rhino/Tekla hoạt động với các phiên bản được hỗ trợ.
2. Nếu assembly riêng không tương thích với loader/build hiện hữu, cô lập mã custom trong thư mục/namespace riêng bên trong project phù hợp, theo đúng conventions đang có. Giữ thay đổi ở code gốc chỉ tại điểm tích hợp tối thiểu cần thiết.
3. Nếu cả hai lựa chọn đều có rủi ro đáng kể, dừng trước khi thay đổi kiến trúc; trình bày lựa chọn, tác động, khả năng cập nhật và khuyến nghị bằng tiếng Việt.

Không tự động tạo project/assembly mới chỉ vì nhìn có vẻ sạch hơn. **Không được giả định DLL custom có thể tồn tại song song với DLL gốc.** Hãy kiểm tra tên assembly, cách loader khám phá DLL, dependency resolution và thư mục cài đặt trước. Không được âm thầm tạo output trùng tên rồi ghi đè bản upstream.

Quy tắc cô lập:

- Giữ logic custom, helper và tài liệu custom trong khu vực có thể nhận diện rõ; đặt tên namespace/file/class không xung đột với dự án gốc.
- Không sao chép toàn bộ mã component cũ để sửa vài dòng; tái sử dụng API/helper hiện hữu khi được phép và phù hợp.
- Không refactor, đổi định dạng hàng loạt hoặc đổi namespace các file gốc chỉ để phục vụ một custom component.
- Không sửa file sinh tự động nếu đã có nguồn sinh ra nó; sửa nguồn và chạy đúng quy trình sinh.
- Không thêm package, DLL bên ngoài, framework mới hoặc license mới nếu chưa kiểm tra nhu cầu, tính tương thích và ảnh hưởng đóng gói. Giải thích trước khi đưa dependency mới vào.
- Mọi cải tiến code gốc phải tách khỏi yêu cầu custom khi có thể, có lý do và có kiểm thử riêng.

## 5. Tính ổn định của component Grasshopper

Các file `.gh`/`.ghx` của người dùng có thể lưu định danh component và cách nối cổng. Vì vậy:

- Mỗi component custom phải có `ComponentGuid` duy nhất; kiểm tra toàn bộ source để tránh trùng. Tạo GUID một lần và ghi nhận nó trong tài liệu custom. **Sau khi component đã được dùng trong definition của người dùng, không được đổi GUID.** Không sinh GUID mới mỗi lần build.
- Không thay đổi `ComponentGuid` của component gốc.
- Không đổi tên, nickname, category/subcategory hoặc cách hiển thị của component gốc nếu không thật sự cần thiết.
- Không thay đổi thứ tự, kiểu dữ liệu, `GH_ParamAccess`, chế độ item/list/tree, trạng thái optional, giá trị mặc định hoặc ý nghĩa input/output hiện có mà không đánh giá tương thích với definition đã lưu.
- Nếu thay đổi port/API đã phát hành là bắt buộc, hãy giải thích tác động, đề xuất cách tương thích/ngừng hỗ trợ và cập nhật tài liệu; không lặng lẽ thay đổi.
- Tuân thủ cơ chế lấy dữ liệu, đặt output, runtime message, icon/resource và metadata của component trong repository. Với input thiếu/sai, báo lỗi rõ bằng cơ chế chuẩn của dự án; không trả dữ liệu giả để che lỗi.
- Đảm bảo tên và mô tả component nói đúng hành vi thực tế. Không tạo component trùng tên hoặc trùng GUID với component có sẵn.

## 6. Tương tác với Tekla và an toàn dữ liệu bản vẽ

Tekla Drawing API có thể tạo, sửa hoặc xóa dữ liệu thực. Hãy dùng đúng kiểu API và pattern mà source hiện tại áp dụng cho đúng phiên bản Tekla.

- Trước khi dùng API mới, xác minh class/method/property trong reference hoặc tài liệu tương ứng với target Tekla; không đoán tên API hoặc tự nâng version reference.
- Không gọi API Tekla/Grasshopper từ background thread (`Task.Run`, thread tự tạo, timer...) trừ khi đã xác minh thao tác đó thread-safe và phù hợp thread/context yêu cầu. Ưu tiên pattern đồng bộ/threading sẵn có trong dự án.
- Custom component mới có thao tác ghi, sửa, tạo hoặc xóa drawing/drawing object phải có cổng chạy rõ ràng như `Run`/`Confirm`, mặc định `false`, trừ khi người dùng yêu cầu và kiến trúc hiện tại có cơ chế an toàn tương đương. Không tự phát sinh thao tác phá hủy chỉ vì Grasshopper recompute.
- Khi có thể, hỗ trợ chế độ preview/dry-run, kiểm tra điều kiện đầu vào trước khi ghi, tránh tạo trùng khi chạy lại và xuất thông báo cho biết đối tượng nào đã/đang được xử lý.
- Không chạy thử nghiệm xóa/tạo/sửa drawing trên model thật hoặc file dự án của người dùng. Kiểm thử thao tác ghi trên model thử nghiệm riêng hoặc dry-run. Không tự chạy macro hay thay đổi drawing đang mở khi chưa có yêu cầu rõ ràng.
- Xử lý trường hợp Tekla chưa mở, model/drawing chưa kết nối, selection rỗng, đối tượng không hợp lệ và lỗi API. Không nuốt exception bằng `catch` rỗng; đưa ra thông báo đủ hữu ích nhưng không làm lộ dữ liệu nhạy cảm.
- Tránh vòng lặp recompute/expire solution vô hạn và side effect lặp đi lặp lại.

## 7. Git và nhịp cập nhật upstream

Mục tiêu là giữ fork có thể nhận thay đổi từ `LetsConstructIT/Grasshopper-TeklaDrawingLink` mà không mất custom code.

- Xác minh remote bằng `git remote -v`; thông thường `origin` là fork của người dùng và `upstream` là repository gốc, nhưng phải kiểm tra URL thực tế, không suy đoán.
- Nếu chưa có remote `upstream`, hãy đề xuất lệnh dùng URL đã xác minh và chỉ thêm sau khi kiểm tra remote hiện tại để không ghi đè cấu hình của người dùng.
- Dùng branch/nhánh mặc định thực sự của repository; không mặc định tên branch là `main` nếu metadata local cho thấy khác.
- Không phát triển trực tiếp trên nhánh đồng bộ upstream nếu có thể tránh. Sau khi khảo sát và bảo toàn cấu hình hiện tại, ưu tiên giữ một nhánh upstream-compatible để nhận bản gốc và các branch `custom/*` hoặc `feature/*` cho thay đổi cá nhân. Không tự động đổi branch hiện có hoặc di chuyển commit.
- Giữ commit custom tách biệt, nhỏ, có thông điệp dễ hiểu; tránh trộn định dạng code, nâng dependency và tính năng trong cùng một commit.
- Khi cập nhật: kiểm tra working tree sạch; fetch upstream/tags; so sánh commit và release note; xem các file upstream thay đổi; đối chiếu API, project file, versioning, loader và build output; dự đoán xung đột; sao lưu bằng branch an toàn trước khi merge.
- Với nhánh custom đã có commit dùng chung, ưu tiên merge thay vì rebase/rewrite history để giữ lịch sử dễ hiểu. Không ép người dùng dùng chiến lược này nếu repository thực tế có workflow khác phù hợp hơn; ghi lại quyết định trong `docs/UPSTREAM_SYNC.md`.
- Không dùng `git reset --hard`, `git clean -fdx`, force-push, xóa branch, rebase công khai hoặc bỏ file thay đổi để giải quyết xung đột, trừ khi người dùng yêu cầu rõ ràng và đã biết hậu quả.
- Không giải quyết xung đột bằng cách chọn toàn bộ “ours” hoặc “theirs” một cách máy móc. Đọc từng khối xung đột, xác định mục đích của cả upstream lẫn custom, giữ cả hai khi hợp lý; nếu không thể xác định hành vi mong muốn, dừng và báo người dùng.
- Không tự push lên GitHub, tạo release, xóa tag hoặc sửa remote khi chưa được yêu cầu.
- Sau mỗi lần đồng bộ, ghi commit/tag upstream đã nhập, xung đột đã xử lý, file custom bị ảnh hưởng, kết quả build và kiểm thử còn thiếu.

## 8. Build, đóng gói và cài đặt

- Dùng đúng solution, configuration, framework, compiler và build commands đã xác minh ở bước khảo sát. Không tự chuyển project cũ sang SDK-style, đổi target framework hoặc cập nhật hàng loạt NuGet để “build cho tiện”.
- Nếu dự án phát ra nhiều DLL cho các phiên bản Tekla, phải build và kiểm tra đúng cấu hình/phiên bản liên quan đến thay đổi. Không lấy một DLL bất kỳ rồi coi là dùng được cho mọi phiên bản.
- Giữ đúng tên assembly, output path, metadata và cấu trúc package mà Live Link/installer yêu cầu. Nếu cần thay đổi, trước tiên giải thích cơ chế hiện tại và đánh giá khả năng tương thích/cài song song/rollback.
- Không tự chép DLL vào thư mục Grasshopper của người dùng, không thay DLL đang dùng và không sửa Tekla installation khi chưa có yêu cầu rõ ràng.
- Không đưa file build phát sinh, DLL, symbol hoặc thư mục output vào Git trừ khi repository có chủ ý theo dõi chúng.
- Nếu không có môi trường Windows/Tekla cần thiết, vẫn có thể chạy các kiểm tra tĩnh phù hợp, nhưng phải ghi rõ runtime integration test chưa thực hiện được.

## 9. Quy trình cho mỗi yêu cầu sửa đổi

Trước khi sửa:

1. Nói ngắn gọn bạn định thay đổi gì và vì sao; nêu file/project chính sẽ bị ảnh hưởng nếu có nhiều file.
2. Đọc component hiện có gần nhất và xác định các điểm tích hợp tối thiểu.
3. Xác định phạm vi thay đổi, tác động đến component cũ, file `.gh/.ghx`, build và upstream merge.
4. Nếu rủi ro cao, thay đổi kiến trúc, dependency, versioning, installer hoặc có thể ghi/xóa dữ liệu Tekla, trình bày phương án an toàn trước khi thực hiện.

Sau khi sửa:

1. Xem `git diff` để chắc chỉ có thay đổi liên quan; kiểm tra file ngoài ý muốn và dữ liệu người dùng.
2. Build bằng cấu hình đã xác minh; chạy kiểm thử phù hợp và smoke test an toàn nếu môi trường cho phép.
3. Kiểm tra tất cả component hiện có vẫn được phát hiện, ID không đổi, output/packaging còn đúng; thử mở lại file definition mẫu nếu có thể.
4. Cập nhật tài liệu custom/changelog khi cần.
5. Trả kết quả bằng tiếng Việt: thay đổi đã làm, file đã sửa, lệnh/test đã chạy, kết quả thật, phần chưa kiểm tra được và cách rollback.

Không được tuyên bố “đã test thành công”, “tương thích Tekla X”, “đồng bộ upstream an toàn” hoặc “đã sửa xong” nếu chưa có bằng chứng tương ứng.

## 10. Tiêu chí hoàn thành

Một thay đổi chỉ được coi là hoàn thành khi tất cả mục liên quan đều được đáp ứng hoặc ghi rõ lý do chưa thể đáp ứng:

- [ ] Giữ nguyên hành vi và GUID của component gốc không liên quan.
- [ ] Custom code được cô lập tối đa, không tạo xung đột/ghi đè DLL ngoài ý muốn.
- [ ] Dùng đúng API/reference và target đã xác minh.
- [ ] Có xử lý lỗi; component ghi/xóa dữ liệu có cơ chế bảo vệ.
- [ ] Build/test được thực hiện theo môi trường sẵn có, kết quả báo cáo trung thực.
- [ ] `git diff` được rà soát; không mất thay đổi của người dùng.
- [ ] Tài liệu cần thiết về cách dùng, build, phiên bản và upstream sync đã được cập nhật.
- [ ] Người dùng hiểu rõ việc đã làm và giới hạn còn lại.
