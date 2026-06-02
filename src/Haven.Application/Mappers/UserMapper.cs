using Haven.Application.Common.Contracts;
using Haven.Domain.Aggregates;
using Riok.Mapperly.Abstractions;

namespace Haven.Application.Mappers;

[Mapper]
public static partial class UserMapper
{
    public static MeResponse ToMeResponse(this User user)
    {
        var response = user.ToMeResponsePartial();
        response.Permissions = user.Permissions.Select(p => p.Name).ToArray();
        return response;
    }
    private static partial MeResponse ToMeResponsePartial(this User user);
    
}