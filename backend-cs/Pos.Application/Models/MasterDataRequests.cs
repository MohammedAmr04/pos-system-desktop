namespace PosCs.Application.Models
{
    public sealed class CreateCategoryRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    /// <summary>Null members keep their current value; an empty string clears the description.</summary>
    public sealed class UpdateCategoryRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool? IsActive { get; set; }
    }

    public sealed class CreateBrandRequest
    {
        public string Name { get; set; }
    }

    public sealed class UpdateBrandRequest
    {
        public string Name { get; set; }
        public bool? IsActive { get; set; }
    }

    public sealed class CreateUnitRequest
    {
        public string Name { get; set; }
    }

    public sealed class UpdateMasterUnitRequest
    {
        public string Name { get; set; }
        public bool? IsActive { get; set; }
    }
}
