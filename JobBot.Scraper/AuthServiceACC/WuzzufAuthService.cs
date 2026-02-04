using Microsoft.Playwright;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public class WuzzufAuthService
{
    private readonly string _profilePath;

    public WuzzufAuthService()
    {
        // تحديد مسار حفظ الجلسة (Cookies & Storage)
        _profilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sessions/Wuzzuf");
    }

    /// <summary>
    /// هذه الميثود تقوم بفتح المتصفح وتنتظر المستخدم حتى يسجل دخوله يدوياً بنجاح.
    /// </summary>
    public async Task<IBrowserContext> GetContextAsync(IPlaywright playwright)
    {
        if (!Directory.Exists(_profilePath)) Directory.CreateDirectory(_profilePath);

        // إعدادات المتصفح لتقليل كشف الأتمتة
        var context = await playwright.Chromium.LaunchPersistentContextAsync(_profilePath, new()
        {
            Headless = false, // يجب أن يكون المتصفح ظاهراً لتتمكن من حل المشاكل يدوياً
            Args = new[] {
                "--disable-blink-features=AutomationControlled",
                "--start-maximized"
            },
            ViewportSize = ViewportSize.NoViewport
        });

        var page = context.Pages.FirstOrDefault() ?? await context.NewPageAsync();

        // إخفاء بصمة البوت برمجياً
        await page.AddInitScriptAsync(@"Object.defineProperty(navigator, 'webdriver', { get: () => undefined });");

        await page.GotoAsync("https://wuzzuf.net/login");

        // التحقق مما إذا كانت الجلسة السابقة لا تزال نشطة
        if (await IsLoggedInAsync(page))
        {
            Console.WriteLine("✅ Wuzzuf: تم استعادة الجلسة بنجاح، أنت مسجل دخول بالفعل.");
        }
        else
        {
            Console.WriteLine("⚠️ تنبيه أمني: يرجى تسجيل الدخول يدوياً وحل الكابتشا في نافذة المتصفح.");
            Console.WriteLine("⚠️ إذا ظهرت رسالة جوجل 'Account Recovery'، قم بحلها بيدك.");

            try
            {
                // سينتظر البوت هنا للأبد (Timeout = 0) حتى تنجح في الدخول وتصل لصفحة الوظائف أو الحساب الشخصي
                await page.WaitForURLAsync(url =>
                    url.Contains("/jobs") ||
                    url.Contains("/me") ||
                    url.Contains("/dashboard") ||
                    url.Contains("/search"),
                    new() { Timeout = 0 });

                Console.WriteLine("✨ ممتاز! تم كشف تسجيل الدخول بنجاح. سيتم حفظ الجلسة الآن.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ حدث خطأ أثناء الانتظار: {ex.Message}");
            }
        }

        return context;
    }

    public async Task<bool> IsLoggedInAsync(IPage page)
    {
        try
        {
            // التحقق من وجود عناصر تظهر فقط بعد تسجيل الدخول (مثل قائمة المستخدم)
            var userMenu = page.Locator("button[class*='UserMenu'], .css-169822a").First;
            return await userMenu.IsVisibleAsync(new() { Timeout = 5000 });
        }
        catch
        {
            return false;
        }
    }
}