using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using PosCs.Domain.Exceptions;

namespace PosCs.Api
{
    /// <summary>Maps domain exceptions to the legacy HTTP error responses.</summary>
    public static class ApiErrors
    {
        /// <summary>True when the exception is a typed domain error with a legacy-mapped status.</summary>
        public static bool IsHandled(Exception ex)
        {
            return ex is DomainValidationException
                || ex is NotFoundException
                || ex is PermissionDeniedException
                || ex is FeatureDisabledException
                || ex is LoginLockedException
                || ex is InsufficientStockException;
        }

        public static HttpResponseMessage From(HttpRequestMessage request, Exception ex, string fallbackMessage)
        {
            if (ex is DomainValidationException)
                return Error(request, HttpStatusCode.BadRequest, ex.Message);
            if (ex is NotFoundException)
                return Error(request, HttpStatusCode.NotFound, ex.Message);
            if (ex is PermissionDeniedException || ex is FeatureDisabledException)
                return Error(request, HttpStatusCode.Forbidden, ex.Message);
            if (ex is LoginLockedException)
                return Error(request, (HttpStatusCode)429, ex.Message);
            if (ex is InsufficientStockException)
                return Error(request, HttpStatusCode.BadRequest, ex.Message);
            return Error(request, HttpStatusCode.InternalServerError, fallbackMessage);
        }

        private static HttpResponseMessage Error(HttpRequestMessage request, HttpStatusCode status, string message)
        {
            return request.CreateErrorResponse(status, message);
        }
    }

    /// <summary>Reads the authenticated user id stored by ApiAuthMiddleware.</summary>
    public static class RequestAuthExtensions
    {
        public const string UserIdKey = "PosCs.UserId";

        public static string GetOwinContextUserId(this HttpRequestMessage request)
        {
            return request.GetOwinContext().Get<string>(UserIdKey);
        }
    }
}
