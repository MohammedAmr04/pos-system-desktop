using System.Collections.Generic;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Domain.Exceptions;

namespace PosCs.Application.Services
{
    /// <summary>Tenant feature toggle use cases.</summary>
    public class TenantFeaturesService
    {
        private readonly IAuthRepository _auth;
        private readonly ITenantFeatureRepository _features;
        private readonly IAccessControl _access;

        public TenantFeaturesService(IAuthRepository auth, ITenantFeatureRepository features, IAccessControl access)
        {
            _auth = auth;
            _features = features;
            _access = access;
        }

        public List<FeatureToggleState> GetFeatures(string currentUserId)
        {
            var tenantId = _access.GetTenantIdForUser(currentUserId);
            var rows = _auth.GetTenantFeatures(tenantId);

            var byKey = new Dictionary<string, bool>();
            foreach (var row in rows)
                byKey[row.FeatureKey] = row.Enabled;

            var result = new List<FeatureToggleState>();
            foreach (var key in FeatureCatalog.Keys)
                result.Add(new FeatureToggleState { Key = key, Enabled = byKey.TryGetValue(key, out var enabled) && enabled });
            return result;
        }

        /// <summary>Returns the tenant id the toggles were applied to (for audit logging).</summary>
        public string SetFeatures(string currentUserId, SetFeaturesRequest dto)
        {
            if (dto?.Features == null || dto.Features.Count == 0)
                throw new DomainValidationException("features required");

            var tenantId = _access.GetTenantIdForUser(currentUserId);

            foreach (var feature in dto.Features)
            {
                if (!FeatureCatalog.IsKnown(feature.Key))
                    throw new DomainValidationException($"Unknown feature: {feature.Key}");

                _features.UpsertFeature(tenantId, feature.Key, feature.Enabled);
            }

            return tenantId;
        }
    }
}
