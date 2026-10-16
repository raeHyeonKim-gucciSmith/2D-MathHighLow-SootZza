using System;
using System.Collections.Generic;
using System.Text;

namespace MathHighLow.Models
{
    [Serializable]
    public sealed class BlindOperand
    {
        public NumberCard Card { get; private set; }
        public bool IsShared { get; private set; }
        public bool HasSquareRoot { get; private set; }

        public BlindOperand(NumberCard card, bool isShared, bool hasSquareRoot)
        {
            Card = card;
            IsShared = isShared;
            HasSquareRoot = hasSquareRoot;
        }

        public int ResolveValue(int sharedValue)
        {
            return IsShared ? sharedValue : Card.Value;
        }

        public BlindOperand Clone()
        {
            return new BlindOperand(Card, IsShared, HasSquareRoot);
        }
    }

    [Serializable]
    public sealed class BlindExpression
    {
        public List<BlindOperand> Operands { get; } = new List<BlindOperand>();
        public List<OperatorCard.OperatorType> Operators { get; } =
            new List<OperatorCard.OperatorType>();

        public bool ExpectingNumber => Operands.Count == Operators.Count;
        public bool IsEmpty => Operands.Count == 0;
        public bool IsComplete => Operands.Count == 3 && Operators.Count == 2;

        public void AddPrivateNumber(NumberCard card, bool withSquareRoot)
        {
            Operands.Add(new BlindOperand(card, false, withSquareRoot));
        }

        public void AddSharedNumber(bool withSquareRoot)
        {
            Operands.Add(new BlindOperand(null, true, withSquareRoot));
        }

        public void AddOperator(OperatorCard.OperatorType op)
        {
            Operators.Add(op);
        }

        public void Clear()
        {
            Operands.Clear();
            Operators.Clear();
        }

        public BlindExpression Clone()
        {
            BlindExpression clone = new BlindExpression();
            foreach (BlindOperand operand in Operands)
            {
                clone.Operands.Add(operand.Clone());
            }

            clone.Operators.AddRange(Operators);
            return clone;
        }

        public string ToDisplayString(bool revealShared = false, int sharedValue = 0)
        {
            if (Operands.Count == 0)
            {
                return "";
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < Operands.Count; i++)
            {
                BlindOperand operand = Operands[i];
                if (operand.HasSquareRoot)
                {
                    builder.Append("√");
                }

                builder.Append(operand.IsShared && !revealShared
                    ? "?"
                    : operand.ResolveValue(sharedValue).ToString());

                if (i < Operators.Count)
                {
                    builder.Append(' ');
                    builder.Append(ToSymbol(Operators[i]));
                    builder.Append(' ');
                }
            }

            return builder.ToString();
        }

        private static string ToSymbol(OperatorCard.OperatorType op)
        {
            switch (op)
            {
                case OperatorCard.OperatorType.Add: return "+";
                case OperatorCard.OperatorType.Subtract: return "-";
                case OperatorCard.OperatorType.Multiply: return "×";
                case OperatorCard.OperatorType.Divide: return "÷";
                default: return "?";
            }
        }
    }
}
