using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.HelloWorld;

public class GetHelloWorldQueryHandler : IQueryHandler<GetHelloWorldQuery, string>
{
    public ValueTask<Result<string>> Handle(GetHelloWorldQuery request, CancellationToken cancellationToken)
    {
        var message = "Hello, World! 🎉";
        return new ValueTask<Result<string>>(Result<string>.Success(message));
    }
}
