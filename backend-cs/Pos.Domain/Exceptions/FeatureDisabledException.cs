using System;

namespace PosCs.Domain.Exceptions
{
    /// <summary>The tenant does not have the required feature enabled (maps to HTTP 403).</summary>
    public class FeatureDisabledException : Exception
    {
        public FeatureDisabledException(string featureKey)
            : base($"Feature disabled: {featureKey}") { }
    }
}
