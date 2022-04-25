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
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        private readonly DataContext _context;
        private readonly ITokenService _tokenService;

        public AccountController(DataContext context, ITokenService tokenService)
        {
            _tokenService = tokenService;
            _context = context;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AppUser>>Register(RegisterDto registerDto)
        {
            if(await UserExists(registerDto.UserName)){
                return BadRequest("Username already exists");
            }
            using(var hmac=new HMACSHA512()){
                var user=new AppUser{
                    UserName=registerDto.UserName.ToLower(),
                    PasswordHash=hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password)),
                    PasswordSalt=hmac.Key
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                return user;
            }
        }
        [HttpPost("login")]
        public async Task<ActionResult<UserDto>>Login(LoginDto loginDto){
            var user=await _context.Users.SingleOrDefaultAsync(x=>x.UserName==loginDto.UserName);
            if(user==null){
                return Unauthorized("Invalid username");
            }

            using(var hmac=new HMACSHA512(user.PasswordSalt)){
                var computedHash=hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));
                if(computedHash.SequenceEqual(user.PasswordHash)){
                    var userDto=new UserDto(){
                        UserName=user.UserName,
                        Token=_tokenService.GenerateJsonWebToken(user)
                    };
                    return userDto;
                }
                else{
                    return Unauthorized("Bad password");
                }
            }

        }
        private async Task<bool> UserExists(string userName){
            var doesMatch=await _context.Users.AnyAsync(x=>x.UserName==userName);
            return doesMatch;
        }
    }
}