using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DatingApp.API.Controllers
{
    [ServiceFilter(typeof(LogUserActivity))] // every time any of the methods get called the LogUserActivity will be used,
                                             // which is updating the LastActive property for that user
    /* Because we added AuthorizeFilter to AuthorizationPolicyBuilder we can remove the [Authorize] attribute                                         
    [Authorize] */
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IDatingRepository _datingRepository;
        private readonly IMapper _mapper;

        public UsersController(
            IDatingRepository datingRepository,
            IMapper mapper)
        {
            _datingRepository = datingRepository;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery]UserParams userParams)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var dbUser = await _datingRepository.GetUser(currentUserId, false);
            userParams.UserId = currentUserId;
            if (string.IsNullOrEmpty(userParams.Gender))
            {
                userParams.Gender = dbUser.Gender == "male" ? "female" : "male";
            }

            var users = await _datingRepository.GetUsers(userParams);
            var returnUsers = _mapper.Map<IEnumerable<UserForListDto>>(users);
            Response.AddPagination(users.CurrentPage, users.PageSize, users.TotalCount, users.TotalPages);

            return Ok(returnUsers);
        }

        private int IEnumerable<T>(IEnumerable<User> users)
        {
            throw new NotImplementedException();
        }

        [HttpGet("{id}", Name = "GetUser")]
        public async Task<IActionResult> GetUser(int id)
        {
            var isCurrentUser = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value) == id;
            var user = await _datingRepository.GetUser(id, isCurrentUser);
            var returnUser = _mapper.Map<UserForDetailsDto>(user);

            return Ok(returnUser);
        }

        // POST api/values
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, UserForUpdateDto userForUpdateDto)
        {
            // if the id passed is not the user from the token we throw unauthorised
            if (id != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();
            
            var dbUser = await _datingRepository.GetUser(id, false);
            _mapper.Map(userForUpdateDto, dbUser);
            if (await _datingRepository.SaveAll())
                return NoContent();

            throw new Exception($"Updating user with {id} failed in save");
        }

        [HttpPost("{id}/like/{recipientId}")]
        public async Task<IActionResult> LikeUser(int id, int recipientId)
        {
            if (id != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            if (await _datingRepository.GetUser(recipientId, false) == null)
                return NotFound();

            var like = await _datingRepository.GetLike(id, recipientId);
            if (like != null)
                return BadRequest("You already liked this user");

            like = new Like
            {
                LikerId = id,
                LikeeId = recipientId
            };

            _datingRepository.Add<Like>(like); // adding into memory

            if (await _datingRepository.SaveAll())
                return Ok();

            return BadRequest("Failed to like user");
        }
    }
}