namespace Text_Caculator
{
    public abstract class BaseUnaryOperationHandler
    {
        public static readonly int UnaryOperationOrder = 2;
        public abstract string Symbol { get; }
        public abstract OperationResult Calculate(double a);
    }

    public sealed class NegateHandler : BaseUnaryOperationHandler
    {
        public override string Symbol { get; } = "-";
        public override OperationResult Calculate(double a) => -a;
    }
}