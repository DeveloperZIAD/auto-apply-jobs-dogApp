using Microsoft.Playwright;

public class YCScraper
{
    private readonly YCAuthService _authService;

    public YCScraper(YCAuthService authService) => _authService = authService;

    public async Task ScrapeJobsAsync(string keyword)
    {
        using var playwright = await Playwright.CreateAsync();
        await using var context = await _authService.GetContextAsync(playwright);
        var page = context.Pages[0];

        // الانتقال لصفحة البحث مع الكلمة المفتاحية
        await page.GotoAsync($"https://www.workatastartup.com/jobs?query={Uri.EscapeDataString(keyword)}");

        Console.WriteLine("⏳ جاري تحميل الوظائف...");
        await page.WaitForSelectorAsync(".job-card", new() { Timeout = 10000 });

        // التمرير لأسفل مرتين لتحميل المزيد من الوظائف (Infinite Scroll)
        for (int i = 0; i < 3; i++)
        {
            await page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight)");
            await Task.Delay(2000);
        }

        var jobCards = await page.Locator(".job-card").AllAsync();
        Console.WriteLine($"🔍 تم العثور على {jobCards.Count} وظيفة.");

        foreach (var card in jobCards)
        {
            try
            {
                var title = await card.Locator(".job-name a").InnerTextAsync();
                var company = await card.Locator(".company-name").InnerTextAsync();
                var link = await card.Locator(".job-name a").GetAttributeAsync("href");

                Console.WriteLine($"📌 [{company}] - {title}");
                Console.WriteLine($"🔗 https://www.workatastartup.com{link}");
                Console.WriteLine("-----------------------------------");
            }
            catch { /* تجاوز أي كارت غير مكتمل البيانات */ }
        }
    }
}