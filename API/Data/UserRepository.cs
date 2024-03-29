using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class UserRepository : IUserRepository
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;
        public UserRepository(DataContext context,IMapper mapper){
            _mapper = mapper;
            _context = context;
        }

        public async Task<MemberDto> GetMemberAsync(string userName)
        {
             return await _context.Users
            .Where(x=>x.UserName==userName)
            .ProjectTo<MemberDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();

        }

        public async Task<MemberDto> GetMemberByIdAsync(int id)
        {
             return await _context.Users
            .Where(x=>x.Id==id)
            .ProjectTo<MemberDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();
        }

        public async Task<PagedList<MemberDto>> GetMembersAsync(UserParams userParams)
        {
            
            var query=  _context.Users.AsQueryable();

            query=query.Where(x=>x.UserName!=userParams.CurrentUserName);
            query=query.Where(x=>x.Gender==userParams.Gender);

            var minDateOfBirth=DateTime.Today.AddYears(-userParams.MaxAge-1);
            var maxDob=DateTime.Today.AddYears(-userParams.MinAge);

            query=query.Where(x=>x.DateOfBirth>=minDateOfBirth && x.DateOfBirth<=maxDob);
            query=userParams.OrderBy switch{
                "created"=>query.OrderByDescending(u=>u.Created),
                _=>query.OrderByDescending(u=>u.LastActive)
            };

            return await PagedList<MemberDto>.CreateAsync(query.ProjectTo<MemberDto>(_mapper.ConfigurationProvider)
            .AsNoTracking(),
            userParams.PageNumber,
            userParams.PageSize);
        }

        public async Task<AppUser> GetUserByIdAsync(int id)
        {
            return await _context.Users
            .FindAsync(id);
        }

        public async Task<AppUser> GetUserByUsername(string userName)
        {
            return await _context.Users
            .Include(p=>p.Photos)
            .SingleOrDefaultAsync(x=>x.UserName==userName);
        }

        public async Task<IEnumerable<AppUser>> GetUsersAsync()
        {
            return await _context.Users
            .Include(p=>p.Photos)
            .ToListAsync();
        }

        public async Task<bool> SaveAllAsync()
        {
            return await _context.SaveChangesAsync()>0;
        }

        public void Update(AppUser user)
        {
            _context.Entry(user).State=EntityState.Modified;
        }
    }
}