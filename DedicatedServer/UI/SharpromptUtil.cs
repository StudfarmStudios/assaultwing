using System;
using System.ComponentModel.DataAnnotations;

namespace AW2.UI
{
    public class SharpromptUtil
    {

        public static Func<object?, ValidationResult?> IntMinMaxInclusive(int min, int max)
        {
            return input =>
            {
                if (input is null)
                {
                    return new ValidationResult($"Value must not be empty");
                }

                if (input is int value)
                {
                    if (value < min || value > max)
                    {
                        return new ValidationResult($"Value must be between {min} and {max}");
                    }
                }
                else
                {
                    return new ValidationResult($"Value must be an integer");
                }

                return ValidationResult.Success;
            };
        }

    }
}
