using BusinessLogic;
using JobBot.Scraper.Iface;
using Microsoft.Playwright;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DataLogic;

namespace JobBot.Core.Services
{
    public class GlassdoorApplyService : IApplyService
    {
        public string PlatformName => "Glassdoor";
        private readonly Random _random = new Random();
        private readonly IPlatformAuthService _authService;

        // Constructor: نقوم بحقن خدمة الـ Auth هنا
        public GlassdoorApplyService(IPlatformAuthService authService)
        {
            _authService = authService;
        }

        public async Task<bool> ApplyAsync(JobPostingDto job, string email, string password)
        {
            using var playwright = await Playwright.CreateAsync();
            Console.WriteLine("🔐 Glassdoor: Requesting authenticated context...");

            // ملاحظة: لا تستخدم await using هنا إذا كان الـ AuthService هو من يدير دورة حياة المتصفح
            var context = await _authService.GetContextAsync(playwright);
            var page = context.Pages.FirstOrDefault() ?? await context.NewPageAsync();

            try
            {
                Console.WriteLine($"🚀 Navigating to Glassdoor Job: {job.JobTitle}");
                await page.GotoAsync(job.SourceUrl, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60000 });

                // انتظر حتى تظهر أزرار التقديم
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // Selector مخصص لـ Easy Apply (التقديم الداخلي)
                // Glassdoor يستخدم غالباً "Easy Apply" أو زر بلون أخضر/مختلف للتقديم الداخلي
                var easyApplyBtn = page.Locator("button:has-text('Easy Apply'), button:has-text('Apply Now')").First;

                if (await easyApplyBtn.IsVisibleAsync(new() { Timeout = 10000 }))
                {
                    Console.WriteLine("🖱️ Internal Apply Button Found. Starting Application...");
                    await easyApplyBtn.ClickAsync();

                    // هنا نستدعي الميثود التي ستتعامل مع النوافذ المنبثقة (Popups)
                    return await HandleEasyApplyFlow(page);
                }

                Console.WriteLine("⚠️ This job seems to be an External Link. Skipping (Easy Apply Only mode).");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Glassdoor Apply Error: {ex.Message}");
                return false;
            }
        }
        private async Task<bool> HandleEasyApplyFlow(IPage page)
        {
            try
            {
                // انتظر ظهور الـ Modal الخاص بالتقديم
                await page.WaitForSelectorAsync(".JobApplicationModal, [data-test='modal-content']", new() { Timeout = 10000 });

                bool isApplying = true;
                int steps = 0;

                while (isApplying && steps < 10) // حد أقصى 10 خطوات للتقديم
                {
                    steps++;
                    // ابحث عن أزرار الاستمرار
                    var nextBtn = page.Locator("button:has-text('Next'), button:has-text('Continue'), button:has-text('Submit Application')").Last;

                    if (await nextBtn.IsVisibleAsync())
                    {
                        await nextBtn.ClickAsync();
                        await Task.Delay(2000); // انتظر تحميل الخطوة التالية

                        // إذا تغير النص إلى "Done" أو ظهرت رسالة نجاح
                        if (await page.Locator("text=Application Submitted, text=Success").First.IsVisibleAsync())
                        {
                            Console.WriteLine("✅ Application Sent Successfully!");
                            return true;
                        }
                    }
                    else
                    {
                        isApplying = false;
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
        private async Task<bool> HandleApplicationFlow(IPage page, Guid jobId)
        {
            // محاولة إكمال حتى 6 خطوات (Resume -> Questions -> Review -> Submit)
            for (int i = 0; i < 6; i++)
            {
                // التحقق من رسائل النجاح
                var successSelectors = new[] { "text=Application Sent", "text=Thank you for applying", "text=Success" };
                foreach (var selector in successSelectors)
                {
                    if (await page.Locator(selector).IsVisibleAsync())
                    {
                        Console.WriteLine("✨ Glassdoor Application Submitted Successfully!");
                        return true;
                    }
                }

                // ملء الأسئلة في الخطوة الحالية
                await FillCurrentStepQuestions(page, jobId);

                // البحث عن زر الاستمرار (Next / Continue / Submit)
                var nextBtn = page.Locator("button:has-text('Continue'), button:has-text('Next'), button:has-text('Submit Application'), [data-test='footer-next']").First;

                if (await nextBtn.IsVisibleAsync() && await nextBtn.IsEnabledAsync())
                {
                    await nextBtn.HoverAsync();
                    await nextBtn.ClickAsync();
                    await RandomDelay(3000, 5000); // انتظار تحميل الخطوة التالية
                }
                else
                {
                    // إذا لم نجد زر "التالي" ولم نجد رسالة نجاح، ربما انتهت الخطوات
                    break;
                }
            }
            return false;
        }

        private async Task FillCurrentStepQuestions(IPage page, Guid jobId)
        {
            // استهداف جميع أنواع الحقول الممكنة
            var fields = await page.Locator("input, select, textarea").AllAsync();

            foreach (var field in fields)
            {
                try
                {
                    // محاولة إيجاد الـ Label المرتبط بالحقل
                    var id = await field.GetAttributeAsync("id");
                    var labelElement = page.Locator($"label[for='{id}'], label:has(input[id='{id}'])").First;
                    string labelText = await labelElement.IsVisibleAsync() ? await labelElement.InnerTextAsync() : "";

                    if (string.IsNullOrEmpty(labelText))
                    {
                        // محاولة إيجاد النص من الـ Placeholder إذا لم يوجد Label
                        labelText = await field.GetAttributeAsync("placeholder") ?? "";
                    }

                    if (string.IsNullOrEmpty(labelText)) continue;

                    // البحث عن أفضل إجابة من قاعدة البيانات/الخدمة الذكية
                    string answer = ApplicationQuestionsService.FindBestAnswer(labelText);
                    if (string.IsNullOrEmpty(answer)) continue;

                    // معالجة خاصة لسنوات الخبرة (تحويل الكسور لأرقام صحيحة)
                    if (labelText.ToLower().Contains("years") || labelText.ToLower().Contains("experience"))
                    {
                        if (double.TryParse(answer, out double num)) answer = Math.Floor(num).ToString();
                    }

                    string tagName = await field.EvaluateAsync<string>("el => el.tagName");

                    if (tagName == "SELECT")
                    {
                        await field.SelectOptionAsync(new SelectOptionValue { Label = answer });
                    }
                    else if (tagName == "INPUT" || tagName == "TEXTAREA")
                    {
                        // محاكاة كتابة بشرية بتأخير عشوائي بين الحروف
                        await field.ClickAsync();
                        await field.TypeAsync(answer, new() { Delay = _random.Next(60, 120) });
                    }

                    await RandomDelay(500, 1000); // فاصل بسيط بين ملء الحقول
                }
                catch { /* تجاهل الأخطاء البسيطة في حقول معينة لإكمال البقية */ }
            }
        }

        private async Task RandomDelay(int min, int max) => await Task.Delay(_random.Next(min, max));
    }
}