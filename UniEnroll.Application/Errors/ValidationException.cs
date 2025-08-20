using System.Runtime.Serialization;

namespace UniEnroll.Application.Errors;

[Serializable]
public sealed class ValidationException : DomainException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(string message)
        : base(message, ErrorCodes.ValidationFailed, status: 422)
        => Errors = new Dictionary<string, string[]>();

    public ValidationException(string message, IDictionary<string, string[]> errors)
        : base(message, ErrorCodes.ValidationFailed, status: 422)
        => Errors = new Dictionary<string, string[]>(errors);

    private ValidationException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        Errors = info.GetValue(nameof(Errors), typeof(Dictionary<string, string[]>)) as Dictionary<string, string[]>
                 ?? new Dictionary<string, string[]>();
    }
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(Errors), Errors, typeof(Dictionary<string, string[]>));
    }
}
