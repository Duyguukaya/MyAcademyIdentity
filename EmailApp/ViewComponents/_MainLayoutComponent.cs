using Microsoft.AspNetCore.Mvc;

namespace EmailApp.ViewComponents
{
    public class _MainLayoutComponent:ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}
