﻿using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace GStore.API
{
    internal class TokenValidationHandler : DelegatingHandler
    {
        private static bool TryRetrieveToken( HttpRequestMessage request, out string token )
        {
            token = null;
            IEnumerable<string> authzHeaders;

            if( !request.Headers.TryGetValues( "Authorization", out authzHeaders ) || authzHeaders.Count() > 1 )
            {
                return false;
            }

            var bearerToken = authzHeaders.ElementAt( 0 );
            token = bearerToken.StartsWith( "Bearer " ) ? bearerToken.Substring( 7 ) : bearerToken;

            return true;
        }

        protected override Task<HttpResponseMessage> SendAsync( HttpRequestMessage request, CancellationToken cancellationToken )
        {
            HttpStatusCode statusCode;

            //determine whether a jwt exists or not
            if( !TryRetrieveToken( request, out string token ) )
            {
                statusCode = HttpStatusCode.Unauthorized;
                //allow requests with no token - whether a action method needs an authentication can be set with the claimsauthorization attribute
                return base.SendAsync( request, cancellationToken );
            }

            try
            {
                const string sec = "401b09eab3c013d4ca54922bb802bec8fd5318192b0a75f201d8b3727429090fb337591abd3e44453b954555b7a0812e1081c39b740293f765eae731f5a65ed1";

                var now = DateTime.UtcNow;
                var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey( System.Text.Encoding.Default.GetBytes( sec ) );

                JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
                TokenValidationParameters validationParameters = new TokenValidationParameters() {
                    ValidAudience = "http://localhost:50416",
                    ValidIssuer = "http://localhost:50416",
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    LifetimeValidator = this.LifetimeValidator,
                    IssuerSigningKey = securityKey
                };

                //extract and assign the user of the jwt
                Thread.CurrentPrincipal = handler.ValidateToken( token, validationParameters, out SecurityToken securityToken );
                //HttpContext.Current.User = handler.ValidateToken( token, validationParameters, out securityToken );

                return base.SendAsync( request, cancellationToken );
            }
            catch( SecurityTokenValidationException ex1 )
            {
                statusCode = HttpStatusCode.Unauthorized;
            }
            catch( Exception ex2  )
            {
                statusCode = HttpStatusCode.InternalServerError;
            }

            return Task<HttpResponseMessage>.Factory.StartNew( () => new HttpResponseMessage( statusCode ) { } );
        }

        public bool LifetimeValidator( DateTime? notBefore, DateTime? expires, SecurityToken securityToken, TokenValidationParameters validationParameters )
        {
            if( expires != null )
            {
                if( DateTime.UtcNow < expires ) return true;
            }

            return false;
        }
    }
}
