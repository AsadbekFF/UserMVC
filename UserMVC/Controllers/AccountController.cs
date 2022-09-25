using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserMVC.Models;
using UserMVC.ViewModel;

namespace UserMVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public AccountController(UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public IActionResult Index()
        {
            ViewData["Users"] = _userManager.Users.ToList();
            return View();
        }
        [HttpGet]
        public IActionResult Register()
        {
            if (_signInManager.IsSignedIn(User))
            {
                var user = _userManager.GetUserAsync(User);
                if (user.Result.Blocked)
                {
                    _signInManager.SignOutAsync();
                    RedirectToAction("Index", "Home");
                }
            }
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                User user = new()
                { Email = model.Email, UserName = model.Email, 
                    Name = model.Name, DateOfRegistration = DateTime.Now,
                    LastLogin = DateTime.Now, Blocked = false};
                // добавляем пользователя
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // установка куки
                    await _signInManager.SignInAsync(user, false);
                    
                    ViewData["Users"] = _userManager.Users.ToList();
                    return RedirectToAction("Index", "Account");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            if (_signInManager.IsSignedIn(User))
            {
                var user = _userManager.GetUserAsync(User);
                if (user.Result.Blocked)
                {
                    _signInManager.SignOutAsync();
                    RedirectToAction("Index", "Home");
                }
            }
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            var users = _userManager.Users.ToList();
            if (ModelState.IsValid && 
                users.First(x => x.Email == model.Email).Blocked == false)
            {
                var result =
                    await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);
                if (result.Succeeded)
                {
                    User user = users.First(x => x.Email == model.Email);
                    user.LastLogin = DateTime.Now;
                    _ = _userManager.UpdateAsync(user);
                    // проверяем, принадлежит ли URL приложению
                    if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    {
                        ViewData["Users"] = users;
                        return Redirect(model.ReturnUrl);
                    }
                    else
                    {
                        return RedirectToAction("Index");
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Неправильный логин и (или) пароль");
                }
            }
            ViewData["ValidateAccount"] = "Not validated";
            ViewData["Users"] = users;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // удаляем аутентификационные куки
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index",
                                    "Home");
        }
    }
}
