/**
 * Cấu hình Tailwind CSS cho TRANG KHÁCH (Views/Shared/_Layout.cshtml).
 * Thay cho Tailwind CDN (cdn.tailwindcss.com) - CDN dựng CSS ngay trong trình duyệt, rất chậm trên điện thoại.
 *
 * ⚠️ Mỗi khi THÊM class Tailwind MỚI vào file .cshtml phải dựng lại CSS:
 *     Windows:  powershell -ExecutionPolicy Bypass -File Scripts\build-css.ps1
 *     (hoặc)    npx tailwindcss@3.4.17 -c tailwind.config.js -i Styles/tailwind.input.css -o wwwroot/css/tailwind.min.css --minify
 * Màu và font giữ nguyên như cấu hình CDN cũ.
 */
module.exports = {
  content: [
    './Views/**/*.cshtml',
    './wwwroot/js/**/*.js',
    './Controllers/**/*.cs',
    './ViewModels/**/*.cs',
    './Core/**/*.cs',
  ],
  // Class xuất hiện trong nội dung lưu ở database (bài viết, mô tả sản phẩm) - không có trong code nên phải khai báo.
  safelist: [],
  theme: {
    extend: {
      colors: {
        hnkgreen: '#007A29',
        hnkdeep: '#003110',
        hnkred: '#E31B23',
        hnkbody: '#F4F4F4',
        hnksub: '#E5E8E7',
        hnkfooter: '#E0E2E1',
        hnktext: '#222222',
        hnkmuted: '#666666',
        // Compatibility aliases
        thaigold: '#007A29',
        thaiamber: '#007A29',
        thaidark: '#FFFFFF',
        thaicard: '#FFFFFF',
        thaimuted: '#F4F4F4',
      },
      fontFamily: {
        sans: ['"Plus Jakarta Sans"', 'sans-serif'],
      },
    },
  },
};
