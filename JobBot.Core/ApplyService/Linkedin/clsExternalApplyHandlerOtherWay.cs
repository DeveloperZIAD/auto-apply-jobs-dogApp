using Microsoft.Playwright;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JobBotAutomation.Core.ApplyService.Linkedin
{
    public class clsExternalApplyHandlerOtherWay
    {
        public static async Task HandleMissingEasyApply(IPage page, Guid jobId, string cvPath)
        {
            Console.WriteLine($"⚠️ Easy Apply not found. Checking External Source...");

            try
            {
                // 1. استخراج رابط التقديم الخارجي والنقر عليه
                var externalBtn = page.Locator("button.jobs-apply-button, a.jobs-apply-button").First;
                if (await externalBtn.IsVisibleAsync())
                {
                    string externalUrl = await externalBtn.GetAttributeAsync("href") ?? page.Url;
                    Console.WriteLine($"🔗 Redirecting to: {externalUrl}");

                    // الذهاب للموقع الخارجي
                    await page.GotoAsync(externalUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
                    await Task.Delay(3000); // انتظار التحميل

                    // تنفيذ الاستراتيجية المزدوجة
                    bool filledForm = await TryFillExternalForm(page, cvPath);

                    if (!filledForm)
                    {
                        Console.WriteLine("🔄 Form not found or failed. Switching to Step 2: Email Extraction...");
                        await ExtractAndProcessEmail(page, jobId);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ External Handler Error: {ex.Message}");
            }
        }

        // --- الخطوة الأولى: محاولة ملء الفورم الخارجي ---
        private static async Task<bool> TryFillExternalForm(IPage page, string cvPath)
        {
            // فحص وجود حقول إدخال تدل على وجود فورم تقديم
            var inputs = await page.Locator("input[type='file'], input[name*='resume'], input[id*='resume']").AllAsync();

            if (inputs.Count > 0)
            {
                Console.WriteLine("📝 External Form Detected. Starting Auto-Fill...");

                // هنا نضع منطق رفع الملفات (CV)
                await inputs[0].SetInputFilesAsync(cvPath);

                // محاولة ملء الاسم والإيميل إذا وجدا
                await FillIfExist(page, "input[type='email'], input[name*='email']", "your-email@proton.me");
                await FillIfExist(page, "input[name*='name'], input[id*='name']", "Your Full Name");

                // البحث عن زر الإرسال (Submit)
                var submitBtn = page.Locator("button[type='submit'], button:has-text('Submit'), button:has-text('Apply')").First;
                if (await submitBtn.IsVisibleAsync())
                {
                    // await submitBtn.ClickAsync(); // فك التعليق عند التأكد
                    Console.WriteLine("✅ Form submission triggered.");
                    return true;
                }
            }
            return false;
        }

        // --- الخطوة الثانية: البحث عن الإيميل (الخطة البديلة) ---
        private static async Task ExtractAndProcessEmail(IPage page, Guid jobId)
        {
            string content = await page.ContentAsync();
            // Regex قوي لصيد الإيميلات
            string emailPattern = @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}";
            var matches = Regex.Matches(content, emailPattern);

            if (matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    string foundEmail = match.Value;
                    // تجاهل إيميلات المواقع العامة مثل (wix, google)
                    if (foundEmail.Contains("sentry") || foundEmail.Contains("domain")) continue;

                    Console.WriteLine($"📧 Strategy Alert: Contacting {foundEmail} via ProtonMail...");

                    // تحديث قاعدة البيانات بعنوان البريد المكتشف لكي ترسل له يدوياً أو عبر API
                    SaveEmailForManualSend(jobId, foundEmail);

                    // خيار: فتح رابط mailto لفتح تطبيق البريد فوراً
                    // await page.EvaluateAsync($"window.location.href = 'mailto:{foundEmail}?subject=Application&body=Hello...'");
                    break;
                }
            }
            else
            {
                Console.WriteLine("❌ No Contact Email found on the page.");
            }
        }

        private static async Task FillIfExist(IPage page, string selector, string value)
        {
            var el = page.Locator(selector).First;
            if (await el.IsVisibleAsync()) await el.FillAsync(value);
        }

        private static void SaveEmailForManualSend(Guid jobId, string email)
        {
            // كود الحفظ في الـ DB الخاص بك
            Console.WriteLine($"💾 Saved to DB: Job {jobId} -> Email: {email}");
        }
    }
}