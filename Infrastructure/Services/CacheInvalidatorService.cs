using Microsoft.AspNetCore.OutputCaching;
using ThaiBeer.Core.Interfaces;

namespace ThaiBeer.Infrastructure.Services;

public class CacheInvalidatorService : ICacheInvalidatorService
{
    private readonly IOutputCacheStore _cacheStore;

    public CacheInvalidatorService(IOutputCacheStore cacheStore)
    {
        _cacheStore = cacheStore;
    }

    public async Task LamMoiTrangChuAsync()
    {
        await _cacheStore.EvictByTagAsync("trang_chu_tag", default);
    }

    public async Task LamMoiSanPhamAsync()
    {
        await _cacheStore.EvictByTagAsync("san_pham_tag", default);
        // Khi sản phẩm thay đổi thì trang chủ cũng cần cập nhật sản phẩm nổi bật
        await _cacheStore.EvictByTagAsync("trang_chu_tag", default);
    }

    public async Task LamMoiTinTucAsync()
    {
        await _cacheStore.EvictByTagAsync("tin_tuc_tag", default);
        await _cacheStore.EvictByTagAsync("trang_chu_tag", default);
    }

    public async Task LamMoiToanBoAsync()
    {
        await _cacheStore.EvictByTagAsync("trang_chu_tag", default);
        await _cacheStore.EvictByTagAsync("san_pham_tag", default);
        await _cacheStore.EvictByTagAsync("tin_tuc_tag", default);
    }
}
