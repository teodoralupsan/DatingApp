using System;
using System.Linq;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DatingApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IDatingRepository _datingRepository;
        private readonly IOptions<CloudinarySettings> _cloudinaryConfig;
        private Cloudinary _cloudinary;

        public AdminController(
            DataContext context,
            UserManager<User> userManager,
            IDatingRepository datingRepository,
            IOptions<CloudinarySettings> cloudinaryConfig)
        {
            _datingRepository = datingRepository;
            _cloudinaryConfig = cloudinaryConfig;
            _userManager = userManager;
            _context = context;

            Account acc = new Account(
                _cloudinaryConfig.Value.CloudName,
                _cloudinaryConfig.Value.ApiKey,
                _cloudinaryConfig.Value.ApiSecret
            );

            _cloudinary = new Cloudinary(acc);
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
            selectedRoles = selectedRoles ?? new string[] { }; // the same result as the upper line

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
        public async Task<IActionResult> GetPhotosForModerators()
        {
            var photosList = await _context.Photos
                .OrderBy(x => x.User.UserName)
                .IgnoreQueryFilters()
                .Where(p => !p.isApproved)
                .Select(photo => new
                {
                    Id = photo.Id,
                    Url = photo.Url,
                    UserName = photo.User.UserName,
                    IsApproved = photo.isApproved
                }).ToListAsync();
            return Ok(photosList);
        }

        [Authorize(Policy = "ModeratePhoto")]
        [HttpPost("approvePhoto/{photoId}")]
        public async Task<IActionResult> ApprovePhoto(int photoId)
        {
            var dbPhoto = await _datingRepository.GetPhoto(photoId);
            if (dbPhoto == null)
                return BadRequest("Photo cannot be approved");
            dbPhoto.isApproved = true;
            if (await _context.SaveChangesAsync() > 0)
                return Ok();

            throw new Exception("Photo failed to be approved");
        }

        [Authorize(Policy = "ModeratePhoto")]
        [HttpDelete("rejectPhoto/{idPhoto}")]
        public async Task<IActionResult> Delete(int idPhoto)
        {
            var dbPhoto = await _datingRepository.GetPhoto(idPhoto);

            if (dbPhoto.PublicId != null)
            {
                var deleteParams = new DeletionParams(dbPhoto.PublicId);
                var result = _cloudinary.Destroy(deleteParams);
                if (result.Result == "ok")
                    _datingRepository.Delete(dbPhoto);
            }
            else
            {
                _datingRepository.Delete(dbPhoto);
            }

            if (await _datingRepository.SaveAll())
                return Ok();

            return BadRequest("Failed to delete the photo");
        }
    }
}