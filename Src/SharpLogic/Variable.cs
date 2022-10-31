using System.Dynamic;

namespace SharpLogic;

public class Variable : TermValue
{
    public Variable(string name)
        : base (null)
    {
        VariableName = name;
    }

    public string VariableName { get; init; }
}