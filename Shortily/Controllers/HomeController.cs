using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Shortily.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Shortily.Service;
using LiteDB;

namespace Shortily.Controllers
{
    
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpPost("shorten")]
        public Task Shorten()
        {
            var context = HttpContext;
            // Perform basic form validation
            if (!context.Request.HasFormContentType || !context.Request.Form.ContainsKey("url"))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return context.Response.WriteAsync("Cannot process request");
            }

            context.Request.Form.TryGetValue("url", out var formData);
            var requestedUrl = formData.ToString();

            // Test our URL
            if (!Uri.TryCreate(requestedUrl, UriKind.Absolute, out Uri result))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return context.Response.WriteAsync("Could not understand URL.");
            }

            var url = result.ToString();

            // Ask for LiteDB and persist a short link
            var liteDB = (ILiteDatabase)context.RequestServices.GetService(typeof(ILiteDatabase));
            var links = liteDB.GetCollection<ShortURL>(BsonAutoId.Int32);
            // Temporary short link 
            var entry = new ShortURL
            {
                Url = url
            };

            // Insert our short-link
            links.Insert(entry);

            //var urlChunk = entry.GetUrlChunk();
            //var responseUri = $"{context.Request.Scheme}://{context.Request.Host}/{urlChunk}";
            //return context.Response.WriteAsync(responseUri);

            var urlChunk = entry.GetUrlChunk();
            var responseUri = $"{context.Request.Scheme}://{context.Request.Host}/{urlChunk}";
            context.Response.Redirect($"/#{responseUri}");
            return Task.CompletedTask;
        }

        [HttpGet("{chunk}")]
        public Task HandleRedirect(string chunk)
        {
            var context = HttpContext;
            var db = (ILiteDatabase)context.RequestServices.GetService(typeof(ILiteDatabase));
            var collection = db.GetCollection<ShortURL>();

            var path = context.Request.Path.ToUriComponent().Trim('/');
            var id = ShortURL.GetId(path);
            var entry = collection.Find(p => p.Id == id).FirstOrDefault();

            if (entry != null)
                context.Response.Redirect(entry.Url);
            else
                context.Response.Redirect("/");

            return Task.CompletedTask;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
