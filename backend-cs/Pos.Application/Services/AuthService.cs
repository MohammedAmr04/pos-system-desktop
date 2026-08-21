using System.Linq;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Domain.Exceptions;

namespace PosCs.Application.Services
{
    /// <summary>
    /// Authentication use cases: login with throttling and access-bundle resolution.
    /// </summary>
    public class AuthService
    {
        private readonly IAuthRepository _repo;
        private readonly IRolesRepository _roles;
        private readonly IPasswordHasher _hasher;
        private readonly ITokenService _tokens;
        private readonly LoginThrottle _throttle;

        public AuthService(IAuthRepository repo, IRolesRepository roles, IPasswordHasher hasher,
            ITokenService tokens, LoginThrottle throttle)
        {
            _repo = repo;
            _roles = roles;
            _hasher = hasher;
            _tokens = tokens;
            _throttle = throttle;
        }

        public LoginResult Login(string username, string password)
        {
            if (_throttle.IsLockedOut(username))
                throw new LoginLockedException();

            var user = _repo.GetUserByUsername(username);
            AccessBundle bundle = null;
            string token = null;

            if (user != null && user.IsActive && _hasher.Verify(user.PasswordHash, password))
            {
                bundle = BuildBundle(user);
                token = _tokens.Issue(_repo.EnsureTokenSecret(), user.Id);
            }

            if (bundle == null)
            {
                _throttle.RecordFailure(username);
                return new LoginResult();
            }

            _throttle.ResetThrottle(username);
            return new LoginResult { Bundle = bundle, Token = token };
        }

        public AccessBundle GetBundleForToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;
            var secret = _repo.EnsureTokenSecret();
            var userId = _tokens.Validate(secret, token);
            if (userId == null)
                return null;
            var user = _repo.GetUserById(userId);
            if (user == null || !user.IsActive)
                return null;
            return BuildBundle(user);
        }

        public AccessBundle BuildBundle(Domain.Entities.User user)
        {
            var bundle = new AccessBundle
            {
                User = user,
                TenantId = _repo.GetTenantIdForUser(user.Id),
                Roles = _repo.GetRoleIdsForUser(user.Id)
                    .Select(id => _roles.GetById(id)?.Name ?? id).ToList(),
                Permissions = _repo.GetPermissionKeysForUser(user.Id).ToList()
            };

            if (!string.IsNullOrEmpty(bundle.TenantId))
                bundle.Features = _repo.GetEnabledFeatureKeys(bundle.TenantId).ToList();

            return bundle;
        }
    }
}
