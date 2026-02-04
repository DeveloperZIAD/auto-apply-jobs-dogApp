using BusinessLogic;
using JobBot.Scraper.Iface;
using Microsoft.Playwright;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataLogic;

namespace JobBot.Core.Services
{
    public class LinkedInApplyService : IApplyService
    {
        public string PlatformName => "LinkedIn";
        private readonly Random _random = new Random();

        public async Task<bool> ApplyAsync(JobPostingDto job, string email, string password)
        {
            using var playwright = await Playwright.CreateAsync();
            string botProfilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LinkedIn_Chrome_Profile");

            await using var context = await playwright.Chromium.LaunchPersistentContextAsync(botProfilePath, new BrowserTypeLaunchPersistentContextOptions
            {
                Headless = false,
                SlowMo = 500, // تقليل الـ SlowMo لزيادة الطبيعية مع الاعتماد على RandomDelay
                Args = new[] { "--disable-blink-features=AutomationControlled", "--start-maximized" },
                ViewportSize = ViewportSize.NoViewport
            });

            var page = context.Pages.FirstOrDefault() ?? await context.NewPageAsync();

            try
            {
                await page.GotoAsync(job.SourceUrl, new PageGotoOptions { WaitUntil = WaitUntilState.Load, Timeout = 60000 });
                await RandomDelay(3000, 5000);

                await page.Mouse.WheelAsync(0, 500);
                await RandomDelay(1000, 2000);

                var easyApplyButton = page.Locator("button:has-text('Easy Apply'), span:has-text('Easy Apply'), button.jobs-apply-button").First;

                if (await easyApplyButton.IsVisibleAsync())
                {
                    Console.WriteLine("🎯 Found Easy Apply! Starting Auto-Fill...");
                    await easyApplyButton.ClickAsync();
                    await RandomDelay(2000, 4000);

                    return await HandleLinkedInFormSteps(page, job.Id);
                }

                Console.WriteLine("⚠️ Easy Apply button not found.");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Critical Error: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> HandleLinkedInFormSteps(IPage page, Guid jobId)
        {
            for (int i = 1; i <= 15; i++) // زيادة لـ 15 للنماذج المعقدة
            {
                await AutoFillQuestions(page, jobId);
                await RandomDelay(1000, 2000);

                // البحث عن أزرار التحكم في أسفل النموذج
                var primaryBtn = page.Locator("footer button.artdeco-button--primary").First;
                
                if (await primaryBtn.IsVisibleAsync() && await primaryBtn.IsEnabledAsync())
                {
                    string btnText = (await primaryBtn.InnerTextAsync()).ToLower().Trim();
                    
                    if (btnText.Contains("submit") || btnText.Contains("إرسال") || btnText.Contains("post"))
                    {
                        await primaryBtn.ClickAsync();
                        await RandomDelay(5000, 7000);
                        Console.WriteLine("🚀 Application Submitted Successfully!");
                        return true;
                    }

                    await primaryBtn.ClickAsync();
                    await RandomDelay(2000, 3000);
                }
                else
                {
                    // التحقق من وجود أخطاء تمنع الانتقال
                    var error = page.Locator(".artdeco-inline-feedback--error").First;
                    if (await error.IsVisibleAsync())
                    {
                        Console.WriteLine($"🛑 Form blocked by error: {await error.InnerTextAsync()}");
                        return false; 
                    }
                    break;
                }
            }
            return false;
        }

        private async Task AutoFillQuestions(IPage page, Guid jobId)
        {
            // استهداف جميع مجموعات الأسئلة (نصوص، راديو، قوائم)
            var containers = await page.Locator(".jobs-easy-apply-form-section__grouping, .fb-dash-form-element, .jobs-easy-apply-form-element").AllAsync();

            foreach (var container in containers)
            {
                try
                {
                    string label = await GetElementLabel(container);
                    if (string.IsNullOrWhiteSpace(label)) continue;

                    string answer = ApplicationQuestionsService.FindBestAnswer(label);

                    if (string.IsNullOrEmpty(answer))
                    {
                        SaveNewQuestion(label, jobId);
                        continue;
                    }

                    // 1. التعامل مع الراديو بوتوم (Yes/No)
                    var radioOptions = await container.Locator("input[type='radio'], .fb-radio-button").AllAsync();
                    if (radioOptions.Count > 0)
                    {
                        var allLabels = await container.Locator("label").AllAsync();
                        await HandleRadioSelection(allLabels, answer);
                        continue;
                    }

                    // 2. التعامل مع القوائم المنسدلة (Select/Combobox)
                    var select = container.Locator("select").First;
                    var combobox = container.Locator("[role='combobox'], .artdeco-typeahead__input").First;

                    if (await select.IsVisibleAsync())
                    {
                        await select.SelectOptionAsync(new SelectOptionValue { Label = answer });
                    }
                    else if (await combobox.IsVisibleAsync())
                    {
                        await HandleDropdownSelection(page, combobox, answer);
                    }
                    // 3. التعامل مع الحقول النصية (الافتراضي)
                    else
                    {
                        var input = container.Locator("input[type='text'], input[type='number'], textarea").First;
                        if (await input.IsVisibleAsync())
                        {
                            string finalAnswer = answer;
                            var inputType = await input.GetAttributeAsync("type");
                            if (inputType == "number" || label.ToLower().Contains("years") || label.ToLower().Contains("experience"))
                            {
                                // استخراج الرقم فقط وتجاهل الكسور (مثال: 5.5 تصبح 5)
                                if (double.TryParse(answer, out double num))
                                {
                                    finalAnswer = Math.Floor(num).ToString();
                                }
                            }

                            await input.FocusAsync();
                            await input.FillAsync("");
                            await input.TypeAsync(finalAnswer, new() { Delay = 50 });
                            await page.Keyboard.PressAsync("Tab");
                            continue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Field Error: {ex.Message.Substring(0, Math.Min(50, ex.Message.Length))}");
                }
            }
        }

        private async Task HandleRadioSelection(IReadOnlyList<ILocator> labelOptions, string answer)
        {
            foreach (var label in labelOptions)
            {
                var text = await label.InnerTextAsync();
                if (text.Trim().Equals(answer, StringComparison.OrdinalIgnoreCase) || 
                    text.Trim().ToLower().Contains(answer.ToLower()))
                {
                    await label.ClickAsync(new() { Force = true });
                    return;
                }
            }
        }

        private async Task HandleDropdownSelection(IPage page, ILocator trigger, string answer)
        {
            await trigger.ClickAsync();
            await page.Keyboard.TypeAsync(answer, new() { Delay = 100 });
            await RandomDelay(1000, 1500);
            
            // محاولة اختيار أول نتيجة تظهر في القائمة المنبثقة
            var firstOption = page.Locator(".artdeco-typeahead__result, [role='option']").First;
            if (await firstOption.IsVisibleAsync())
                await firstOption.ClickAsync();
            else
                await page.Keyboard.PressAsync("Enter");
        }

        private async Task<string> GetElementLabel(ILocator element)
        {
            var label = element.Locator("label, .fb-dash-form-element__label, legend").First;
            return await label.IsVisibleAsync() ? (await label.InnerTextAsync()).Trim() : "";
        }

        private void SaveNewQuestion(string question, Guid jobId)
        {
            var existing = ApplicationQuestionsDataLogic.GetAll()
                            .Any(q => q.QuestionText.Equals(question, StringComparison.OrdinalIgnoreCase));

            if (!existing)
            {
                ApplicationQuestionsDataLogic.Insert(new ApplicationQuestionInfo
                {
                    Id = Guid.NewGuid(),
                    JobId = jobId,
                    QuestionText = question,
                    AnswerText = "",
                    QuestionType = "Auto-Detected",
                    CreatedAt = DateTime.Now
                });
                Console.WriteLine($"🆕 Saved New Question: {question}");
            }
        }

        private async Task RandomDelay(int min, int max) => await Task.Delay(_random.Next(min, max));
    }
}