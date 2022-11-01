namespace SharpLogic.ByteCodeVM;

public enum OpCode : byte
{
    // Unification
    UnValCst,       // Unify a value constant with byte index in Px                 Arg size : 2
    UnRefCst,       // Unify a reference constant with byte index in Px             Arg size : 2
    UnValCstLgIdx,  // Unify a value constant with int index in Px                  Arg size : 5
    UnRefCstLgIdx,  // Unify a reference constant with int index in Px              Arg size : 5
    UnTrue,         // Unify true in Px                                             Arg size : 1
    UnFalse,        // Unify false in Px                                            Arg size : 1
    UnNull,         // Unify null in Px                                             Arg size : 1

    // Rule
    StackPxToAy,    // Store a register Px in a register Ay                         Arg size : 2

    // Rule and query
    NewEnvironment, // Create a new environment with n registers                    Arg size : 0
    Goal,           // Try a goal with a given functor store in managed constant    Arg size : 4
    Proceed,

    Fail,
    SwitchNot,

    // Comparison
    GreaterThan,
    LessThan,
    GreaterThanOrEqual,
    LessThanOrEqual,
    Equal,
    NotEqual,

    Add,
    Substract,
    Multiply,
    Divide,
    Modulus,

    Cut,

    UnifyReg,       // Unify two Registers                                          Arg size : 2
    NewVar,         // Declare a new variable in a register                         Arg size : 5
    OfType,         // Fail is variable is not of expected OfType                   Arg size : 5
    MbAccess,       // Access to a member of a type                                 Arg size : 6
}