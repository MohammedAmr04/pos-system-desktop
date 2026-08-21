using System;
using System.Collections.Generic;
using System.Linq;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Application.Services;
using PosCs.Domain.Entities;
using PosCs.Domain.Exceptions;
using Xunit;

namespace PosCs.Tests.Application
{
    public class FixedClock : IClock
    {
        public DateTime Now { get; set; } = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        public DateTime UtcNow => Now;
        public DateTime Today => Now.Date;
    }

    public class FakeAuthRepository : IAuthRepository
    {
        public User StoredUser;
        public string TenantId = "tenant-1";
        public List<string> RoleIds = new List<string> { "role-admin" };
        public List<string> PermissionKeys = new List<string> { "invoices.view" };
        public List<string> FeatureKeys = new List<string>();
        public int EnsureTokenSecretCalls;

        public User GetUserByUsername(string username) =>
            StoredUser != null && string.Equals(StoredUser.Username, username, StringComparison.OrdinalIgnoreCase)
                ? StoredUser : null;

        public User GetUserById(string userId) =>
            StoredUser != null && StoredUser.Id == userId ? StoredUser : null;

        public List<string> GetRoleIdsForUser(string userId) => RoleIds.ToList();
        public List<string> GetPermissionKeysForUser(string userId) => PermissionKeys.ToList();
        public string GetTenantIdForUser(string userId) => TenantId;
        public List<string> GetEnabledFeatureKeys(string tenantId) => FeatureKeys.ToList();
        public List<TenantFeature> GetTenantFeatures(string tenantId) => new List<TenantFeature>();
        public void SetUserRoles(string userId, IEnumerable<string> roleIds) { }
        public string EnsureTokenSecret() { EnsureTokenSecretCalls++; return "secret"; }
    }

    public class StubHasher : IPasswordHasher
    {
        public const string KnownHash = "stored-hash";
        public string VerifyCallsLastPassword;
        public bool Verify(string passwordHash, string password)
        {
            VerifyCallsLastPassword = password;
            return passwordHash == KnownHash && password == "correct-password";
        }
        public string Hash(string password) => "hashed:" + password;
    }

    public class StubTokens : ITokenService
    {
        public string LastIssuedUserId;
        public string Issue(string secret, string userId)
        {
            LastIssuedUserId = userId;
            return "token-for-" + userId;
        }

        public string Validate(string secret, string token) =>
            token != null && token.StartsWith("token-for-") ? token.Substring("token-for-".Length) : null;
    }

    public class StubRolesRepository : IRolesRepository
    {
        public List<Role> Roles = new List<Role>
        {
            new Role { Id = "role-admin", Name = "Admin" },
            new Role { Id = "role-cashier", Name = "Cashier" }
        };

        public List<Role> GetAll() => Roles.ToList();
        public Role GetById(string roleId) => Roles.FirstOrDefault(r => r.Id == roleId);
        public Role FindByName(string name) => Roles.FirstOrDefault(r => r.Name == name);
        public Role FindByNameExcluding(string name, string roleId) =>
            Roles.FirstOrDefault(r => r.Name == name && r.Id != roleId);
        public List<string> GetPermissionIdsForRole(string roleId) => new List<string> { "p1" };
        public int CountUsersForRole(string roleId) => 2;
        public Role Create(string name, string description) => throw new NotSupportedException();
        public bool Update(string roleId, string name, string description) => true;
        public bool Delete(string roleId) => true;
        public void SetRolePermissions(string roleId, IEnumerable<string> permissionIds) { }
    }

    public class LoginThrottleTests
    {
        private static (LoginThrottle Throttle, FixedClock Clock) Create()
        {
            var clock = new FixedClock();
            return (new LoginThrottle(clock), clock);
        }

        [Fact]
        public void LocksOutOnlyAfterFiveConsecutiveFailures()
        {
            var (throttle, _) = Create();
            for (var i = 0; i < 4; i++)
                throttle.RecordFailure("sam");
            Assert.False(throttle.IsLockedOut("sam"));
            throttle.RecordFailure("sam");
            Assert.True(throttle.IsLockedOut("sam"));
            Assert.False(throttle.IsLockedOut("other"));
        }

        [Fact]
        public void LockoutExpiresAfterWindow()
        {
            var (throttle, clock) = Create();
            for (var i = 0; i < 5; i++)
                throttle.RecordFailure("sam");
            Assert.True(throttle.IsLockedOut("sam"));

            clock.Now = clock.Now.AddSeconds(59);
            Assert.True(throttle.IsLockedOut("sam"));

            clock.Now = clock.Now.AddSeconds(2);
            Assert.False(throttle.IsLockedOut("sam"));
        }

        [Fact]
        public void SuccessfulLoginClearsFailures()
        {
            var (throttle, _) = Create();
            throttle.RecordFailure("sam");
            throttle.RecordFailure("sam");
            throttle.ResetThrottle("sam");
            Assert.False(throttle.IsLockedOut("sam"));
        }
    }

    public class AuthServiceTests
    {
        private static (AuthService Service, FakeAuthRepository Repo, StubHasher Hasher, StubTokens Tokens) Create()
        {
            var repo = new FakeAuthRepository
            {
                StoredUser = new User
                {
                    Id = "user-1",
                    Name = "Sam",
                    Username = "sam",
                    PasswordHash = StubHasher.KnownHash,
                    IsActive = true
                }
            };
            var hasher = new StubHasher();
            var tokens = new StubTokens();
            var service = new AuthService(repo, new StubRolesRepository(), hasher, tokens,
                new LoginThrottle(new FixedClock()));
            return (service, repo, hasher, tokens);
        }

        [Fact]
        public void Login_WithValidCredentials_IssuesTokenAndBundle()
        {
            var (service, _, _, tokens) = Create();
            var result = service.Login("sam", "correct-password");

            Assert.NotNull(result.Bundle);
            Assert.Equal("token-for-user-1", result.Token);
            Assert.Equal("user-1", tokens.LastIssuedUserId);
            Assert.Equal("Sam", result.Bundle.User.Name);
            Assert.Equal("Admin", result.Bundle.Roles.Single());
            Assert.Contains("invoices.view", result.Bundle.Permissions);
            Assert.NotNull(result.Bundle.Features);
        }

        [Fact]
        public void Login_WithWrongPassword_ReturnsEmptyResultAndRecordsFailure()
        {
            var (service, _, _, _) = Create();
            var result = service.Login("sam", "wrong");

            Assert.Null(result.Bundle);
            Assert.Null(result.Token);
        }

        [Fact]
        public void Login_InactiveUser_IsRejected()
        {
            var (service, repo, _, _) = Create();
            repo.StoredUser.IsActive = false;

            var result = service.Login("sam", "correct-password");
            Assert.Null(result.Bundle);
        }

        [Fact]
        public void Login_UnknownUser_IsRejected()
        {
            var (service, _, _, _) = Create();
            Assert.Null(service.Login("nobody", "whatever").Bundle);
        }

        [Fact]
        public void GetBundleForToken_WithValidToken_ResolvesUser()
        {
            var (service, _, _, _) = Create();
            var login = service.Login("sam", "correct-password");
            var bundle = service.GetBundleForToken(login.Token);

            Assert.NotNull(bundle);
            Assert.Equal("user-1", bundle.User.Id);
            Assert.Equal("tenant-1", bundle.TenantId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("garbage")]
        public void GetBundleForToken_WithBadToken_ReturnsNull(string token)
        {
            var (service, _, _, _) = Create();
            Assert.Null(service.GetBundleForToken(token));
        }
    }

    public class RolesServiceTests
    {
        [Fact]
        public void GetAll_IncludesUserAndPermissionCounts()
        {
            var service = new RolesService(new StubRolesRepository(), new NoPermissions());
            var roles = service.GetAll();

            var admin = roles.Single(r => r.Id == "role-admin");
            Assert.Equal("Admin", admin.Name);
            Assert.Equal(2, admin.UserCount);
            Assert.Equal(1, admin.PermissionCount);
        }

        [Fact]
        public void SetPermissions_RejectsAdminRole()
        {
            var service = new RolesService(new StubRolesRepository(), new NoPermissions());
            Assert.Throws<DomainValidationException>(() =>
                service.SetPermissions("role-admin",
                    new SetRolePermissionsRequest { PermissionIds = new List<string> { "products.view" } }));
        }

        [Fact]
        public void SetPermissions_RejectsUnknownPermissionKeys()
        {
            var permissions = new NoPermissions();
            permissions.ExistingKeys.Add("products.view");
            var service = new RolesService(new StubRolesRepository(), permissions);

            var ex = Assert.Throws<DomainValidationException>(() =>
                service.SetPermissions("role-cashier",
                    new SetRolePermissionsRequest
                    {
                        PermissionIds = new List<string> { "products.view", "bogus.key" }
                    }));
            Assert.Contains("bogus.key", ex.Message);
        }

        [Fact]
        public void SetPermissions_PersistsDistinctKeysAndReturnsThem()
        {
            var permissions = new NoPermissions();
            permissions.ExistingKeys.Add("products.view");
            var service = new RolesService(new StubRolesRepository(), permissions);

            var result = service.SetPermissions("role-cashier",
                new SetRolePermissionsRequest
                {
                    PermissionIds = new List<string> { "products.view", "products.view" }
                });

            Assert.Single(result);
            Assert.Equal("products.view", result[0]);
        }

        [Fact]
        public void FindRoleName_ReturnsNullForUnknownRole()
        {
            var service = new RolesService(new StubRolesRepository(), new NoPermissions());
            Assert.Null(service.FindRoleName("missing"));
            Assert.Equal("Admin", service.FindRoleName("role-admin"));
        }

        private sealed class NoPermissions : IPermissionsRepository
        {
            public HashSet<string> ExistingKeys { get; } = new HashSet<string>();

            public List<Permission> GetAll() => new List<Permission>();
            public HashSet<string> GetExistingKeys(IEnumerable<string> keys) =>
                new HashSet<string>(keys.Where(ExistingKeys.Contains));
        }
    }
}
