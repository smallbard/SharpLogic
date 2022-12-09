using SharpLogic.ByteCodeVM;
using SharpLogic.ByteCodeVM.Compilation;
using SharpLogic.ByteCodeVM.Indexing;

namespace SharpLogic;

public class Logic
{
    private readonly ValueConstants _valueConstants = new ValueConstants(null);
    private readonly ManagedConstants _managedConstants = new ManagedConstants(null);
    private readonly Compiler _compiler = new Compiler();
    private readonly ClausesIndex _clausesIndex;

    public Logic(Action<dynamic, IPredicates> termBuilding)
    {
        var astBuilder = new ASTBuilder();
        var dtb = new DynamicTermBuilder(astBuilder);
        termBuilding(dtb, dtb);

        var (codeContainer, offsets) = _compiler.Compile(astBuilder.Terms, _valueConstants, _managedConstants);

        _clausesIndex = new ClausesIndex();
        var code = codeContainer.Code.Slice(0, codeContainer.CodeLength);
        _clausesIndex.AddOffsets(offsets, code, IndexingMode.Append);
    }

    public Query<T> Query<T>(Action<dynamic, IPredicates> queryBuilder)
    {
        var astBuilder = new ASTBuilder();
        var dtb = new DynamicTermBuilder(astBuilder);
        queryBuilder(dtb, dtb);

        var (codeContainer, offsets) = _compiler.Compile(new[] { new Rule(string.Empty, Array.Empty<TermValue>(), astBuilder.Terms.ToArray()) }, _valueConstants, _managedConstants, true);
        var code = codeContainer.Code.Slice(0, codeContainer.CodeLength);
        _clausesIndex.AddOffsets(offsets, code, IndexingMode.Append); //TODO : remove this at the end of query

        return new Query<T>(
            new ValueConstants(_valueConstants),
            new ManagedConstants(_managedConstants),
            _clausesIndex,
            codeContainer);
    }

    public bool Any(Action<dynamic, IPredicates> queryBuilder)
    {
        return  Query<object>(queryBuilder).Any();
    }
}