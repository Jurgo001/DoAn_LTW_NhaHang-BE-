-- ========================================
-- TẠO CƠ SỞ DỮ LIỆU
-- ========================================
CREATE DATABASE QL_NhaHangg;
GO
USE QL_NhaHangg;
GO

-- ========================================
-- BẢNG CHÍNH
-- ========================================

CREATE TABLE tblNCC (
    MaNCC INT IDENTITY(1,1) PRIMARY KEY,
    TenNCC NVARCHAR(100),
    DiaChi NVARCHAR(255),
    DienThoai NVARCHAR(20)
);

CREATE TABLE tblLoaiMon (
    MaLoai INT IDENTITY(1,1) PRIMARY KEY,
    TenLoai NVARCHAR(100),
    MoTa NVARCHAR(255)
);

CREATE TABLE tblMonAn (
    MaMon INT IDENTITY(1,1) PRIMARY KEY,
    TenMon NVARCHAR(200),
    DonGia DECIMAL(18,2),
    MoTa NVARCHAR(MAX),
    AnhDaiDien NVARCHAR(255),
    MaLoai INT FOREIGN KEY REFERENCES tblLoaiMon(MaLoai),
    MaNCC INT FOREIGN KEY REFERENCES tblNCC(MaNCC),
    TrangThai BIT DEFAULT 1
);

CREATE TABLE tblHinhAnh (
    ID INT IDENTITY(1,1) PRIMARY KEY,
    MaMon INT FOREIGN KEY REFERENCES tblMonAn(MaMon),
    TenHinh NVARCHAR(255)
);

CREATE TABLE tblKhachHang (
    MaKH INT IDENTITY(1,1) PRIMARY KEY,
    TenKH NVARCHAR(100),
    MatKhau NVARCHAR(100),
    GioiTinh NVARCHAR(10),
    NamSinh INT,
    Avarta NVARCHAR(255),
    DienThoai NVARCHAR(20),
    Email NVARCHAR(100),
    DiaChi NVARCHAR(255)
);

CREATE TABLE tblVaiTro (
    IDVaiTro INT IDENTITY(1,1) PRIMARY KEY,
    TenVaiTro NVARCHAR(50),
    MoTa NVARCHAR(255)
);

CREATE TABLE tblNhanVien (
    MaNV INT IDENTITY(1,1) PRIMARY KEY,
    MatKhau NVARCHAR(100),
    TenNV NVARCHAR(100),
    GioiTinh NVARCHAR(10),
    NamSinh INT,
    Email NVARCHAR(100),
    VaiTro INT FOREIGN KEY REFERENCES tblVaiTro(IDVaiTro)
);

CREATE TABLE tblTinhTrang (
    ID INT IDENTITY(1,1) PRIMARY KEY,
    TinhTrang NVARCHAR(50)
);

CREATE TABLE tblHoaDon (
    MaHD INT IDENTITY(1,1) PRIMARY KEY,
    MaKH INT FOREIGN KEY REFERENCES tblKhachHang(MaKH),
    MaNV INT FOREIGN KEY REFERENCES tblNhanVien(MaNV),
    NgayLap DATETIME,
    TongTien DECIMAL(18,2),
    TinhTrang INT FOREIGN KEY REFERENCES tblTinhTrang(ID),
    BanSo INT,
    GhiChu NVARCHAR(255),
    DaThanhToan BIT
);

CREATE TABLE tblChiTietHoaDon (
    MaHD INT FOREIGN KEY REFERENCES tblHoaDon(MaHD),
    MaMon INT FOREIGN KEY REFERENCES tblMonAn(MaMon),
    SoLuong INT,
    DonGia DECIMAL(18,2),
    PRIMARY KEY (MaHD, MaMon)
);
CREATE TABLE tblDatBan (
    MaDatBan INT IDENTITY(1,1) PRIMARY KEY,
    TenKhachHang NVARCHAR(100),
    Email NVARCHAR(100),
    DienThoai NVARCHAR(20), -- Nên thêm cột này dù form chưa có
    NgayDat DATETIME,
    SoNguoi INT,
    GhiChu NVARCHAR(500),
    TrangThai INT DEFAULT 0 -- 0: Mới đặt, 1: Đã xác nhận, 2: Đã hủy
);
GO
CREATE TABLE tblDatBan (
    MaDatBan INT IDENTITY(1,1) PRIMARY KEY,
    TenKhachHang NVARCHAR(100),
    Email NVARCHAR(100),
    DienThoai NVARCHAR(20), -- Nên thêm cột này dù form chưa có
    NgayDat DATETIME,
    SoNguoi INT,
    GhiChu NVARCHAR(500),
    TrangThai INT DEFAULT 0 -- 0: Mới đặt, 1: Đã xác nhận, 2: Đã hủy
);
GO
-- 1. Bảng lưu danh sách các loại Voucher
CREATE TABLE tblVoucher (
    MaVoucher INT IDENTITY(1,1) PRIMARY KEY,
    TenVoucher NVARCHAR(100), -- Ví dụ: Giảm 50k
    GiaTri DECIMAL(18,0),     -- Số tiền giảm: 50000
    DiemDoi INT,              -- Điểm cần để đổi: 100
    SoLuong INT,              -- Số lượng còn lại
    NgayHetHan DATETIME
);

-- 2. Bảng lưu lịch sử đổi (Voucher khách hàng đã sở hữu)
CREATE TABLE tblLichSuDoiDiem (
    MaLichSu INT IDENTITY(1,1) PRIMARY KEY,
    MaKH INT,
    MaVoucher INT,
    NgayDoi DATETIME DEFAULT GETDATE(),
    DiemDaTru INT, -- Lưu lại số điểm đã trừ tại thời điểm đổi
    MaCode NVARCHAR(20), -- Code unique để áp dụng khi thanh toán
    TrangThai BIT DEFAULT 0, -- 0: Chưa dùng, 1: Đã dùng
    FOREIGN KEY (MaKH) REFERENCES tblKhachHang(MaKH),
    FOREIGN KEY (MaVoucher) REFERENCES tblVoucher(MaVoucher)
);
CREATE TABLE tblLienHe (
    MaLH INT IDENTITY(1,1) PRIMARY KEY,
    TenNguoiGui NVARCHAR(100) NOT NULL,
    Email VARCHAR(100),
    SoDienThoai VARCHAR(20),
    ChuDe NVARCHAR(200),
    NoiDung NTEXT,
    NgayGui DATETIME DEFAULT GETDATE(),
    TrangThai BIT DEFAULT 0 -- 0: Chưa xem, 1: Đã xem
);
-- Thêm vài dữ liệu mẫu
INSERT INTO tblVoucher VALUES (N'Giảm 20k', 20000, 200, 100, '2025-12-31');
INSERT INTO tblVoucher VALUES (N'Giảm 50k', 50000, 500, 50, '2025-12-31');
INSERT INTO tblVoucher VALUES (N'Giảm 100k', 100000, 1000, 10, '2025-12-31');
-- ========================================
-- DỮ LIỆU MẪU
-- ========================================

-- Nhà Cung Cấp (10 dòng)
INSERT INTO tblNCC (TenNCC, DiaChi, DienThoai) VALUES
(N'Công ty Thực Phẩm Sạch', N'TP.HCM', '0281112223'),
(N'Hải Sản Xanh', N'Đà Nẵng', '0236223344'),
(N'Nông Trại Rau Hữu Cơ', N'Lâm Đồng', '02633777888'),
(N'Xưởng Bánh Việt', N'Hà Nội', '02444555666'),
(N'Đồ Uống Fresh', N'Bình Dương', '0274223333'),
(N'Thịt Tươi Hoàng Gia', N'TP.HCM', '0287788990'),
(N'Công Ty Trứng Sạch', N'Đồng Nai', '0251888999'),
(N'Nước Mắm Phú Quốc', N'Kiên Giang', '0297333444'),
(N'Nông Sản Xanh', N'Cần Thơ', '0292223344'),
(N'Trái Cây Việt', N'Vĩnh Long', '0277888999');

-- Loại Món (10 dòng)
INSERT INTO tblLoaiMon (TenLoai, MoTa) VALUES
(N'Món Khai Vị', N'Món nhẹ đầu bữa'),
(N'Món Chính', N'Món ăn chính'),
(N'Tráng Miệng', N'Món ngọt sau bữa'),
(N'Đồ Uống', N'Nước giải khát'),
(N'Lẩu', N'Món nước dùng chung'),
(N'Nướng', N'Món nướng than'),
(N'Hải Sản', N'Món tôm, cá, mực...'),
(N'Món Chay', N'Không có thịt'),
(N'Món Cơm', N'Món cơm Việt Nam'),
(N'Mì - Bún', N'Món sợi đặc trưng');

-- Món Ăn (10 dòng)
INSERT INTO tblMonAn (TenMon, DonGia, MoTa, AnhDaiDien, MaLoai, MaNCC) VALUES
(N'Gỏi Cuốn Tôm Thịt', 25000, N'Món khai vị tươi mát', N'goicuon.jpg', 1, 1),
(N'Cơm Chiên Dương Châu', 55000, N'Cơm chiên trứng, xúc xích, tôm', N'comchien.jpg', 9, 1),
(N'Lẩu Thái Hải Sản', 180000, N'Lẩu cay chua với tôm mực cá', N'lauthai.jpg', 5, 2),
(N'Bò Lúc Lắc', 95000, N'Thịt bò xào ớt chuông', N'boluclac.jpg', 2, 6),
(N'Kem Dừa', 35000, N'Món tráng miệng mát lạnh', N'kemdua.jpg', 3, 10),
(N'Sinh Tố Bơ', 40000, N'Sinh tố bơ tươi', N'sinhtobo.jpg', 4, 10),
(N'Mì Xào Hải Sản', 65000, N'Mì xào tôm mực', N'mixaohs.jpg', 10, 2),
(N'Đậu Hũ Chiên Sả', 30000, N'Món chay thơm ngon', N'dauhu.jpg', 8, 3),
(N'Gà Nướng Mật Ong', 120000, N'Gà nướng mật ong', N'ga_nuong_mat.jpg', 6, 6),
(N'Tôm Rang Me', 90000, N'Tôm rang chua ngọt', N'tomrangme.jpg', 7, 2);

-- Vai Trò
INSERT INTO tblVaiTro (TenVaiTro, MoTa) VALUES
(N'Quản trị viên', N'Quản lý toàn hệ thống'),
(N'Thu ngân', N'Xử lý thanh toán'),
(N'Phục vụ', N'Ghi món, dọn bàn'),
(N'Đầu bếp', N'Nấu ăn'),
(N'Lễ tân', N'Đón khách'),
(N'Pha chế', N'Chuẩn bị đồ uống'),
(N'Bảo vệ', N'Giữ xe'),
(N'Kế toán', N'Kiểm soát thu chi'),
(N'Nhân sự', N'Quản lý nhân viên'),
(N'Trợ lý', N'Hỗ trợ quản lý');
-- ĐÂY LÀ BOSS
INSERT INTO tblVaiTro (TenVaiTro, MoTa) VALUES
(N'Chủ nhà hàng', N'Điều hành nhà hàng');

-- Nhân Viên
INSERT INTO tblNhanVien (MatKhau, TenNV, GioiTinh, NamSinh, Email, VaiTro) VALUES
('123', N'Nguyễn Văn A', N'Nam', 1995, 'a.nguyen@nhahang.vn', 1),
('123', N'Lê Thị B', N'Nữ', 1998, 'b.le@nhahang.vn', 2),
('123', N'Phạm Văn C', N'Nam', 1992, 'c.pham@nhahang.vn', 3),
('123', N'Trần Thị D', N'Nữ', 1997, 'd.tran@nhahang.vn', 4),
('123', N'Đỗ Văn E', N'Nam', 1990, 'e.do@nhahang.vn', 5),
('123', N'Vũ Thị F', N'Nữ', 1996, 'f.vu@nhahang.vn', 6),
('123', N'Bùi Văn G', N'Nam', 1989, 'g.bui@nhahang.vn', 7),
('123', N'Ngô Thị H', N'Nữ', 1993, 'h.ngo@nhahang.vn', 8),
('123', N'Lý Văn I', N'Nam', 1994, 'i.ly@nhahang.vn', 9),
('123', N'Huỳnh Thị J', N'Nữ', 1999, 'j.huynh@nhahang.vn', 10);
-- ĐÂY LÀ BOSS
INSERT INTO tblNhanVien (MatKhau, TenNV, GioiTinh, NamSinh, Email, VaiTro) VALUES
('123', N'Tôn Bằng', N'Nữ', 1989, 'truongmilan@nhahang.vn', 10);


-- Tình Trạng
INSERT INTO tblTinhTrang (TinhTrang) VALUES
(N'Chờ xác nhận'),
(N'Đang chế biến'),
(N'Đang phục vụ'),
(N'Đã thanh toán'),
(N'Đã hủy');

-- Khách Hàng (10 dòng)
INSERT INTO tblKhachHang (TenKH, MatKhau, GioiTinh, NamSinh, Avarta, DienThoai, Email, DiaChi) VALUES
(N'Lê Minh Khoa', '123', N'Nam', 1998, N'khoa.jpg', '0911111111', 'khoa@gmail.com', N'TP.HCM'),
(N'Trần Thị Lan', '123', N'Nữ', 1999, N'lan.jpg', '0922222222', 'lan@gmail.com', N'Hà Nội'),
(N'Nguyễn Hữu Tài', '123', N'Nam', 1997, N'tai.jpg', '0933333333', 'tai@gmail.com', N'Đà Nẵng'),
(N'Võ Thị My', '123', N'Nữ', 2000, N'my.jpg', '0944444444', 'my@gmail.com', N'Cần Thơ'),
(N'Phan Quang Huy', '123', N'Nam', 1995, N'huy.jpg', '0955555555', 'huy@gmail.com', N'Lâm Đồng'),
(N'Trịnh Thanh Hằng', '123', N'Nữ', 1998, N'hang.jpg', '0966666666', 'hang@gmail.com', N'Đồng Nai'),
(N'Đinh Công Sơn', '123', N'Nam', 1994, N'son.jpg', '0977777777', 'son@gmail.com', N'Bình Dương'),
(N'Bùi Minh Ngọc', '123', N'Nữ', 1996, N'ngoc.jpg', '0988888888', 'ngoc@gmail.com', N'TP.HCM'),
(N'Lâm Đức Anh', '123', N'Nam', 1993, N'anh.jpg', '0999999999', 'anh@gmail.com', N'Vũng Tàu'),
(N'Hoàng Phương Nhi', '123', N'Nữ', 2001, N'nhi.jpg', '0901010101', 'nhi@gmail.com', N'Huế');
INSERT INTO tblKhachHang (TenKH, MatKhau, GioiTinh, NamSinh, Avarta, DienThoai, Email, DiaChi) VALUES
 (N'Trần Quốc Hèo', '123', N'Nam', 1998, N'heo.jpg', '0912345678', 'kheo66688@gmail.com', N'Đà Nẵng'),
(N'Nguyễn Thị Nhẽ', '123', N'Nữ', 2000, N'nhe.jpg', '0934567890', 'bang189a@gmail.com' , N'Hà Nội'),
(N'Phạm Minh Queng', '123', N'Nam', 1995, N'qminh.jpg', '0977777777', 'qminh88327@gmail.com', N'Cần Thơ'),
(N'Lê Thuỳ Thenh', '123', N'Nữ', 2002, N'thenh.jpg', '0902222333', 'thanhnha0522@gmail.com', N'Bình Dương');
INSERT INTO tblKhachHang (TenKH, MatKhau, GioiTinh, NamSinh, Avarta, DienThoai, Email, DiaChi) VALUES
 (N'Trần Văn Jun', '123', N'Nam', 1998, N'heo.jpg', '0912345678', 'alizanha147@gmail.com', N'Mỹ Tho');

-- Hóa Đơn (10 dòng)
INSERT INTO tblHoaDon (MaKH, MaNV, NgayLap, TongTien, TinhTrang, BanSo, GhiChu, DaThanhToan) VALUES
(1, 2, GETDATE(), 275000, 4, 1, N'Đã thanh toán', 1),
(2, 3, GETDATE(), 195000, 3, 2, N'Đang phục vụ', 0),
(3, 4, GETDATE(), 95000, 2, 3, N'Chờ món chính', 0),
(4, 5, GETDATE(), 320000, 4, 4, N'Khách quen', 1),
(5, 6, GETDATE(), 90000, 4, 5, N'Mua mang về', 1),
(6, 7, GETDATE(), 180000, 3, 6, N'Đang phục vụ', 0),
(7, 8, GETDATE(), 55000, 1, 7, N'Đặt bàn trước', 0),
(8, 9, GETDATE(), 215000, 4, 8, N'Khách thanh toán thẻ', 1),
(9, 10, GETDATE(), 120000, 2, 9, N'Giao tận nơi', 0),
(10, 1, GETDATE(), 135000, 4, 10, N'Khách VIP', 1);

-- Chi Tiết Hóa Đơn
INSERT INTO tblChiTietHoaDon (MaHD, MaMon, SoLuong, DonGia) VALUES
(1, 1, 2, 25000),
(1, 2, 2, 55000),
(1, 5, 1, 35000),
(2, 4, 1, 95000),
(2, 10, 1, 90000),
(3, 9, 1, 120000),
(4, 3, 1, 180000),
(4, 7, 1, 65000),
(4, 5, 2, 35000),
(5, 6, 2, 40000),
(6, 7, 2, 65000),
(6, 1, 1, 25000),
(7, 2, 1, 55000),
(8, 9, 1, 120000),
(8, 10, 1, 90000),
(9, 3, 1, 180000),
(10, 4, 1, 95000),
(10, 5, 1, 35000);

------------ nhập liệu thêm cho ds món ăn -----------------
INSERT INTO tblMonAn (TenMon, DonGia, MoTa, AnhDaiDien, MaLoai, MaNCC) VALUES
(N'Súp Hải Sản', 45000, N'Khai vị nóng', 'sup_hs.jpg', 1, 2),

(N'Salad Trộn Dầu Giấm', 35000, N'Salad rau tươi', 'salad.jpg', 1, 3),

(N'Bánh Mì Bơ Tỏi', 30000, N'Mềm giòn thơm bơ', 'bmt.jpg', 1, 4),

(N'Gỏi Ngó Sen Tôm Thịt', 55000, N'Món khai vị truyền thống', 'ngo_sen.jpg', 1, 1),

(N'Chả Giò Hải Sản', 50000, N'Cuốn giòn nhân tôm', 'cha_gio_hs.jpg', 1, 2),

(N'Khoai Tây Chiên', 30000, N'Món nhẹ hợp mọi lứa tuổi', 'khoaitay.jpg', 1, 4),

(N'Heo Quay Giòn Bì', 120000, N'Thịt heo quay giòn rụm', 'heo_quay.jpg', 2, 6),

(N'Bò Sốt Tiêu Đen', 150000, N'Bò mềm sốt tiêu', 'bo_tieu.jpg', 2, 6),

(N'Mực Xào Sa Tế', 95000, N'Mực tươi cay nhẹ', 'muc_xao.jpg', 2, 2),

(N'Cơm Tấm Sườn Bì Chả', 65000, N'Sườn nướng thơm', 'comtam.jpg', 2, 1),

(N'Gà Sốt Mật Ong', 110000, N'Gà mềm sốt ngọt', 'ga_mat_ong.jpg', 2, 6),

(N'Vịt Quay Bắc Kinh', 180000, N'Món vịt da giòn', 'vit_quay.jpg', 2, 6),

(N'Cá Hồi Áp Chảo', 160000, N'Cá hồi Na Uy', 'ca_hoi.jpg', 2, 2),

(N'Rau Câu Dừa', 25000, N'Mềm mát', 'raucau.jpg', 3, 10),

(N'Bánh Flan Caramen', 20000, N'Flan mềm', 'flan.jpg', 3, 4),

(N'Sữa Chua Trái Cây', 30000, N'Chua ngọt', 'suachua.jpg', 3, 10),

(N'Kem Socola', 35000, N'Kem lạnh', 'kem_socola.jpg', 3, 10),

(N'Bánh flan Trứng', 25000, N'Món ngọt truyền thống', 'banhflan.jpg', 3, 4),

(N'Trà Sữa Trân Châu', 35000, N'Món quốc dân', 'trasua.jpg', 3, 5),

(N'Trà Đào Cam Sả', 35000, N'Thức uống hot trend', 'tra_dao.jpg', 4, 5),

(N'Cà Phê Sữa Đá', 25000, N'Đậm vị Việt', 'caphe_sua.jpg', 4, 5),

(N'Cam Vắt', 30000, N'Nước cam tươi', 'camvat.jpg', 4, 10),

(N'Lẩu Mắm', 200000, N'Lẩu miền Tây', 'laumam.jpg', 5, 9),

(N'Lẩu Cá Bớp', 180000, N'Lẩu hải sản', 'laubop.jpg', 5, 2),

(N'Lẩu Nấm Chay', 150000, N'Thanh đạm', 'launam.jpg', 5, 3),

(N'Lẩu Tôm Chua Cay', 175000, N'Kiểu Thái', 'lautom.jpg', 5, 2),

(N'Lẩu Gà Ớt Hiểm', 165000, N'Cay nhẹ', 'lauga.jpg', 5, 6),

(N'Lẩu Hải Sản Thập Cẩm', 220000, N'Tôm mực cá', 'lautc.jpg', 5, 2),

(N'Ba Chỉ Nướng Mọi', 120000, N'Ba chỉ heo nướng than', 'bachi_nuong.jpg', 6, 6),

(N'Gà Nướng Muối Ớt', 140000, N'Gà nướng cay nhẹ', 'ga_nuong_muoi.jpg', 6, 6),

(N'Cánh Gà Nướng Mật Ong', 110000, N'Cánh gà sốt ngọt', 'canhga_nuong.jpg', 6, 6),

(N'Hàu Nướng Mỡ Hành', 95000, N'Hàu tươi nướng', 'hau_nuong.jpg', 6, 2),

(N'Đậu Hũ Kho Nấm', 50000, N'Món chay truyền thống', 'dauhu_kho.jpg', 8, 3),

(N'Rau Củ Xào Thập Cẩm', 45000, N'Nhiều loại rau tươi', 'rau_xao.jpg', 8, 3),

(N'Bún Bò Huế Chay', 55000, N'Hương vị như thật', 'bunbo_chay.jpg', 8, 3),

(N'Cơm Gà Xối Mỡ', 65000, N'Gà giòn ngon', 'comga.jpg', 9, 6),

(N'Cơm Chiên Dương Châu', 55000, N'Cơm chiên nổi tiếng', 'com_duongchau.jpg', 9, 1),

(N'Cơm Sườn Nướng', 60000, N'Sườn nướng thơm', 'com_suon.jpg', 9, 1),

(N'Phở Bò Tái', 65000, N'Nước phở Bắc', 'pho_bo.jpg', 10, 1),

(N'Phở Gà', 55000, N'Gà ta', 'pho_ga.jpg', 10, 6),

(N'Bún Bò Huế', 60000, N'Đặc sản Huế', 'bunbo.jpg', 10, 1),

(N'Bún Riêu', 50000, N'Bún riêu cua', 'bunrieu.jpg', 10, 9),

(N'Bún Thịt Nướng', 55000, N'Thịt nướng thơm', 'bun_thitnuong.jpg', 10, 6),

(N'Mì Quảng', 55000, N'Mì Quảng thịt', 'mi_quang.jpg', 10, 9),

(N'Hủ Tiếu Nam Vang', 60000, N'Tôm thịt đầy đủ', 'hutieu.jpg', 10, 2);


INSERT INTO tblMonAn (TenMon, DonGia, MoTa, AnhDaiDien, MaLoai, MaNCC) VALUES
(N'Tôm Sú Nướng', 135000, N'Tôm nướng muối ớt', 'tom_nuong.jpg', 2, 2),

(N'Ếch Xào Sả Ớt', 95000, N'Món đồng quê', 'ech_xao.jpg', 2, 9),

(N'Mực Nướng Sa Tế', 140000, N'Mực tươi nướng', 'muc_nuong.jpg', 2, 2),

(N'Cá Lóc Hấp Bầu', 130000, N'Món dân dã', 'ca_loc.jpg', 2, 9),

(N'Heo Kho Tộ', 90000, N'Vị truyền thống', 'kho_to.jpg', 2, 6);
--Thêm mới
ALTER TABLE tblLienHe ADD SoSao INT DEFAULT 5;
ALTER TABLE tblLienHe ADD HinhAnhBinhLuan NVARCHAR(MAX); -- Lưu chuỗi Base64 hoặc link ảnh