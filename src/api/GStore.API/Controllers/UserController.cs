﻿using System.Linq;
using System.Threading.Tasks;
using GStore.Core.Data;
using GStore.Core.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using GStore.API.Models;
using System.Security.Claims;
using MongoDB.Bson;
using GStore.API.Comon;
using Microsoft.AspNetCore.Authorization;

namespace GStore.API.Controllers
{
    [ApiVersion( "1.0" )]
    [Route( "api/v{version:apiVersion}/user" )]
    public class UserController : BaseController
    {
        public UserController( IConfiguration config, ILogger<UserController> logger, DataContext context ) :
            base( config, logger, context ) { }

        [HttpPost( "authenticate" )]
        public async Task<IActionResult> Authenticate( string username, string password )
        {
            Logger.LogDebug( "POST[Authenticate]" );

            var user = await UnitOfWork.Repository<User>()
                                       .GetSingleAsync( u => u.Username == username &&
                                                             u.Password == password &&
                                                             !u.Deleted );

            if( user == null ) return Forbid();

            return Ok( new {
                token = TokenService.GenerateToken( user )
            } );
        }

        [Authorize( Policy = "AdminApi" )]
        [HttpGet("list")]
        public async Task<IActionResult> List()
        {
            Logger.LogDebug( "GET[List]" );

            var users = await UnitOfWork.Repository<User>().GetListAsync( u => !u.Deleted );

            var result = users.Select( u => UserResult.Create( u ) );

            return Ok( result );
        }

        [Authorize( Policy = "AdminApi" )]
        [HttpGet( "{id}" )]
        public async Task<IActionResult> Get( string id )
        {
            Logger.LogDebug( "GET[User]" );

            if( ObjectId.TryParse( id, out ObjectId oid ) )
            {
                var user = await UnitOfWork.Repository<User>().GetByIdAsync( oid );

                if( user == null ) return new NotFoundObjectResult( oid );

                return Ok( UserResult.Create( user ) );
            }

            return BadRequest();
        }

        [Authorize]
        [HttpGet( "info" )]
        public async Task<IActionResult> Info()
        {
            Logger.LogDebug( "GET[Info]" );

            if( TokenService.ReadToken( Request, out ClaimsPrincipal principal ) )
            {
                string uid = principal.Claims.Where( c => c.Type == "UserId" )
                                      .Select( c => c.Value )
                                      .FirstOrDefault();

                if( ObjectId.TryParse( uid, out ObjectId oid ) )
                {
                    var user = await UnitOfWork.Repository<User>().GetByIdAsync( oid );

                    if( user == null ) return new NotFoundObjectResult( oid );

                    return Ok( UserResult.Create( user ) );
                }

                return BadRequest();
            }

            return BadRequest();
        }
    }
}