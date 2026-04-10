using Rsk.Enforcer.Oasis.PolicyModel;
using Rsk.Enforcer.PIP;
using Rsk.Enforcer.PolicyModels;

namespace AuthZenPolicyServer;

public class AcmeCorpPerson()
{
    [PolicyAttributeValue(PolicyAttributeCategories.Subject, "role")]
    public IEnumerable<string> Roles { get; init; } = [];
}

public class SubjectAttributeProvider : RecordAttributeValueProvider<AcmeCorpPerson>
{
    private static readonly Dictionary<string, AcmeCorpPerson> people = new()
    {
        ["bob"] = new AcmeCorpPerson() { Roles = ["employee"]},
        ["alice"] = new AcmeCorpPerson() { Roles = ["employee","manager"]},
    };
    
    protected override async Task<AcmeCorpPerson> GetRecordValue(IAttributeResolver attributeResolver, CancellationToken ct)
    {
        IReadOnlyCollection<string>? identifiers = await attributeResolver
            .Resolve<string>(Rsk.Enforcer.Oasis.Attributes.Subject.Identifier, ct);

        string? identifier = identifiers.SingleOrDefault();
        if (identifier == null) return null!;

        AcmeCorpPerson person = new AcmeCorpPerson();

        return people[identifier];
    }
}