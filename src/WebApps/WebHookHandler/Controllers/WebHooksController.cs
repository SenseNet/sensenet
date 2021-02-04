using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

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

        [HttpGet()]
        [ActionName("test")]
        public IActionResult TestGet([FromQuery] WebHookModel model)
        {
            SaveModel(model);

            return new OkResult();
        }
        [HttpPost]
        [ActionName("test")]
        public IActionResult Test([FromBody] WebHookModel model)
        {
            SaveModel(model);

            return new OkResult();
        }
        [HttpPatch]
        [ActionName("test")]
        public IActionResult TestPatch([FromBody] WebHookModel model)
        {
            SaveModel(model);

            return new OkResult();
        }
        [HttpPut]
        [ActionName("test")]
        public IActionResult TestPut([FromBody] WebHookModel model)
        {
            SaveModel(model);

            return new OkResult();
        }
        [HttpDelete]
        [ActionName("test")]
        public IActionResult TestDel([FromBody] WebHookModel model)
        {
            SaveModel(model);

            return new OkResult();
        }

        private void SaveModel(WebHookModel model)
        {
            model.requestDate = DateTime.UtcNow;

            lock (_sync)
            {
                WebHooks.Add(model);
            }
        }
    }
}
