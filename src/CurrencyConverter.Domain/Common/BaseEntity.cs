using System.ComponentModel.DataAnnotations;

namespace CurrencyConverter.Domain.Common;

public abstract class BaseEntity : IBaseEntity
{
    [Key]
    public int Id { get; private set; }

    /// <summary>
    /// This property is set/overwritten automatically during the SaveChangesAsync method.
    /// </summary>
    public DateTime DateCreated { get; set; }

    /// <summary>
    /// This property is set automatically during the SaveChangesAsync method for new records.
    /// </summary>
    public DateTime? DateUpdated { get; set; }
}
