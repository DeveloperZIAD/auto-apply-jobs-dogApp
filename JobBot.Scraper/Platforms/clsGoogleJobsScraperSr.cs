using BusinessLogic;
using JobBot.Scraper.Iface;
using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class GoogleJobsScraper : IJobScraper
{
    public string PlatformName => "GoogleJobs";
    public async Task<List<JobPostingDto>> ScrapeJobsAsync(string keyword, int maxJobs)
    {
        var jobs = new List<JobPostingDto>();
        using var playwright = await Playwright.CreateAsync();

        // نستخدم ميزة التخفي عن طريق فتح متصفح Chrome العادي المثبت على ويندوز
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false,
            Channel = "chrome", // سيفتح متصفح Chrome الحقيقي لزيادة الثقة لدى جوجل
            Args = new[] {
            "--disable-blink-features=AutomationControlled",
            "--disable-features=IsolateOrigins,site-per-process"
        }
        });

        var context = await browser.NewContextAsync();

        // سكريبت إضافي يمحو أي أثر للـ WebDriver داخل المتصفح
        var page = await context.NewPageAsync();
        await page.AddInitScriptAsync("() => { Object.defineProperty(navigator, 'webdriver', { get: () => false }); }");

        try
        {
            // إضافة "in Egypt" أو أي دولة للبحث لضمان امتلاء القائمة
            string url = $"https://www.google.com/search?q={Uri.EscapeDataString(keyword)}+jobs+in+Egypt&ibp=htl;jobs";
            await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.Load });

            Console.WriteLine("🧐 Observing Google interface...");

            // انتظار ذكي جداً: يبحث عن أي عنصر يحتوي على نص "Apply" أو كروت الوظائف
            await page.WaitForSelectorAsync("[role='listitem'], .iFne8e, h3", new PageWaitForSelectorOptions { Timeout = 30000 });

            // سحب الكروت باستخدام Selector عام جداً (أي LI داخل الـ List الرئيسية)
            var jobCards = await page.QuerySelectorAllAsync("[role='listitem']");

            foreach (var card in jobCards.Take(maxJobs))
            {
                var titleElem = await card.QuerySelectorAsync("[role='heading'], .vNEEBe, .PUpvYf");
                var companyElem = await card.QuerySelectorAsync(".vNEEBe, .v7757c, span");

                if (titleElem == null) continue;

                var title = await titleElem.InnerTextAsync();
                var company = companyElem != null ? await companyElem.InnerTextAsync() : "Google Source";

                jobs.Add(new JobPostingDto
                {
                    ExternalJobId = "GOOG_" + Guid.NewGuid().ToString().Substring(0, 8),
                    Platform = PlatformName,
                    JobTitle = title.Trim(),
                    CompanyName = company.Trim(),
                    SourceUrl = url,
                    ScrapedDate = DateTime.Now
                });
            }
        }
        catch (Exception ex) { Console.WriteLine($"❌ Google Final Shield: {ex.Message}"); }

        return jobs;
    }
}