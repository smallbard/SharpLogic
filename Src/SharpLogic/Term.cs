namespace SharpLogic;

public class Term : TermValue
{
    public Term(string functor, TermValue[] args)
        : base(null)
    {
        Functor = functor;
        Args = args;

        foreach (var arg in Args) arg.Parent = this;
    } 

    public string Functor { get; init; }

    public TermValue[] Args{ get; init; }

    public virtual Term CloneWithNewArgs(TermValue[] args)
    {
        return new Term(Functor, args);
    }

    public override string ToString()
    {
        return $"{Functor}/{Args.Length}";
    }
}