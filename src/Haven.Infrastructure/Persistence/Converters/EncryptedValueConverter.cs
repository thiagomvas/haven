using Haven.Application.Common.Interfaces;
using Haven.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Haven.Infrastructure.Persistence.Converters;

public sealed class EncryptedValueConverter : ValueConverter<EncryptedValue, string>
{
    public EncryptedValueConverter(IEncryptionService encryptionService)
        : base(
            v => encryptionService.Encrypt(v.Value),
            v => EncryptedValue.From(encryptionService.Decrypt(v)))
    {
    }
}