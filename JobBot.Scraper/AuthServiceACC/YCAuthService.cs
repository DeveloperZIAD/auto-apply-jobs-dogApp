using Microsoft.Playwright;
using System;
using System.IO;
using System.Threading.Tasks;

public class YCAuthService
{
    private readonly string _profilePath;
    private readonly string _loginUrl = "https://www.workatastartup.com/inc/login";

    public YCAuthService(string? yCombinatormail, string? yCombinatorpassword, string? yCombinatorProfilePath)
    {
        _profilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sessions/YCombinator");
    }

    public async Task<IBrowserContext> GetContextAsync(IPlaywright playwright)
    {
        if (!Directory.Exists(_profilePath)) Directory.CreateDirectory(_profilePath);

        var context = await playwright.Chromium.LaunchPersistentContextAsync(_profilePath, new()
        {
            Headless = false, // نتركه ظاهراً في البداية لتجاوز أي حماية يدوياً
            Args = new[] { "--disable-blink-features=AutomationControlled" }
        });

        var page = context.Pages[0];
        await page.GotoAsync("https://www.workatastartup.com/jobs");

        // التحقق من تسجيل الدخول (عادة يظهر زر 'Log out' أو أيقونة بروفايل)
        bool isLoggedIn = await page.Locator("a:has-text('Log out'), .user-profile").First.IsVisibleAsync(new() { Timeout = 5000 });

        if (!isLoggedIn)
        {
            Console.WriteLine("🔑 YC: مطلوب تسجيل الدخول. يرجى إتمام العملية يدوياً...");
            await page.GotoAsync(_loginUrl);

            // انتظر حتى ينجح المستخدم في الوصول لصفحة الوظائف
            await page.WaitForURLAsync(url => url.Contains("/jobs"), new() { Timeout = 0 });
            Console.WriteLine("✅ YC: تم حفظ الجلسة.");
        }

        return context;
    }
}