using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DatingApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // by default every user will have to authenticate to every single method, 
    // but for this controller we need allow anonymous since the user it's registering or logs in
    [AllowAnonymous]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IMapper _mapper;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public AuthController(IConfiguration config, IMapper mapper,
            UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _mapper = mapper;
            _config = config;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserForRegisterDto userForRegisterDto)
        {
            // userManager verifies the unicity of the user by default

            var userToCreate = _mapper.Map<User>(userForRegisterDto);

            var result = await _userManager.CreateAsync(userToCreate, userForRegisterDto.Password);
            if (result.Succeeded)
            {
                var userToReturn = _mapper.Map<UserForDetailsDto>(userToCreate);
                return CreatedAtRoute(
                    "GetUser",
                    new { controller = "Users", id = userToCreate.Id},
                    userToReturn
                );
            }

            return BadRequest(result.Errors);
        }   

        /* Register method witout IdentityUser
        [HttpPost("register")]
        public async Task<IActionResult> Register(UserForRegisterDto userForRegisterDto)
        {
            userForRegisterDto.Username = userForRegisterDto.Username.ToLower();
            if (await _repo.UserExists(userForRegisterDto.Username))
                return BadRequest("Username already exists!");
            var userToCreate = _mapper.Map<User>(userForRegisterDto);
            var cretedUser = await _repo.Register(userToCreate, userForRegisterDto.Password);
            var userToReturn = _mapper.Map<UserForDetailsDto>(cretedUser);
            return CreatedAtRoute(
                "GetUser",
                new { controller = "Users", id = cretedUser.Id },
                userToReturn);
        }*/

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserForLoginDto userForLoginDto)
        {
            var dbUser = await _userManager.FindByNameAsync(userForLoginDto.Username);

            var result = await _signInManager.CheckPasswordSignInAsync(dbUser, userForLoginDto.Password, false);
            if (result.Succeeded)
            {
                var user = _mapper.Map<UserForListDto>(dbUser);
                return Ok(new
                {
                    token = GenerateJwtToken(dbUser).Result,
                    user
                });
            }
            return Unauthorized();
        }

        /* Login method witout IdentityUser
        [HttpPost("login")]
        public async Task<IActionResult> Login(UserForLoginDto userForLoginDto)
        {
            var dbUser = await _repo.Login(userForLoginDto.Username.ToLower(), userForLoginDto.Password);
            if (dbUser == null)
                return Unauthorized();

            var user = _mapper.Map<UserForListDto>(dbUser);

            return Ok(new
            {
                token = GenerateJwtToken(dbUser),
                user
            });
        } */

        private async Task<string> GenerateJwtToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName)
            };

            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var appSettingsSecurityKey = _config.GetSection("AppSettings:Token").Value; //used for creating a securitty key for the token
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(appSettingsSecurityKey));
            var cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = cred
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}