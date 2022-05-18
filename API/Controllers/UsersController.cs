using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Authorize]
    public class UsersController : BaseApiController
    {
        private readonly IUserRepository _repository;
        private readonly IMapper _mapper;
        private readonly IPhotoService _photoService;
        public UsersController(IUserRepository repository, IMapper mapper, IPhotoService photoService)
        {
            _mapper = mapper;
            _repository = repository;
            _photoService = photoService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MemberDto>>> GetUsers()
        {
            var users = await _repository.GetMembersAsync();
            return Ok(users);
        }

        [HttpGet("{username}", Name = "GetUser")]
        public async Task<ActionResult<MemberDto>> GetUser(string username)
        {
            var awaitableMember = await _repository.GetMemberAsync(username);
            return awaitableMember;
        }

        [HttpPut]
        public async Task<ActionResult> UpdateUser(MemberUpdateDto memberUpdateDto)
        {
            var userName = User.GetUserName();
            var user = await _repository.GetUserByUsername(userName);

            _mapper.Map(memberUpdateDto, user);
            _repository.Update(user);

            if (await _repository.SaveAllAsync())
            {
                return NoContent();
            }
            return BadRequest("Saving data failed in API");
        }

        [HttpPost("add-photo")]
        public async Task<ActionResult<PhotoDto>> AddPhoto(IFormFile file)
        {
            var userName = User.GetUserName();
            var user = await _repository.GetUserByUsername(userName);

            var result = await _photoService.AddPhotoAsync(file);

            if (result.Error != null)
            {
                return BadRequest(result.Error.Message);
            }
            var photo = new Photo
            {
                Url = result.SecureUrl.AbsoluteUri,
                PublicId = result.PublicId
            };

            if (user.Photos.Count == 0)
            {
                photo.IsMain = true;
            }
            user.Photos.Add(photo);

            if (await _repository.SaveAllAsync())
            {
                var photoDto = _mapper.Map<Photo, PhotoDto>(photo);
                return CreatedAtRoute("GetUser", new { username = user.UserName }, photoDto);
            }
            return BadRequest("Problems adding photos");
        }

        [HttpPut("set-main-photo/{photoId}")]
        public async Task<ActionResult> SetMainPhoto(int photoId)
        {
            var user = await _repository.GetUserByUsername(User.GetUserName());
            if (user != null)
            {
                var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);
                if (photo.IsMain)
                {
                    return BadRequest("This is already your main photo");
                }
                var currentMainPhoto = user.Photos.FirstOrDefault(x => x.IsMain).IsMain = false;
                photo.IsMain = true;
                if (await _repository.SaveAllAsync())
                {
                    return NoContent();
                }

            }
            return BadRequest("Somethig went wrong with setting main photo");
        }
        [HttpDelete("delete-photo/{photoId}")]
        public async Task<ActionResult>DeletePhoto(int photoId)
        {
            var user = await _repository.GetUserByUsername(User.GetUserName());
            if (user != null)
            {
                var photoToDelete= user.Photos.FirstOrDefault(x => x.Id == photoId);
                if(photoToDelete==null){
                    return NotFound();
                }
                if(photoToDelete.IsMain){
                    return BadRequest("You cannot delete your main photo");
                }
                if(photoToDelete.PublicId!=null){
                    var deletionResult=await _photoService.DeletePhotoAsync(photoToDelete.PublicId);
                    if(deletionResult.Error!=null){
                        return BadRequest(deletionResult.Error.Message);
                    }
                }

                user.Photos.Remove(photoToDelete);
                if(await _repository.SaveAllAsync()){
                    return Ok();
                }

            }
                            return BadRequest("Photo failed to delete");
        }
    }
}