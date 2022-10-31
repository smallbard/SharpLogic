namespace SharpLogic.ByteCodeVM.Execution;

public class QueryVariable
{
    private Environment? _bindEnvironment;

    public QueryVariable(string name)
    {
        Name = name;
    }

    public string Name { get; init; }

    public object? Value { get; private set; }

    public bool IsBound => _bindEnvironment != null;

    public void Bind(object? value, Environment currentEnvironment)
    {
        Value = value;
        _bindEnvironment = currentEnvironment;
    }

    public void Unbind(Environment environment)
    {
        if (environment == _bindEnvironment)
        {
            Value = null;
            _bindEnvironment = null;
        }
    }
}