using Haven.Application.Common;
using Haven.Application.Common.Contracts;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.System.Queries.GetSystemInformation;

[AdminOnly]
public sealed class GetSystemInformationQuery : IQuery<SystemInformation>
{

}