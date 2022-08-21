using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        private readonly DataContext _context;
        private readonly ITokenService _tokenService;
        private readonly IMapper _mapper;

        public AccountController(DataContext context, ITokenService tokenService, IMapper mapper)
        {
            _tokenService = tokenService;
            _mapper = mapper;
            _context = context;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
        {
            if (await UserExists(registerDto.UserName))
            {
                return BadRequest("Username already exists");
            }

            var user = _mapper.Map<AppUser>(registerDto);
            using (var hmac = new HMACSHA512())
            {
                user.UserName = registerDto.UserName.ToLower();
                user.PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password));
                user.PasswordSalt = hmac.Key;
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                
            }
            return new UserDto{
                Username=user.UserName,
                Token=_tokenService.GenerateJsonWebToken(user),
                KnownAs=user.KnownAs,
                Gender=user.Gender
            };
        }
        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            var user = await _context.Users
            .Include(p => p.Photos)
            .SingleOrDefaultAsync(x => x.UserName == loginDto.UserName);
            if (user == null)
            {
                return Unauthorized("Invalid username");
            }

            using (var hmac = new HMACSHA512(user.PasswordSalt))
            {
                var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));
                if (computedHash.SequenceEqual(user.PasswordHash))
                {
                    var userDto = new UserDto()
                    {
                        Username = user.UserName,
                        Token = _tokenService.GenerateJsonWebToken(user),
                        PhotoUrl = user.Photos.FirstOrDefault(x => x.IsMain)?.Url,
                        KnownAs=user.KnownAs,
                        Gender=user.Gender
                    };
                    return userDto;
                }
                else
                {
                    return Unauthorized("Bad password");
                }
            }

        }
        private async Task<bool> UserExists(string userName)
        {
            var doesMatch = await _context.Users.AnyAsync(x => x.UserName == userName);
            return doesMatch;
        }
    }
}