namespace TodoApp.Exceptions;

public class InvalidForeignKeyException : Exception
{
    public InvalidForeignKeyException(string msg) : base(msg)
    { }
}