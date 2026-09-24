namespace SmartMicrogrid.API.Services
{
    public sealed class TransactionConflictException : Exception
    {
        public TransactionConflictException(string message) : base(message)
        {
        }
    }
}
