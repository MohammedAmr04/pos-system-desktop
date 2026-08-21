using System;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PosCs.Domain.Entities;

namespace PosCs.Api
{
    /// <summary>
    /// Camel-cases everything like the legacy global serializer, except
    /// Invoice.InvoiceDetail which was pinned to PascalCase via [JsonProperty]
    /// in the legacy model. The frontend reads both spellings; this keeps the
    /// wire format byte-identical to the legacy API.
    /// </summary>
    public class LegacyJsonContractResolver : CamelCasePropertyNamesContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            if (member.DeclaringType == typeof(Invoice) &&
                string.Equals(property.PropertyName, "invoiceDetail", StringComparison.Ordinal))
            {
                property.PropertyName = "InvoiceDetail";
            }
            return property;
        }
    }
}
