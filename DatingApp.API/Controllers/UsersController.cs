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
    [Authorize]
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
            var dbUser = await _datingRepository.GetUser(currentUserId);
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
            var user = await _datingRepository.GetUser(id);
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
            
            var dbUser = await _datingRepository.GetUser(id);
            _mapper.Map(userForUpdateDto, dbUser);
            if (await _datingRepository.SaveAll())
                return NoContent();

            throw new Exception($"Updating user with {id} failed in save");
        }
    }
}