using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserMVC.Models;
using UserMVC.ViewModel;

namespace UserMVC.Controllers
{
    public class UsersController : Controller
    {
        UserManager<User> _userManager;
        SignInManager<User> _signInManager;

        public UsersController(UserManager<User> userManager,
            SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public IActionResult Index()
        {
            ViewData["Users"] = _userManager.Users.ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string submit, 
            List<string> checkbox)
        {
            if (!_signInManager.IsSignedIn(User) || IsBlockedCurrentUser())
                return RedirectToAction("Index", "Home");
            
            if (ModelState.IsValid)
            {
                for (int i = 0; i < checkbox.Count; i++)
                {
                    User user = await _userManager.FindByIdAsync(checkbox[i]);
                    if (user != null)
                    {
                        if (submit == "Block")
                        {
                            await Block(user);
                            if (IsCurrentUser(user))
                                return RedirectToAction("Index", "Home");
                        }
                        else if (submit == "Unblock")
                            await Unblock(user);
                        else if (submit == "Delete")
                        {
                            await Delete(user);
                            if (IsCurrentUser(user))
                                return RedirectToAction("Index", "Home");
                        }
                    }
                }
            }
            return RedirectToAction("Index", "Account");
        }

        public async Task<IActionResult> LogOut()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        public async Task Block(User user)
        {
            user.Blocked = true;
            _ = await _userManager.UpdateAsync(user);
            if (IsCurrentUser(user))
                await _signInManager.SignOutAsync();
        }

        public async Task Unblock(User user)
        {
            user.Blocked = false;
            _ = await _userManager.UpdateAsync(user);
        }

        public async Task Delete(User user)
        {
                var currentUser = await _userManager.GetUserAsync(User);
                await _userManager.DeleteAsync(user);
                if (IsCurrentUser(user))
                {
                    _ = Task.FromResult(_signInManager.SignOutAsync());
                }
                else
                {
                    await _signInManager.SignInAsync(user, false);
                    await _signInManager.SignOutAsync();
                    await _signInManager.SignInAsync(currentUser, false);
                }
        }

        public bool IsCurrentUser(User user)
        {
            return Task.FromResult(_userManager.GetUserAsync(User).Result).Result.Equals(user);
        }

        public bool IsBlockedCurrentUser()
        {
            return Task.FromResult(_userManager.GetUserAsync(User).Result.Blocked).Result;
        }
    }
}
