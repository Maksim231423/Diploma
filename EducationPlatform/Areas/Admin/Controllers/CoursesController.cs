using EducationPlatform.Data;
using EducationPlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EducationPlatform.Areas.Admin.Controllers
{
    [Area("Admin")] // Кажемо, що це зона адміна
    [Authorize(Roles = "Admin")] // Надає доступ лише адміну
    public class CoursesController : Controller
    {
        // 1. Головна сторінка (Дашборд)
        public IActionResult Index()
        {
            return View();
        }

        // 2. Сторінка "Курси, Уроки та Теги"
        public IActionResult Courses()
        {
            return View();
        }

        // 3. Сторінка "Домашні завдання"
        public IActionResult Homeworks()
        {
            return View();
        }

        // 4. Сторінка "Email розсилка"
        public IActionResult Newsletter()
        {
            return View();
        }
    }
}