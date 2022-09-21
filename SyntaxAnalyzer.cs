using System;
using System.Diagnostics;
using System.Reflection;

namespace Text_Caculator
{
    internal abstract class BaseOperation
    {
        public int startChar { get; set; } = -1;
        public int endChar { get; set; } = -1;

        public int layer { get; init; } = -1;
        public int order { get; init; } = -1;
    }

    internal sealed class UnaryOperation : BaseOperation
    {
        public UnaryOperation(BaseUnaryOperationHandler operationHandler)
        {
            this.operationHandler = operationHandler;
        }

        public BaseUnaryOperationHandler operationHandler { get; }
        public BaseOperation? inside { get; set; }
    }
    internal sealed class BinaryOperation : BaseOperation
    {
        public BinaryOperation(BaseBinaryOperationHandler operationHandler)
        {
            this.operationHandler = operationHandler;
        }

        public BaseBinaryOperationHandler operationHandler { get; }
        public BaseOperation? left { get; set; }
        public BaseOperation? right { get; set; }
    }

    internal sealed class LiteralOperation : BaseOperation
    {
        public LiteralOperation(double literalValue)
        {
            this.literalValue = literalValue;
        }

        public double literalValue { get; }
    }

    /*internal sealed class UnknownOperation : BaseOperation
    {
        public UnknownOperation(string errorMessage, int errorStartingCharIndex)
        {
            this.errorMessage = errorMessage;
            this.errorStartingCharIndex = errorStartingCharIndex;
        }

        public string errorMessage { get; }
        public int errorStartingCharIndex { get; }
    }*/


    [Serializable]
    public class SyntaxException : Exception
    {
        public int startIndex { get; }
        public int endIndex { get; }

        public SyntaxException(string message, int start, int end) : base(message)
        {
            startIndex = start;
            endIndex = end;
        }
    }

    internal static class SyntaxAnalyzer
    {
        enum WordType
        {
            None,
            Number,
            VarName,
            UnaryOp
        }

        enum ConstructingWordType
        {
            None,
            Number,
            Word
        }

        static Stack<BaseOperation> constructingOperations = new();
        static Stack<BaseOperation> finishedOperations = new();

        public static BaseOperation Analysis(ReadOnlySpan<char> text)
        {
            constructingOperations.Clear();
            finishedOperations.Clear();

            int wordStartIndex = 0;
            int layer = 0;

            double number = 0;
            int lastValidDigit = -1;
            int decimalPointDepth = -1;

            WordType lastWord = WordType.None;
            ConstructingWordType constructingWord = ConstructingWordType.None;

            void StartConstructWord(int i, ConstructingWordType constructingWordType)
            {
                if (constructingWord != ConstructingWordType.None) return;

                constructingWord = constructingWordType;
                wordStartIndex = i;

                number = 0;
                lastValidDigit = -1;
                decimalPointDepth = -1;
            }

            void FinishConstructWord(int i)
            {
                if (constructingWord == ConstructingWordType.None) return;

                if (wordStartIndex < i)
                {
                    ThrowIf(lastValidDigit != i - 1, ErrorMessages.UnknownWord, wordStartIndex, i);
                    
                    if (decimalPointDepth > 0)
                        number /= Math.Pow(10, decimalPointDepth);
                    finishedOperations.Push(new LiteralOperation(number) { startChar = wordStartIndex, endChar = i });
                }

                lastWord = constructingWord == ConstructingWordType.Number ? WordType.Number : WordType.UnaryOp;
                constructingWord = ConstructingWordType.None;
            }

            static void AddBinaryOperation(int layer, BaseBinaryOperationHandler handler, int indexForError)
            {
                ThrowIf(finishedOperations.Count < 1, ErrorMessages.EmptyOperation, indexForError);

                if (constructingOperations.Count >= 1)
                {
                    var beforeOp = constructingOperations.Peek();
                    if (beforeOp.layer > layer || beforeOp.layer == layer && beforeOp.order >= handler.order)
                    {
                        var op = constructingOperations.Pop();
                        ThrowIf(!PushOp(op), ErrorMessages.InvalidOperation, indexForError);
                    }
                }

                constructingOperations.Push(new BinaryOperation(handler) { layer = layer, order = handler.order });
            }

            for (int i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (char.IsWhiteSpace(c))
                {
                    FinishConstructWord(i);
                    continue;
                }

                if (c == '.' || c >= '0' && c <= '9')
                {
                    if (constructingWord == ConstructingWordType.Word)
                        FinishConstructWord(i);
                    if (constructingWord == ConstructingWordType.None)
                    {
                        StartConstructWord(i, ConstructingWordType.Number);
                        ThrowIf(lastWord == WordType.Number || lastWord == WordType.VarName, "two numbers back to back", i);
                    }
                }

                if (c == ',' && constructingWord == ConstructingWordType.Number)
                    continue;

                switch (c)
                {
                    case '(':
                        if (constructingWord != ConstructingWordType.None)
                            FinishConstructWord(i);
                        if (lastWord == WordType.Number || lastWord == WordType.VarName)
                        {
                            lastWord = WordType.None;
                            AddBinaryOperation(layer, SymbolConvertor.multiplicationHandler, i);
                        }

                        layer++;
                        continue;
                    case ')':
                        ThrowIf(layer == 0, ErrorMessages.TooManyBackets, i);

                        if (constructingWord != ConstructingWordType.None)
                            FinishConstructWord(i);

                        ThrowIf(lastWord == WordType.None, "Empty Backets", i);

                        layer--;
                        continue;
                    case '.':
                        ThrowIf(decimalPointDepth != -1, "Too many decimal point", i);

                        decimalPointDepth = 0;
                        continue;
                    case >= '0' and <= '9':
                        lastValidDigit = i;

                        number *= 10;
                        number += c - '0';
                        if (decimalPointDepth >= 0)
                            decimalPointDepth++;
                        continue;
                    default:
                        if (constructingWord == ConstructingWordType.Number)
                            FinishConstructWord(i);

                        var handler = SymbolConvertor.SymbolToBinaryHandler(c);
                        if (handler == null)
                        {
                            StartConstructWord(i, ConstructingWordType.Word);
                            continue;
                        }

                        lastWord = WordType.None;
                        AddBinaryOperation(layer, handler, i);
                        continue;
                }
            }

            if (constructingWord != ConstructingWordType.None)
                FinishConstructWord(text.Length);
            while (constructingOperations.TryPop(out var op) && PushOp(op))
                ;

            ThrowIf(finishedOperations.Count != 1, "finished Op is not 1", text.Length - 1);
            return finishedOperations.Pop();
        }

        private static bool PushOp(BaseOperation op)
        {
            if (op is BinaryOperation biOp)
            {
                if (!finishedOperations.TryPop(out var subOp2) || !finishedOperations.TryPop(out var subOp1))
                    return false;

                biOp.left = subOp1;
                biOp.right = subOp2;
                op.startChar = subOp1.startChar;
                op.endChar = subOp2.endChar;
            }
            if (op is UnaryOperation uOp)
            {
                if (!finishedOperations.TryPop(out var subOp))
                    return false;
                uOp.inside = subOp;
                op.endChar = subOp.endChar;
            }

            finishedOperations.Push(op);
            return true;
        }

        static void ThrowIf(bool condition, string message, int index)
        {
            if (condition)
                throw new SyntaxException(message, index, index + 1);
        }

        static void ThrowIf(bool condition, string message, int start, int end)
        {
            if (condition)
                throw new SyntaxException(message, start, end);
        }
    }
}
