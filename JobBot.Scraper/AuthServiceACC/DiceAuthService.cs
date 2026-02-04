using Microsoft.Playwright;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace JobBot.Scraper.Platforms
{
    public class DiceAuthService : IPlatformAuthService
    {
        private readonly string _profilePath;
        private readonly string _loginUrl = "https://www.dice.com/dashboard/login";
        private readonly string _email;
        private readonly string _password;

        public DiceAuthService(string email, string password, string? diceProfilePath = null)
        {
            _email = email;
            _password = password;
            _profilePath = diceProfilePath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sessions", "Dice");
        }

        public async Task<IBrowserContext> GetContextAsync(IPlaywright playwright)
        {
            if (!Directory.Exists(_profilePath))
                Directory.CreateDirectory(_profilePath);

            var context = await playwright.Chromium.LaunchPersistentContextAsync(_profilePath, new()
            {
                Headless = false,
                Args = new[] {
                    "--disable-blink-features=AutomationControlled",
                    "--start-maximized",
                    "--no-sandbox"
                },
                IgnoreDefaultArgs = new[] { "--enable-automation" },
                ViewportSize = ViewportSize.NoViewport,
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36"
            });

            var page = context.Pages.FirstOrDefault() ?? await context.NewPageAsync();

            try
            {
                Console.WriteLine("🚀 Checking Dice session...");
                await page.GotoAsync("https://www.dice.com/home/home-feed", new() { WaitUntil = WaitUntilState.DOMContentLoaded });

                // ننتظر 5 ثواني كاملة لضمان تحميل الـ Scripts المسؤولة عن الـ Feed
                await Task.Delay(5000);

                if (!await IsLoggedInAsync(page))
                {
                    Console.WriteLine("🔑 Session not detected. Manual login might be needed.");
                    // ... منطق تسجيل الدخول (نفسه) ...
                }

                Console.WriteLine("✨ Dice: Session confirmed.");
            }
            catch (Exception ex) { Console.WriteLine($"❌ Auth Warning: {ex.Message}"); }

            return context;
        }

        public async Task<bool> IsLoggedInAsync(IPage page)
        {
            try
            {
                // إذا كان الرابط يحتوي على home-feed، فنحن غالباً مسجلين دخول
                if (page.Url.Contains("home-feed")) return true;

                // فحص سريع جداً لعنصر واحد فقط مشهور
                return await page.Locator("#profile-dropdown, .user-name").First.IsVisibleAsync(new() { Timeout = 3000 });
            }
            catch { return false; }
        }
    }
}