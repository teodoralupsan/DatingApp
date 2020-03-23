using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DatingApp.API.Controllers
{
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
        public async Task<IActionResult> GetUsers()
        {
            var users = await _datingRepository.GetUsers();
            var returnUsers = _mapper.Map<IEnumerable<UserForListDto>>(users);

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