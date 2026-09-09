namespace ThaiBeer.Core.Interfaces;

public interface ICacheInvalidatorService
{
    Task LamMoiTrangChuAsync();
    Task LamMoiSanPhamAsync();
    Task LamMoiTinTucAsync();
    Task LamMoiToanBoAsync();
}
