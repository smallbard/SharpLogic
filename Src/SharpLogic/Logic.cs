using SharpLogic.ByteCodeVM;
using SharpLogic.ByteCodeVM.Compilation;
using SharpLogic.ByteCodeVM.Indexing;

namespace SharpLogic;

public class Logic
{
    private readonly ValueConstants _valueConstants = new ValueConstants(null);
    private readonly ManagedConstants _managedConstants = new ManagedConstants(null);
    private readonly Compiler _compiler = new Compiler();
    private readonly KbdCodeContainer _kbdCodeContainer;

    public Logic(Action<dynamic, IPredicates> termBuilding)
    {
        var astBuilder = new ASTBuilder();
        var dtb = new DynamicTermBuilder(astBuilder);
        termBuilding(dtb, dtb);

        var (codeContainer, getOffsets) = _compiler.Compile(astBuilder.Terms, _valueConstants, _managedConstants);
        _kbdCodeContainer = new KbdCodeContainer((codeContainer.Code.Slice(0, codeContainer.CodeLength), getOffsets));
    }

    public Query<T> Query<T>(Action<dynamic, IPredicates> queryBuilder)
    {
        var astBuilder = new ASTBuilder();
        var dtb = new DynamicTermBuilder(astBuilder);
        queryBuilder(dtb, dtb);

        return new Query<T>(
            new ValueConstants(_valueConstants),
            new ManagedConstants(_managedConstants),
            _kbdCodeContainer,
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
            _kbdCodeContainer,
            _compiler.Compile(new[] { new Rule(string.Empty, Array.Empty<TermValue>(), astBuilder.Terms.ToArray()) }, _valueConstants, _managedConstants, true).Code).Any();
    }
}