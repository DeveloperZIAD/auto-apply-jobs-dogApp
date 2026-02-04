using Microsoft.Playwright;

public interface IPlatformAuthService
{
    // لاستعادة سياق المتصفح (المسجل فيه دخول مسبقاً)
    Task<IBrowserContext> GetContextAsync(IPlaywright playwright);

    // للتأكد من صلاحية الجلسة الحالية
    Task<bool> IsLoggedInAsync(IPage page);
}