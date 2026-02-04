using BusinessLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobBot.Scraper.Iface
{
    public interface IApplyService
    {
        string PlatformName { get; }
        Task<bool> ApplyAsync(JobPostingDto job, string email, string password);
    }
}
