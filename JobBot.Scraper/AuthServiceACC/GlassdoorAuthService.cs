using Microsoft.Playwright;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JobBot.Scraper.Iface;

namespace JobBot.Scraper.Platforms
{
    public class GlassdoorAuthService : IPlatformAuthService
    {
        private readonly string _email;
        private readonly string _password;
        private readonly string _profilePath;

        public GlassdoorAuthService(string email, string password, string profilePath)
        {
            _email = email;
            _password = password;
            // استخدام مسار ثابت للبروفايل لضمان حفظ الجلسة وعدم تكرار تسجيل الدخول
            _profilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sessions/Glassdoor");
        }

        public async Task<IBrowserContext> GetContextAsync(IPlaywright playwright)
        {
            if (!Directory.Exists(_profilePath)) Directory.CreateDirectory(_profilePath);

            var context = await playwright.Chromium.LaunchPersistentContextAsync(_profilePath, new BrowserTypeLaunchPersistentContextOptions
            {
                Headless = false,
                SlowMo = 150, // إبطاء العمل لتجنب الـ Detection
                Args = new[] {
            "--disable-blink-features=AutomationControlled",
            "--start-maximized"
        },
                ViewportSize = ViewportSize.NoViewport,
                // تحديث الـ UserAgent لنسخة حديثة جداً
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36"
            });

            var page = context.Pages.FirstOrDefault() ?? await context.NewPageAsync();

            try
            {
                Console.WriteLine("🌐 Glassdoor: Navigating to homepage...");
                // الذهاب للصفحة الرئيسية أولاً لتجنب الحظر
                await page.GotoAsync("https://www.glassdoor.com/index.htm", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60000 });

                // فحص: هل نحن داخل الحساب؟ (عن طريق البحث عن أيقونة البروفايل)
                var isLoggedIn = await page.Locator("[data-test='nav-profile-icon'], .UserMenu").IsVisibleAsync(new() { Timeout = 5000 });

                if (isLoggedIn)
                {
                    Console.WriteLine("✅ Glassdoor: Session active.");
                    return context;
                }

                Console.WriteLine("🔑 Glassdoor: Redirecting to login page...");
                await page.GotoAsync("https://www.glassdoor.com/profile/login_input.htm");

                // تنفيذ منطق الزر الموحد (Unified Auth) الذي أشرت إليه
                var unifiedBtn = page.Locator("[data-test='unified-auth-indeed-button']");
                if (await unifiedBtn.IsVisibleAsync(new() { Timeout = 5000 }))
                {
                    await unifiedBtn.ClickAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Glassdoor Load Issue: {ex.Message}. Attempting to continue...");
            }

            return context;
        }
        public async Task<bool> IsLoggedInAsync(IPage page)
        {
            // فحص الرابط وفحص وجود أيقونة الملف الشخصي
            return !page.Url.Contains("login") &&
                   (await page.Locator("[data-test='nav-profile-icon'], .UserMenu, a[href*='/member/profile']").First.IsVisibleAsync(new() { Timeout = 5000 }));
        }
    }
}