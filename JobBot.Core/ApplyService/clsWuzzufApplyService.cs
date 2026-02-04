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
    public class WuzzufApplyService : IApplyService
    {
        public string PlatformName => "Wuzzuf";
        private readonly Random _random = new Random();

        public async Task<bool> ApplyAsync(JobPostingDto job, string email, string password)
        {
            using var playwright = await Playwright.CreateAsync();
            string botProfilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Wuzzuf_Chrome_Profile");

            await using var context = await playwright.Chromium.LaunchPersistentContextAsync(botProfilePath, new BrowserTypeLaunchPersistentContextOptions
            {
                Headless = false,
                SlowMo = 500,
                Args = new[] { "--disable-blink-features=AutomationControlled", "--start-maximized" },
                ViewportSize = ViewportSize.NoViewport
            });

            var page = context.Pages.FirstOrDefault() ?? await context.NewPageAsync();

            try
            {
                // 1. تسجيل الدخول
                await page.GotoAsync("https://wuzzuf.net/login");
                if (page.Url.Contains("login"))
                {
                    await LoginAsync(page, email, password);
                }

                // 2. التوجه لرابط الوظيفة
                Console.WriteLine($"🚀 Navigating to Wuzzuf Job: {job.JobTitle}");
                await page.GotoAsync(job.SourceUrl);
                await RandomDelay(2000, 3000);

                // 3. زر التقديم الأول (Apply for Job)
                var applyButton = page.Locator("button:has-text('Apply for Job'), .css-1m070z4, .css-1l3yxhx").First;

                if (await applyButton.IsVisibleAsync())
                {
                    await applyButton.ClickAsync();
                    await RandomDelay(2000, 4000);

                    // 4. التعامل مع الأسئلة والخطوات (Loop لأن Wuzzuf قد يحتوي على صفحات متعددة)
                    return await HandleWuzzufSteps(page, job.Id);
                }

                Console.WriteLine("⚠️ Job might be already applied or expired.");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Wuzzuf Error: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> HandleWuzzufSteps(IPage page, Guid jobId)
        {
            for (int i = 0; i < 5; i++) // أقصى حد 5 خطوات
            {
                // إذا وجدنا رسالة نجاح أو رابط شكراً
                if (page.Url.Contains("application-success") || await page.Locator("text=Application Sent").IsVisibleAsync())
                {
                    Console.WriteLine("✨ Wuzzuf Application Sent Successfully!");
                    return true;
                }

                // ملء الأسئلة في الصفحة الحالية
                await AutoFillWuzzufQuestions(page, jobId);

                // البحث عن زر الاستمرار (قد يكون 'Submit Application', 'Next', 'Save and Continue')
                var nextBtn = page.Locator("button:has-text('Submit Application'), button:has-text('Apply'), button:has-text('Next'), button:has-text('Save and Continue'), .css-668j95").First;

                if (await nextBtn.IsVisibleAsync() && await nextBtn.IsEnabledAsync())
                {
                    await nextBtn.ClickAsync();
                    await RandomDelay(2000, 3000);
                }
                else
                {
                    // إذا لم نجد زر، ربما انتهينا
                    break;
                }
            }
            return false;
        }

        private async Task AutoFillWuzzufQuestions(IPage page, Guid jobId)
        {
            // Wuzzuf يستخدم غالباً divs تحتوي على الحقول
            var questionContainers = await page.Locator(".css-12vky7n, .css-1p5p95, .form-control-container").AllAsync();

            foreach (var container in questionContainers)
            {
                try
                {
                    // استخراج السؤال
                    string label = await container.Locator("label, .css-1n9m685").First.InnerTextAsync();
                    if (string.IsNullOrEmpty(label)) continue;

                    string answer = ApplicationQuestionsService.FindBestAnswer(label);
                    if (string.IsNullOrEmpty(answer))
                    {
                        SaveNewQuestion(label, jobId);
                        continue;
                    }

                    // 1. التعامل مع الـ Radio (Yes/No)
                    var radios = await container.Locator("input[type='radio']").AllAsync();
                    if (radios.Count > 0)
                    {
                        var radioLabels = await container.Locator("label").AllAsync();
                        foreach (var rl in radioLabels)
                        {
                            if ((await rl.InnerTextAsync()).Contains(answer, StringComparison.OrdinalIgnoreCase))
                            {
                                await rl.ClickAsync();
                                break;
                            }
                        }
                    }
                    // 2. التعامل مع الـ Select
                    else if (await container.Locator("select").First.IsVisibleAsync())
                    {
                        await container.Locator("select").First.SelectOptionAsync(new SelectOptionValue { Label = answer });
                    }
                    // 3. الحقول النصية (مع تنظيف الأرقام كما طلبتم)
                    else
                    {
                        var input = container.Locator("input, textarea").First;
                        if (await input.IsVisibleAsync())
                        {
                            string finalAnswer = answer;
                            // تنظيف الأرقام لسنوات الخبرة
                            if (label.ToLower().Contains("years") || label.ToLower().Contains("experience"))
                            {
                                if (double.TryParse(answer, out double num)) finalAnswer = Math.Floor(num).ToString();
                            }

                            await input.FillAsync(finalAnswer);
                        }
                    }
                }
                catch { /* تجاهل أخطاء الحقول الفردية */ }
            }
        }

        private async Task LoginAsync(IPage page, string email, string password)
        {
            await page.FillAsync("input[name='email']", email);
            await page.FillAsync("input[name='password']", password);
            await page.ClickAsync("button[type='submit']");
            // انتظر حتى تختفي صفحة اللوجين
            await page.WaitForFunctionAsync("() => !window.location.href.includes('login')");
        }

        private void SaveNewQuestion(string question, Guid jobId)
        {
            var existing = ApplicationQuestionsDataLogic.GetAll().Any(q => q.QuestionText.Equals(question, StringComparison.OrdinalIgnoreCase));
            if (!existing)
            {
                ApplicationQuestionsDataLogic.Insert(new ApplicationQuestionInfo
                {
                    Id = Guid.NewGuid(),
                    JobId = jobId,
                    QuestionText = question,
                    AnswerText = "",
                    QuestionType = "Wuzzuf-Auto",
                    CreatedAt = DateTime.Now
                });
            }
        }

        private async Task RandomDelay(int min, int max) => await Task.Delay(_random.Next(min, max));
    }
}