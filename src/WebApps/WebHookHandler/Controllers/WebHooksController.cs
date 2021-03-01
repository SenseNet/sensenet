using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace WebHookHandler.Controllers
{
    public class WebHookModel
    {
        public int nodeId { get; set; }
        public int versionId { get; set; }
        public string version { get; set; }
        public string previousVersion { get; set; }
        public DateTime versionModificationDate { get; set; }
        public int modifiedBy { get; set; }
        public string path { get; set; }
        public string name { get; set; }
        public string displayName { get; set; }
        public string eventName { get; set; }
        public int subscriptionId { get; set; }
        public DateTime sentTime { get; set; }
        public DateTime arrivedTime { get; set; }
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

        [HttpGet]
        public IActionResult Clear()
        {
            WebHooks.Clear();
            return Redirect("/");
        }

        private void SaveModel(WebHookModel model)
        {
            model.arrivedTime = DateTime.UtcNow;

            lock (_sync)
            {
                WebHooks.Add(model);
            }
        }
    }
}
