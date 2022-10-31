namespace SharpLogic;

public class Term : TermValue
{
    public Term(string functor, TermValue[] args)
        : base(null)
    {
        Functor = functor;
        Args = args;
        Value = this;
    } 

    public string Functor { get; init; }

    public TermValue[] Args{ get; init; }
}