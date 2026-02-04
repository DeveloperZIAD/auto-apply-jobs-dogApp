using Microsoft.Playwright;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JobBot.Scraper.Iface;

namespace JobBot.Scraper.Platforms
{
    public class ArcDevAuthService : IPlatformAuthService
    {
        private readonly string _email;
        private readonly string _password;
        private readonly string _profilePath;

        public ArcDevAuthService(string email, string password, string profilePath)
        {
            _email = email;
            _password = password;
            _profilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sessions/ArcDev");
        }

        public async Task<IBrowserContext> GetContextAsync(IPlaywright playwright)
        {
            if (!Directory.Exists(_profilePath)) Directory.CreateDirectory(_profilePath);

            var context = await playwright.Chromium.LaunchPersistentContextAsync(_profilePath, new BrowserTypeLaunchPersistentContextOptions
            {
                Headless = false,
                SlowMo = 150,
                Args = new[] {
            "--disable-blink-features=AutomationControlled", // أهم سطر لتخفي الهوية
            "--start-maximized",
            "--no-sandbox"
        },
                ViewportSize = ViewportSize.NoViewport,
                // إضافة User Agent حقيقي لتجنب الحظر
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
            });

            var page = context.Pages.FirstOrDefault() ?? await context.NewPageAsync();

            // 1. الذهاب للـ Dashboard مباشرة
            await page.GotoAsync("https://arc.dev/dashboard", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

            // 2. التحقق من حالة الدخول
            if (page.Url.Contains("/login"))
            {
                Console.WriteLine("🔑 ArcDev: Login needed. Injecting credentials...");

                try
                {
                    // Arc.dev قد يستخدم أسماء حقول معقدة، نستخدم التحديد المرن
                    var emailInput = page.Locator("input[type='email'], input[name='email']").First;
                    var passwordInput = page.Locator("input[type='password'], input[name='password']").First;

                    await emailInput.WaitForAsync(new() { State = WaitForSelectorState.Visible });

                    // ملء البيانات بمحاكاة بشرية
                    await emailInput.FillAsync(_email);
                    await Task.Delay(500);
                    await passwordInput.FillAsync(_password);

                    // الضغط على زر الدخول
                    var loginBtn = page.Locator("button[type='submit'], button:has-text('Log in')").First;
                    await loginBtn.ClickAsync();

                    // 3. الانتظار الحاسم: ننتظر حتى يختفي رابط اللوجن ويظهر رابط الداشبورد
                    await page.WaitForURLAsync(url => url.Contains("/dashboard"), new() { Timeout = 30000 });
                    Console.WriteLine("✅ ArcDev: Login successful.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ ArcDev: Failed to complete login flow: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("✅ ArcDev: Session active. Skipping login injection.");
            }

            return context;
        }
        public async Task<bool> IsLoggedInAsync(IPage page)
        {
            // فحص بسيط: إذا كان الرابط يحتوي على كلمة login فنحن لسنا مسجلين
            if (page.Url.Contains("/login")) return false;

            // فحص وجود أي عنصر يدل على الدخول (مثل زر Logout أو Profile)
            try
            {
                var profileIcon = page.Locator("img[class*='avatar'], .user-menu, a[href*='settings']").First;
                return await profileIcon.IsVisibleAsync(new() { Timeout = 5000 });
            }
            catch
            {
                return false;
            }
        }
       
    }
}