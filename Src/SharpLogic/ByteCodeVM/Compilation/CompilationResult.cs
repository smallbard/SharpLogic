namespace SharpLogic.ByteCodeVM.Compilation;

public class CompilationResult
{
    public bool Succeed => string.IsNullOrEmpty(ErrorMessage);

    public string? ErrorMessage { get; init; }

    public static CompilationResult Success { get; } = new CompilationResult();

    public static CompilationResult Error(string error) => new CompilationResult { ErrorMessage = error };
}