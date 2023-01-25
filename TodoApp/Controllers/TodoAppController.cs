
using Microsoft.AspNetCore.Mvc;

namespace TodoApp.Controllers;

[Route("")]
public class TodoAppController : ControllerBase
{
    public IActionResult Index()
    {
        string templatePath = Path.GetFullPath("templates/index.html");
        return PhysicalFile(templatePath, "text/html");
    }
}