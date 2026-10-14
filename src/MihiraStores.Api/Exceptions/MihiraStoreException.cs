namespace MihiraStores.Api.Exceptions;

public abstract class MihiraStoreException : Exception
{
    protected MihiraStoreException(string message) : base(message) { }
    protected MihiraStoreException(string message, Exception innerException) : base(message, innerException) { }
}

public class EntityNotFoundException : MihiraStoreException
{
    public EntityNotFoundException(string message) : base(message) { }
}

public class BusinessRuleException : MihiraStoreException
{
    public BusinessRuleException(string message) : base(message) { }
}

public class InsufficientStockException : BusinessRuleException
{
    public InsufficientStockException(string message) : base(message) { }
}

public class InvalidTransferStateException : BusinessRuleException
{
    public InvalidTransferStateException(string message) : base(message) { }
}

public class ConcurrencyConflictException : MihiraStoreException
{
    public ConcurrencyConflictException(string message) : base(message) { }
    public ConcurrencyConflictException(string message, Exception innerException) : base(message, innerException) { }
}
