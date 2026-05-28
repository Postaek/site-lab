using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using site.Data;
using site.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace site.Controllers
{
    public class ArticleController : Controller
    {
        private readonly AppDbContext _context;

        private readonly UserManager<User> _userManager;

        public ArticleController(AppDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            var articles = _context.Articles.ToList();
            return View(articles);
        }

        public IActionResult Create()
        {
            return View();
        }

        // 🔥 СОЗДАНИЕ СТАТЬИ (ТОЛЬКО ДЛЯ АВТОРИЗОВАННЫХ)
        [Authorize]
        [HttpPost]
        public IActionResult Create(Article article)
        {
            article.CreatedAt = DateTime.UtcNow;

            // 🔥 ПОЛУЧАЕМ ТЕКУЩЕГО ПОЛЬЗОВАТЕЛЯ
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            article.UserId = userId;

            _context.Articles.Add(article);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        public IActionResult Details(int id)
        {
            var article = _context.Articles.FirstOrDefault(x => x.Id == id);

            if (article == null)
                return NotFound();

            return View(article);
        }

        // 🔥 УДАЛЕНИЕ С ПРОВЕРКОЙ РОЛЕЙ
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var article = _context.Articles.FirstOrDefault(x => x.Id == id);

            if (article == null)
                return NotFound();

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var currentUser = await _userManager.FindByIdAsync(userId);

            if (currentUser == null)
                return Unauthorized();

            // 🔥 ВОТ ГЛАВНАЯ ЛОГИКА
            if (await _userManager.IsInRoleAsync(currentUser, "Admin") || article.UserId == userId)
            {
                _context.Articles.Remove(article);
                _context.SaveChanges();
                return Ok();
            }

            return Forbid();
        }
    }
}