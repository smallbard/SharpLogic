using System.Security;
using SharpLogic.ByteCodeVM.Execution;

namespace SharpLogic.Tests;

[TestClass]
public class UnificationTests
{
    private Unification? _unification;
    private StackFrame? _currentStackFrame;

    [DataTestMethod]
    [DataRow(5)]
    [DataRow(2.3)]
    [DataRow(3.5f)]
    [DataRow('c')]
    [DataRow("str")]
    public void UnifyPrimitivesAndString(object value)
    {
        TestUnify(value);
    }

    [TestMethod]
    public void UnifyDecimal()
    {
        TestUnify(2.3m);
    }

    [TestMethod]
    public void UnifyRefType()
    {
        TestUnify(new User("test", 1));
    }

    [TestMethod]
    public void UnifyStruct()
    {
        TestUnify(new UserStruct { Login = "test", Id = 5 });
    }

    [DataTestMethod]
    [DataRow(4, 4, false)]
    [DataRow(4, 3, true)]
    public void UnifyTwoBoundVars(object value1, object value2, bool failed)
    {
        var v1 = new QueryVariable("v1");
        v1.Instantiate(value1, _currentStackFrame!);

        var v2 = new QueryVariable("v2");
        v2.Instantiate(value2, _currentStackFrame!);

        Assert.AreEqual(failed, _unification!.Unify(new RegisterValue { Value = v1 }, new RegisterValue { Value = v2 }));
    }

    [TestMethod]
    public void UnifyBoundVarWithUnboundVar()
    {
        var v1 = new QueryVariable("v1");
        v1.Instantiate("5", _currentStackFrame!);

        var v2 = new QueryVariable("v2");

        Assert.IsFalse(_unification!.Unify(new RegisterValue { Value = v1 }, new RegisterValue { Value = v2 }));
        Assert.AreEqual(v1.Value, v2.Value);

        v1 = new QueryVariable("v1");
        
        v2 = new QueryVariable("v2");
        v2.Instantiate("5", _currentStackFrame!);

        Assert.IsFalse(_unification!.Unify(new RegisterValue { Value = v1 }, new RegisterValue { Value = v2 }));
        Assert.AreEqual(v2.Value, v1.Value);
    }

    [TestMethod]
    public void UnifyTwoUnboundVars()
    {
        var v1 = new QueryVariable("v1");
        var v2 = new QueryVariable("v2");

        Assert.IsFalse(_unification!.Unify(new RegisterValue { Value = v1 }, new RegisterValue { Value = v2 }));

        v1.Instantiate(5, _currentStackFrame!);
        Assert.AreEqual(v1.Value, v2.Value);
    }

    private void TestUnify(object value)
    {
        // Unify to bound register to expected value
        var register = new RegisterValue { Value = value };
        Assert.IsFalse(_unification!.Unify(value, register));

        // Unify to bound register to unexpected value
        register = new RegisterValue { Value = "unexpected!"};
        Assert.IsTrue(_unification.Unify(value, register));

        // Unify to unbound register
        register = new RegisterValue();
        Assert.IsFalse(_unification.Unify(value, register));
        Assert.AreEqual(value, register.Value);

        // Unify to unbound variable
        var variable = new QueryVariable("X");
        register = new RegisterValue { Value = variable };
        Assert.IsFalse(_unification!.Unify(value, register));
        Assert.IsTrue(variable.Instantiated);
        Assert.AreEqual(value, variable.Value);

        // Unify to bound variable with expected value
        Assert.IsFalse(_unification!.Unify(value, register));

        // Unify to bound variable with unexpected value
        variable.Uninstantiate(_currentStackFrame!);
        variable.Instantiate("unexpected!", _currentStackFrame!);
        Assert.IsTrue(_unification!.Unify(value, register));
    }

    [TestInitialize]
    public void Setup()
    {
        _currentStackFrame = new StackFrame(null);
        _unification = new Unification(() => _currentStackFrame);
    }

    public record User(string Login, int Id);

    public struct UserStruct
    {
        public string Login;
        public int Id;
    }
}