using System;

namespace DurableBotEngine.Core.Exceptions
{
    public class UnavailableSkillException : Exception
    {
        public UnavailableSkillException(string message) : base(message)
        {
        }
    }
}
