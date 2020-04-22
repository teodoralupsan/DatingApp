using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DatingApp.API.Controllers
{
    /* Because we added AuthorizeFilter to AuthorizationPolicyBuilder we can remove the [Authorize] attribute
    [Authorize] */
    [Route("api/users/{userId}/photos")]
    public class PhotosController : ControllerBase
    {
        private readonly IDatingRepository _datingRepository;
        private readonly IMapper _mapper;
        private readonly IOptions<CloudinarySettings> _cloudinaryConfig;
        private Cloudinary _cloudinary;

        public PhotosController(
            IDatingRepository datingRepository,
            IMapper mapper,
            IOptions<CloudinarySettings> cloudinaryConfig)
        {
            _datingRepository = datingRepository;
            _mapper = mapper;
            _cloudinaryConfig = cloudinaryConfig;

            Account acc = new Account(
                _cloudinaryConfig.Value.CloudName,
                _cloudinaryConfig.Value.ApiKey,
                _cloudinaryConfig.Value.ApiSecret
            );

            _cloudinary = new Cloudinary(acc);
        }

        [HttpGet("{id}", Name="GetPhoto")]
        public async Task<IActionResult> GetPhoto(int id)
        {
            var dbPhoto = await _datingRepository.GetPhoto(id);

            var photo = _mapper.Map<PhotoForReturnDto>(dbPhoto);

            return Ok(photo);
        }


        [HttpPost]
        public async Task<IActionResult> AddPhotoForUser(int userId, PhotoForCreationDto photoForCreationDto)
        {
            // if the id passed is not the user from the token we throw unauthorised
            if (!IsUserAuthorized(userId))
                return Unauthorized();
            
            var dbUser = await _datingRepository.GetUser(userId, true);

            var file = photoForCreationDto.File;

            string cloudinaryUrl = string.Empty;
            string cloudinaryPublicId = string.Empty;

            if (file.Length > 0)
            {
                UploadPhoto(file, out cloudinaryUrl, out cloudinaryPublicId);
            }

            photoForCreationDto.Url = cloudinaryUrl;
            photoForCreationDto.PublicId = cloudinaryPublicId;

            var photo = _mapper.Map<Photo>(photoForCreationDto);

            if (!dbUser.Photos.Any(u => u.IsMain))
            {
                photo.IsMain = true;
            }

            dbUser.Photos.Add(photo);

            if (await _datingRepository.SaveAll())
            {
                var photoToReturn = _mapper.Map<PhotoForReturnDto>(photo);
                return CreatedAtRoute("GetPhoto", new {id = photo.Id}, photoToReturn);
            }

            return BadRequest("Could not add the photo");
        }

        [HttpPost("{idPhoto}/setMain")]
        public async Task<IActionResult> SetMainPhoto(int userId, int idPhoto)
        {
            if (!IsUserAuthorized(userId))
                return Unauthorized();

            var dbUser = await _datingRepository.GetUser(userId, true);
            if (!dbUser.Photos.Any(p => p.Id == idPhoto))
                return Unauthorized();

            var dbPhoto = await _datingRepository.GetPhoto(idPhoto);
            if (dbPhoto.IsMain)
                return BadRequest("This is already the main photo!");

            var currentMainPhoto = await _datingRepository.GetMainPhotoForUser(userId);
            currentMainPhoto.IsMain = false;
            dbPhoto.IsMain = true;

            if (await _datingRepository.SaveAll())
                return NoContent();

            return BadRequest("Could not set photo to main");
        }

        [HttpDelete("{idPhoto}")]
        public async Task<IActionResult> DeletePhoto(int userId, int idPhoto)
        {
            if (!IsUserAuthorized(userId))
                return Unauthorized();

            var dbUser = await _datingRepository.GetUser(userId, true);
            if (!dbUser.Photos.Any(p => p.Id == idPhoto))
                return Unauthorized();

            var dbPhoto = await _datingRepository.GetPhoto(idPhoto);
            if (dbPhoto.IsMain)
                return BadRequest("You cannot delete yout main photo!");
                
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

        private void UploadPhoto(IFormFile file, out string CloudinaryUrl, out string CloudinaryPublicId)
        {
            var uploadResut = new ImageUploadResult();

            using (var stream = file.OpenReadStream())
            {
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(file.Name, stream),
                    Transformation = new Transformation()
                        .Width(500).Height(500).Crop("fill").Gravity("face")
                };

                uploadResut = _cloudinary.Upload(uploadParams);
            }
            CloudinaryUrl = uploadResut.Uri.ToString();
            CloudinaryPublicId = uploadResut.PublicId;
        }

        private bool IsUserAuthorized(int userId)
        {
            return userId == int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        }
    }
}