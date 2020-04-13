using System.Linq;
using System.Threading.Tasks;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DatingApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly UserManager<User> _userManager;
        public AdminController(DataContext context, UserManager<User> userManager)
        {
            _userManager = userManager;
            _context = context;
        }

        [Authorize(Policy = "RequireAdminRole")]
        [HttpGet("getUsersWithRoles")]
        public async Task<IActionResult> GetUsersWithRoles()
        {
            var usersList = await _context.Users
                .OrderBy(x => x.Id)
                .Select(user => new
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Roles = (from userRole in user.UserRoles
                             join role in _context.Roles
                             on userRole.RoleId
                             equals role.Id
                             select role.Name).ToList()
                }).ToListAsync();
            return Ok(usersList);
        }

        [Authorize(Policy = "RequireAdminRole")]
        [HttpPost("editRoles/{userName}")]
        public async Task<IActionResult> EditRoles(string userName, RoleEditDto roleEdtDto)
        {
            var dbUser = await _userManager.FindByNameAsync(userName);
            if (dbUser == null)
                return BadRequest("User not found");

            var dbUserRoles = await _userManager.GetRolesAsync(dbUser);
            // roleEditDto will contain the list of role names that we want our user to have (already linked and new ones)
            var selectedRoles = roleEdtDto.RoleNames;
            // selectedRoles = selectedRoles != null ? selectedRoles : new string[] {};
            selectedRoles = selectedRoles ?? new string[] {}; // the same result as the upper line

            // Add the roles that are not already linked
            var result = await _userManager.AddToRolesAsync(dbUser, selectedRoles.Except(dbUserRoles));
            if (!result.Succeeded)
                return BadRequest("Failed to add to roles");

            // remove the roles that are alreay linked but not wanted anymore
            result = await _userManager.RemoveFromRolesAsync(dbUser, dbUserRoles.Except(selectedRoles));
            if (!result.Succeeded)
                return BadRequest("Failed to remove the roles");

            return Ok(await _userManager.GetRolesAsync(dbUser));
        }

        [Authorize(Policy = "ModeratePhoto")]
        [HttpGet("getPhotosForModerators")]
        public IActionResult GetPhotosForModerators()
        {
            return Ok("Admins and moderators can access this");
        }
    }
}