using BusinessLogic;
using JobBot.Scraper.Iface;
using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public class GulfTalentScraper : IJobScraper
{
    public string PlatformName => "GulfTalent";

    // استبدل ميثود السحب بهذا الكود المرن
    public async Task<List<JobPostingDto>> ScrapeJobsAsync(string keyword, int maxJobs)
    {
        var jobs = new List<JobPostingDto>();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false });
        var page = await browser.NewPageAsync();

        try
        {
            // استخدام رابط البحث المباشر مع الكلمة المفتاحية
            string url = $"https://www.gulftalent.com/jobs/search?q={Uri.EscapeDataString(keyword)}";
            await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

            // الانتظار حتى تظهر الحاوية الرئيسية للنتائج
            await page.WaitForSelectorAsync("table.table, .job-result, [data-job-id]", new PageWaitForSelectorOptions { Timeout = 15000 });

            // استهداف صفوف الجدول أو الكروت التي تحتوي فعلياً على معرف وظيفة
            // GulfTalent غالباً ما يضع الوظائف في <tr> داخل <tbody>
            var jobElements = await page.QuerySelectorAllAsync("tr[class*='job'], .job-result, a:has(h2)");

            foreach (var element in jobElements.Take(maxJobs))
            {
                // البحث عن العنوان والرابط داخل العنصر المختار فقط وليس الصفحة كاملة
                var linkElement = await element.QuerySelectorAsync("a");
                if (linkElement == null) continue;

                var title = await linkElement.InnerTextAsync();
                var href = await linkElement.GetAttributeAsync("href");

                // تصفية: يجب أن يحتوي الرابط على كلمة /jobs/ وأرقام (ID الوظيفة)
                if (string.IsNullOrEmpty(href) || !href.Contains("/jobs/view/")) continue;

                var companyElement = await element.QuerySelectorAsync("td:nth-child(2), .company-name");

                jobs.Add(new JobPostingDto
                {
                    ExternalJobId = "GT_" + Guid.NewGuid().ToString().Substring(0, 8),
                    Platform = PlatformName,
                    JobTitle = title.Trim(),
                    CompanyName = await companyElement?.InnerTextAsync() ?? "Gulf Talent Employer",
                    SourceUrl = href.StartsWith("http") ? href : "https://www.gulftalent.com" + href,
                    ScrapedDate = DateTime.Now
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ GulfTalent Specific Error: {ex.Message}");
        }

        return jobs;
    }
}