using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using SharpLogic.ByteCodeVM;
using SharpLogic.ByteCodeVM.Compilation;

namespace SharpLogic;

public class Logic
{
    private readonly ValueConstants _valueConstants = new ValueConstants(null);
    private readonly ManagedConstants _managedConstants = new ManagedConstants(null);
    private readonly Compiler _compiler = new Compiler();
    private (ByteCodeContainer Code,  GetOffsetsDelegate GetOffsets) _factAndRule;

    public Logic(Action<dynamic, IPredicates> termBuilding)
    {
        var astBuilder = new ASTBuilder();
        var dtb = new DynamicTermBuilder(astBuilder);
        termBuilding(dtb, dtb);

        _factAndRule = _compiler.Compile(astBuilder.Terms, _valueConstants, _managedConstants);
    }

    public Query<T> Query<T>(Action<dynamic, IPredicates> queryBuilder)
    {
        var astBuilder = new ASTBuilder();
        var dtb = new DynamicTermBuilder(astBuilder);
        queryBuilder(dtb, dtb);

        return new Query<T>(
            new ValueConstants(_valueConstants),
            new ManagedConstants(_managedConstants),
            _factAndRule,
            _compiler.Compile(new[] { new Rule(string.Empty, Array.Empty<TermValue>(), astBuilder.Terms.ToArray()) }, _valueConstants, _managedConstants, true).Code);
    }

    public bool Any(Action<dynamic, IPredicates> queryBuilder)
    {
        var astBuilder = new ASTBuilder();
        var dtb = new DynamicTermBuilder(astBuilder);
        queryBuilder(dtb, dtb);

        return new Query<object>(
            new ValueConstants(_valueConstants),
            new ManagedConstants(_managedConstants),
            _factAndRule,
            _compiler.Compile(new[] { new Rule(string.Empty, Array.Empty<TermValue>(), astBuilder.Terms.ToArray()) }, _valueConstants, _managedConstants, true).Code).Any();
    }
}