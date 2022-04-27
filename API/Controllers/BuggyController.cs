using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Data;
using API.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class BuggyController:BaseApiController
    {
        private readonly DataContext _dataContext;
        public BuggyController(DataContext dataContext){
            _dataContext = dataContext;
        }

        [Authorize]
        [HttpGet("auth")]
        public ActionResult<string>GetAuthException(){
            return "random things"; 
        }

        [HttpGet("not-found")]
        public ActionResult<AppUser>GetNotFound(){
            var test=_dataContext.Users.Find(-1);
            if(test==null){
                return NotFound();
            }
            else{
                return Ok(test);
            }
        }
        [HttpGet("server-error")]
        public ActionResult<string>GetServerError(){
            var test=_dataContext.Users.Find(-1);
            var thingToReturn=test.ToString();
            return thingToReturn;
        }
        [HttpGet("bad-request")]
        public ActionResult<string>GetBadRequest(){
            return BadRequest("This was not a good request");
        }

    }
}