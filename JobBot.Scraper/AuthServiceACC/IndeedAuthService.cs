using Microsoft.Playwright;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace JobBot.Scraper.Platforms
{
    public class IndeedAuthService : IPlatformAuthService
    {
        private readonly string _email;
        private readonly string _profilePath;

        public IndeedAuthService(string email, string profilePath)
        {
            _email = email;
            // مسار خاص بـ Indeed لفصل الجلسات
            _profilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, profilePath ?? "Sessions/Indeed");
        }

        public async Task<IBrowserContext> GetContextAsync(IPlaywright playwright)
        {
            if (!Directory.Exists(_profilePath)) Directory.CreateDirectory(_profilePath);

            var context = await playwright.Chromium.LaunchPersistentContextAsync(_profilePath, new BrowserTypeLaunchPersistentContextOptions
            {
                Headless = false,
                SlowMo = 50,
                Args = new[] { "--disable-blink-features=AutomationControlled", "--start-maximized" },
                IgnoreDefaultArgs = new[] { "--enable-automation" },
                ViewportSize = ViewportSize.NoViewport
            });

            var page = context.Pages.FirstOrDefault() ?? await context.NewPageAsync();

            // نذهب لصفحة رئيسية محايدة أولاً لفحص حالة الدخول
            Console.WriteLine("🌐 Checking Indeed login status...");
            await page.GotoAsync("https://eg.indeed.com/", new PageGotoOptions { WaitUntil = WaitUntilState.Load });

            if (await IsLoggedInAsync(page))
            {
                Console.WriteLine("✅ Already logged in to Indeed. Skipping login workflow.");
            }
            else
            {
                // نذهب لصفحة الدخول فقط إذا لم نكن مسجلين
                await page.GotoAsync("https://secure.indeed.com/auth");
                await PerformLoginWorkflowAsync(page);
            }

            return context;
        }
        public async Task<bool> IsLoggedInAsync(IPage page)
        {
            // Indeed يظهر أيقونة الملف الشخصي أو شريط البحث عند تسجيل الدخول
            try
            {
                var loggedInIndicator = page.Locator("#pwa-Nav-TopBar-UserMenu, .gnav-UserIcon, [aria-label*='Account']");
                return await loggedInIndicator.First.IsVisibleAsync(new() { Timeout = 5000 });
            }
            catch { return false; }
        }

        private async Task PerformLoginWorkflowAsync(IPage page)
        {
            Console.WriteLine("🔑 Attempting Physical Interaction...");

            var emailInput = page.Locator("input[type='email'], #ifl-InputFormField-3").First;

            // 1. الانتظار حتى يظهر الحقل
            await emailInput.WaitForAsync(new() { State = WaitForSelectorState.Visible });

            // 2. الضغط الفعلي لتحفيز الحقل
            await emailInput.ClickAsync(new LocatorClickOptions { Force = true });
            await Task.Delay(500);

            // 3. مسح الحقل يدوياً (Ctrl+A ثم Backspace)
            await page.Keyboard.DownAsync("Control");
            await page.Keyboard.PressAsync("a");
            await page.Keyboard.UpAsync("Control");
            await page.Keyboard.PressAsync("Backspace");

            // 4. الكتابة بسرعة بشرية (Delay عشوائي)
            foreach (var c in _email)
            {
                await page.Keyboard.TypeAsync(c.ToString());
                await Task.Delay(new Random().Next(100, 200));
            }

            // 5. الضغط على Enter بدلاً من زر الموقع
            await page.Keyboard.PressAsync("Enter");

            // 6. الانتظار اليدوي النهائي
            Console.WriteLine("⏳ Please finish OTP or Captcha in the browser window...");
            await page.WaitForSelectorAsync("[data-testid='gnav-account-container']", new() { Timeout = 300000 });
        }
    }
}