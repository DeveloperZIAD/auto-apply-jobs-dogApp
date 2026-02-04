using Microsoft.Playwright;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace JobBot.Scraper.Platforms
{
    public class LinkedInAuthService : IPlatformAuthService
    {
        private readonly string _email;
        private readonly string _password;
        private readonly string _profilePath;

        public LinkedInAuthService(string email, string password, string profilePath)
        {
            _email = email;
            _password = password;
            // توحيد المسار ليكون داخل مجلد LinkedIn
            _profilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, profilePath ?? "Sessions/LinkedIn");
        }

        public async Task<IBrowserContext> GetContextAsync(IPlaywright playwright)
        {
            if (!Directory.Exists(_profilePath)) Directory.CreateDirectory(_profilePath);

            var context = await playwright.Chromium.LaunchPersistentContextAsync(_profilePath, new BrowserTypeLaunchPersistentContextOptions
            {
                Headless = false,
                SlowMo = 100,
                Args = new[] {
                    "--disable-blink-features=AutomationControlled",
                    "--start-maximized"
                },
                IgnoreDefaultArgs = new[] { "--enable-automation" }
            });

            var page = context.Pages.FirstOrDefault() ?? await context.NewPageAsync();

            Console.WriteLine("🌐 Navigating to LinkedIn...");
            await page.GotoAsync("https://www.linkedin.com/feed/", new() { WaitUntil = WaitUntilState.Load });

            if (await IsLoggedInAsync(page))
            {
                Console.WriteLine("✅ Already logged in to LinkedIn.");
            }
            else
            {
                // إذا لم يكن مسجلاً، نقوم بعملية تسجيل الدخول
                await PerformLoginAsync(page);
            }

            return context;
        }

        public async Task<bool> IsLoggedInAsync(IPage page)
        {
            try
            {
                // في LinkedIn، وجود زر الـ "Me" أو شريط البحث يعني أنك مسجل دخول
                return await page.Locator(".global-nav__me-menu-trigger, #global-nav-typeahead").First.IsVisibleAsync(new() { Timeout = 5000 });
            }
            catch { return false; }
        }

        private async Task PerformLoginAsync(IPage page)
        {
            Console.WriteLine("🔐 Login required. Redirecting to Login page...");
            await page.GotoAsync("https://www.linkedin.com/login");

            try
            {
                await page.FillAsync("#username", _email);
                await page.FillAsync("#password", _password);
                await page.ClickAsync("button[type='submit']");

                // الانتظار للتحقق من النجاح أو الكابتشا
                Console.WriteLine("⏳ Checking for security checkpoints (CAPTCHA)...");

                // ننتظر حتى يختفي رابط تسجيل الدخول أو يظهر الـ Feed
                await page.WaitForURLAsync(url => url.Contains("/feed") || url.Contains("/checkpoint"), new() { Timeout = 60000 });

                if (page.Url.Contains("/checkpoint"))
                {
                    Console.WriteLine("⚠️ CAPTCHA Detected! Please solve it manually in the browser window.");
                    // ننتظر حتى يحل المستخدم الكابتشا ويصل للـ Feed
                    await page.WaitForURLAsync("**/feed/**", new() { Timeout = 300000 });
                }

                Console.WriteLine("✅ Login procedure completed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Login failed: {ex.Message}");
            }
        }
    }
}