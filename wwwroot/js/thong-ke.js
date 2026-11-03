/*
 * Thống kê truy cập ThaiBeer - gửi lượt xem trang và thời gian ở lại về /api/thong-ke.
 * - Hỗ trợ Turbo Drive: mỗi lần chuyển trang (turbo:load) là 1 lượt xem mới.
 * - Thời gian ở lại chỉ tính lúc tab ĐANG HIỂN THỊ (ẩn tab / thu nhỏ thì dừng đếm).
 * - Mã khách & mã phiên là GUID ngẫu nhiên, ẩn danh; phiên hết hạn sau 30 phút không hoạt động.
 */
(function () {
    "use strict";
    if (window.__thaiBeerThongKe) return;
    window.__thaiBeerThongKe = true;

    var API = "/api/thong-ke";
    var HET_HAN_PHIEN_MS = 30 * 60 * 1000;
    var NHIP_GUI_MS = 15000;

    function taoGuid() {
        if (window.crypto && crypto.randomUUID) return crypto.randomUUID();
        return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, function (c) {
            var r = (Math.random() * 16) | 0;
            return (c === "x" ? r : (r & 0x3) | 0x8).toString(16);
        });
    }
    function doc(k) { try { return localStorage.getItem(k); } catch (e) { return null; } }
    function ghi(k, v) { try { localStorage.setItem(k, v); } catch (e) { } }

    var maKhach = doc("tb_mk");
    if (!maKhach) { maKhach = taoGuid(); ghi("tb_mk", maKhach); }

    var maPhien = null;
    var phienMoi = false;
    function layMaPhien() {
        var bayGio = Date.now();
        var mp = maPhien || doc("tb_mp");
        var t = parseInt(doc("tb_mp_t") || "0", 10);
        phienMoi = false;
        if (!mp || bayGio - t > HET_HAN_PHIEN_MS) { mp = taoGuid(); phienMoi = true; }
        maPhien = mp;
        ghi("tb_mp", mp);
        ghi("tb_mp_t", String(bayGio));
        return mp;
    }
    function giaHanPhien() { ghi("tb_mp_t", String(Date.now())); }

    // ---- Trạng thái lượt xem hiện tại ----
    var hienTai = null; // { href, id, msDaXem, hienTu, daGuiGiay }
    var dangCho = null; // promise lấy id

    function laTrangQuanTri() {
        var p = location.pathname.toLowerCase();
        return p.indexOf("/quan-tri") === 0 || p.indexOf("/adminthaibeer") === 0;
    }

    function giayDaXem(lx) {
        var ms = lx.msDaXem + (lx.hienTu ? Date.now() - lx.hienTu : 0);
        return Math.round(ms / 1000);
    }

    function guiThoiGian(lx, dungBeacon) {
        if (!lx || !lx.id) return;
        var giay = giayDaXem(lx);
        if (giay <= lx.daGuiGiay) return;
        lx.daGuiGiay = giay;
        var body = JSON.stringify({ id: lx.id, maPhien: maPhien, giay: giay });
        if (dungBeacon && navigator.sendBeacon) {
            navigator.sendBeacon(API + "/thoi-gian", new Blob([body], { type: "application/json" }));
        } else {
            fetch(API + "/thoi-gian", { method: "POST", headers: { "Content-Type": "application/json" }, body: body, keepalive: true }).catch(function () { });
        }
        giaHanPhien();
    }

    function ketThucTrang(dungBeacon) {
        if (!hienTai) return;
        var lx = hienTai;
        if (lx.hienTu) { lx.msDaXem += Date.now() - lx.hienTu; lx.hienTu = null; }
        if (lx.id) guiThoiGian(lx, dungBeacon);
        else if (dangCho) dangCho.then(function () { guiThoiGian(lx, false); });
        hienTai = null;
    }

    function batDauTrang() {
        if (laTrangQuanTri()) return;
        if (hienTai && hienTai.href === location.href) return; // chống đếm trùng (DOMContentLoaded + turbo:load)
        ketThucTrang(false);

        var mp = layMaPhien();
        var lx = { href: location.href, id: null, msDaXem: 0, hienTu: document.visibilityState === "visible" ? Date.now() : null, daGuiGiay: 0 };
        hienTai = lx;

        var duLieu = {
            maKhach: maKhach,
            maPhien: mp,
            duongDan: location.pathname + location.search,
            tieuDe: document.title,
            nguonGioiThieu: phienMoi ? document.referrer : null,
            manHinh: screen.width + "x" + screen.height
        };
        dangCho = fetch(API + "/luot-xem", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(duLieu),
            keepalive: true
        }).then(function (r) { return r.status === 200 ? r.json() : null; })
          .then(function (kq) { if (kq && kq.id) lx.id = kq.id; })
          .catch(function () { });
    }

    // Nhịp gửi định kỳ khi đang xem (đề phòng trình duyệt đóng đột ngột không kịp gửi)
    setInterval(function () {
        if (hienTai && hienTai.hienTu && document.visibilityState === "visible") guiThoiGian(hienTai, false);
    }, NHIP_GUI_MS);

    document.addEventListener("visibilitychange", function () {
        if (!hienTai) return;
        if (document.visibilityState === "hidden") {
            if (hienTai.hienTu) { hienTai.msDaXem += Date.now() - hienTai.hienTu; hienTai.hienTu = null; }
            guiThoiGian(hienTai, true);
        } else if (!hienTai.hienTu) {
            hienTai.hienTu = Date.now();
            giaHanPhien();
        }
    });
    window.addEventListener("pagehide", function () { ketThucTrang(true); });
    window.addEventListener("pageshow", function (e) { if (e.persisted) batDauTrang(); }); // quay lại bằng nút Back (bfcache)

    // Turbo Drive: rời trang hiện tại -> chốt thời gian; trang mới hiển thị -> lượt xem mới
    document.addEventListener("turbo:before-visit", function () { ketThucTrang(false); });
    document.addEventListener("turbo:load", batDauTrang);

    // Không có Turbo (hoặc Turbo nạp lỗi) vẫn đếm được trang đầu tiên
    if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", batDauTrang);
    else batDauTrang();
})();
