using Microsoft.AspNetCore.Http;

namespace ThaiBeer.Core.Interfaces;

public interface IImageOptimizerService
{
    Task<string> ToiUuVaLuuWebPAsync(IFormFile tapTin, string thuMucCon = "san-pham", int? chieuRongToiDa = 1200, int chatLuong = 82);
    void XoaHinhAnh(string? duongDanTuongDoi);
}
