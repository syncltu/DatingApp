using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace API.Extensions
{
    public static class ClaimsPrincipleExtensions
    {
        public static string GetUserName(this ClaimsPrincipal user){
            var userName=user.FindFirst(ClaimTypes.Name)?.Value;
            return userName;
        }
         public static int GetUserId(this ClaimsPrincipal user){
            var userId=user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(userId);
        }
    }
}