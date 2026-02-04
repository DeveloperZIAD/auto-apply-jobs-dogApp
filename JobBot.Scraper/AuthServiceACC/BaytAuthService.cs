using Microsoft.Playwright;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JobBot.Scraper.Iface;

namespace JobBot.Scraper.Platforms
{
    public class BaytAuthService : IPlatformAuthService
    {
        private readonly string _email;
        private readonly string _password;
        private readonly string _profilePath;

        public BaytAuthService(string email, string password, string profilePath)
        {
            _email = email;
            _password = password;

            // تصحيح خطأ المسار: التأكد من عدم استخدام رابط URL كمجلد
            string safePath = (string.IsNullOrEmpty(profilePath) || profilePath.Contains("http"))
                              ? "Sessions/Bayt"
                              : profilePath;

            _profilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, safePath);
        }

        public async Task<IBrowserContext> GetContextAsync(IPlaywright playwright)
        {
            // 1. التأكد من وجود مجلد البروفايل لحفظ الجلسة
            if (!Directory.Exists(_profilePath)) Directory.CreateDirectory(_profilePath);

            // 2. فتح متصفح "مستمر" (Persistent) يحفظ الكوكيز والملفات
            var context = await playwright.Chromium.LaunchPersistentContextAsync(_profilePath, new BrowserTypeLaunchPersistentContextOptions
            {
                Headless = false,
                SlowMo = 50,
                Args = new[] {
            "--disable-blink-features=AutomationControlled", // لتجنب اكتشاف البوت
            "--start-maximized"
        },
                ViewportSize = ViewportSize.NoViewport
            });

            var page = context.Pages.FirstOrDefault() ?? await context.NewPageAsync();

            // 3. الذهاب لصفحة "مساحة العمل" مباشرة
            // إذا كان مسجل دخول، سيفتحها. إذا لم يكن، الموقع سيعيد توجيهه تلقائياً لصفحة الـ Login
            Console.WriteLine("🔍 Checking Bayt session via workspace URL...");
            await page.GotoAsync("https://www.bayt.com/en/myworkspace-j/", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 30000
            });

            // 4. الفحص الذهبي: هل الرابط الحالي هو رابط مساحة العمل؟
            // نستخدم StringComparison لتجنب مشاكل الحروف الكبيرة والصغيرة
            if (page.Url.Contains("myworkspace-j", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("✅ Bayt: Session active (Confirmed by Workspace URL).");
            }
            else
            {
                // إذا قام الموقع بتحويلنا (Redirect) لصفحة الـ login أو أي صفحة أخرى
                Console.WriteLine("🔑 Bayt: Redirected or session expired. Starting Login flow...");

                // الانتقال لصفحة تسجيل الدخول إذا لم نكن فيها بالفعل
                if (!page.Url.Contains("/login/"))
                {
                    await page.GotoAsync("https://www.bayt.com/en/login/", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
                }

                await PerformLoginAsync(page);

                // تأكيد أخير بعد الدخول
                try
                {
                    await page.WaitForURLAsync("**/myworkspace-j/**", new() { Timeout = 15000 });
                    Console.WriteLine("✅ Bayt: Login successful, reached Workspace.");
                }
                catch
                {
                    Console.WriteLine("⚠️ Bayt: Reached a different page after login. Please check manually.");
                }
            }

            return context;
        }
        private async Task PerformLoginAsync(IPage page)
        {
            try
            {
                Console.WriteLine("⌨️ Bayt: Found exact fields. Starting injection...");

                // 1. استخدام الـ IDs الصريحة من الكود الذي أرسلته
                var usernameSelector = "#LoginForm_username";
                var passwordSelector = "#LoginForm_password";
                var loginBtnSelector = "#login-button";

                // الانتظار للتأكد من وجود الحقل
                await page.WaitForSelectorAsync(usernameSelector, new() { State = WaitForSelectorState.Visible, Timeout = 15000 });

                // 2. إدخال الإيميل (بأسلوب يحاكي الواقع)
                await page.FocusAsync(usernameSelector);
                await page.FillAsync(usernameSelector, _email);
                Console.WriteLine("✅ Username field filled.");

                // 3. إدخال الباسورد
                await page.FocusAsync(passwordSelector);
                await page.FillAsync(passwordSelector, _password);
                Console.WriteLine("✅ Password field filled.");

                // 4. التأكد من علامة "Stay logged in"
                await page.CheckAsync("#LoginForm_rememberMe");

                // 5. الضغط على الزر (استخدام ID الصريح)
                await page.ClickAsync(loginBtnSelector);
                Console.WriteLine("🚀 Login button clicked via ID.");

                // 6. الانتظار حتى النجاح
                await page.WaitForURLAsync(url => !url.Contains("login"), new() { Timeout = 30000 });
                Console.WriteLine("✨ Bayt: Authentication Complete!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine("🚨 Please solve any Captcha manually if it appeared.");
                // انتظار يدوي أخير إذا فشل كل شيء
                await page.WaitForSelectorAsync("a[data-unconfirmed-action='logout']", new() { Timeout = 60000 });
            }
        }
        public async Task<bool> IsLoggedInAsync(IPage page)
        {
            // فحص الرابط أو وجود اسم المستخدم "ziad Mohamed" الظاهر في الصورة
            return page.Url.Contains("myworkspace") ||
                   page.Url.Contains("dashboard") ||
                   await page.Locator(".is-logged-in, a[data-unconfirmed-action='logout']").First.IsVisibleAsync();
        }
    }
}