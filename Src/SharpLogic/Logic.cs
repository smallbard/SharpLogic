using System.Reflection;
using System.Runtime.CompilerServices;
using SharpLogic.ByteCodeVM;
using SharpLogic.ByteCodeVM.Definition;

namespace SharpLogic;

public class Logic
{
    private readonly FactAndRule _factsAndRules;
    private readonly ValueConstants _valueConstants = new ValueConstants(null);
    private readonly ManagedConstants _managedConstants = new ManagedConstants(null);

    public Logic(Action<dynamic, IPredicates> termBuilding)
    {
        _factsAndRules = new FactAndRule(_valueConstants, _managedConstants);
        var dtb = new DynamicTermBuilder(_factsAndRules, null);
        termBuilding(dtb, dtb);
        dtb.CreateRemainingFacts();
    }

    public IQuery<T> Query<T>(Action<dynamic, IPredicates> queryBuilder)
    {
        var query = new Query<T>(new ValueConstants(_valueConstants), new ManagedConstants(_managedConstants), _factsAndRules);
        var dtb = new DynamicTermBuilder(null, query.AddTerm);
        queryBuilder(dtb, dtb);
        dtb.CreateRemainingFacts();

        return query;
    }

    public bool Any(Action<dynamic, IPredicates> queryBuilder)
    {
        var query = new Query<object>(new ValueConstants(_valueConstants), new ManagedConstants(_managedConstants), _factsAndRules);
        var dtb = new DynamicTermBuilder(null, query.AddTerm);
        queryBuilder(dtb, dtb);
        dtb.CreateRemainingFacts();

        return query.Any();
    }
}