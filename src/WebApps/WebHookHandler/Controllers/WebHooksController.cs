using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebHookHandler.Controllers
{
    public class WebHookModel
    {
        public int nodeId { get; set; }
        public string path { get; set; }
        public string name { get; set; }
        public string displayName { get; set; }
        public string eventName { get; set; }
        public int subscriptionId { get; set; }
        public DateTime requestDate { get; set; }
    }

    public class WebHooksController : Controller
    {
        private static object _sync = new object();
        public static List<WebHookModel> WebHooks { get; } = new List<WebHookModel>();

        [HttpPost]
        public IActionResult Test([FromBody]WebHookModel model)
        {
            model.requestDate = DateTime.UtcNow;

            lock (_sync)
            {
                WebHooks.Add(model);
            }

            return new OkResult();
        }
    }
}
