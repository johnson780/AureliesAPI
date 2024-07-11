using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace Aurelius_API
{
    public class JwtMiddleware : DelegatingHandler
    {
        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (RequiresAuthorization(request))
            {
                IEnumerable<string> authorizationHeader;
                if (!request.Headers.TryGetValues("Authorization", out authorizationHeader))
                {
                    return request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Authorization header is missing");
                }

                var authorizationToken = authorizationHeader.FirstOrDefault();

                if (string.IsNullOrEmpty(authorizationToken) || !authorizationToken.StartsWith("Bearer "))
                {
                    return request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Authorization header is not in the correct format or does not have a token");
                }

                var encryptedToken = authorizationToken.Split(' ')[1];

                if (string.IsNullOrEmpty(encryptedToken))
                {
                    return request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Token is missing");
                }

                string decryptedToken = "";
                try
                {
                    decryptedToken = JwtToken.AesEncryption.DecryptToken(encryptedToken);
                }
                catch(Exception ex)
                {
                    return request.CreateErrorResponse(HttpStatusCode.Unauthorized, ex.Message);
                }

                if (JwtToken.IsTokenExpired(decryptedToken))
                {
                    return request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Token has expired");
                }

                if (!JwtToken.ValidateJwtToken(decryptedToken))
                {
                    return request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Token is not valid");
                }

                string companyID = JwtToken.GetCompanyIdFromToken(decryptedToken);
                string userID = JwtToken.GetUserIdFromToken(decryptedToken);
                //string password = JwtToken.GetPasswordFromToken(decryptedToken);

                HttpContext.Current.Items["EncryptedToken"] = encryptedToken;
                HttpContext.Current.Items["DecryptedToken"] = decryptedToken;
                HttpContext.Current.Items["CompanyID"] = companyID;
                HttpContext.Current.Items["UserID"] = userID;
                //request.Properties["Password"] = password;

                // Optionally update request header with decrypted token
                // request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", decryptedToken);
            }

            return await base.SendAsync(request, cancellationToken);
        }

        private bool RequiresAuthorization(HttpRequestMessage request)
        {
            var path = request.RequestUri.AbsolutePath;

            return path.StartsWith("/api/authentication/getcompany")
                || path.StartsWith("/api/doctor/savedoctorcontract")
                // Add other paths as per your requirement
                ;
        }
    }
}