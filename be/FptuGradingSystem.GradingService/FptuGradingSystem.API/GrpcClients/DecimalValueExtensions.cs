using System;

namespace FptuGradingSystem.Grpc
{
    public static class DecimalValueExtensions
    {
        private const decimal NanoFactor = 1_000_000_000m;

        public static DecimalValue ToDecimalValue(this decimal value)
        {
            var units = decimal.ToInt64(value);
            var nanos = decimal.ToInt32((value - units) * NanoFactor);
            return new DecimalValue
            {
                Units = units,
                Nanos = nanos
            };
        }

        public static decimal ToDecimal(this DecimalValue? decimalValue)
        {
            if (decimalValue == null) return 0m;
            return decimalValue.Units + (decimalValue.Nanos / NanoFactor);
        }
    }
}
